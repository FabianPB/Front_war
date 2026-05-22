namespace War.Core.Economy;

/// <summary>
/// Proveedor dinámico del cap de posesión por moneda. Lo implementa el servicio que conoce
/// la Capilla de Economía del jugador; el wallet no tiene por qué conocer la Capilla directamente.
/// </summary>
public interface IWalletCapProvider
{
    long GetPossessionCap(Guid playerId, CurrencyType currency);
}

/// <summary>
/// Cap fijo por defecto — útil para tests y como fallback si nadie inyecta la Capilla.
/// Devuelve el cap absoluto de <see cref="CurrencyDefinitions"/>.
/// </summary>
public sealed class AbsoluteCapProvider : IWalletCapProvider
{
    public long GetPossessionCap(Guid playerId, CurrencyType currency) => currency switch
    {
        CurrencyType.Copper => CurrencyDefinitions.AbsoluteMaxCopper,
        CurrencyType.Silver => CurrencyDefinitions.AbsoluteMaxSilver,
        CurrencyType.Gold   => CurrencyDefinitions.AbsoluteMaxGold,
        CurrencyType.Energy => CurrencyDefinitions.AbsoluteMaxEnergy,
        _ => throw new ArgumentOutOfRangeException(nameof(currency))
    };
}

/// <summary>
/// Wallet monetario del jugador. Almacena cobre, plata, oro y energía.
/// </summary>
/// <remarks>
/// REGLAS DE ORO:
/// 1. Ninguna propiedad de saldo es settable desde fuera. Todo cambio pasa por Credit/Debit/SpendMulti.
/// 2. Cada operación emite una WalletTransaction inmutable. Sin transacción = no hubo cambio.
/// 3. Credit rechaza negativos. Debit rechaza si no hay saldo suficiente. Ningún overdraft.
/// 4. Las operaciones son thread-safe bajo el lock interno _walletLock.
/// 5. El cap por moneda es dinámico (lo consulta al <see cref="IWalletCapProvider"/> en cada
///    ingreso), así la Capilla de Economía puede cambiar el techo sin que el wallet tenga que
///    saber nada del sistema de Capilla.
/// 6. La energía NO regenera por tiempo. Solo entra por meditación/recompensas.
///
/// La persistencia NO vive aquí. El log de transacciones y la persistencia son responsabilidad
/// del WalletAuditLog / PlayerWalletService en la capa de aplicación.
/// </remarks>
public sealed class PlayerWallet
{
    private readonly object _walletLock = new();
    private readonly IWalletCapProvider _capProvider;

    public Guid PlayerId { get; }

    public long Copper { get; private set; }
    public long Silver { get; private set; }
    public long Gold   { get; private set; }
    public long Energy { get; private set; }

    public PlayerWallet(
        Guid playerId,
        IWalletCapProvider capProvider,
        long initialCopper = 0,
        long initialSilver = 0,
        long initialGold = 0,
        long initialEnergy = 0)
    {
        if (initialCopper < 0 || initialSilver < 0 || initialGold < 0 || initialEnergy < 0)
            throw new ArgumentException("Initial balances cannot be negative.");

        PlayerId = playerId;
        _capProvider = capProvider ?? throw new ArgumentNullException(nameof(capProvider));

        Copper = Math.Min(initialCopper, capProvider.GetPossessionCap(playerId, CurrencyType.Copper));
        Silver = Math.Min(initialSilver, capProvider.GetPossessionCap(playerId, CurrencyType.Silver));
        Gold   = Math.Min(initialGold,   capProvider.GetPossessionCap(playerId, CurrencyType.Gold));
        Energy = Math.Min(initialEnergy, capProvider.GetPossessionCap(playerId, CurrencyType.Energy));
    }

    // ── Consultas (no mutan) ─────────────────────────────────────────────────

    public long GetBalance(CurrencyType currency) => currency switch
    {
        CurrencyType.Copper => Copper,
        CurrencyType.Silver => Silver,
        CurrencyType.Gold   => Gold,
        CurrencyType.Energy => Energy,
        _ => throw new ArgumentOutOfRangeException(nameof(currency))
    };

    /// <summary>
    /// Cap de posesión efectivo actual (consultado al proveedor, típicamente la Capilla).
    /// </summary>
    public long GetCap(CurrencyType currency) => _capProvider.GetPossessionCap(PlayerId, currency);

    public bool CanAfford(CurrencyCost cost)
    {
        lock (_walletLock)
        {
            return Copper >= cost.Copper
                && Silver >= cost.Silver
                && Gold   >= cost.Gold
                && Energy >= cost.Energy;
        }
    }

    // ── Credit (ingreso) ─────────────────────────────────────────────────────

    public WalletOperationResult Credit(
        CurrencyType currency,
        long amount,
        TransactionSource source,
        string description,
        Guid? relatedEntityId = null)
    {
        if (amount <= 0)
            return WalletOperationResult.Fail("NON_POSITIVE", "El monto debe ser positivo.");

        if (!IsCreditSource(source))
            return WalletOperationResult.Fail("SOURCE_NOT_CREDIT", $"La fuente {source} no es un ingreso válido.");

        lock (_walletLock)
        {
            var before = GetBalance(currency);
            var cap = _capProvider.GetPossessionCap(PlayerId, currency);
            var after = Math.Min(before + amount, cap);
            var actualApplied = after - before;

            SetBalance(currency, after);

            var tx = new WalletTransaction(
                Id: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                PlayerId: PlayerId,
                Currency: currency,
                Direction: TransactionDirection.Credit,
                Amount: actualApplied,
                Source: source,
                Description: actualApplied < amount
                    ? $"{description} [capped: +{actualApplied}/{amount}]"
                    : description,
                BalanceBefore: before,
                BalanceAfter: after,
                RelatedEntityId: relatedEntityId);

            return WalletOperationResult.Ok(tx);
        }
    }

    // ── Debit (egreso) ───────────────────────────────────────────────────────

    public WalletOperationResult Debit(
        CurrencyType currency,
        long amount,
        TransactionSource source,
        string description,
        Guid? relatedEntityId = null)
    {
        if (amount <= 0)
            return WalletOperationResult.Fail("NON_POSITIVE", "El monto debe ser positivo.");

        if (!IsDebitSource(source))
            return WalletOperationResult.Fail("SOURCE_NOT_DEBIT", $"La fuente {source} no es un egreso válido.");

        lock (_walletLock)
        {
            var before = GetBalance(currency);
            if (before < amount)
                return WalletOperationResult.Fail("INSUFFICIENT_FUNDS",
                    $"Saldo insuficiente de {currency}: {before} < {amount}.");

            var after = before - amount;
            SetBalance(currency, after);

            var tx = new WalletTransaction(
                Id: Guid.NewGuid(),
                Timestamp: DateTime.UtcNow,
                PlayerId: PlayerId,
                Currency: currency,
                Direction: TransactionDirection.Debit,
                Amount: amount,
                Source: source,
                Description: description,
                BalanceBefore: before,
                BalanceAfter: after,
                RelatedEntityId: relatedEntityId);

            return WalletOperationResult.Ok(tx);
        }
    }

    // ── SpendMulti (egreso atómico multi-moneda) ────────────────────────────

    public WalletMultiOperationResult SpendMulti(
        CurrencyCost cost,
        TransactionSource source,
        string description,
        Guid? relatedEntityId = null)
    {
        if (cost.IsZero)
            return WalletMultiOperationResult.Ok(Array.Empty<WalletTransaction>());

        if (!IsDebitSource(source))
            return WalletMultiOperationResult.Fail("SOURCE_NOT_DEBIT",
                $"La fuente {source} no es un egreso válido.");

        lock (_walletLock)
        {
            if (!CanAffordInternal(cost))
                return WalletMultiOperationResult.Fail("INSUFFICIENT_FUNDS",
                    $"Saldo insuficiente para el coste: {DescribeCost(cost)}.");

            var transactions = new List<WalletTransaction>();
            foreach (var (currency, amount) in cost.EnumerateNonZero())
            {
                var before = GetBalance(currency);
                var after = before - amount;
                SetBalance(currency, after);

                transactions.Add(new WalletTransaction(
                    Id: Guid.NewGuid(),
                    Timestamp: DateTime.UtcNow,
                    PlayerId: PlayerId,
                    Currency: currency,
                    Direction: TransactionDirection.Debit,
                    Amount: amount,
                    Source: source,
                    Description: description,
                    BalanceBefore: before,
                    BalanceAfter: after,
                    RelatedEntityId: relatedEntityId));
            }

            return WalletMultiOperationResult.Ok(transactions);
        }
    }

    // ── Internos ─────────────────────────────────────────────────────────────

    private bool CanAffordInternal(CurrencyCost cost) =>
        Copper >= cost.Copper && Silver >= cost.Silver && Gold >= cost.Gold && Energy >= cost.Energy;

    private void SetBalance(CurrencyType currency, long newValue)
    {
        switch (currency)
        {
            case CurrencyType.Copper: Copper = newValue; break;
            case CurrencyType.Silver: Silver = newValue; break;
            case CurrencyType.Gold:   Gold   = newValue; break;
            case CurrencyType.Energy: Energy = newValue; break;
            default: throw new ArgumentOutOfRangeException(nameof(currency));
        }
    }

    private static string DescribeCost(CurrencyCost cost)
    {
        var parts = new List<string>();
        if (cost.Gold   > 0) parts.Add($"{cost.Gold} oro");
        if (cost.Silver > 0) parts.Add($"{cost.Silver} plata");
        if (cost.Copper > 0) parts.Add($"{cost.Copper} cobre");
        if (cost.Energy > 0) parts.Add($"{cost.Energy} energía");
        return parts.Count == 0 ? "0" : string.Join(" + ", parts);
    }

    private static bool IsCreditSource(TransactionSource s) => (int)s < 100;
    private static bool IsDebitSource(TransactionSource s) => (int)s >= 100;
}
