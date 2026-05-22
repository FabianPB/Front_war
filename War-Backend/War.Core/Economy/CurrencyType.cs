namespace War.Core.Economy;

/// <summary>
/// Recursos rectores del jugador que NO viven en el inventario.
/// Cada moneda tiene su propio slot en el wallet, con auditoría estricta por transacción.
/// </summary>
/// <remarks>
/// Decision: Copper / Silver / Gold forman una jerarquía clásica 100:1 (1 Oro = 100 Plata = 10 000 Cobre).
/// El cliente puede mostrarlas unificadas, pero el servidor las mantiene separadas para que la auditoría
/// reconozca exactamente qué denominación entró/salió en cada transacción.
///
/// Decision: Energy es un recurso con cap y regeneración temporal; no se apila infinitamente.
/// El consumo concreto por acción se decide en la capa de aplicación — el wallet solo provee las operaciones.
///
/// NOTA: Las gemas NO aparecen aquí: son objetos equipables, no moneda. Se modelan como ítems del inventario.
/// </remarks>
public enum CurrencyType
{
    Copper = 0,
    Silver = 1,
    Gold   = 2,
    Energy = 3,
}
