namespace War.Core.Economy;

/// <summary>
/// Resultado de cada operación sobre el wallet. Los rechazos traen razón legible.
/// </summary>
public sealed record WalletOperationResult(
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    WalletTransaction? Transaction)
{
    public static WalletOperationResult Ok(WalletTransaction transaction) =>
        new(true, null, null, transaction);

    public static WalletOperationResult Fail(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage, null);
}

/// <summary>
/// Resultado de una operación que involucra múltiples monedas (ej. crafteo que cobra oro + plata).
/// Atómico: todas las transacciones se aplican o ninguna.
/// </summary>
public sealed record WalletMultiOperationResult(
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<WalletTransaction> Transactions)
{
    public static WalletMultiOperationResult Ok(IReadOnlyList<WalletTransaction> transactions) =>
        new(true, null, null, transactions);

    public static WalletMultiOperationResult Fail(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage, Array.Empty<WalletTransaction>());
}

/// <summary>
/// Describe un coste agregado que puede involucrar varias monedas a la vez.
/// Usado por crafteo, expansión de inventario, ascensión de skills, etc.
/// </summary>
public sealed record CurrencyCost(
    long Copper = 0,
    long Silver = 0,
    long Gold = 0,
    long Energy = 0)
{
    public static readonly CurrencyCost Zero = new();

    public bool IsZero => Copper == 0 && Silver == 0 && Gold == 0 && Energy == 0;

    public CurrencyCost Add(CurrencyCost other) =>
        new(Copper + other.Copper, Silver + other.Silver, Gold + other.Gold, Energy + other.Energy);

    public IEnumerable<(CurrencyType Currency, long Amount)> EnumerateNonZero()
    {
        if (Copper > 0) yield return (CurrencyType.Copper, Copper);
        if (Silver > 0) yield return (CurrencyType.Silver, Silver);
        if (Gold   > 0) yield return (CurrencyType.Gold,   Gold);
        if (Energy > 0) yield return (CurrencyType.Energy, Energy);
    }
}
