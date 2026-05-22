using War.Core.Combat;
using War.Core.Entities;
using War.Core.Stats;

namespace War.Core.Skills;

public sealed record SkillCadenceSnapshot(
    decimal BaseCooldownSeconds,
    decimal EffectiveCooldownSeconds,
    decimal CooldownReductionPercentage,
    decimal SkillRecoveryRate,
    bool UsedCooldownReduction,
    bool UsedSkillRecoveryRate,
    string? Note = null);

public sealed record SkillCombatTranslationContext(
    Character Actor,
    Character Target,
    CharacterSkillProgress? SkillProgress = null,
    CombatTargetClassification TargetClassification = CombatTargetClassification.None,
    IReadOnlyCollection<CombatConditionType>? TargetActiveConditions = null,
    IReadOnlyCollection<CombatProtectionState>? TargetActiveProtections = null);

public sealed record SkillScheduledCombatEventPlan(
    string EventKey,
    SkillExecutionTriggerPhase TriggerPhase,
    CombatEventContext EventContext,
    int RepeatCount = 1,
    decimal? TotalDurationSeconds = null,
    decimal? IntervalSeconds = null,
    bool EffectsResolvePerExecution = true,
    string? Note = null);

public sealed record SkillCombatActionPlan(
    SkillAvailabilityResult Availability,
    ResolvedSkillDefinition? Skill,
    CombatEventContext? CombatEventContext,
    SkillCadenceSnapshot? Cadence,
    SkillTargetingProfile? Targeting,
    IReadOnlyList<CombatResourceCost>? CastResourceCosts = null,
    IReadOnlyList<SkillScheduledCombatEventPlan>? ScheduledEvents = null,
    IReadOnlyList<CombatProtectionGrantIntent>? CastProtectionGrants = null,
    IReadOnlyList<SkillPendingDatum>? PendingData = null,
    IReadOnlyList<string>? SecurityNotes = null,
    IReadOnlyList<string>? Notes = null)
{
    public bool CanExecute => Availability.IsAvailable && CombatEventContext is not null;

    public bool HasBlockingPendingData =>
        (PendingData ?? Array.Empty<SkillPendingDatum>()).Any(item => item.BlocksExactCombatSimulation);
}

public interface ISkillAscensionResolver
{
    ResolvedSkillDefinition Resolve(SkillDefinition definition, int ascensionLevel);
}

public sealed class SkillAscensionResolver : ISkillAscensionResolver
{
    private static readonly IReadOnlyDictionary<int, SkillAscensionOverrides> EmptyOverrides =
        new Dictionary<int, SkillAscensionOverrides>();

    public ResolvedSkillDefinition Resolve(SkillDefinition definition, int ascensionLevel)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (ascensionLevel < SkillCatalogRules.MinimumAscensionLevel ||
            ascensionLevel > SkillCatalogRules.MaximumAscensionLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ascensionLevel),
                ascensionLevel,
                $"Skill ascension level must be between {SkillCatalogRules.MinimumAscensionLevel} and {SkillCatalogRules.MaximumAscensionLevel}.");
        }

        var tuning = NormalizeTuning(definition.BaseTuning);
        var notes = new List<string>();
        var overrides = definition.AscensionOverrides ?? EmptyOverrides;

        foreach (var level in Enumerable.Range(SkillCatalogRules.MinimumAscensionLevel + 1, ascensionLevel - SkillCatalogRules.MinimumAscensionLevel))
        {
            if (!overrides.TryGetValue(level, out var ascensionOverride))
            {
                continue;
            }

            tuning = ApplyOverrides(tuning, ascensionOverride, notes);

            if (!string.IsNullOrWhiteSpace(ascensionOverride.Note))
            {
                notes.Add($"Ascension {level}: {ascensionOverride.Note}");
            }
        }

        return new ResolvedSkillDefinition(
            definition.Id,
            definition.Name,
            definition.Description,
            definition.ClassType,
            definition.Slot,
            definition.IsUltimate,
            definition.UnlockLevel,
            ascensionLevel,
            tuning,
            definition.Elements,
            definition.Roles,
            definition.Notes,
            definition.Metadata,
            Array.AsReadOnly(notes.ToArray()),
            definition.PendingData,
            definition.SecurityNotes);
    }

    private static SkillTuningSnapshot NormalizeTuning(SkillTuningSnapshot tuning)
    {
        return tuning with
        {
            ResourceCosts = Array.AsReadOnly((tuning.ResourceCosts ?? Array.Empty<SkillResourceCostDefinition>()).ToArray()),
            Effects = Array.AsReadOnly((tuning.Effects ?? Array.Empty<SkillConditionEffectDefinition>()).ToArray()),
            CastProtections = Array.AsReadOnly((tuning.CastProtections ?? Array.Empty<SkillProtectionGrantDefinition>()).ToArray()),
            TriggeredActions = Array.AsReadOnly((tuning.TriggeredActions ?? Array.Empty<SkillTriggeredActionDefinition>()).ToArray())
        };
    }

    private static SkillTuningSnapshot ApplyOverrides(
        SkillTuningSnapshot current,
        SkillAscensionOverrides ascensionOverride,
        List<string> notes)
    {
        var action = ascensionOverride.Action
            ?? current.Action with
            {
                MagnitudeProfile = ascensionOverride.MagnitudeProfile ?? current.Action.MagnitudeProfile
            };

        var targeting = ascensionOverride.Targeting ?? current.Targeting;
        var cadence = ascensionOverride.Cadence ?? current.Cadence;
        var resourceCosts = ascensionOverride.ResourceCosts is not null
            ? Array.AsReadOnly(ascensionOverride.ResourceCosts.ToArray())
            : current.ResourceCosts ?? Array.Empty<SkillResourceCostDefinition>();
        var effects = ApplyEffectOverrides(current.Effects ?? Array.Empty<SkillConditionEffectDefinition>(), ascensionOverride, notes);
        var multiHit = ascensionOverride.MultiHit ?? current.MultiHit;
        var castProtections = ascensionOverride.CastProtections is not null
            ? Array.AsReadOnly(ascensionOverride.CastProtections.ToArray())
            : current.CastProtections ?? Array.Empty<SkillProtectionGrantDefinition>();
        var triggeredActions = ascensionOverride.TriggeredActions is not null
            ? Array.AsReadOnly(ascensionOverride.TriggeredActions.ToArray())
            : current.TriggeredActions ?? Array.Empty<SkillTriggeredActionDefinition>();

        return new SkillTuningSnapshot(
            action,
            targeting,
            cadence,
            resourceCosts,
            effects,
            multiHit,
            castProtections,
            triggeredActions);
    }

    private static IReadOnlyList<SkillConditionEffectDefinition> ApplyEffectOverrides(
        IReadOnlyList<SkillConditionEffectDefinition> currentEffects,
        SkillAscensionOverrides ascensionOverride,
        List<string> notes)
    {
        var mutableEffects = currentEffects.ToDictionary(effect => effect.EffectKey, StringComparer.OrdinalIgnoreCase);

        foreach (var removedEffectKey in ascensionOverride.RemovedEffectKeys ?? Array.Empty<string>())
        {
            if (mutableEffects.Remove(removedEffectKey))
            {
                notes.Add($"Ascension {ascensionOverride.AscensionLevel} removed effect '{removedEffectKey}'.");
            }
        }

        foreach (var effectOverride in ascensionOverride.EffectOverrides ?? Array.Empty<SkillConditionEffectOverride>())
        {
            if (!mutableEffects.TryGetValue(effectOverride.EffectKey, out var existingEffect))
            {
                throw new InvalidOperationException(
                    $"Skill ascension {ascensionOverride.AscensionLevel} tried to override unknown effect '{effectOverride.EffectKey}'.");
            }

            mutableEffects[effectOverride.EffectKey] = existingEffect with
            {
                BaseDurationSeconds = effectOverride.BaseDurationSeconds ?? existingEffect.BaseDurationSeconds,
                BaseApplyChance = effectOverride.BaseApplyChance ?? existingEffect.BaseApplyChance,
                ApplyChanceFlatBonus = effectOverride.ApplyChanceFlatBonus ?? existingEffect.ApplyChanceFlatBonus,
                ApplyChanceMultiplier = effectOverride.ApplyChanceMultiplier ?? existingEffect.ApplyChanceMultiplier,
                Note = effectOverride.Note ?? existingEffect.Note
            };
        }

        foreach (var addedEffect in ascensionOverride.AddedEffects ?? Array.Empty<SkillConditionEffectDefinition>())
        {
            mutableEffects[addedEffect.EffectKey] = addedEffect;
            notes.Add($"Ascension {ascensionOverride.AscensionLevel} added effect '{addedEffect.EffectKey}'.");
        }

        var preservedOrder = currentEffects
            .Select(effect => effect.EffectKey)
            .Where(mutableEffects.ContainsKey)
            .Select(effectKey => mutableEffects[effectKey]);
        var addedOrder = mutableEffects.Values
            .Where(effect => currentEffects.All(existing => !string.Equals(existing.EffectKey, effect.EffectKey, StringComparison.OrdinalIgnoreCase)));

        return Array.AsReadOnly(preservedOrder.Concat(addedOrder).ToArray());
    }
}

public interface ISkillCadenceResolver
{
    SkillCadenceSnapshot Resolve(SkillCadenceProfile cadence, FinalStats actorStats);
}

public sealed class SkillCadenceResolver : ISkillCadenceResolver
{
    public SkillCadenceSnapshot Resolve(SkillCadenceProfile cadence, FinalStats actorStats)
    {
        ArgumentNullException.ThrowIfNull(actorStats);

        var cooldownReduction = cadence.AffectedByCooldownReduction
            ? actorStats.Get(StatType.CooldownReduction)
            : 0m;
        var skillRecoveryRate = cadence.AffectedBySkillRecoveryRate
            ? actorStats.Get(StatType.SkillRecoveryRate)
            : 0m;

        var cooldownAfterReduction = cadence.BaseCooldownSeconds * Math.Max(0m, 1m - cooldownReduction);
        var effectiveCooldown = cadence.AffectedBySkillRecoveryRate
            ? cooldownAfterReduction / Math.Max(0.0001m, 1m + skillRecoveryRate)
            : cooldownAfterReduction;

        return new SkillCadenceSnapshot(
            cadence.BaseCooldownSeconds,
            effectiveCooldown,
            cooldownReduction,
            skillRecoveryRate,
            cadence.AffectedByCooldownReduction,
            cadence.AffectedBySkillRecoveryRate,
            cadence.BaseCooldownSeconds == 0m
                ? "This skill has no base cooldown, so cooldown cadence is informational only."
                : null);
    }
}

public interface ISkillCombatTranslator
{
    SkillCombatActionPlan Prepare(SkillDefinition definition, SkillCombatTranslationContext context);
}

public sealed class SkillCombatTranslator : ISkillCombatTranslator
{
    private readonly ISkillAvailabilityEvaluator _availabilityEvaluator;
    private readonly ISkillAscensionResolver _ascensionResolver;
    private readonly ISkillCadenceResolver _cadenceResolver;

    public SkillCombatTranslator(
        ISkillAvailabilityEvaluator? availabilityEvaluator = null,
        ISkillAscensionResolver? ascensionResolver = null,
        ISkillCadenceResolver? cadenceResolver = null)
    {
        _availabilityEvaluator = availabilityEvaluator ?? new SkillAvailabilityEvaluator();
        _ascensionResolver = ascensionResolver ?? new SkillAscensionResolver();
        _cadenceResolver = cadenceResolver ?? new SkillCadenceResolver();
    }

    public SkillCombatActionPlan Prepare(SkillDefinition definition, SkillCombatTranslationContext context)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        var availability = _availabilityEvaluator.Evaluate(definition, context.Actor, context.SkillProgress);
        if (!availability.IsAvailable)
        {
            return new SkillCombatActionPlan(
                availability,
                null,
                null,
                null,
                null,
                Notes: availability.Notes);
        }

        var resolvedSkill = _ascensionResolver.Resolve(definition, availability.ResolvedAscensionLevel);
        var cadence = _cadenceResolver.Resolve(resolvedSkill.Tuning.Cadence, context.Actor.Stats);
        var castResourceCosts = MapResourceCosts(resolvedSkill.Tuning.ResourceCosts);
        var primaryCombatEvent = BuildCombatEvent(
            resolvedSkill.Tuning.Action,
            resolvedSkill.Tuning.Effects,
            actor: context.Actor,
            target: context.Target,
            targetClassification: context.TargetClassification,
            targetActiveConditions: context.TargetActiveConditions,
            targetActiveProtections: context.TargetActiveProtections,
            actionName: resolvedSkill.Name,
            declaredResourceCosts: Array.Empty<CombatResourceCost>());
        var scheduledEvents = BuildScheduledEvents(resolvedSkill, context, primaryCombatEvent);
        var castProtectionGrants = BuildCastProtectionGrants(resolvedSkill);

        var notes = new List<string>();
        notes.AddRange(availability.Notes ?? Array.Empty<string>());
        notes.AddRange(resolvedSkill.ResolutionNotes ?? Array.Empty<string>());
        notes.AddRange((resolvedSkill.PendingData ?? Array.Empty<SkillPendingDatum>())
            .Select(item => $"Pending data: {item.Description}"));
        notes.AddRange(resolvedSkill.SecurityNotes ?? Array.Empty<string>());

        if (castResourceCosts.Count > 0)
        {
            notes.Add("Cast resource costs are exposed at the skill-plan level so repeated hit schedules do not spend the same cost more than once.");
        }

        if (!string.IsNullOrWhiteSpace(cadence.Note))
        {
            notes.Add(cadence.Note);
        }

        return new SkillCombatActionPlan(
            availability,
            resolvedSkill,
            primaryCombatEvent,
            cadence,
            resolvedSkill.Tuning.Targeting,
            castResourceCosts,
            Array.AsReadOnly(scheduledEvents.ToArray()),
            Array.AsReadOnly(castProtectionGrants.ToArray()),
            resolvedSkill.PendingData,
            resolvedSkill.SecurityNotes,
            Array.AsReadOnly(notes.ToArray()));
    }

    private static List<SkillScheduledCombatEventPlan> BuildScheduledEvents(
        ResolvedSkillDefinition resolvedSkill,
        SkillCombatTranslationContext context,
        CombatEventContext primaryCombatEvent)
    {
        var tuning = resolvedSkill.Tuning;
        var plans = new List<SkillScheduledCombatEventPlan>();

        if (tuning.MultiHit is not null)
        {
            plans.Add(new SkillScheduledCombatEventPlan(
                $"{resolvedSkill.Id}.primary-active-window",
                SkillExecutionTriggerPhase.DuringActiveWindow,
                primaryCombatEvent,
                tuning.MultiHit.HitCount,
                tuning.MultiHit.ActiveDurationSeconds,
                tuning.MultiHit.HitIntervalSeconds,
                tuning.MultiHit.EffectsResolvePerHit,
                tuning.MultiHit.Note));
        }
        else
        {
            plans.Add(new SkillScheduledCombatEventPlan(
                $"{resolvedSkill.Id}.primary-single",
                SkillExecutionTriggerPhase.OnCast,
                primaryCombatEvent,
                1,
                null,
                null,
                true,
                "Single combat event without multi-hit scheduling."));
        }

        foreach (var triggeredAction in tuning.TriggeredActions ?? Array.Empty<SkillTriggeredActionDefinition>())
        {
            var target = triggeredAction.TargetSelector == SkillTriggeredActionTargetSelector.Self
                ? context.Actor
                : context.Target;
            var targetClassification = triggeredAction.TargetSelector == SkillTriggeredActionTargetSelector.Self
                ? CombatTargetClassification.None
                : context.TargetClassification;
            var targetConditions = triggeredAction.TargetSelector == SkillTriggeredActionTargetSelector.Self
                ? Array.Empty<CombatConditionType>()
                : context.TargetActiveConditions;
            var targetProtections = triggeredAction.TargetSelector == SkillTriggeredActionTargetSelector.Self
                ? Array.Empty<CombatProtectionState>()
                : context.TargetActiveProtections;
            var eventContext = BuildCombatEvent(
                triggeredAction.Action,
                triggeredAction.Effects,
                actor: context.Actor,
                target: target,
                targetClassification: targetClassification,
                targetActiveConditions: targetConditions,
                targetActiveProtections: targetProtections,
                actionName: $"{resolvedSkill.Name}:{triggeredAction.ActionKey}",
                declaredResourceCosts: Array.Empty<CombatResourceCost>());

            plans.Add(new SkillScheduledCombatEventPlan(
                $"{resolvedSkill.Id}.{triggeredAction.ActionKey}",
                triggeredAction.TriggerPhase,
                eventContext,
                1,
                null,
                null,
                true,
                triggeredAction.Note));
        }

        return plans;
    }

    private static List<CombatProtectionGrantIntent> BuildCastProtectionGrants(ResolvedSkillDefinition resolvedSkill)
    {
        return (resolvedSkill.Tuning.CastProtections ?? Array.Empty<SkillProtectionGrantDefinition>())
            .Select(protection => new CombatProtectionGrantIntent(
                protection.ProtectionType,
                protection.Blocks,
                protection.BaseDurationSeconds,
                protection.RefreshPolicy,
                protection.RemovesExistingNegativeEffects,
                protection.GrantKey,
                protection.Note))
            .ToList();
    }

    private static CombatEventContext BuildCombatEvent(
        SkillActionDefinition action,
        IReadOnlyList<SkillConditionEffectDefinition>? effects,
        Character actor,
        Character target,
        CombatTargetClassification targetClassification,
        IReadOnlyCollection<CombatConditionType>? targetActiveConditions,
        IReadOnlyCollection<CombatProtectionState>? targetActiveProtections,
        string actionName,
        IReadOnlyCollection<CombatResourceCost>? declaredResourceCosts)
    {
        return new CombatEventContext(
            actor,
            target,
            MapActionType(action.ActionType),
            CombatActionSource.Skill,
            BaseMagnitude: 0m,
            DamageType: MapDamageType(action.DamageType),
            CanCrit: action.CanCrit,
            RequiresHitCheck: action.RequiresHitCheck,
            TargetResourceType: action.TargetResourceType,
            DamageConditionType: action.DamageConditionType,
            TargetClassification: targetClassification,
            TargetActiveConditions: targetActiveConditions,
            TargetActiveProtections: targetActiveProtections,
            PotentialEffects: MapEffects(effects),
            DeclaredResourceCosts: declaredResourceCosts,
            MagnitudeProfile: MapMagnitude(ApplyConditionSynergies(action.MagnitudeProfile, action.ConditionSynergies, targetActiveConditions)),
            ActionName: actionName);
    }

    private static CombatActionKind MapActionType(SkillActionType actionType)
    {
        return actionType switch
        {
            SkillActionType.Damage => CombatActionKind.Damage,
            SkillActionType.Heal => CombatActionKind.Heal,
            SkillActionType.Utility => CombatActionKind.Utility,
            _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, "Unsupported skill action type.")
        };
    }

    private static CombatDamageType? MapDamageType(SkillDamageType? damageType)
    {
        return damageType switch
        {
            null => null,
            SkillDamageType.Physical => CombatDamageType.Physical,
            SkillDamageType.Magical => CombatDamageType.Magical,
            SkillDamageType.True => CombatDamageType.True,
            _ => throw new ArgumentOutOfRangeException(nameof(damageType), damageType, "Unsupported skill damage type.")
        };
    }

    private static SkillMagnitudeProfile ApplyConditionSynergies(
        SkillMagnitudeProfile magnitudeProfile,
        IReadOnlyList<SkillConditionSynergyDefinition>? synergies,
        IReadOnlyCollection<CombatConditionType>? targetActiveConditions)
    {
        if ((synergies?.Count ?? 0) == 0 || (targetActiveConditions?.Count ?? 0) == 0)
        {
            return magnitudeProfile;
        }

        var adjustedBaseMagnitude = magnitudeProfile.BaseMagnitude;
        var adjustedScalingCoefficient = magnitudeProfile.ScalingCoefficient;
        var matchedAny = false;

        foreach (var synergy in synergies ?? Array.Empty<SkillConditionSynergyDefinition>())
        {
            if (!(targetActiveConditions ?? Array.Empty<CombatConditionType>()).Contains(synergy.RequiredTargetCondition))
            {
                continue;
            }

            adjustedBaseMagnitude = (adjustedBaseMagnitude + synergy.FlatBaseMagnitudeBonus) * synergy.MagnitudeMultiplier;
            adjustedScalingCoefficient *= synergy.MagnitudeMultiplier;
            matchedAny = true;
        }

        return matchedAny
            ? magnitudeProfile with
            {
                BaseMagnitude = adjustedBaseMagnitude,
                ScalingCoefficient = adjustedScalingCoefficient
            }
            : magnitudeProfile;
    }
    private static CombatActionMagnitudeProfile MapMagnitude(SkillMagnitudeProfile magnitudeProfile)
    {
        return new CombatActionMagnitudeProfile(
            magnitudeProfile.BaseMagnitude,
            magnitudeProfile.ScalingType switch
            {
                SkillScalingType.FixedOnly => CombatActionScalingType.FixedOnly,
                SkillScalingType.PhysicalAttack => CombatActionScalingType.PhysicalAttack,
                SkillScalingType.MagicAttack => CombatActionScalingType.MagicAttack,
                SkillScalingType.TargetMissingHp => CombatActionScalingType.TargetMissingHp,
                _ => throw new ArgumentOutOfRangeException(nameof(magnitudeProfile.ScalingType), magnitudeProfile.ScalingType, "Unsupported skill scaling type.")
            },
            magnitudeProfile.ScalingCoefficient,
            magnitudeProfile.ConfigurationName);
    }

    private static IReadOnlyList<CombatResourceCost> MapResourceCosts(IReadOnlyList<SkillResourceCostDefinition>? resourceCosts)
    {
        return Array.AsReadOnly((resourceCosts ?? Array.Empty<SkillResourceCostDefinition>())
            .Select(cost => new CombatResourceCost(cost.ResourceType, cost.Amount, cost.AbortIfInsufficient))
            .ToArray());
    }

    private static IReadOnlyList<CombatConditionApplicationIntent> MapEffects(IReadOnlyList<SkillConditionEffectDefinition>? effects)
    {
        return Array.AsReadOnly((effects ?? Array.Empty<SkillConditionEffectDefinition>())
            .Select(effect => new CombatConditionApplicationIntent(
                effect.Condition,
                effect.BaseDurationSeconds,
                new CombatConditionApplicationChanceProfile(
                    effect.BaseApplyChance,
                    effect.ApplyChanceFlatBonus,
                    effect.ApplyChanceMultiplier),
                effect.RequiredTargetConditions,
                effect.EffectKey,
                effect.Note))
            .ToArray());
    }
}







