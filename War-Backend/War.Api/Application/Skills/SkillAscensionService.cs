using War.Api.Application.Economy;
using War.Api.Application.GameWorld;
using War.Core.Economy;
using War.Core.Equipment;
using War.Core.Skills;
using War.Core.Skills.Ascension;
using War.Core.Skills.Books;

namespace War.Api.Application.Skills;

/// <summary>
/// Orquestador de ascensión de habilidades. Punto único del servidor donde se sube el nivel
/// de ascensión de cualquier skill de cualquier jugador.
/// </summary>
/// <remarks>
/// FLUJO DE AscendSkill:
///   1. Validar skillId + que el jugador tiene la skill.
///   2. Leer nivel actual y paso objetivo (cur+1). Max 10.
///   3. Resolver si es ultimate (vía SkillDefinition.IsUltimate del catálogo).
///   4. Calcular coste (libros + moneda + energía) para ese paso.
///   5. Localizar los libros requeridos en el inventario, contar stacks totales.
///   6. Validar que el wallet puede afrontar el coste de moneda/energía.
///   7. Atómicamente (bajo inventory lock + wallet lock):
///        a. Descontar libros del inventario (consume stacks).
///        b. Cobrar moneda/energía del wallet (SpendMulti).
///        c. Subir SkillAscension[skillId] en 1.
///   8. Si falla cualquier paso intermedio, rollback (no se cobra ni se quitan libros).
///   9. Emitir resultado auditado.
/// </remarks>
public sealed class SkillAscensionService
{
    private readonly PlayerWalletService _wallets;

    public SkillAscensionService(PlayerWalletService wallets)
    {
        _wallets = wallets;
    }

    /// <summary>
    /// Preview del coste del siguiente paso de ascensión de una skill (sin aplicar nada).
    /// </summary>
    public SkillAscensionPreview? PreviewNextStep(OnlinePlayer player, string skillId)
    {
        var def = LookupSkill(skillId);
        if (def is null) return null;

        var currentLevel = GetCurrentLevel(player, skillId);
        if (currentLevel >= SkillAscensionCostTable.MaxStep) return new SkillAscensionPreview(skillId, currentLevel, null, null);

        var step = currentLevel + 1;
        var cost = SkillAscensionCostTable.GetStepCost(step, def.IsUltimate);
        var bookDefId = SkillBookCatalog.GetSpecificBookId(skillId, cost.Books.Rarity);

        return new SkillAscensionPreview(
            SkillId: skillId,
            CurrentLevel: currentLevel,
            NextStepCost: cost,
            BookDefinitionId: bookDefId);
    }

    /// <summary>
    /// Intenta aplicar un paso de ascensión. Devuelve resultado detallado.
    /// </summary>
    public SkillAscensionResult AscendSkill(OnlinePlayer player, string skillId)
    {
        var def = LookupSkill(skillId);
        if (def is null)
            return SkillAscensionResult.Fail("UNKNOWN_SKILL", $"La skill '{skillId}' no existe en el catálogo.");

        // Verificar que el jugador tiene la skill cargada
        if (!player.Skills.Any(s => s.SkillId == skillId))
            return SkillAscensionResult.Fail("SKILL_NOT_EQUIPPED", "El jugador no tiene esta skill.");

        if (!Guid.TryParse(player.PlayerId, out var playerGuid))
            return SkillAscensionResult.Fail("INVALID_PLAYER_ID", "ID de jugador inválido.");

        var currentLevel = GetCurrentLevel(player, skillId);
        if (currentLevel >= SkillAscensionCostTable.MaxStep)
            return SkillAscensionResult.Fail("MAX_ASCENSION", "La skill ya está al máximo nivel de ascensión.");

        var step = currentLevel + 1;
        var cost = SkillAscensionCostTable.GetStepCost(step, def.IsUltimate);
        var bookDefId = SkillBookCatalog.GetSpecificBookId(skillId, cost.Books.Rarity);

        // ── Buscar libros en el inventario ──
        var inv = player.Inventory;
        lock (inv)
        {
            var bookStacks = inv.AllItems
                .Where(i => i.ItemType == InventoryItemType.Resource && i.DefinitionId == bookDefId)
                .ToList();
            var totalBooks = bookStacks.Sum(s => s.Quantity);
            if (totalBooks < cost.Books.Count)
                return SkillAscensionResult.Fail("NOT_ENOUGH_BOOKS",
                    $"Requiere {cost.Books.Count} libros de tipo '{bookDefId}' (tienes {totalBooks}).");

            // ── Validar wallet ──
            var wallet = _wallets.GetOrCreate(playerGuid);
            var currencyCost = SkillAscensionCostTable.GetCurrencyCost(cost);
            if (!wallet.CanAfford(currencyCost))
                return SkillAscensionResult.Fail("INSUFFICIENT_FUNDS",
                    "Saldo insuficiente de moneda o energía para esta ascensión.");

            // ── Cobrar wallet ──
            var spend = _wallets.Spend(
                playerGuid,
                currencyCost,
                TransactionSource.SkillAscension,
                $"Ascensión {skillId} {currentLevel}→{step}");
            if (!spend.Success)
                return SkillAscensionResult.Fail(spend.ErrorCode ?? "SPEND_FAIL",
                    spend.ErrorMessage ?? "No se pudo cobrar el wallet.");

            // ── Consumir libros ──
            // (Si falla aquí por algún motivo, ya cobramos el wallet; hacemos rollback con un credit de refund.)
            if (!ConsumeBooks(inv, bookStacks, cost.Books.Count))
            {
                // Rollback: devolver las monedas cobradas.
                foreach (var tx in spend.Transactions)
                {
                    _wallets.Credit(playerGuid, tx.Currency, tx.Amount,
                        TransactionSource.RefundCancelled,
                        $"Rollback ascensión fallida: {skillId} {currentLevel}→{step}");
                }
                return SkillAscensionResult.Fail("BOOK_CONSUME_FAIL",
                    "No se pudo consumir los libros (inventario inconsistente).");
            }

            // ── Subir el nivel de ascensión ──
            player.SkillAscension[skillId] = step;

            return SkillAscensionResult.Ok(
                skillId: skillId,
                previousLevel: currentLevel,
                newLevel: step,
                costApplied: cost,
                bookDefinitionId: bookDefId,
                transactions: spend.Transactions);
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static int GetCurrentLevel(OnlinePlayer player, string skillId) =>
        player.SkillAscension.TryGetValue(skillId, out var lvl) ? lvl : 0;

    private static SkillDefinition? LookupSkill(string skillId)
    {
        try { return SkillCatalogRegistry.GetRequired(skillId); }
        catch (KeyNotFoundException) { return null; }
    }

    /// <summary>
    /// Descuenta <paramref name="countRequired"/> libros de los stacks indicados. Los stacks vienen
    /// ya filtrados por DefinitionId. Opera sobre el inventario (bajo el lock externo del llamador).
    /// Mutualmente atómico: si no hay suficientes, no toca nada y devuelve false.
    /// </summary>
    private static bool ConsumeBooks(PlayerInventory inv, IReadOnlyList<InventoryItem> stacks, int countRequired)
    {
        var remaining = countRequired;
        foreach (var stack in stacks)
        {
            if (remaining <= 0) break;
            var take = Math.Min(stack.Quantity, remaining);

            // Si stack se agota, removemos; si queda saldo, reducimos.
            if (take >= stack.Quantity)
            {
                if (!inv.ForceRemoveItem(stack.ItemId)) return false;
            }
            else
            {
                stack.Quantity -= take;
                stack.LastModifiedAt = DateTime.UtcNow;
            }
            remaining -= take;
        }
        return remaining == 0;
    }
}

// ── Resultados ──────────────────────────────────────────────────────────────

public sealed record SkillAscensionPreview(
    string SkillId,
    int CurrentLevel,
    SkillAscensionStepCost? NextStepCost,
    string? BookDefinitionId);

public sealed record SkillAscensionResult(
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    string SkillId,
    int PreviousLevel,
    int NewLevel,
    SkillAscensionStepCost? CostApplied,
    string? BookDefinitionId,
    IReadOnlyList<WalletTransaction> Transactions)
{
    public static SkillAscensionResult Ok(
        string skillId,
        int previousLevel,
        int newLevel,
        SkillAscensionStepCost costApplied,
        string bookDefinitionId,
        IReadOnlyList<WalletTransaction> transactions) =>
        new(true, null, null, skillId, previousLevel, newLevel, costApplied, bookDefinitionId, transactions);

    public static SkillAscensionResult Fail(string code, string msg) =>
        new(false, code, msg, "", 0, 0, null, null, Array.Empty<WalletTransaction>());
}
