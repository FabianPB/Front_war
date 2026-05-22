using War.Core.Combat;
using War.Core.Stats;

namespace War.Core.PowerScore;

public interface IPowerScoreCalculator
{
    PowerScoreResult Calculate(PowerScoreCalculationContext context);
}

public sealed class PowerScoreCalculator : IPowerScoreCalculator
{
    private readonly IPowerScorePolicyCatalog _policyCatalog;
    private readonly IPowerScoreClassUsageAnalyzer _usageAnalyzer;
    private readonly IMitigationResolver _mitigationResolver;
    private readonly IHitChanceResolver _hitChanceResolver;
    private readonly ICriticalChanceResolver _criticalChanceResolver;
    private readonly ICrowdControlDurationResolver _crowdControlDurationResolver;

    public PowerScoreCalculator(
        IPowerScorePolicyCatalog? policyCatalog = null,
        IPowerScoreClassUsageAnalyzer? usageAnalyzer = null,
        IMitigationResolver? mitigationResolver = null,
        IHitChanceResolver? hitChanceResolver = null,
        ICriticalChanceResolver? criticalChanceResolver = null,
        ICrowdControlDurationResolver? crowdControlDurationResolver = null)
    {
        _policyCatalog = policyCatalog ?? PowerScorePolicyCatalog.Default;
        _usageAnalyzer = usageAnalyzer ?? new PowerScoreClassUsageAnalyzer(_policyCatalog);
        _mitigationResolver = mitigationResolver ?? new MitigationResolver();
        _hitChanceResolver = hitChanceResolver ?? new HitChanceResolver();
        _criticalChanceResolver = criticalChanceResolver ?? new CriticalChanceResolver();
        _crowdControlDurationResolver = crowdControlDurationResolver ?? new CrowdControlDurationResolver();
    }

    public PowerScoreResult Calculate(PowerScoreCalculationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Character);

        var character = context.Character;
        var usageAnalysis = _usageAnalyzer.Analyze(context);
        var notes = new List<string>();
        notes.AddRange(usageAnalysis.Notes ?? Array.Empty<string>());

        var statBreakdowns = new List<PowerScoreStatContributionBreakdown>();

        foreach (var policy in _policyCatalog.GetAll().OrderBy(policy => policy.Category).ThenBy(policy => policy.StatType))
        {
            if (policy.IsExcluded)
            {
                continue;
            }

            var actualValue = character.Stats.Get(policy.StatType);
            var (effectiveQuantity, transformNote) = ResolveEffectiveQuantity(policy, actualValue);
            var rawContribution = effectiveQuantity * policy.UnitValue;
            var classWeight = ResolveClassWeight(policy, usageAnalysis);
            var finalContribution = rawContribution * classWeight * policy.CategoryAdjustment;
            var breakdownNotes = new List<string>
            {
                policy.Reason
            };

            if (!string.IsNullOrWhiteSpace(policy.PendingReason))
            {
                breakdownNotes.Add($"Pending valuation note: {policy.PendingReason}");
            }

            if (!string.IsNullOrWhiteSpace(transformNote))
            {
                breakdownNotes.Add(transformNote);
            }

            if (policy.UsesClassContextWeight && usageAnalysis.StatFactors.TryGetValue(policy.StatType, out var factor))
            {
                breakdownNotes.AddRange(factor.Notes ?? Array.Empty<string>());
            }

            statBreakdowns.Add(new PowerScoreStatContributionBreakdown(
                policy.StatType,
                actualValue,
                effectiveQuantity,
                policy.UnitValue,
                rawContribution,
                classWeight,
                policy.CategoryAdjustment,
                finalContribution,
                policy.Category,
                policy.Inclusion,
                policy.ValueTransform,
                Array.AsReadOnly(character.Stats.GetContributions(policy.StatType).ToArray()),
                Array.AsReadOnly(breakdownNotes.ToArray())));

            if (actualValue > 0m && !string.IsNullOrWhiteSpace(policy.PendingReason))
            {
                notes.Add($"Pending policy data affects {policy.StatType}: {policy.PendingReason}");
            }
        }

        var totalScore = statBreakdowns.Sum(breakdown => breakdown.FinalContribution);
        var categoryBreakdowns = statBreakdowns
            .GroupBy(breakdown => breakdown.Category)
            .Select(group => new PowerScoreCategoryContributionBreakdown(
                group.Key,
                group.Sum(item => item.FinalContribution),
                totalScore <= 0m ? 0m : group.Sum(item => item.FinalContribution) / totalScore,
                group.Count()))
            .OrderByDescending(group => group.Contribution)
            .ToArray();

        var orderedStatBreakdowns = statBreakdowns
            .OrderByDescending(breakdown => breakdown.FinalContribution)
            .ThenBy(breakdown => breakdown.StatType)
            .ToArray();

        return new PowerScoreResult(
            totalScore,
            character.ClassType,
            Array.AsReadOnly(orderedStatBreakdowns),
            Array.AsReadOnly(categoryBreakdowns),
            _policyCatalog.Audit(),
            usageAnalysis,
            _policyCatalog.Tuning,
            Array.AsReadOnly(notes.Distinct(StringComparer.Ordinal).ToArray()));
    }

    private decimal ResolveClassWeight(PowerScoreStatPolicy policy, PowerScoreClassUsageAnalysis usageAnalysis)
    {
        if (!policy.UsesClassContextWeight)
        {
            return 1m;
        }

        return usageAnalysis.StatFactors.TryGetValue(policy.StatType, out var factor)
            ? factor.FinalWeight
            : 1m;
    }

    private (decimal EffectiveQuantity, string? Note) ResolveEffectiveQuantity(PowerScoreStatPolicy policy, decimal actualValue)
    {
        var sanitizedValue = actualValue < 0m ? 0m : actualValue;

        switch (policy.ValueTransform)
        {
            case PowerScoreValueTransform.DirectFraction:
                return (sanitizedValue, "The stat uses its direct percentage or normalized value for referential scoring.");

            case PowerScoreValueTransform.RatioToReference:
                return (policy.ReferenceValue <= 0m ? sanitizedValue : sanitizedValue / policy.ReferenceValue,
                    $"The stat was normalized against a reference value of {policy.ReferenceValue}.");

            case PowerScoreValueTransform.MitigationRatio:
                var mitigation = _mitigationResolver.Resolve(policy.StatType, sanitizedValue);
                return (mitigation.MitigationRatio,
                    mitigation.UsesProvisionalCurve
                        ? "The stat was converted through the current mitigation curve for referential scoring."
                        : "The stat was converted through the mitigation curve.");

            case PowerScoreValueTransform.EffectiveHitChanceAgainstReference:
                var hitChance = _hitChanceResolver.Resolve(sanitizedValue, _policyCatalog.Tuning.ReferenceEvasion);
                return (hitChance.ChanceToHit,
                    $"The stat was evaluated against reference evasion {_policyCatalog.Tuning.ReferenceEvasion} using the current hit-chance curve.");

            case PowerScoreValueTransform.HitAvoidanceAgainstReference:
                var avoidance = _hitChanceResolver.Resolve(_policyCatalog.Tuning.ReferenceAccuracy, sanitizedValue);
                return (1m - avoidance.ChanceToHit,
                    $"The stat was evaluated as hit avoidance against reference accuracy {_policyCatalog.Tuning.ReferenceAccuracy}.");

            case PowerScoreValueTransform.EffectiveCritChanceAgainstReference:
                var critChance = _criticalChanceResolver.Resolve(sanitizedValue, _policyCatalog.Tuning.ReferenceCriticalEvasion);
                return (critChance.EffectiveCritChance,
                    $"The stat was evaluated against reference critical evasion {_policyCatalog.Tuning.ReferenceCriticalEvasion}.");

            case PowerScoreValueTransform.CrowdControlResistanceRatio:
                var ccDuration = _crowdControlDurationResolver.Resolve(sanitizedValue);
                return (1m - ccDuration.DurationMultiplier,
                    ccDuration.UsesProvisionalCurve
                        ? "The stat was converted into prevented CC-duration share through the current tenacity policy."
                        : "The stat was converted into prevented CC-duration share.");

            case PowerScoreValueTransform.EffectiveStatusApplyChanceAgainstReference:
                var effectiveApplyChance = ClampProbability(sanitizedValue) * (1m - ClampProbability(_policyCatalog.Tuning.ReferenceStatusEvadeChance));
                return (effectiveApplyChance,
                    $"The stat was evaluated as effective apply chance against reference evade chance {_policyCatalog.Tuning.ReferenceStatusEvadeChance}.");

            case PowerScoreValueTransform.EffectiveStatusResistanceAgainstReference:
                var effectiveResistChance = ClampProbability(_policyCatalog.Tuning.ReferenceStatusApplyChance) * ClampProbability(sanitizedValue);
                return (effectiveResistChance,
                    $"The stat was evaluated as prevented status application against reference apply chance {_policyCatalog.Tuning.ReferenceStatusApplyChance}.");

            case PowerScoreValueTransform.RecoveryAccelerationFromRate:
                var acceleration = sanitizedValue <= 0m
                    ? 0m
                    : 1m - (1m / (1m + sanitizedValue));
                return (acceleration,
                    "The stat was converted into effective recovery-time saved share using 1 - (1 / (1 + rate)).");

            default:
                throw new ArgumentOutOfRangeException(nameof(policy), policy.ValueTransform, "Unknown power-score value transform.");
        }
    }

    private static decimal ClampProbability(decimal value)
    {
        return Math.Clamp(value, 0m, 1m);
    }
}
