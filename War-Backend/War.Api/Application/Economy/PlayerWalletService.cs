using System.Collections.Concurrent;
using War.Core.Economy;

namespace War.Api.Application.Economy;

/// <summary>
/// Orquestador de wallets: es el ÚNICO punto por el que debe pasar cualquier cambio
/// de saldo del servidor. Cualquier lugar del código que quiera dar oro a un jugador
/// lo hace llamando a este servicio, jamás manipulando el PlayerWallet directamente.
/// </summary>
/// <remarks>
/// Responsabilidades:
///   1. Mantener el mapa PlayerId → PlayerWallet (singleton).
///   2. Ejecutar la transacción contra el wallet.
///   3. Registrar el movimiento en el <see cref="WalletAuditLog"/>.
///   4. Devolver el resultado tipado con transacción adjunta.
///
/// Seguridad:
///   · Cualquier método que otorgue recursos debe declarar explícitamente la fuente (enum).
///   · Los strings de descripción son libres pero el enum TransactionSource filtra lo permitido.
///   · Los duplicados (double-spend) se previenen por el lock interno del PlayerWallet.
///
/// Caps dinámicos: el wallet recibe un <see cref="IWalletCapProvider"/> que consulta la Capilla
/// de Economía del jugador. El wallet no sabe nada de la Capilla directamente.
/// </remarks>
public sealed class PlayerWalletService
{
    private readonly ConcurrentDictionary<Guid, PlayerWallet> _wallets = new();
    private readonly WalletAuditLog _auditLog;
    private readonly IWalletCapProvider _capProvider;

    public PlayerWalletService(WalletAuditLog auditLog, IWalletCapProvider capProvider)
    {
        _auditLog = auditLog;
        _capProvider = capProvider;
    }

    /// <summary>
    /// Obtiene (o crea) el wallet de un jugador. Idempotente: llamarlo dos veces devuelve el mismo.
    /// </summary>
    public PlayerWallet GetOrCreate(Guid playerId)
    {
        return _wallets.GetOrAdd(playerId, id => new PlayerWallet(id, _capProvider));
    }

    /// <summary>
    /// Desconecta el wallet de un jugador (al hacer logout). Lo conserva en memoria por si vuelve;
    /// en producción con BD aquí se descargaría la versión in-memory tras persistir.
    /// </summary>
    public void Detach(Guid playerId)
    {
        // En el demo in-memory no borramos — el wallet persiste por si reconecta.
        // Cuando haya BD, esto se convierte en un flush + evict.
    }

    // ── INGRESO (Credit) ─────────────────────────────────────────────────────

    public WalletOperationResult Credit(
        Guid playerId,
        CurrencyType currency,
        long amount,
        TransactionSource source,
        string description,
        Guid? relatedEntityId = null)
    {
        var wallet = GetOrCreate(playerId);
        var result = wallet.Credit(currency, amount, source, description, relatedEntityId);
        if (result.Success && result.Transaction is not null)
            _auditLog.Record(result.Transaction);
        return result;
    }

    // ── EGRESO UNIVERSAL (Debit simple o multi-moneda) ──────────────────────

    public WalletOperationResult Debit(
        Guid playerId,
        CurrencyType currency,
        long amount,
        TransactionSource source,
        string description,
        Guid? relatedEntityId = null)
    {
        var wallet = GetOrCreate(playerId);
        var result = wallet.Debit(currency, amount, source, description, relatedEntityId);
        if (result.Success && result.Transaction is not null)
            _auditLog.Record(result.Transaction);
        return result;
    }

    public WalletMultiOperationResult Spend(
        Guid playerId,
        CurrencyCost cost,
        TransactionSource source,
        string description,
        Guid? relatedEntityId = null)
    {
        var wallet = GetOrCreate(playerId);
        var result = wallet.SpendMulti(cost, source, description, relatedEntityId);
        if (result.Success && result.Transactions.Count > 0)
            _auditLog.RecordMany(result.Transactions);
        return result;
    }

    // ── CONSULTAS ───────────────────────────────────────────────────────────

    public IReadOnlyList<WalletTransaction> GetHistory(Guid playerId, int limit = 100) =>
        _auditLog.GetHistoryForPlayer(playerId, limit);

    public int TotalActiveWallets => _wallets.Count;
}
