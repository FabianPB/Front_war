using War.Core.Skills;
using War.Core.Stats;

namespace War.Core.Equipment;

// ══════════════════════════════════════════════════════════════════
// ENUMS
// ══════════════════════════════════════════════════════════════════

/// <summary>9 slots de equipamiento. 6 clase-específicos + 3 globales.</summary>
public enum EquipmentSlot
{
    // Clase-específicos (se ajustan por clase)
    Weapon,       // Arma: Báculo (Sorc), Espada de Luz (Jura), Lanza (Lanc), Hacha/Martillo (Brui)
    Helmet,       // Casco / artículo de cabeza
    Chestplate,   // Pechera
    Boots,        // Botas
    Bracers,      // Brazaletes
    Gloves,       // Guantes

    // Globales (iguales para todas las clases)
    Earrings,     // Aretes
    Ring,         // Anillo
    Necklace      // Collar
}

/// <summary>4 rangos de rareza con salto de poder significativo entre cada uno.</summary>
public enum EquipmentRarity
{
    Common,       // Común — stats básicos, 2 variantes (ofensiva/defensiva)
    Special,      // Especial — stats mejorados, 2 variantes
    Epic,         // Épico — stats altos, 2 variantes
    Legendary     // Legendario — combina ofensiva + defensiva, 1 sola variante
}

/// <summary>
/// Orientación de la pieza. De Común a Épico hay 2 variantes por slot.
/// Legendario siempre es Hybrid (ambos stats).
/// </summary>
public enum EquipmentVariant
{
    Offensive,    // Stats ofensivos (ataque, crit, penetración, etc.)
    Defensive,    // Stats defensivos (HP, defensa, resistencia, evasion, etc.)
    Hybrid        // Solo Legendario: combina ambos al ~80% de cada especialización
}

// ══════════════════════════════════════════════════════════════════
// DEFINICIÓN BASE (catálogo — lo que "existe" en el juego)
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// Definición de una pieza de equipo en el catálogo. Representa un diseño base
/// (slot + rango + variante + clase). Los stats aquí son Tier 1, Desarrollo 1.
/// Los valores finales se calculan aplicando los multiplicadores de tier y desarrollo.
/// </summary>
public sealed record EquipmentDefinition(
    string Id,                                      // "weapon.sorcerer.common.offensive"
    string Name,                                    // "Báculo de Aprendiz"
    string Description,                             // Descripción temática
    EquipmentSlot Slot,
    EquipmentRarity Rarity,
    EquipmentVariant Variant,
    ClassType? RequiredClass,                        // null = global (aretes, anillo, collar)
    IReadOnlyList<EquipmentStatGrant> BaseStats);    // Stats en Tier 1, Desarrollo 1

/// <summary>
/// Un stat otorgado por una pieza de equipo. El valor aquí es el base
/// (Tier 1, Desarrollo 1). El valor final se multiplica por tier × desarrollo.
/// </summary>
public sealed record EquipmentStatGrant(
    StatType Stat,
    decimal BaseValue,
    bool IsPercentage);    // true = se muestra como % (e.g., CritChance 2.5%)

// ══════════════════════════════════════════════════════════════════
// INSTANCIA (lo que el jugador TIENE equipado)
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// Una pieza de equipo concreta que un jugador posee. Tiene un tier (1-4)
/// y un nivel de desarrollo (1-30). Los stats se calculan en runtime.
/// </summary>
public sealed class EquipmentInstance
{
    public required string DefinitionId { get; init; }  // Referencia al catálogo
    public required EquipmentSlot Slot { get; init; }
    public int Tier { get; set; } = 1;                  // 1-4
    public int DevelopmentLevel { get; set; } = 1;      // 1-30

    /// <summary>
    /// ID único de esta instancia (para cuando el jugador tenga inventario
    /// y pueda comparar/intercambiar piezas). Por ahora cada slot tiene una.
    /// </summary>
    public string InstanceId { get; init; } = Guid.NewGuid().ToString("N");
}

// ══════════════════════════════════════════════════════════════════
// LOADOUT (el set completo de un jugador)
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// Vista del equipamiento del jugador. NO es una estructura independiente —
/// es un helper que lee directamente del inventario (la fuente de verdad).
///
/// Un equipo "equipado" es un InventoryItem con IsEquipped=true. No sale
/// del inventario, no se duplica, no se copia. Solo se marca.
/// </summary>
public static class EquipmentLoadoutHelper
{
    /// <summary>
    /// Obtiene la pieza equipada en un slot leyendo del inventario.
    /// </summary>
    public static InventoryItem? GetEquipped(PlayerInventory inventory, EquipmentSlot slot)
    {
        return inventory.GetEquippedInSlot(slot);
    }

    /// <summary>
    /// Calcula los stats totales de todo el equipo actualmente equipado,
    /// leyendo directamente del inventario.
    /// </summary>
    public static Dictionary<Stats.StatType, decimal> CalculateTotalEquipmentStats(
        PlayerInventory inventory, EquipmentStatService statService)
    {
        var totals = new Dictionary<Stats.StatType, decimal>();

        foreach (var equipped in inventory.GetAllEquipped())
        {
            if (equipped.ItemType != InventoryItemType.Equipment) continue;

            var definition = EquipmentCatalog.Get(equipped.DefinitionId);
            foreach (var grant in definition.BaseStats)
            {
                var finalValue = EquipmentFormulas.CalculateStatValue(
                    grant.BaseValue, equipped.Tier, equipped.DevelopmentLevel);

                if (totals.ContainsKey(grant.Stat))
                    totals[grant.Stat] += finalValue;
                else
                    totals[grant.Stat] = finalValue;
            }
        }

        return totals;
    }

    /// <summary>
    /// Construye el DTO del loadout para enviar al cliente.
    /// </summary>
    public static EquipmentLoadoutDto BuildLoadoutDto(
        PlayerInventory inventory, EquipmentStatService statService)
    {
        var items = new List<EquipmentSlotDto>();

        foreach (var equipped in inventory.GetAllEquipped())
        {
            if (equipped.ItemType != InventoryItemType.Equipment) continue;

            var definition = EquipmentCatalog.Get(equipped.DefinitionId);
            var stats = statService.CalculateStats(definition, equipped.Tier, equipped.DevelopmentLevel);

            items.Add(new EquipmentSlotDto(
                equipped.DefinitionId,
                definition.Name,
                definition.Slot.ToString(),
                definition.Rarity.ToString(),
                definition.Variant.ToString(),
                equipped.Tier,
                equipped.DevelopmentLevel,
                stats));
        }

        return new EquipmentLoadoutDto(items);
    }
}

// ══════════════════════════════════════════════════════════════════
// DTOs (para broadcasting al cliente)
// ══════════════════════════════════════════════════════════════════

public sealed record EquipmentSlotDto(
    string DefinitionId,
    string Name,
    string SlotName,
    string RarityName,
    string VariantName,
    int Tier,
    int DevelopmentLevel,
    IReadOnlyList<EquipmentStatValueDto> Stats);

public sealed record EquipmentStatValueDto(
    string StatName,
    decimal Value,
    bool IsPercentage);

public sealed record EquipmentLoadoutDto(
    IReadOnlyList<EquipmentSlotDto> EquippedItems);
