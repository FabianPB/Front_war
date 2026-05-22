using System.Globalization;
using System.Text.RegularExpressions;
using War.Core.PowerScore;
using War.Core.Stats;

namespace War.Api.Application.Characters;

internal enum CharacterSnapshotValueKind
{
    Number,
    Percentage
}

internal sealed record CharacterStatPresentationMetadata(
    StatType StatType,
    string Key,
    string Label,
    string SectionKey,
    string SectionLabel,
    int SectionOrder,
    int StatOrder,
    CharacterSnapshotValueKind ValueKind);

internal static class CharacterSnapshotPresentationCatalog
{
    private static readonly HashSet<StatType> ResourceStats =
    [
        StatType.MaxHp,
        StatType.MaxMana,
        StatType.HpRegen,
        StatType.ManaRegen,
        StatType.UltimateChargeMax
    ];

    private static readonly HashSet<StatType> OffensiveStats =
    [
        StatType.PhysicalAttack,
        StatType.MagicAttack,
        StatType.AttackSpeed,
        StatType.CritChance,
        StatType.CritDamage,
        StatType.Accuracy,
        StatType.DefensePenetration,
        StatType.MagicPenetration,
        StatType.AttackRange,
        StatType.BasicAttackDamageIncrease,
        StatType.SkillDamageIncrease,
        StatType.CritDamageIncrease,
        StatType.MonsterDamageIncrease,
        StatType.BossDamageIncrease,
        StatType.PvPDamageIncrease,
        StatType.HeatDamageIncrease,
        StatType.ColdDamageIncrease,
        StatType.ElectrifiedDamageIncrease,
        StatType.PoisonDamageIncrease
    ];

    private static readonly HashSet<StatType> DefensiveStats =
    [
        StatType.Defense,
        StatType.MagicResistance,
        StatType.Evasion,
        StatType.CriticalEvasion,
        StatType.Tenacity,
        StatType.HealingReceived,
        StatType.BasicAttackDamageReduction,
        StatType.SkillDamageReduction,
        StatType.CritDamageTakenReduction,
        StatType.MonsterDamageReduction,
        StatType.BossDamageReduction,
        StatType.PvPDamageReduction,
        StatType.HeatDamageReduction,
        StatType.ColdDamageReduction,
        StatType.ElectrifiedDamageReduction,
        StatType.PoisonDamageReduction
    ];

    private static readonly HashSet<StatType> StatusStats =
    [
        StatType.HeatApplyChance,
        StatType.ColdApplyChance,
        StatType.ElectrifiedApplyChance,
        StatType.PoisonApplyChance,
        StatType.WeakenApplyChance,
        StatType.BlindApplyChance,
        StatType.StunApplyChance,
        StatType.FreezeApplyChance,
        StatType.ParalyzeApplyChance,
        StatType.HeatEvadeChance,
        StatType.ColdEvadeChance,
        StatType.ElectrifiedEvadeChance,
        StatType.PoisonEvadeChance,
        StatType.WeakenEvadeChance,
        StatType.BlindEvadeChance,
        StatType.StunEvadeChance,
        StatType.FreezeEvadeChance,
        StatType.ParalyzeEvadeChance
    ];

    private static readonly HashSet<StatType> PercentageStats =
    [
        StatType.AttackSpeed,
        StatType.CritChance,
        StatType.CritDamage,
        StatType.CriticalEvasion,
        StatType.DefensePenetration,
        StatType.MagicPenetration,
        StatType.Tenacity,
        StatType.CooldownReduction,
        StatType.SkillRecoveryRate,
        StatType.HealingEffectiveness,
        StatType.HealingReceived,
        StatType.ExpGain,
        StatType.DropRate,
        StatType.DropQuality,
        StatType.HeatApplyChance,
        StatType.ColdApplyChance,
        StatType.ElectrifiedApplyChance,
        StatType.PoisonApplyChance,
        StatType.WeakenApplyChance,
        StatType.BlindApplyChance,
        StatType.StunApplyChance,
        StatType.FreezeApplyChance,
        StatType.ParalyzeApplyChance,
        StatType.HeatEvadeChance,
        StatType.ColdEvadeChance,
        StatType.ElectrifiedEvadeChance,
        StatType.PoisonEvadeChance,
        StatType.WeakenEvadeChance,
        StatType.BlindEvadeChance,
        StatType.StunEvadeChance,
        StatType.FreezeEvadeChance,
        StatType.ParalyzeEvadeChance,
        StatType.BasicAttackDamageIncrease,
        StatType.SkillDamageIncrease,
        StatType.CritDamageIncrease,
        StatType.MonsterDamageIncrease,
        StatType.BossDamageIncrease,
        StatType.PvPDamageIncrease,
        StatType.HeatDamageIncrease,
        StatType.ColdDamageIncrease,
        StatType.ElectrifiedDamageIncrease,
        StatType.PoisonDamageIncrease,
        StatType.BasicAttackDamageReduction,
        StatType.SkillDamageReduction,
        StatType.CritDamageTakenReduction,
        StatType.MonsterDamageReduction,
        StatType.BossDamageReduction,
        StatType.PvPDamageReduction,
        StatType.HeatDamageReduction,
        StatType.ColdDamageReduction,
        StatType.ElectrifiedDamageReduction,
        StatType.PoisonDamageReduction
    ];

    private static readonly IReadOnlyList<CharacterStatPresentationMetadata> AllStatMetadata = Enum
        .GetValues<StatType>()
        .Select((statType, index) => BuildMetadata(statType, index))
        .OrderBy(metadata => metadata.SectionOrder)
        .ThenBy(metadata => metadata.StatOrder)
        .ToArray();

    private static readonly IReadOnlyDictionary<StatType, CharacterStatPresentationMetadata> MetadataByStatType =
        AllStatMetadata.ToDictionary(metadata => metadata.StatType);

    public static IReadOnlyList<CharacterStatPresentationMetadata> GetAllStatMetadata()
    {
        return AllStatMetadata;
    }

    public static CharacterStatPresentationMetadata GetMetadata(StatType statType)
    {
        return MetadataByStatType[statType];
    }

    public static string FormatStatValue(StatType statType, decimal value)
    {
        return GetValueKind(statType) switch
        {
            CharacterSnapshotValueKind.Percentage => FormatPercentage(value),
            _ => FormatNumber(value)
        };
    }

    public static string FormatPowerScore(decimal value)
    {
        return value.ToString("N2", CultureInfo.InvariantCulture);
    }

    public static string FormatShare(decimal value)
    {
        return FormatPercentage(value);
    }

    public static string GetPowerScoreCategoryKey(PowerScoreCategory category)
    {
        return ToKebabCase(category.ToString());
    }

    public static string GetPowerScoreCategoryLabel(PowerScoreCategory category)
    {
        return category switch
        {
            PowerScoreCategory.Offensive => "Ofensivas",
            PowerScoreCategory.Defensive => "Defensivas",
            PowerScoreCategory.Recovery => "Recuperacion",
            PowerScoreCategory.Utility => "Utilidad",
            PowerScoreCategory.Status => "Estados",
            PowerScoreCategory.Progression => "Progresion",
            _ => category.ToString()
        };
    }

    public static string GetClassKey(string className)
    {
        return ToKebabCase(className);
    }

    public static string FormatNumber(decimal value)
    {
        var rounded = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        return rounded == decimal.Truncate(rounded)
            ? rounded.ToString("N0", CultureInfo.InvariantCulture)
            : rounded.ToString("N2", CultureInfo.InvariantCulture);
    }

    public static string FormatPercentage(decimal value)
    {
        return decimal.Round(value * 100m, 2, MidpointRounding.AwayFromZero).ToString("N2", CultureInfo.InvariantCulture) + "%";
    }

    private static CharacterStatPresentationMetadata BuildMetadata(StatType statType, int order)
    {
        var (sectionKey, sectionLabel, sectionOrder) = ResolveSection(statType);

        return new CharacterStatPresentationMetadata(
            statType,
            ToKebabCase(statType.ToString()),
            ResolveLabel(statType),
            sectionKey,
            sectionLabel,
            sectionOrder,
            order,
            GetValueKind(statType));
    }

    private static (string SectionKey, string SectionLabel, int SectionOrder) ResolveSection(StatType statType)
    {
        if (ResourceStats.Contains(statType))
        {
            return ("resources", "Recursos", 0);
        }

        if (OffensiveStats.Contains(statType))
        {
            return ("offensive", "Ofensivas", 1);
        }

        if (DefensiveStats.Contains(statType))
        {
            return ("defensive", "Defensivas", 2);
        }

        if (StatusStats.Contains(statType))
        {
            return ("status", "Estados", 3);
        }

        return ("utility", "Utilidad", 4);
    }

    private static CharacterSnapshotValueKind GetValueKind(StatType statType)
    {
        return PercentageStats.Contains(statType)
            ? CharacterSnapshotValueKind.Percentage
            : CharacterSnapshotValueKind.Number;
    }

    private static string ResolveLabel(StatType statType)
    {
        return statType switch
        {
            StatType.MaxHp => "Max HP",
            StatType.MaxMana => "Max Mana",
            StatType.HpRegen => "HP Regen",
            StatType.ManaRegen => "Mana Regen",
            StatType.UltimateChargeMax => "Ultimate Charge Max",
            StatType.PvPDamageIncrease => "PvP Damage Increase",
            StatType.PvPDamageReduction => "PvP Damage Reduction",
            StatType.CritChance => "Crit Chance",
            StatType.CritDamage => "Crit Damage",
            StatType.CritDamageIncrease => "Crit Damage Increase",
            StatType.CritDamageTakenReduction => "Crit Damage Taken Reduction",
            StatType.CriticalEvasion => "Critical Evasion",
            _ => Humanize(statType.ToString())
        };
    }

    private static string Humanize(string value)
    {
        var withSpaces = Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");
        return withSpaces
            .Replace("Hp", "HP", StringComparison.Ordinal)
            .Replace("Pv P", "PvP", StringComparison.Ordinal);
    }

    private static string ToKebabCase(string value)
    {
        return Regex.Replace(value, "([a-z0-9])([A-Z])", "$1-$2")
            .Replace("Pv-P", "pvp", StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();
    }
}
