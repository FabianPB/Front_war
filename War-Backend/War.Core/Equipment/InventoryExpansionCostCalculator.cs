using War.Core.Economy;

namespace War.Core.Equipment;

/// <summary>
/// Coste de expandir el inventario en lotes de 50 slots.
/// El primer lote es asequible, los siguientes escalan para que las expansiones tardías sean un gasto real.
/// </summary>
/// <remarks>
/// Inicial: 80 slots. Tope: 280 slots (= 4 expansiones).
/// Cada expansión suma 50 slots y cuesta más que la anterior.
///
/// Costes definidos directamente en oro (las expansiones son eventos late-early/mid-game):
///   1ª (80→130): 10 oro
///   2ª (130→180): 50 oro
///   3ª (180→230): 250 oro
///   4ª (230→280): 1 000 oro
///
/// La progresión es ~×5 por cada paso — no tan agresiva como el crafteo, pero tampoco barata.
/// </remarks>
public static class InventoryExpansionCostCalculator
{
    public const int InitialCapacity = 80;
    public const int MaxCapacity = 280;
    public const int ExpansionBatchSize = 50;
    public const int MaxExpansions = (MaxCapacity - InitialCapacity) / ExpansionBatchSize; // 4

    private static readonly long[] CostByExpansionIndex =
    {
        10,      // 1ª expansión
        50,      // 2ª
        250,     // 3ª
        1_000    // 4ª
    };

    /// <summary>
    /// Coste de la siguiente expansión a aplicar, dado cuántas expansiones ya se han comprado.
    /// </summary>
    /// <param name="expansionsPurchased">0 = ninguna aún, 4 = al máximo.</param>
    public static CurrencyCost GetNextExpansionCost(int expansionsPurchased)
    {
        if (expansionsPurchased < 0)
            throw new ArgumentOutOfRangeException(nameof(expansionsPurchased));
        if (expansionsPurchased >= MaxExpansions)
            throw new InvalidOperationException("El inventario ya está al tope de expansiones.");

        return new CurrencyCost(Gold: CostByExpansionIndex[expansionsPurchased]);
    }

    /// <summary>
    /// Capacidad resultante tras aplicar N expansiones.
    /// </summary>
    public static int GetCapacityAfter(int expansionsPurchased) =>
        InitialCapacity + expansionsPurchased * ExpansionBatchSize;

    /// <summary>
    /// ¿Se puede seguir expandiendo?
    /// </summary>
    public static bool CanExpandFurther(int expansionsPurchased) =>
        expansionsPurchased < MaxExpansions;
}
