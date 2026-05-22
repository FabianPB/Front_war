using War.Core.Combat;

namespace War.Core.Stats;

public static class StatTypeExtensions
{
    public static StatDefinition GetDefinition(this StatType statType)
    {
        return StatCatalog.Get(statType);
    }

    public static StatCategory GetCategory(this StatType statType)
    {
        return statType.GetDefinition().Category;
    }

    public static string GetDescription(this StatType statType)
    {
        return statType.GetDefinition().Description;
    }

    public static StatValueKind GetValueKind(this StatType statType)
    {
        return statType.GetDefinition().ValueKind;
    }

    public static StatValueScale GetValueScale(this StatType statType)
    {
        return statType.GetDefinition().ValueScale;
    }

    public static StatMeasurementUnit GetMeasurementUnit(this StatType statType)
    {
        return statType.GetDefinition().MeasurementUnit;
    }

    public static StatBehaviorFamily GetBehaviorFamily(this StatType statType)
    {
        return statType.GetDefinition().BehaviorFamily;
    }

    public static StatUsageKind GetUsageKind(this StatType statType)
    {
        return statType.GetDefinition().UsageKind;
    }

    public static StatResolutionKind GetResolutionKind(this StatType statType)
    {
        return statType.GetDefinition().ResolutionKind;
    }

    public static SystemQueryStage GetQueryStages(this StatType statType)
    {
        return statType.GetDefinition().QueryStages;
    }

    public static StatInfluence GetInfluences(this StatType statType)
    {
        return statType.GetDefinition().Influences;
    }

    public static StatConstraint GetConstraints(this StatType statType)
    {
        return statType.GetDefinition().Constraints;
    }

    public static bool IsCalculatedStat(this StatType statType)
    {
        return statType.GetDefinition().IsCalculatedStat;
    }

    public static bool IsMaximum(this StatType statType)
    {
        return statType.GetDefinition().IsMaximum;
    }

    public static bool AllowsContributions(this StatType statType)
    {
        return statType.GetDefinition().AllowsContributions;
    }

    public static bool IsCombatStat(this StatType statType)
    {
        return statType.GetDefinition().IsCombatStat;
    }

    public static bool RequiresMathematicalResolution(this StatType statType)
    {
        return statType.GetDefinition().RequiresMathematicalResolution;
    }

    public static bool RequiresContextualLookup(this StatType statType)
    {
        return statType.GetDefinition().RequiresContextualLookup;
    }

    public static bool RequiresContextualEvaluation(this StatType statType)
    {
        return statType.GetDefinition().RequiresContextualEvaluation;
    }

    public static bool RequiresBothMathematicalAndContextualProcessing(this StatType statType)
    {
        return statType.GetDefinition().RequiresBothMathematicalAndContextualProcessing;
    }

    public static bool RequiresPreUseProcessing(this StatType statType)
    {
        return statType.GetDefinition().RequiresPreUseProcessing;
    }

    public static bool RequiresResolution(this StatType statType)
    {
        return statType.GetDefinition().RequiresResolution;
    }

    public static StatType? GetOpposedByStatType(this StatType statType)
    {
        return statType.GetDefinition().OpposedByStatType;
    }

    public static DamageModifierContext? GetDamageModifierContext(this StatType statType)
    {
        return statType.GetDefinition().DamageModifierContext;
    }

    public static CombatConditionType? GetRelatedCondition(this StatType statType)
    {
        return statType.GetDefinition().RelatedCondition;
    }

    public static string? GetFutureRuleNote(this StatType statType)
    {
        return statType.GetDefinition().FutureRuleNote;
    }
}
