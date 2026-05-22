using War.Core.Stats;

namespace War.Core.Equipment;

/// <summary>
/// Servicio de cálculo de stats de equipamiento.
///
/// Responsabilidades:
///   1. Calcular los stats finales de una pieza individual (tier × desarrollo)
///   2. Calcular los stats totales de un loadout completo (suma de todas las piezas)
///   3. Proveer una vista de stats para el cliente (DTOs)
///
/// Este servicio es stateless y puede ser singleton. No modifica ningún estado.
/// </summary>
public sealed class EquipmentStatService
{
    /// <summary>
    /// Calcula los stats finales de una pieza de equipo individual.
    /// Aplica la fórmula: baseValue × tierMult × devMult.
    /// </summary>
    public IReadOnlyList<EquipmentStatValueDto> CalculateInstanceStats(EquipmentInstance instance)
    {
        var definition = EquipmentCatalog.Get(instance.DefinitionId);
        return CalculateStats(definition, instance.Tier, instance.DevelopmentLevel);
    }

    /// <summary>
    /// Calcula los stats finales de una definición en un tier y desarrollo específicos.
    /// </summary>
    public IReadOnlyList<EquipmentStatValueDto> CalculateStats(
        EquipmentDefinition definition, int tier, int developmentLevel)
    {
        var results = new List<EquipmentStatValueDto>(definition.BaseStats.Count);

        foreach (var grant in definition.BaseStats)
        {
            var finalValue = EquipmentFormulas.CalculateStatValue(
                grant.BaseValue, tier, developmentLevel);
            results.Add(new EquipmentStatValueDto(
                grant.Stat.ToString(),
                finalValue,
                grant.IsPercentage));
        }

        return results;
    }

    /// <summary>
    /// Calcula la contribución total de stats del equipamiento de un jugador.
    /// Lee directamente del inventario (fuente de verdad).
    /// El resultado se puede sumar a los stats base del personaje.
    /// </summary>
    public Dictionary<StatType, decimal> CalculateEquippedStats(PlayerInventory inventory)
    {
        return EquipmentLoadoutHelper.CalculateTotalEquipmentStats(inventory, this);
    }

    /// <summary>
    /// Construye el DTO del loadout para enviar al cliente.
    /// Lee directamente del inventario.
    /// </summary>
    public EquipmentLoadoutDto BuildLoadoutDto(PlayerInventory inventory)
    {
        return EquipmentLoadoutHelper.BuildLoadoutDto(inventory, this);
    }

    /// <summary>
    /// Compara dos piezas y devuelve la diferencia de stats (nueva - actual).
    /// Útil para que el cliente muestre "+X" o "-X" al comparar equipo.
    /// </summary>
    public IReadOnlyList<(string StatName, decimal Difference, bool IsPercentage)> CompareInstances(
        EquipmentInstance current, EquipmentInstance candidate)
    {
        var currentStats = CalculateInstanceStats(current);
        var candidateStats = CalculateInstanceStats(candidate);

        var currentDict = currentStats.ToDictionary(s => s.StatName, s => s);
        var candidateDict = candidateStats.ToDictionary(s => s.StatName, s => s);

        var allKeys = currentDict.Keys.Union(candidateDict.Keys).Distinct();
        var diffs = new List<(string StatName, decimal Difference, bool IsPercentage)>();

        foreach (var key in allKeys)
        {
            var curVal = currentDict.TryGetValue(key, out var c) ? c.Value : 0m;
            var canVal = candidateDict.TryGetValue(key, out var n) ? n.Value : 0m;
            var isPct = (c?.IsPercentage ?? n?.IsPercentage) ?? false;

            if (curVal != canVal)
                diffs.Add((key, canVal - curVal, isPct));
        }

        return diffs;
    }
}
