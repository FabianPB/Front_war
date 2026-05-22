using War.Core.Chapel;
using War.Core.Economy;

namespace War.Api.Application.Economy;

/// <summary>
/// Orquestador de conversiones de moneda (crafteo de recursos rectores).
/// </summary>
/// <remarks>
/// FLUJO:
///   1. Validar que el par (from, to) es legal (cobre→plata 500:1 · plata→oro 1 000:1).
///   2. Consultar la Capilla del jugador para obtener los límites vigentes.
///   3. Consultar el QuotaTracker para ver si el monto a crear cabe en las 3 ventanas.
///   4. Cobrar el origen y acreditar el destino en el wallet (ambas transacciones).
///   5. Si los 2 pasos del wallet son ok, registrar en el QuotaTracker.
///
/// ATOMICIDAD: si la acreditación falla (p. ej. cap de posesión alcanzado), se devuelve el
/// origen al saldo con un Credit de fuente <see cref="TransactionSource.RefundCancelled"/>.
/// El audit log deja la huella de los 3 movimientos (debit cobre, credit fallido capped, refund).
/// </remarks>
public sealed class CurrencyConversionService
{
    private readonly PlayerWalletService _wallets;
    private readonly PlayerChapelService _chapels;
    private readonly ConversionQuotaTracker _quotas;

    public CurrencyConversionService(
        PlayerWalletService wallets,
        PlayerChapelService chapels,
        ConversionQuotaTracker quotas)
    {
        _wallets = wallets;
        _chapels = chapels;
        _quotas = quotas;
    }

    /// <summary>Snapshot del uso de quotas del jugador (cuánto convirtió hoy/semana/mes).</summary>
    public QuotaSnapshot GetQuotaSnapshot(Guid playerId) => _quotas.GetUsage(playerId);

    /// <summary>
    /// Convierte moneda de menor a mayor denominación. <paramref name="amountToCreate"/> es la
    /// cantidad DEL DESTINO que se quiere crear (p. ej. 10 plata → 10).
    /// </summary>
    public CurrencyConversionResult Convert(
        Guid playerId,
        CurrencyType from,
        CurrencyType to,
        long amountToCreate)
    {
        if (amountToCreate <= 0)
            return CurrencyConversionResult.Fail("NON_POSITIVE", "La cantidad a crear debe ser positiva.");

        // ── 1. Validar par y calcular consumo ──
        long consumedFromSource;
        if (from == CurrencyType.Copper && to == CurrencyType.Silver)
            consumedFromSource = amountToCreate * CurrencyDefinitions.CopperPerSilverConversion;
        else if (from == CurrencyType.Silver && to == CurrencyType.Gold)
            consumedFromSource = amountToCreate * CurrencyDefinitions.SilverPerGoldConversion;
        else
            return CurrencyConversionResult.Fail("INVALID_PAIR",
                $"Conversión no soportada: {from} → {to}. Solo Cobre→Plata y Plata→Oro.");

        // ── 2. Cuotas según Capilla ──
        var limits = _chapels.GetConversionLimits(playerId);
        long perDay, perWeek, perMonth;
        if (to == CurrencyType.Silver)
            (perDay, perWeek, perMonth) = (limits.SilverDaily, limits.SilverWeekly, limits.SilverMonthly);
        else
            (perDay, perWeek, perMonth) = (limits.GoldDaily, limits.GoldWeekly, limits.GoldMonthly);

        var check = _quotas.CheckAllowance(playerId, to, amountToCreate, perDay, perWeek, perMonth);
        if (!check.Allowed)
            return CurrencyConversionResult.Fail("QUOTA_EXCEEDED", check.Reason ?? "Cuota excedida.");

        // ── 3. Cobrar el origen ──
        var description = $"Conversión {amountToCreate} {to} (consume {consumedFromSource} {from})";
        var debit = _wallets.Debit(playerId, from, consumedFromSource,
            TransactionSource.CurrencyConversionOut, description);
        if (!debit.Success)
            return CurrencyConversionResult.Fail(debit.ErrorCode ?? "DEBIT_FAIL",
                debit.ErrorMessage ?? "No se pudo cobrar el origen.");

        // ── 4. Acreditar el destino ──
        var credit = _wallets.Credit(playerId, to, amountToCreate,
            TransactionSource.CurrencyConversionIn, description);
        if (!credit.Success)
        {
            // Rollback: devolver lo cobrado con un refund auditado.
            _wallets.Credit(playerId, from, consumedFromSource,
                TransactionSource.RefundCancelled,
                $"Rollback de conversión fallida: {credit.ErrorMessage}");
            return CurrencyConversionResult.Fail(credit.ErrorCode ?? "CREDIT_FAIL",
                credit.ErrorMessage ?? "No se pudo acreditar el destino.");
        }

        // Si el credit fue capped (se perdió parte porque el wallet estaba al tope), lo reportamos
        // pero la conversión sigue siendo exitosa por la porción que sí entró.
        var amountActuallyCredited = credit.Transaction!.Amount;

        // ── 5. Registrar en el quota tracker ──
        _quotas.RecordConversion(playerId, to, amountActuallyCredited);

        return CurrencyConversionResult.Ok(
            from: from,
            to: to,
            consumed: consumedFromSource,
            created: amountActuallyCredited,
            debitTx: debit.Transaction!,
            creditTx: credit.Transaction!);
    }
}

public sealed record CurrencyConversionResult(
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    CurrencyType From,
    CurrencyType To,
    long Consumed,
    long Created,
    WalletTransaction? DebitTransaction,
    WalletTransaction? CreditTransaction)
{
    public static CurrencyConversionResult Ok(
        CurrencyType from,
        CurrencyType to,
        long consumed,
        long created,
        WalletTransaction debitTx,
        WalletTransaction creditTx) =>
        new(true, null, null, from, to, consumed, created, debitTx, creditTx);

    public static CurrencyConversionResult Fail(string code, string msg) =>
        new(false, code, msg, CurrencyType.Copper, CurrencyType.Copper, 0, 0, null, null);
}
