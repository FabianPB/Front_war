using War.Core.Combat;
using War.Core.Skills;
using War.Core.Skills.Catalogs;

namespace War.Core.PowerScore;

public sealed record BasicAttackPowerScalingComponent(
    SkillScalingType ScalingType,
    decimal SignalWeight,
    string? Note = null);

public sealed record BasicAttackPowerProfile(
    string Id,
    string Name,
    ClassType ClassType,
    SkillScalingType ScalingType,
    SkillDamageType? DamageType,
    bool UsesAttackSpeed = false,
    bool CanCrit = true,
    bool RequiresHitCheck = true,
    decimal UsageWeight = 1m,
    IReadOnlyList<BasicAttackPowerScalingComponent>? ScalingComponents = null,
    IReadOnlyList<CombatConditionType>? AppliedConditions = null,
    IReadOnlyList<string>? Notes = null);

public sealed record ClassPowerScoreProfile(
    ClassType ClassType,
    int ExpectedBasicSourceCount,
    int ExpectedStandardSkillSourceCount,
    int ExpectedUltimateSkillSourceCount,
    IReadOnlyList<BasicAttackPowerProfile>? BasicAttacks = null,
    IReadOnlyDictionary<string, decimal>? SkillSourceWeightOverrides = null,
    IReadOnlyList<string>? PendingData = null,
    IReadOnlyList<string>? Notes = null);

public static class PowerScoreClassProfileRegistry
{
    public static IReadOnlyList<ClassPowerScoreProfile> Current { get; } = Array.AsReadOnly(
    [
        CreateSorcererProfile(),
        CreateImplementedClassProfile(ClassType.Juramentada, JuramentadaSkillCatalog.AvatarDelJuramentoSkillId),
        CreateImplementedClassProfile(ClassType.Lancero, LanceroSkillCatalog.DragonDeMilLanzasSkillId),
        CreateImplementedClassProfile(ClassType.Bruiser, BruiserSkillCatalog.TitanDeGuerraSkillId)
    ]);

    public static ClassPowerScoreProfile GetRequired(ClassType classType)
    {
        return Current.FirstOrDefault(profile => profile.ClassType == classType)
            ?? throw new KeyNotFoundException($"No power-score class profile is registered for {classType}.");
    }

    private static ClassPowerScoreProfile CreateSorcererProfile()
    {
        return new ClassPowerScoreProfile(
            ClassType.Sorcerer,
            ExpectedBasicSourceCount: 1,
            ExpectedStandardSkillSourceCount: SkillCatalogRules.SkillsPerClass - 1,
            ExpectedUltimateSkillSourceCount: 1,
            BasicAttacks:
            [
                CreateBasicAttackProfile(ClassBasicAttackCatalog.Default.GetRequired(ClassType.Sorcerer))
            ],
            SkillSourceWeightOverrides: new Dictionary<string, decimal>
            {
                [SorcererSkillCatalog.TempestadDraconicaSkillId] = 1.40m
            },
            PendingData:
            [
                "Sorcerer numeric skill tuning is still provisional and will need a balance pass once authoritative cooldowns and costs are closed.",
                "Several Sorcerer skills carry metadata-only runtime notes for chain, persistent-area, or detonation behavior that the current combat translator does not execute spatially yet."
            ],
            Notes:
            [
                "Sorcerer offensive specialization now derives from the full class kit, with strong leverage on MagicAttack and elemental status application stats.",
                "Sorcerer basic attacks now feed the model from the real combo profile instead of the earlier placeholder."
            ]);
    }

    private static ClassPowerScoreProfile CreateImplementedClassProfile(ClassType classType, string ultimateSkillId)
    {
        return new ClassPowerScoreProfile(
            classType,
            ExpectedBasicSourceCount: 1,
            ExpectedStandardSkillSourceCount: SkillCatalogRules.SkillsPerClass - 1,
            ExpectedUltimateSkillSourceCount: 1,
            BasicAttacks:
            [
                CreateBasicAttackProfile(ClassBasicAttackCatalog.Default.GetRequired(classType))
            ],
            SkillSourceWeightOverrides: new Dictionary<string, decimal>
            {
                [ultimateSkillId] = 1.40m
            },
            PendingData:
            [
                $"{classType} numeric skill tuning is still provisional and will need a balance pass once authoritative cooldowns and costs are closed.",
                $"Several {classType} skills carry metadata-only runtime notes for chain, persistent-area, or detonation behavior that the current combat translator does not execute spatially yet."
            ],
            Notes:
            [
                $"{classType} offensive specialization now derives from the full class kit.",
                $"{classType} basic attacks now feed the model from the real combo profile."
            ]);
    }

    private static BasicAttackPowerProfile CreateBasicAttackProfile(BasicAttackDefinition definition)
    {
        var components = new List<BasicAttackPowerScalingComponent>();

        if (definition.BaseStageCoefficients.MagicAttackCoefficient > 0m)
        {
            components.Add(new BasicAttackPowerScalingComponent(
                SkillScalingType.MagicAttack,
                definition.BaseStageCoefficients.MagicAttackCoefficient,
                $"Basic attack '{definition.Name}' converts {definition.BaseStageCoefficients.MagicAttackCoefficient:P1} of MagicAttack on stage 1."));
        }

        if (definition.BaseStageCoefficients.PhysicalAttackCoefficient > 0m)
        {
            components.Add(new BasicAttackPowerScalingComponent(
                SkillScalingType.PhysicalAttack,
                definition.BaseStageCoefficients.PhysicalAttackCoefficient,
                $"Basic attack '{definition.Name}' converts {definition.BaseStageCoefficients.PhysicalAttackCoefficient:P1} of PhysicalAttack on stage 1."));
        }

        return new BasicAttackPowerProfile(
            definition.Id,
            definition.Name,
            definition.ClassType,
            ResolvePrimaryScalingType(definition),
            definition.DamageType switch
            {
                CombatDamageType.Physical => SkillDamageType.Physical,
                CombatDamageType.Magical => SkillDamageType.Magical,
                _ => null
            },
            UsesAttackSpeed: false,
            CanCrit: definition.CanCrit,
            RequiresHitCheck: definition.RequiresHitCheck,
            UsageWeight: 1m,
            ScalingComponents: Array.AsReadOnly(components.ToArray()),
            AppliedConditions: definition.PotentialEffects?.Select(effect => effect.Condition).Distinct().ToArray(),
            Notes: Array.AsReadOnly(new[]
            {
                $"The real combo profile uses {definition.Combo.StageCount} stages with a {definition.Combo.ContinuationWindowSeconds:0.##}s continuation window.",
                $"Basic casts for {definition.ClassType} currently take {definition.CastTimeSeconds:0.##}s and do not consume AttackSpeed in the combat runtime.",
                definition.Notes ?? string.Empty
            }.Where(note => !string.IsNullOrWhiteSpace(note)).ToArray()));
    }

    private static SkillScalingType ResolvePrimaryScalingType(BasicAttackDefinition definition)
    {
        return definition.BaseStageCoefficients.MagicAttackCoefficient >= definition.BaseStageCoefficients.PhysicalAttackCoefficient
            ? SkillScalingType.MagicAttack
            : SkillScalingType.PhysicalAttack;
    }
}
