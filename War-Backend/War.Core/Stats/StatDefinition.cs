using War.Core.Combat;

namespace War.Core.Stats;

public sealed record StatDefinition(
    StatType Type,
    string Description,
    StatCategory Category,
    StatValueKind ValueKind,
    StatValueScale ValueScale,
    StatMeasurementUnit MeasurementUnit,
    StatBehaviorFamily BehaviorFamily,
    StatUsageKind UsageKind,
    StatResolutionKind ResolutionKind,
    SystemQueryStage QueryStages,
    StatInfluence Influences,
    StatConstraint Constraints,
    bool IsMaximum,
    bool AllowsContributions,
    bool IsCombatStat,
    StatType? OpposedByStatType = null,
    DamageModifierContext? DamageModifierContext = null,
    CombatConditionType? RelatedCondition = null,
    string? FutureRuleNote = null)
{
    public bool IsCalculatedStat => true;

    public bool RequiresMathematicalResolution =>
        ResolutionKind is StatResolutionKind.AsymptoticMitigationCurve
            or StatResolutionKind.OpposedHitChanceCurve
            or StatResolutionKind.CriticalChanceSuppression
            or StatResolutionKind.CrowdControlDurationReduction;

    public bool RequiresContextualLookup =>
        ResolutionKind is StatResolutionKind.DamageModifierLookup
            or StatResolutionKind.StatusChanceLookup;

    public bool RequiresContextualEvaluation =>
        UsageKind == StatUsageKind.Contextual || RequiresContextualLookup;

    public bool RequiresBothMathematicalAndContextualProcessing =>
        RequiresMathematicalResolution && RequiresContextualEvaluation;

    public bool RequiresPreUseProcessing =>
        RequiresMathematicalResolution || RequiresContextualEvaluation;

    public bool RequiresResolution => RequiresPreUseProcessing;
}
