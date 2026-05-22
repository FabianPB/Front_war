using War.Core.Skills;
using War.Core.Stats;

namespace War.Core.Equipment;

/// <summary>
/// Catálogo completo de equipamiento. Genera los 189 diseños base
/// (6 slots clase-específicos × 4 clases × 7 variantes + 3 slots globales × 7)
/// a partir de plantillas por slot y clase.
///
/// Los stats base aquí son para Rango Común, Tier 1, Desarrollo 1.
/// Los rangos superiores se generan multiplicando por el factor de rango.
/// Los tiers y desarrollos se calculan en runtime por EquipmentFormulas.
/// </summary>
public static class EquipmentCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, EquipmentDefinition>> _definitions =
        new(BuildAllDefinitions);

    public static IReadOnlyDictionary<string, EquipmentDefinition> All => _definitions.Value;

    public static EquipmentDefinition Get(string definitionId)
    {
        return All.TryGetValue(definitionId, out var def)
            ? def
            : throw new KeyNotFoundException($"Equipment definition '{definitionId}' not found.");
    }

    public static IReadOnlyList<EquipmentDefinition> GetForClass(ClassType classType)
    {
        return All.Values.Where(d => d.RequiredClass == null || d.RequiredClass == classType).ToArray();
    }

    // ══════════════════════════════════════════════════════════════
    // GENERACIÓN DEL CATÁLOGO
    // ══════════════════════════════════════════════════════════════

    private static IReadOnlyDictionary<string, EquipmentDefinition> BuildAllDefinitions()
    {
        var definitions = new Dictionary<string, EquipmentDefinition>();

        // ── Slots clase-específicos (6 slots × 4 clases) ──
        foreach (var classType in new[] { ClassType.Sorcerer, ClassType.Juramentada, ClassType.Lancero, ClassType.Bruiser })
        {
            var className = classType.ToString().ToLowerInvariant();
            var templates = GetClassSpecificTemplates(classType);

            foreach (var template in templates)
            {
                GenerateAllRarities(definitions, template, className, classType);
            }
        }

        // ── Slots globales (3 slots, sin clase) ──
        var globalTemplates = GetGlobalTemplates();
        foreach (var template in globalTemplates)
        {
            GenerateAllRarities(definitions, template, "global", null);
        }

        return definitions;
    }

    private static void GenerateAllRarities(
        Dictionary<string, EquipmentDefinition> defs,
        SlotTemplate template,
        string classKey,
        ClassType? requiredClass)
    {
        var slotKey = template.Slot.ToString().ToLowerInvariant();

        foreach (var rarity in new[] { EquipmentRarity.Common, EquipmentRarity.Special, EquipmentRarity.Epic })
        {
            var rarityKey = rarity.ToString().ToLowerInvariant();
            var mult = EquipmentFormulas.GetRarityMultiplier(rarity);

            // Variante ofensiva
            var offId = $"{slotKey}.{classKey}.{rarityKey}.offensive";
            defs[offId] = new EquipmentDefinition(
                offId,
                $"{template.OffensiveName} ({RarityLabel(rarity)})",
                template.OffensiveDescription,
                template.Slot, rarity, EquipmentVariant.Offensive, requiredClass,
                ScaleStats(template.OffensiveStats, mult));

            // Variante defensiva
            var defId = $"{slotKey}.{classKey}.{rarityKey}.defensive";
            defs[defId] = new EquipmentDefinition(
                defId,
                $"{template.DefensiveName} ({RarityLabel(rarity)})",
                template.DefensiveDescription,
                template.Slot, rarity, EquipmentVariant.Defensive, requiredClass,
                ScaleStats(template.DefensiveStats, mult));
        }

        // Legendario (hybrid: combina ambas al 80%)
        var legMult = EquipmentFormulas.GetRarityMultiplier(EquipmentRarity.Legendary);
        var hybridStats = CombineHybridStats(
            ScaleStats(template.OffensiveStats, legMult),
            ScaleStats(template.DefensiveStats, legMult));
        var legId = $"{slotKey}.{classKey}.legendary.hybrid";
        defs[legId] = new EquipmentDefinition(
            legId,
            $"{template.LegendaryName} (Legendario)",
            template.LegendaryDescription,
            template.Slot, EquipmentRarity.Legendary, EquipmentVariant.Hybrid, requiredClass,
            hybridStats);
    }

    private static IReadOnlyList<EquipmentStatGrant> ScaleStats(
        IReadOnlyList<EquipmentStatGrant> baseStats, decimal multiplier)
    {
        return baseStats.Select(s => s with
        {
            BaseValue = decimal.Round(s.BaseValue * multiplier, 2, MidpointRounding.AwayFromZero)
        }).ToArray();
    }

    private static IReadOnlyList<EquipmentStatGrant> CombineHybridStats(
        IReadOnlyList<EquipmentStatGrant> offStats, IReadOnlyList<EquipmentStatGrant> defStats)
    {
        var combined = new List<EquipmentStatGrant>();
        var factor = EquipmentFormulas.LegendaryHybridFactor;

        foreach (var s in offStats)
            combined.Add(s with { BaseValue = decimal.Round(s.BaseValue * factor, 2) });
        foreach (var s in defStats)
        {
            var existing = combined.FindIndex(c => c.Stat == s.Stat);
            if (existing >= 0)
            {
                // Mismo stat: sumar (no duplicar)
                combined[existing] = combined[existing] with
                {
                    BaseValue = combined[existing].BaseValue + decimal.Round(s.BaseValue * factor, 2)
                };
            }
            else
            {
                combined.Add(s with { BaseValue = decimal.Round(s.BaseValue * factor, 2) });
            }
        }
        return combined;
    }

    private static string RarityLabel(EquipmentRarity rarity) => rarity switch
    {
        EquipmentRarity.Common => "Común",
        EquipmentRarity.Special => "Especial",
        EquipmentRarity.Epic => "Épico",
        EquipmentRarity.Legendary => "Legendario",
        _ => rarity.ToString()
    };

    // ══════════════════════════════════════════════════════════════
    // PLANTILLAS POR SLOT — STATS BASE (Común, T1, Dev1)
    // ══════════════════════════════════════════════════════════════

    private sealed record SlotTemplate(
        EquipmentSlot Slot,
        string OffensiveName,
        string OffensiveDescription,
        IReadOnlyList<EquipmentStatGrant> OffensiveStats,
        string DefensiveName,
        string DefensiveDescription,
        IReadOnlyList<EquipmentStatGrant> DefensiveStats,
        string LegendaryName,
        string LegendaryDescription,
        string LegendaryFlavorText = "");

    // ── Helpers para construir stats ──
    private static EquipmentStatGrant Flat(StatType stat, decimal value) => new(stat, value, false);
    private static EquipmentStatGrant Pct(StatType stat, decimal value) => new(stat, value, true);

    // ══════════════════════════════════════════════════════════════
    // CLASE-ESPECÍFICOS
    // ══════════════════════════════════════════════════════════════

    private static IReadOnlyList<SlotTemplate> GetClassSpecificTemplates(ClassType classType)
    {
        // Determinar stats primarios según clase
        var primaryAttack = classType is ClassType.Sorcerer or ClassType.Juramentada
            ? StatType.MagicAttack : StatType.PhysicalAttack;
        var primaryDefense = classType is ClassType.Sorcerer or ClassType.Juramentada
            ? StatType.MagicResistance : StatType.Defense;
        var primaryPenetration = classType is ClassType.Sorcerer or ClassType.Juramentada
            ? StatType.MagicPenetration : StatType.DefensePenetration;

        var weaponName = classType switch
        {
            ClassType.Sorcerer => "Báculo",
            ClassType.Juramentada => "Espada de Luz",
            ClassType.Lancero => "Lanza",
            ClassType.Bruiser => "Hacha de Guerra",
            _ => "Arma"
        };

        return new SlotTemplate[]
        {
            // ── ARMA ──
            new(EquipmentSlot.Weapon,
                $"{weaponName} de Ataque", $"{weaponName} forjada para maximizar el poder ofensivo.",
                new[] { Flat(primaryAttack, 18m), Pct(StatType.CritChance, 2.5m) },
                $"{weaponName} de Protección", $"{weaponName} templada para resistir el combate prolongado.",
                new[] { Flat(primaryAttack, 12m), Flat(StatType.HpRegen, 3m) },
                $"{weaponName} Ancestral", $"{weaponName} legendaria que canaliza poder ofensivo y defensivo.",
                $"{weaponName} Ancestral legendaria."),

            // ── CASCO ──
            new(EquipmentSlot.Helmet,
                "Yelmo de Precisión", "Artículo de cabeza que agudiza la puntería del portador.",
                new[] { Flat(StatType.Accuracy, 12m), Pct(StatType.CritDamage, 3.0m) },
                "Yelmo de Fortaleza", "Artículo de cabeza que protege la mente y el cuerpo.",
                new[] { Flat(StatType.MaxHp, 80m), Flat(StatType.Tenacity, 5m) },
                "Corona del Conquistador", "Artículo legendario que otorga precisión y resistencia.",
                "Corona legendaria de poder absoluto."),

            // ── PECHERA ──
            new(EquipmentSlot.Chestplate,
                "Pechera de Penetración", "Armadura que amplifica el poder de las habilidades.",
                new[] { Pct(StatType.SkillDamageIncrease, 3.0m), Flat(primaryPenetration, 8m) },
                "Pechera de Muralla", "Armadura pesada que absorbe el castigo del combate.",
                new[] { Flat(primaryDefense, 12m), Flat(StatType.MaxHp, 60m) },
                "Coraza del Titán", "Armadura legendaria que potencia y protege.",
                "Coraza legendaria de equilibrio supremo."),

            // ── BOTAS ──
            new(EquipmentSlot.Boots,
                "Botas de Agilidad", "Calzado diseñado para la movilidad y la evasión.",
                new[] { Flat(StatType.MoveSpeed, 4m), Flat(StatType.Evasion, 6m) },
                "Botas de Raíces", "Calzado que ancla al portador y lo protege de desplazamientos.",
                new[] { Flat(StatType.Evasion, 10m), Flat(StatType.MoveSpeed, 3m) },
                "Sandalias del Viento", "Calzado legendario de velocidad y estabilidad.",
                "Sandalias legendarias de equilibrio perfecto."),

            // ── BRAZALETES ──
            new(EquipmentSlot.Bracers,
                "Brazaletes de Furia", "Refuerzan la potencia de los ataques directos.",
                new[] { Pct(StatType.BasicAttackDamageIncrease, 3.0m), Pct(StatType.AttackSpeed, 2.0m) },
                "Brazaletes de Escudo", "Refuerzan la defensa contra impactos directos.",
                new[] { Flat(primaryDefense, 8m), Pct(StatType.CritDamageTakenReduction, 2.0m) },
                "Muñequeras del Dragón", "Brazaletes legendarios de ataque y defensa.",
                "Muñequeras legendarias de poder dracónico."),

            // ── GUANTES ──
            new(EquipmentSlot.Gloves,
                "Guantes de Filo", "Guantes que afinan los golpes críticos.",
                new[] { Pct(StatType.CritChance, 2.0m), Pct(StatType.CritDamageIncrease, 2.5m) },
                "Guantes de Guardia", "Guantes que desvían golpes críticos enemigos.",
                new[] { Pct(StatType.CriticalEvasion, 3.0m), Pct(StatType.BasicAttackDamageReduction, 2.0m) },
                "Garras del Coloso", "Guantes legendarios de precisión y resiliencia.",
                "Garras legendarias que dominan el campo de batalla."),
        };
    }

    // ══════════════════════════════════════════════════════════════
    // GLOBALES (Aretes, Anillo, Collar — mismos para todas las clases)
    // ══════════════════════════════════════════════════════════════

    private static IReadOnlyList<SlotTemplate> GetGlobalTemplates()
    {
        return new SlotTemplate[]
        {
            // ── ARETES ──
            new(EquipmentSlot.Earrings,
                "Aretes de Dominio", "Joyería que amplifica la presión en combate PvP.",
                new[] { Pct(StatType.PvPDamageIncrease, 2.0m), Flat(StatType.Accuracy, 6m) },
                "Aretes de Resistencia", "Joyería que mitiga el daño recibido en combate PvP.",
                new[] { Pct(StatType.PvPDamageReduction, 2.0m), Flat(StatType.Tenacity, 5m) },
                "Aretes del Oráculo", "Joyería legendaria que otorga dominio y resistencia en PvP.",
                "Aretes legendarios de supremacía en combate."),

            // ── ANILLO ──
            new(EquipmentSlot.Ring,
                "Anillo de Fortuna", "Un anillo que favorece los golpes decisivos.",
                new[] { Pct(StatType.CritChance, 1.5m), Pct(StatType.CritDamage, 2.0m) },
                "Anillo de Vitalidad", "Un anillo que refuerza la vida y la recuperación.",
                new[] { Flat(StatType.MaxHp, 50m), Pct(StatType.HealingReceived, 2.0m) },
                "Sortija del Destino", "Anillo legendario que une la fortuna con la supervivencia.",
                "Sortija legendaria de poder y vida."),

            // ── COLLAR ──
            new(EquipmentSlot.Necklace,
                "Collar de Canalización", "Joyería que acelera la recuperación de habilidades.",
                new[] { Pct(StatType.SkillDamageIncrease, 2.0m), Pct(StatType.CooldownReduction, 1.5m) },
                "Collar de Protección", "Joyería que reduce el impacto de las habilidades enemigas.",
                new[] { Pct(StatType.SkillDamageReduction, 2.0m), Flat(StatType.ManaRegen, 3m) },
                "Amuleto del Abismo", "Collar legendario de poder arcano y protección mística.",
                "Amuleto legendario de canalización y resiliencia."),
        };
    }
}
