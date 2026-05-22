using System.Collections.ObjectModel;
using War.Core.Skills;
using War.Core.Stats;

namespace War.Core.Progression;

public interface IClassLevelGrowthCatalog
{
    ClassLevelGrowthProfile GetRequired(ClassType classType);

    IReadOnlyCollection<ClassLevelGrowthProfile> GetAll();
}

public sealed record ClassLevelGrowthProfile(
    ClassType ClassType,
    IReadOnlyDictionary<StatType, decimal> PerLevelStatGains,
    IReadOnlyList<string>? Notes = null)
{
    public decimal GetPerLevelGain(StatType statType)
    {
        return PerLevelStatGains.TryGetValue(statType, out var value) ? value : 0m;
    }
}

public sealed class ClassLevelGrowthCatalog : IClassLevelGrowthCatalog
{
    private readonly IReadOnlyDictionary<ClassType, ClassLevelGrowthProfile> _profiles;

    public ClassLevelGrowthCatalog()
    {
        _profiles = new ReadOnlyDictionary<ClassType, ClassLevelGrowthProfile>(
            CreateProfiles().ToDictionary(profile => profile.ClassType));
    }

    public static ClassLevelGrowthCatalog Default { get; } = new();

    public ClassLevelGrowthProfile GetRequired(ClassType classType)
    {
        return _profiles.TryGetValue(classType, out var profile)
            ? profile
            : throw new KeyNotFoundException($"No level-growth profile is registered for class {classType}.");
    }

    public IReadOnlyCollection<ClassLevelGrowthProfile> GetAll()
    {
        return _profiles.Values.ToArray();
    }

    private static IReadOnlyList<ClassLevelGrowthProfile> CreateProfiles()
    {
        return Array.AsReadOnly(
        [
            CreateProfile(
                ClassType.Sorcerer,
                (StatType.MaxHp, 42m),
                (StatType.MaxMana, 30m),
                (StatType.PhysicalAttack, 4m),
                (StatType.MagicAttack, 14m),
                (StatType.Defense, 2m),
                (StatType.MagicResistance, 3m),
                (StatType.Accuracy, 5m),
                (StatType.Evasion, 6m),
                (StatType.CritChance, 0.06m),
                (StatType.CriticalEvasion, 0.05m),
                (StatType.CritDamage, 5m),
                (StatType.HpRegen, 0.8m),
                (StatType.ManaRegen, 1.5m),
                (StatType.DefensePenetration, 0.005m),
                (StatType.MagicPenetration, 0.015m),
                (StatType.Tenacity, 0.003m),
                (StatType.HealingEffectiveness, 0.015m),
                (StatType.HealingReceived, 0.015m),
                (StatType.MoveSpeed, 0.05m),
                (StatType.AttackRange, 0m)),

            CreateProfile(
                ClassType.Juramentada,
                (StatType.MaxHp, 52m),
                (StatType.MaxMana, 26m),
                (StatType.PhysicalAttack, 8m),
                (StatType.MagicAttack, 10m),
                (StatType.Defense, 3m),
                (StatType.MagicResistance, 3m),
                (StatType.Accuracy, 5m),
                (StatType.Evasion, 5m),
                (StatType.CritChance, 0.05m),
                (StatType.CriticalEvasion, 0.05m),
                (StatType.CritDamage, 4m),
                (StatType.HpRegen, 1.0m),
                (StatType.ManaRegen, 1.2m),
                (StatType.DefensePenetration, 0.008m),
                (StatType.MagicPenetration, 0.010m),
                (StatType.Tenacity, 0.003m),
                (StatType.HealingEffectiveness, 0.025m),
                (StatType.HealingReceived, 0.020m),
                (StatType.MoveSpeed, 0.05m),
                (StatType.AttackRange, 0m)),

            CreateProfile(
                ClassType.Lancero,
                (StatType.MaxHp, 56m),
                (StatType.MaxMana, 20m),
                (StatType.PhysicalAttack, 13m),
                (StatType.MagicAttack, 5m),
                (StatType.Defense, 3m),
                (StatType.MagicResistance, 2m),
                (StatType.Accuracy, 6m),
                (StatType.Evasion, 5m),
                (StatType.CritChance, 0.05m),
                (StatType.CriticalEvasion, 0.04m),
                (StatType.CritDamage, 4m),
                (StatType.HpRegen, 1.0m),
                (StatType.ManaRegen, 0.9m),
                (StatType.DefensePenetration, 0.012m),
                (StatType.MagicPenetration, 0.005m),
                (StatType.Tenacity, 0.003m),
                (StatType.HealingEffectiveness, 0.010m),
                (StatType.HealingReceived, 0.015m),
                (StatType.MoveSpeed, 0.05m),
                (StatType.AttackRange, 0m)),

            CreateProfile(
                ClassType.Bruiser,
                (StatType.MaxHp, 68m),
                (StatType.MaxMana, 16m),
                (StatType.PhysicalAttack, 11m),
                (StatType.MagicAttack, 4m),
                (StatType.Defense, 5m),
                (StatType.MagicResistance, 4m),
                (StatType.Accuracy, 4m),
                (StatType.Evasion, 4m),
                (StatType.CritChance, 0.04m),
                (StatType.CriticalEvasion, 0.05m),
                (StatType.CritDamage, 3m),
                (StatType.HpRegen, 1.2m),
                (StatType.ManaRegen, 0.8m),
                (StatType.DefensePenetration, 0.008m),
                (StatType.MagicPenetration, 0.003m),
                (StatType.Tenacity, 0.003m),
                (StatType.HealingEffectiveness, 0.010m),
                (StatType.HealingReceived, 0.020m),
                (StatType.MoveSpeed, 0.05m),
                (StatType.AttackRange, 0m))
        ]);
    }

    private static ClassLevelGrowthProfile CreateProfile(ClassType classType, params (StatType StatType, decimal GainPerLevel)[] gains)
    {
        var normalizedGains = gains.ToDictionary(entry => entry.StatType, entry => entry.GainPerLevel);

        return new ClassLevelGrowthProfile(
            classType,
            new ReadOnlyDictionary<StatType, decimal>(normalizedGains),
            Notes:
            [
                "Linear level growth starts applying from level 2 onward, so level 1 contributes zero growth stacks.",
                "Percentage-like stats are stored as normalized fractions in FinalStats (for example 6% => 0.06, 1.5% => 0.015, and 0.3% => 0.003).",
                "AttackRange is explicitly configured at +0 per level."
            ]);
    }
}
