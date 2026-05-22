using War.Core.Stats;

namespace War.Core.Combat;

public interface ICombatConditionApplicationService
{
    IReadOnlyList<CombatConditionResolution> Resolve(CombatEventContext context, bool impactSucceeded);
}

public sealed class CombatConditionApplicationService : ICombatConditionApplicationService
{
    private readonly IStatusChanceResolver _statusChanceResolver;
    private readonly ICrowdControlDurationResolver _crowdControlDurationResolver;
    private readonly ICombatProbabilityService _probabilityService;

    public CombatConditionApplicationService(
        IStatusChanceResolver? statusChanceResolver = null,
        ICrowdControlDurationResolver? crowdControlDurationResolver = null,
        ICombatProbabilityService? probabilityService = null)
    {
        _statusChanceResolver = statusChanceResolver ?? new StatusChanceResolver();
        _crowdControlDurationResolver = crowdControlDurationResolver ?? new CrowdControlDurationResolver();
        _probabilityService = probabilityService ?? new RandomCombatProbabilityService();
    }

    public IReadOnlyList<CombatConditionResolution> Resolve(CombatEventContext context, bool impactSucceeded)
    {
        ArgumentNullException.ThrowIfNull(context);

        var results = new List<CombatConditionResolution>();
        var intents = context.PotentialEffects ?? Array.Empty<CombatConditionApplicationIntent>();
        var targetProtections = context.TargetActiveProtections ?? Array.Empty<CombatProtectionState>();
        var targetActiveConditions = context.TargetActiveConditions ?? Array.Empty<CombatConditionType>();

        foreach (var intent in intents)
        {
            var definition = CombatConditionCatalog.Get(intent.Condition);

            if (CombatProtectionRules.BlocksCondition(definition, targetProtections))
            {
                results.Add(new CombatConditionResolution(
                    intent.Condition,
                    definition.Category,
                    CombatConditionApplicationStatus.BlockedByProtection,
                    CombatConditionApplicationSource.DirectEffect,
                    BaseDurationSeconds: intent.BaseDurationSeconds,
                    RequiredTargetConditions: intent.RequiredTargetConditions,
                    EffectKey: intent.EffectKey,
                    Note: CombineNotes(intent.Note, CombatProtectionRules.DescribeBlockingProtections(targetProtections))));
                continue;
            }

            if (!impactSucceeded && definition.RequiresHitBeforeApplication)
            {
                results.Add(new CombatConditionResolution(
                    intent.Condition,
                    definition.Category,
                    CombatConditionApplicationStatus.SkippedBecauseImpactFailed,
                    CombatConditionApplicationSource.DirectEffect,
                    BaseDurationSeconds: intent.BaseDurationSeconds,
                    RequiredTargetConditions: intent.RequiredTargetConditions,
                    EffectKey: intent.EffectKey,
                    Note: "Effect resolution was skipped because the action did not connect."));
                continue;
            }

            if (!HasRequiredTargetConditions(intent.RequiredTargetConditions, targetActiveConditions))
            {
                results.Add(new CombatConditionResolution(
                    intent.Condition,
                    definition.Category,
                    CombatConditionApplicationStatus.SkippedByConditionRequirement,
                    CombatConditionApplicationSource.DirectEffect,
                    BaseDurationSeconds: intent.BaseDurationSeconds,
                    RequiredTargetConditions: intent.RequiredTargetConditions,
                    EffectKey: intent.EffectKey,
                    Note: BuildMissingConditionNote(intent)));
                continue;
            }

            var actorProfile = _statusChanceResolver.Resolve(context.Actor.Stats, intent.Condition);
            var requestedApplyChance = ResolveRequestedApplyChance(actorProfile.ApplyChance, intent.ChanceProfile);
            var applyCheck = _probabilityService.Evaluate(
                requestedApplyChance,
                $"Apply check for {intent.Condition}.");

            if (!applyCheck.Succeeded)
            {
                results.Add(new CombatConditionResolution(
                    intent.Condition,
                    definition.Category,
                    CombatConditionApplicationStatus.FailedApplyChance,
                    CombatConditionApplicationSource.DirectEffect,
                    ApplyCheck: applyCheck,
                    BaseDurationSeconds: intent.BaseDurationSeconds,
                    RequiredTargetConditions: intent.RequiredTargetConditions,
                    EffectKey: intent.EffectKey,
                    Note: intent.Note));
                continue;
            }

            CombatProbabilityCheckResult? evadeCheck = null;

            if (definition.ChecksEvadeAfterApplyRoll)
            {
                var targetProfile = _statusChanceResolver.Resolve(context.Target.Stats, intent.Condition);
                evadeCheck = _probabilityService.Evaluate(
                    targetProfile.EvadeChance,
                    $"Evade check for {intent.Condition}.");
            }

            if (evadeCheck?.Succeeded == true)
            {
                results.Add(new CombatConditionResolution(
                    intent.Condition,
                    definition.Category,
                    CombatConditionApplicationStatus.Evaded,
                    CombatConditionApplicationSource.DirectEffect,
                    ApplyCheck: applyCheck,
                    EvadeCheck: evadeCheck,
                    BaseDurationSeconds: intent.BaseDurationSeconds,
                    RequiredTargetConditions: intent.RequiredTargetConditions,
                    EffectKey: intent.EffectKey,
                    Note: intent.Note));
                continue;
            }

            results.Add(CreateAppliedResolution(intent, definition, context.Target.Stats.Get(StatType.Tenacity), applyCheck, evadeCheck));
        }

        return Array.AsReadOnly(results.ToArray());
    }

    private CombatConditionResolution CreateAppliedResolution(
        CombatConditionApplicationIntent intent,
        CombatConditionDefinition definition,
        decimal targetTenacity,
        CombatProbabilityCheckResult applyCheck,
        CombatProbabilityCheckResult? evadeCheck)
    {
        if (!definition.DurationAffectedByTenacity)
        {
            return new CombatConditionResolution(
                intent.Condition,
                definition.Category,
                CombatConditionApplicationStatus.Applied,
                CombatConditionApplicationSource.DirectEffect,
                ApplyCheck: applyCheck,
                EvadeCheck: evadeCheck,
                BaseDurationSeconds: intent.BaseDurationSeconds,
                EffectiveDurationSeconds: intent.BaseDurationSeconds,
                RequiredTargetConditions: intent.RequiredTargetConditions,
                EffectKey: intent.EffectKey,
                Note: intent.Note);
        }

        if (!intent.BaseDurationSeconds.HasValue)
        {
            return new CombatConditionResolution(
                intent.Condition,
                definition.Category,
                CombatConditionApplicationStatus.Applied,
                CombatConditionApplicationSource.DirectEffect,
                ApplyCheck: applyCheck,
                EvadeCheck: evadeCheck,
                RequiredTargetConditions: intent.RequiredTargetConditions,
                EffectKey: intent.EffectKey,
                Note: CombineNotes(
                    intent.Note,
                    "Crowd-control applied without a base duration. Effective duration remains undefined until the effect data is supplied."));
        }

        var durationResolution = _crowdControlDurationResolver.Resolve(targetTenacity);
        var effectiveDurationSeconds = intent.BaseDurationSeconds.Value * durationResolution.DurationMultiplier;

        return new CombatConditionResolution(
            intent.Condition,
            definition.Category,
            CombatConditionApplicationStatus.Applied,
            CombatConditionApplicationSource.DirectEffect,
            ApplyCheck: applyCheck,
            EvadeCheck: evadeCheck,
            BaseDurationSeconds: intent.BaseDurationSeconds,
            EffectiveDurationSeconds: effectiveDurationSeconds,
            DurationResolution: durationResolution,
            RequiredTargetConditions: intent.RequiredTargetConditions,
            EffectKey: intent.EffectKey,
            Note: durationResolution.UsesProvisionalCurve
                ? CombineNotes(intent.Note, "Effective CC duration was resolved through the current provisional tenacity policy.")
                : intent.Note);
    }

    private static bool HasRequiredTargetConditions(
        IReadOnlyList<CombatConditionType>? requiredTargetConditions,
        IReadOnlyCollection<CombatConditionType> activeTargetConditions)
    {
        if ((requiredTargetConditions?.Count ?? 0) == 0)
        {
            return true;
        }

        return requiredTargetConditions!.All(activeTargetConditions.Contains);
    }

    private static string BuildMissingConditionNote(CombatConditionApplicationIntent intent)
    {
        var requirement = string.Join(
            ", ",
            (intent.RequiredTargetConditions ?? Array.Empty<CombatConditionType>())
                .Select(condition => condition.ToString()));

        return CombineNotes(
            intent.Note,
            $"Effect resolution requires the target to already have: {requirement}.")
            ?? $"Effect resolution requires the target to already have: {requirement}.";
    }

    private static decimal ResolveRequestedApplyChance(
        decimal actorApplyChance,
        CombatConditionApplicationChanceProfile? chanceProfile)
    {
        if (chanceProfile is null)
        {
            return actorApplyChance;
        }

        if (chanceProfile.ApplyChanceMultiplier < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chanceProfile),
                chanceProfile.ApplyChanceMultiplier,
                "Condition apply chance multiplier cannot be negative.");
        }

        var requestedChance = actorApplyChance + chanceProfile.FlatApplyChanceBonus;

        if (chanceProfile.BaseApplyChance.HasValue)
        {
            requestedChance += chanceProfile.BaseApplyChance.Value;
        }

        return requestedChance * chanceProfile.ApplyChanceMultiplier;
    }

    private static string? CombineNotes(string? primary, string secondary)
    {
        if (string.IsNullOrWhiteSpace(primary))
        {
            return secondary;
        }

        return $"{primary} {secondary}";
    }
}
