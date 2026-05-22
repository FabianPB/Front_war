namespace War.Core.Stats;

[Flags]
public enum SystemQueryStage
{
    None = 0,
    BuildAggregation = 1 << 0,
    ResourceValidation = 1 << 1,
    ResourceConsumption = 1 << 2,
    ResourceRecovery = 1 << 3,
    DamageApplication = 1 << 4,
    HealingApplication = 1 << 5,
    AttackTiming = 1 << 6,
    HitCheck = 1 << 7,
    CriticalCheck = 1 << 8,
    DamageModification = 1 << 9,
    DamageMitigation = 1 << 10,
    StatusApplication = 1 << 11,
    CrowdControlDuration = 1 << 12,
    SpatialEvaluation = 1 << 13,
    ProgressionEvaluation = 1 << 14,
    Persistence = 1 << 15,
    SkillTiming = 1 << 16
}

public enum StatValueKind
{
    Flat,
    Percentage,
    Rate,
    Spatial,
    RuntimeResource
}

public enum StatValueScale
{
    RawDecimal,
    NormalizedFraction
}

public enum StatMeasurementUnit
{
    Points,
    Percent,
    Probability,
    PointsPerSecond,
    ResourcePointsPerSecond,
    AttacksPerSecond,
    UnitsPerSecond,
    GameUnits,
    ResourcePoints
}

public enum StatBehaviorFamily
{
    DirectValue,
    ResourceMaximum,
    ResourceRecovery,
    ConvertedMitigation,
    MitigationBypass,
    OpposedHitChance,
    OpposedCriticalChance,
    DamageModifier,
    HealingModifier,
    CrowdControlDuration,
    StatusApplyChance,
    StatusEvadeChance,
    ProgressionModifier,
    MobilityModifier,
    SkillCadenceModifier
}

public enum StatUsageKind
{
    Direct,
    ResolvedBeforeUse,
    Contextual
}

public enum StatResolutionKind
{
    None,
    AsymptoticMitigationCurve,
    OpposedHitChanceCurve,
    CriticalChanceSuppression,
    CrowdControlDurationReduction,
    DamageModifierLookup,
    StatusChanceLookup
}

[Flags]
public enum StatInfluence
{
    None = 0,
    DamageOutput = 1 << 0,
    DamageTaken = 1 << 1,
    HealingOutput = 1 << 2,
    HealingTaken = 1 << 3,
    HitChance = 1 << 4,
    CriticalOutcome = 1 << 5,
    ResourceMaximum = 1 << 6,
    ResourceRecovery = 1 << 7,
    CrowdControlDuration = 1 << 8,
    StatusApplication = 1 << 9,
    StatusResistance = 1 << 10,
    Movement = 1 << 11,
    ProgressionRewards = 1 << 12,
    SpatialReach = 1 << 13,
    CooldownFlow = 1 << 14,
    SkillCadence = 1 << 15,
    AttackCadence = 1 << 16
}

[Flags]
public enum StatConstraint
{
    None = 0,
    NonNegative = 1 << 0,
    NeverUseLinearly = 1 << 1,
    RequiresOpposedResolution = 1 << 2,
    EvaluateHitBeforeStatusChecks = 1 << 3,
    EvaluateApplyBeforeEvade = 1 << 4,
    UsedAsRuntimeMaximum = 1 << 5,
    ProvisionalBalanceFormula = 1 << 6
}

public enum DamageModifierContext
{
    BasicAttack,
    Skill,
    Critical,
    Monster,
    Boss,
    PvP,
    Heat,
    Cold,
    Electrified,
    Poison
}
