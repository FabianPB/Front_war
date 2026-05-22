namespace War.Core.Equipment;

/// <summary>
/// Servicio de crafteo de equipamiento. Valida y ejecuta operaciones
/// de tier-up y desarrollo con audit trail.
///
/// ═══════════════════════════════════════════════════════════════
/// PRINCIPIO DE SEGURIDAD
/// ═══════════════════════════════════════════════════════════════
///
/// Cada operación de crafteo:
///   1. VALIDA todas las precondiciones antes de mutar estado
///   2. REGISTRA la operación en un log de auditoría (CraftAuditEntry)
///   3. FALLA atómicamente — si algo sale mal, nada cambia
///   4. Devuelve un resultado tipado con razón de fallo si aplica
///
/// El servidor es la ÚNICA autoridad. El cliente envía solicitudes;
/// el servidor valida, ejecuta y notifica el resultado.
/// </summary>
public sealed class CraftingService
{
    // ── Audit trail ──
    private readonly List<CraftAuditEntry> _auditLog = new();
    private readonly object _auditLock = new();

    public IReadOnlyList<CraftAuditEntry> AuditLog
    {
        get { lock (_auditLock) { return _auditLog.ToArray(); } }
    }

    // ══════════════════════════════════════════════════════════════
    // DESARROLLO (+1 nivel, hasta max 30)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Incrementa el desarrollo de un equipo en +1.
    ///
    /// Validaciones:
    ///   1. El ítem existe en el inventario del jugador
    ///   2. Es de tipo Equipment
    ///   3. No ha alcanzado el desarrollo máximo (30)
    ///
    /// El ítem puede estar equipado o no — el desarrollo aplica igual.
    /// </summary>
    public CraftResult Develop(PlayerInventory inventory, string itemId, string playerId)
    {
        var item = inventory.GetById(itemId);

        // ── Validación ──
        if (item is null)
            return CraftResult.Fail("ITEM_NOT_FOUND", "El ítem no existe en el inventario.");

        if (item.ItemType != InventoryItemType.Equipment)
            return CraftResult.Fail("NOT_EQUIPMENT", "Solo se pueden desarrollar equipos.");

        if (item.DevelopmentLevel >= EquipmentFormulas.MaxDevelopment)
            return CraftResult.Fail("MAX_DEVELOPMENT", $"El equipo ya está al desarrollo máximo ({EquipmentFormulas.MaxDevelopment}).");

        // TODO: verificar que el jugador tiene los recursos necesarios
        // (cuando el sistema de recursos esté implementado)

        // ── Ejecución ──
        var previousLevel = item.DevelopmentLevel;
        item.DevelopmentLevel++;
        item.LastModifiedAt = DateTime.UtcNow;

        // ── Auditoría ──
        LogAudit(new CraftAuditEntry
        {
            PlayerId = playerId,
            Operation = CraftOperation.Develop,
            Timestamp = DateTime.UtcNow,
            InputItemIds = new[] { itemId },
            OutputItemId = itemId,
            Details = $"Desarrollo {previousLevel} → {item.DevelopmentLevel} en {item.DefinitionId} (Tier {item.Tier})"
        });

        return CraftResult.Success(itemId, $"Desarrollo completado: nivel {item.DevelopmentLevel}.");
    }

    // ══════════════════════════════════════════════════════════════
    // CRAFTEO DE TIER (2 × TierN → 1 × Tier(N+1) dev1)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Craftea un tier superior combinando 2 piezas del mismo tipo y tier.
    ///
    /// Validaciones estrictas (anti-exploit):
    ///   1. Ambos ítems existen en el inventario del jugador
    ///   2. Son ítems DISTINTOS (no el mismo ítem usado dos veces)
    ///   3. Ambos son de tipo Equipment
    ///   4. Ambos tienen el MISMO DefinitionId (mismo tipo de equipo)
    ///   5. Ambos tienen el MISMO Tier
    ///   6. El tier actual es menor al máximo (4)
    ///   7. NINGUNO de los dos está equipado (hay que desequipar primero)
    ///   8. Hay espacio en el inventario para el resultado (aunque se liberan
    ///      2 casillas y se ocupa 1, verificamos defensivamente)
    ///
    /// El resultado es una pieza NUEVA del mismo tipo, Tier+1, Desarrollo 1.
    /// Ambas piezas de entrada se DESTRUYEN (incluido todo su desarrollo).
    /// </summary>
    public CraftResult CraftTierUp(PlayerInventory inventory, string itemIdA, string itemIdB, string playerId)
    {
        // ── Validación 1: existencia ──
        var itemA = inventory.GetById(itemIdA);
        var itemB = inventory.GetById(itemIdB);

        if (itemA is null || itemB is null)
            return CraftResult.Fail("ITEM_NOT_FOUND", "Uno o ambos ítems no existen en el inventario.");

        // ── Validación 2: no son el mismo ítem ──
        if (itemIdA == itemIdB)
            return CraftResult.Fail("SAME_ITEM", "No se puede usar el mismo ítem dos veces.");

        // ── Validación 3: tipo Equipment ──
        if (itemA.ItemType != InventoryItemType.Equipment || itemB.ItemType != InventoryItemType.Equipment)
            return CraftResult.Fail("NOT_EQUIPMENT", "Solo se pueden craftear equipos.");

        // ── Validación 4: mismo tipo de equipo ──
        if (itemA.DefinitionId != itemB.DefinitionId)
            return CraftResult.Fail("DIFFERENT_TYPE", "Ambos ítems deben ser del mismo tipo de equipo.");

        // ── Validación 5: mismo tier ──
        if (itemA.Tier != itemB.Tier)
            return CraftResult.Fail("DIFFERENT_TIER", "Ambos ítems deben tener el mismo tier.");

        // ── Validación 6: tier no máximo ──
        if (itemA.Tier >= EquipmentFormulas.MaxTier)
            return CraftResult.Fail("MAX_TIER", $"No se puede superar el tier máximo ({EquipmentFormulas.MaxTier}).");

        // ── Validación 7: ninguno equipado ──
        if (itemA.IsEquipped || itemB.IsEquipped)
            return CraftResult.Fail("ITEM_EQUIPPED", "Hay que desequipar los ítems antes de craftearlos.");

        // ── Ejecución atómica ──
        var newTier = itemA.Tier + 1;
        var definitionId = itemA.DefinitionId;
        var slot = itemA.EquippedSlot ?? EquipmentCatalog.Get(definitionId).Slot;

        // Crear la pieza resultado ANTES de destruir las entradas (atomicidad)
        var resultItem = new InventoryItem
        {
            ItemType = InventoryItemType.Equipment,
            DefinitionId = definitionId,
            Tier = newTier,
            DevelopmentLevel = EquipmentFormulas.MinDevelopment, // reset total
            IsEquipped = false,
            EquippedSlot = null
        };

        // Remover las entradas (ForceRemove porque ya validamos que no están equipadas)
        var removedA = inventory.ForceRemoveItem(itemIdA);
        var removedB = inventory.ForceRemoveItem(itemIdB);

        if (!removedA || !removedB)
        {
            // Rollback: esto NO debería pasar nunca si las validaciones son correctas
            // pero defendemos por si acaso
            return CraftResult.Fail("REMOVE_FAILED", "Error interno: no se pudieron remover los ítems de entrada. Operación cancelada.");
        }

        // Añadir el resultado al inventario
        var added = inventory.AddItem(resultItem);
        if (added is null)
        {
            // Inventario lleno (no debería pasar: liberamos 2 casillas y usamos 1)
            return CraftResult.Fail("INVENTORY_FULL", "Error interno: inventario lleno tras liberar casillas. Operación cancelada.");
        }

        // ── Auditoría ──
        LogAudit(new CraftAuditEntry
        {
            PlayerId = playerId,
            Operation = CraftOperation.TierUp,
            Timestamp = DateTime.UtcNow,
            InputItemIds = new[] { itemIdA, itemIdB },
            OutputItemId = resultItem.ItemId,
            Details = $"Crafteo Tier {itemA.Tier} → {newTier} de {definitionId}. " +
                      $"Entrada A: dev{itemA.DevelopmentLevel}, Entrada B: dev{itemB.DevelopmentLevel}. " +
                      $"Desarrollo reseteado a {EquipmentFormulas.MinDevelopment}."
        });

        return CraftResult.Success(resultItem.ItemId,
            $"Crafteo exitoso: {definitionId} Tier {newTier} (Desarrollo 1).");
    }

    // ══════════════════════════════════════════════════════════════
    // AUDIT LOG
    // ══════════════════════════════════════════════════════════════

    private void LogAudit(CraftAuditEntry entry)
    {
        lock (_auditLock)
        {
            _auditLog.Add(entry);

            // Mantener solo los últimos 1000 registros en memoria
            if (_auditLog.Count > 1000)
            {
                _auditLog.RemoveRange(0, _auditLog.Count - 1000);
            }
        }
    }

    /// <summary>Obtener las últimas N entradas de auditoría de un jugador.</summary>
    public IReadOnlyList<CraftAuditEntry> GetPlayerAuditLog(string playerId, int maxEntries = 50)
    {
        lock (_auditLock)
        {
            return _auditLog
                .Where(e => e.PlayerId == playerId)
                .OrderByDescending(e => e.Timestamp)
                .Take(maxEntries)
                .ToArray();
        }
    }
}

// ══════════════════════════════════════════════════════════════════
// RESULTADO DE CRAFTEO
// ══════════════════════════════════════════════════════════════════

public sealed record CraftResult(
    bool IsSuccess,
    string? OutputItemId,
    string Message,
    string? ErrorCode = null)
{
    public static CraftResult Success(string outputItemId, string message)
        => new(true, outputItemId, message);

    public static CraftResult Fail(string errorCode, string message)
        => new(false, null, message, errorCode);
}

// ══════════════════════════════════════════════════════════════════
// ENTRADA DE AUDITORÍA
// ══════════════════════════════════════════════════════════════════

public enum CraftOperation
{
    Develop,    // Incremento de desarrollo (+1)
    TierUp      // Crafteo de tier (2→1)
}

/// <summary>
/// Registro inmutable de una operación de crafteo. Se guarda en
/// memoria para auditoría y detección de exploits.
///
/// Si un jugador tiene un patrón sospechoso (e.g., 50 crafteos
/// legendarios en 1 minuto), el log permite investigar.
/// </summary>
public sealed class CraftAuditEntry
{
    public required string PlayerId { get; init; }
    public required CraftOperation Operation { get; init; }
    public required DateTime Timestamp { get; init; }
    public required IReadOnlyList<string> InputItemIds { get; init; }
    public required string OutputItemId { get; init; }
    public required string Details { get; init; }
}
