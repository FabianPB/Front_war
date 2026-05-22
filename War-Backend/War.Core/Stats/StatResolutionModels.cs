using War.Core.Combat;

namespace War.Core.Stats;

public sealed record MitigationCurveOptions(decimal AsymptoticCapRatio = 0.90m, decimal Softness = 100m);

public sealed record MitigationResolution(
    StatType SourceStat,
    decimal RawValue,
    decimal MitigationRatio,
    decimal TheoreticalCapRatio,
    bool UsesProvisionalCurve);

public sealed record HitChanceCurveOptions(
    decimal BaseChanceWhenEqual = 0.70m,
    decimal MinimumChance = 0.05m,
    decimal MaximumChance = 0.95m,
    decimal SmoothingValue = 100m);

public sealed record HitChanceResolution(
    decimal Accuracy,
    decimal Evasion,
    decimal ChanceToHit,
    decimal BaseChanceWhenEqual,
    decimal MinimumChance,
    decimal MaximumChance,
    bool UsesProvisionalCurve);

public sealed record CriticalChanceResolution(
    decimal RawCritChance,
    decimal CriticalEvasion,
    decimal UnboundedEffectiveCritChance,
    decimal EffectiveCritChance,
    bool WasClampedToProbabilityBounds);

public sealed record CrowdControlDurationOptions(
    decimal BaseDurationMultiplier = 1m,
    decimal DurationReductionPerTenacity = 1m,
    decimal MinimumDurationMultiplier = 0m,
    decimal MaximumDurationMultiplier = 1m);

public sealed record CrowdControlDurationResolution(
    decimal Tenacity,
    decimal UnboundedDurationMultiplier,
    decimal DurationMultiplier,
    decimal MinimumDurationMultiplier,
    decimal MaximumDurationMultiplier,
    bool UsesProvisionalCurve,
    bool WasClampedToConfiguredBounds);

public sealed record DamageModifierProfile(
    DamageModifierContext Context,
    decimal IncreasePercentage,
    decimal ReductionPercentage);

public sealed record StatusChanceProfile(
    CombatConditionType Condition,
    decimal ApplyChance,
    decimal EvadeChance,
    bool RequiresHitBeforeApplication,
    bool ChecksEvadeAfterApplyRoll,
    bool DurationAffectedByTenacity);
