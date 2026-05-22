namespace War.Core.Economy;

/// <summary>
/// Registro inmutable de un único movimiento de moneda. Es la unidad de auditoría.
/// </summary>
/// <param name="Id">Identificador único — se genera en el momento de creación.</param>
/// <param name="Timestamp">Cuándo ocurrió (UTC).</param>
/// <param name="PlayerId">Jugador afectado.</param>
/// <param name="Currency">Moneda que se movió.</param>
/// <param name="Direction">Credit (entró) / Debit (salió). Derivable del signo, explícito para auditoría.</param>
/// <param name="Amount">Cantidad absoluta del movimiento (siempre positivo; la dirección la da Direction).</param>
/// <param name="Source">Origen semántico del movimiento (MobDrop, CraftingTierUp, etc.).</param>
/// <param name="Description">Texto libre para el auditor: "Craft T2→T3 Espada Épica dev14" u similar.</param>
/// <param name="BalanceBefore">Saldo de la moneda ANTES de aplicar la transacción.</param>
/// <param name="BalanceAfter">Saldo de la moneda DESPUÉS de aplicar la transacción.</param>
/// <param name="RelatedEntityId">
/// ID opcional del objeto relacionado (instancia de equipo crafteada, ID de mob, etc.).
/// Permite reconstruir la cadena causal desde la auditoría sin ambigüedad.
/// </param>
/// <remarks>
/// Una vez creada, una transacción NO se modifica ni se borra. Las correcciones se aplican como
/// nuevas transacciones (AdminGrant / AdminDeduct) con referencia a la original en Description.
/// </remarks>
public sealed record WalletTransaction(
    Guid Id,
    DateTime Timestamp,
    Guid PlayerId,
    CurrencyType Currency,
    TransactionDirection Direction,
    long Amount,
    TransactionSource Source,
    string Description,
    long BalanceBefore,
    long BalanceAfter,
    Guid? RelatedEntityId = null);

public enum TransactionDirection
{
    Credit = 0,   // entra al wallet
    Debit = 1,    // sale del wallet
}
