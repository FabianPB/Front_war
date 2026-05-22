using War.Core.Entities;
using War.Core.Skills;
using War.Core.Stats;

namespace War.Core.PowerScore;

public enum PowerScoreCategory
{
    Offensive,
    Defensive,
    Recovery,
    Utility,
    Status,
    Progression
}

public enum PowerScoreStatInclusion
{
    Direct,
    IndirectLowWeight,
    Excluded
}

public enum PowerScoreValueTransform
{
    DirectFraction,
    RatioToReference,
    MitigationRatio,
    EffectiveHitChanceAgainstReference,
    HitAvoidanceAgainstReference,
    EffectiveCritChanceAgainstReference,
    CrowdControlResistanceRatio,
    EffectiveStatusApplyChanceAgainstReference,
    EffectiveStatusResistanceAgainstReference,
    RecoveryAccelerationFromRate
}

public enum PowerScoreSourceKind
{
    BasicAttack,
    Skill,
    SkillTriggeredAction
}

public sealed record PowerScoreGlobalTuning(
    decimal BasicAttackSourceWeight = 1.00m,
    decimal StandardSkillSourceWeight = 1.00m,
    decimal UltimateSkillSourceWeight = 0.85m,
    decimal TriggeredActionWeightScale = 0.50m,
    decimal MinimumCoverageBlend = 0.25m,
    decimal ReferenceAccuracy = 100m,
    decimal ReferenceEvasion = 100m,
    decimal ReferenceCriticalEvasion = 0.10m,
    decimal ReferenceStatusApplyChance = 0.20m,
    decimal ReferenceStatusEvadeChance = 0.10m);

public sealed record PowerScoreStatPolicy(
    StatType StatType,
    PowerScoreCategory Category,
    PowerScoreStatInclusion Inclusion,
    decimal UnitValue,
    PowerScoreValueTransform ValueTransform,
    decimal ReferenceValue = 1m,
    bool UsesClassContextWeight = false,
    decimal MinimumClassWeight = 1m,
    decimal MaximumClassWeight = 1m,
    decimal ReferenceSignal = 1m,
    decimal CategoryAdjustment = 1m,
    string Reason = "",
    string? PendingReason = null)
{
    public bool IsExcluded => Inclusion == PowerScoreStatInclusion.Excluded;
}

public sealed record PowerScoreStatAuditEntry(
    StatType StatType,
    PowerScoreCategory Category,
    PowerScoreStatInclusion Inclusion,
    bool UsesClassContextWeight,
    string Reason,
    string? PendingReason = null);

public sealed record PowerScoreCalculationContext(
    Character Character,
    CharacterSkillProgressCollection? SkillProgress = null,
    SkillCatalog? SkillCatalog = null,
    ClassPowerScoreProfile? ClassProfile = null);

public sealed record PowerScoreSourceSignal(
    StatType StatType,
    decimal Signal,
    string Reason);

public sealed record PowerScoreUsageSourceBreakdown(
    string SourceKey,
    PowerScoreSourceKind SourceKind,
    string DisplayName,
    decimal BaseWeight,
    decimal EffectiveWeight,
    bool IsModeled,
    bool IsAvailable,
    int? ResolvedAscensionLevel = null,
    IReadOnlyList<PowerScoreSourceSignal>? Signals = null,
    IReadOnlyList<string>? Notes = null);

public sealed record PowerScoreClassFactorBreakdown(
    StatType StatType,
    decimal SignalTotal,
    decimal ReferenceSignal,
    decimal SignalRatio,
    decimal CoverageBlend,
    decimal MinimumWeight,
    decimal MaximumWeight,
    decimal FinalWeight,
    IReadOnlyList<string>? Notes = null);

public sealed record PowerScoreClassUsageAnalysis(
    ClassType ClassType,
    int ExpectedBasicSourceCount,
    int ExpectedStandardSkillSourceCount,
    int ExpectedUltimateSkillSourceCount,
    int ModeledBasicSourceCount,
    int ModeledSkillSourceCount,
    int AvailableBasicSourceCount,
    int AvailableSkillSourceCount,
    decimal ModeledSourceWeight,
    decimal ExpectedSourceWeight,
    decimal CoverageRatio,
    decimal CoverageBlend,
    IReadOnlyList<PowerScoreUsageSourceBreakdown> Sources,
    IReadOnlyDictionary<StatType, PowerScoreClassFactorBreakdown> StatFactors,
    IReadOnlyList<string>? Notes = null);

public sealed record PowerScoreStatContributionBreakdown(
    StatType StatType,
    decimal ActualStatValue,
    decimal EffectiveQuantity,
    decimal UnitValue,
    decimal RawContribution,
    decimal ClassWeight,
    decimal CategoryAdjustment,
    decimal FinalContribution,
    PowerScoreCategory Category,
    PowerScoreStatInclusion Inclusion,
    PowerScoreValueTransform ValueTransform,
    IReadOnlyList<StatContribution>? FinalStatSources = null,
    IReadOnlyList<string>? Notes = null);

public sealed record PowerScoreCategoryContributionBreakdown(
    PowerScoreCategory Category,
    decimal Contribution,
    decimal ShareOfTotal,
    int IncludedStatCount);

public sealed record PowerScoreResult(
    decimal TotalScore,
    ClassType ClassType,
    IReadOnlyList<PowerScoreStatContributionBreakdown> StatContributions,
    IReadOnlyList<PowerScoreCategoryContributionBreakdown> CategoryContributions,
    IReadOnlyList<PowerScoreStatAuditEntry> StatAudit,
    PowerScoreClassUsageAnalysis UsageAnalysis,
    PowerScoreGlobalTuning Tuning,
    IReadOnlyList<string>? Notes = null);
