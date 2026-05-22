using War.Core.Resources;
using War.Core.Stats;

namespace War.Core.Combat;

public interface ICombatEventResolver
{
    CombatResolutionResult Resolve(CombatEventContext context);
}

public sealed class CombatEventResolver : ICombatEventResolver
{
    private readonly IHitChanceResolver _hitChanceResolver;
    private readonly ICriticalChanceResolver _criticalChanceResolver;
    private readonly IMitigationResolver _mitigationResolver;
    private readonly IDamageModifierResolver _damageModifierResolver;
    private readonly ICombatConditionApplicationService _conditionApplicationService;
    private readonly ICombatConditionInteractionService _conditionInteractionService;
    private readonly ICombatResourceProjectionService _resourceProjectionService;
    private readonly ICombatActionResourceService _actionResourceService;
    private readonly ICombatActionMagnitudeService _actionMagnitudeService;
    private readonly ICombatProbabilityService _probabilityService;

    public CombatEventResolver(
        IHitChanceResolver? hitChanceResolver = null,
        ICriticalChanceResolver? criticalChanceResolver = null,
        IMitigationResolver? mitigationResolver = null,
        IDamageModifierResolver? damageModifierResolver = null,
        ICombatConditionApplicationService? conditionApplicationService = null,
        ICombatConditionInteractionService? conditionInteractionService = null,
        ICombatResourceProjectionService? resourceProjectionService = null,
        ICombatActionResourceService? actionResourceService = null,
        ICombatActionMagnitudeService? actionMagnitudeService = null,
        ICombatProbabilityService? probabilityService = null)
    {
        _hitChanceResolver = hitChanceResolver ?? new HitChanceResolver();
        _criticalChanceResolver = criticalChanceResolver ?? new CriticalChanceResolver();
        _mitigationResolver = mitigationResolver ?? new MitigationResolver();
        _damageModifierResolver = damageModifierResolver ?? new DamageModifierResolver();
        _resourceProjectionService = resourceProjectionService ?? new CombatResourceProjectionService();
        _probabilityService = probabilityService ?? new RandomCombatProbabilityService();
        _conditionApplicationService = conditionApplicationService ?? new CombatConditionApplicationService(probabilityService: _probabilityService);
        _conditionInteractionService = conditionInteractionService ?? new CombatConditionInteractionService();
        _actionResourceService = actionResourceService ?? new CombatActionResourceService(_resourceProjectionService);
        _actionMagnitudeService = actionMagnitudeService ?? new CombatActionMagnitudeService();
    }

    public CombatResolutionResult Resolve(CombatEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ValidateContext(context);

        var notes = new List<string>();
        var targetActiveConditions = context.TargetActiveConditions ?? Array.Empty<CombatConditionType>();
        var targetActiveProtections = context.TargetActiveProtections ?? Array.Empty<CombatProtectionState>();
        var declaredResourceCosts = context.DeclaredResourceCosts ?? Array.Empty<CombatResourceCost>();
        var proposedResourceChanges = new List<CombatResourceChangeProposal>();
        var actionMagnitudeResolution = _actionMagnitudeService.Resolve(context);
        var resolvedBaseMagnitude = actionMagnitudeResolution.FinalBaseMagnitude;
        var actionResourceEvaluation = _actionResourceService.Evaluate(context);

        if (!string.IsNullOrWhiteSpace(actionMagnitudeResolution.Note))
        {
            notes.Add(actionMagnitudeResolution.Note);
        }

        notes.AddRange(actionResourceEvaluation.Notes);

        if (actionResourceEvaluation.HasDeclaredCosts)
        {
            notes.Add("Action-cost projections are reported separately from impact-driven resource changes.");
        }

        if (actionResourceEvaluation.WasAbortedByInsufficientResources)
        {
            notes.Add("Combat event stopped before hit, damage, healing, or condition processing because a blocking resource cost was not affordable.");

            return new CombatResolutionResult(
                CombatImpactOutcome.Aborted,
                CombatImpactFailureReason.InsufficientResources,
                false,
                false,
                resolvedBaseMagnitude,
                actionMagnitudeResolution,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                null,
                null,
                null,
                null,
                null,
                actionResourceEvaluation,
                Array.Empty<CombatResourceChangeProposal>(),
                Array.Empty<CombatConditionResolution>(),
                Array.Empty<CombatConditionInteractionActivation>(),
                declaredResourceCosts,
                false,
                notes);
        }
        if (context.ActionKind == CombatActionKind.Damage && CombatProtectionRules.BlocksDamage(targetActiveProtections))
        {
            notes.Add(CombatProtectionRules.DescribeBlockingProtections(targetActiveProtections));

            var blockedConditionResults = _conditionApplicationService.Resolve(context, impactSucceeded: true);

            return new CombatResolutionResult(
                CombatImpactOutcome.Blocked,
                CombatImpactFailureReason.TargetProtected,
                false,
                false,
                resolvedBaseMagnitude,
                actionMagnitudeResolution,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                0m,
                null,
                null,
                null,
                null,
                null,
                actionResourceEvaluation,
                Array.Empty<CombatResourceChangeProposal>(),
                blockedConditionResults,
                Array.Empty<CombatConditionInteractionActivation>(),
                declaredResourceCosts,
                false,
                notes);
        }

        HitChanceResolution? hitChanceResolution = null;
        CombatProbabilityCheckResult? hitCheck = null;
        var impactSucceeded = true;

        if (context.RequiresHitCheck)
        {
            hitChanceResolution = _hitChanceResolver.Resolve(
                context.Actor.Stats.Get(StatType.Accuracy),
                context.Target.Stats.Get(StatType.Evasion));
            hitCheck = _probabilityService.Evaluate(hitChanceResolution.ChanceToHit, "Hit check.");
            impactSucceeded = hitCheck.Succeeded;
        }
        else
        {
            notes.Add("Hit check was skipped by the combat context.");
        }

        CriticalChanceResolution? criticalChanceResolution = null;
        CombatProbabilityCheckResult? criticalCheck = null;
        var wasCritical = false;

        if (context.ActionKind == CombatActionKind.Damage && impactSucceeded && context.CanCrit)
        {
            criticalChanceResolution = _criticalChanceResolver.Resolve(
                context.Actor.Stats.Get(StatType.CritChance),
                context.Target.Stats.Get(StatType.CriticalEvasion));
            criticalCheck = _probabilityService.Evaluate(criticalChanceResolution.EffectiveCritChance, "Critical check.");
            wasCritical = criticalCheck.Succeeded;
        }
        else if (context.CanCrit && context.ActionKind != CombatActionKind.Damage)
        {
            notes.Add("Critical resolution was ignored because only damage actions can crit in the current pipeline.");
        }

        var baseDamage = 0m;
        var criticalBonusDamage = 0m;
        var damageBeforeMitigation = 0m;
        var damageMitigatedAmount = 0m;
        var damageAfterMitigation = 0m;
        var damageAfterSourceReductions = 0m;
        var damageAfterConditionModifiers = 0m;
        var finalDamage = 0m;
        var finalHealing = 0m;
        var outgoingDamageIncreasePercentage = 0m;
        var criticalDamageIncreasePercentage = 0m;
        var incomingDamageReductionPercentage = 0m;
        var criticalDamageTakenReductionPercentage = 0m;
        var conditionDamageIncreasePercentage = 0m;
        var conditionDamageReductionPercentage = 0m;
        var interactionDamageIncreasePercentage = 0m;
        MitigationResolution? mitigationResolution = null;

        if (context.ActionKind == CombatActionKind.Damage && impactSucceeded)
        {
            baseDamage = resolvedBaseMagnitude;
            criticalBonusDamage = wasCritical ? context.Actor.Stats.Get(StatType.CritDamage) : 0m;
            damageBeforeMitigation = baseDamage + criticalBonusDamage;

            outgoingDamageIncreasePercentage = GetOutgoingDamageIncreasePercentage(context);
            criticalDamageIncreasePercentage = GetCriticalDamageIncreasePercentage(context, wasCritical);

            var baseDamageAfterOutgoingModifiers = ApplyIncrease(baseDamage, outgoingDamageIncreasePercentage);
            var criticalDamageAfterOutgoingModifiers = wasCritical
                ? ApplyIncrease(criticalBonusDamage, outgoingDamageIncreasePercentage + criticalDamageIncreasePercentage)
                : 0m;
            var totalDamageAfterOutgoingModifiers = baseDamageAfterOutgoingModifiers + criticalDamageAfterOutgoingModifiers;

            mitigationResolution = ResolveMitigation(context, totalDamageAfterOutgoingModifiers, notes);

            var baseDamageAfterMitigation = mitigationResolution is null
                ? baseDamageAfterOutgoingModifiers
                : baseDamageAfterOutgoingModifiers * (1m - mitigationResolution.MitigationRatio);
            var criticalDamageAfterMitigation = mitigationResolution is null
                ? criticalDamageAfterOutgoingModifiers
                : criticalDamageAfterOutgoingModifiers * (1m - mitigationResolution.MitigationRatio);

            damageAfterMitigation = baseDamageAfterMitigation + criticalDamageAfterMitigation;
            damageMitigatedAmount = totalDamageAfterOutgoingModifiers - damageAfterMitigation;

            incomingDamageReductionPercentage = GetIncomingDamageReductionPercentage(context);
            criticalDamageTakenReductionPercentage = GetCriticalDamageTakenReductionPercentage(context, wasCritical);

            var baseDamageAfterSourceReductions = ApplyReduction(baseDamageAfterMitigation, incomingDamageReductionPercentage);
            var criticalDamageAfterSourceReductions = wasCritical
                ? ApplyReduction(criticalDamageAfterMitigation, incomingDamageReductionPercentage + criticalDamageTakenReductionPercentage)
                : 0m;

            damageAfterSourceReductions = baseDamageAfterSourceReductions + criticalDamageAfterSourceReductions;

            (conditionDamageIncreasePercentage, conditionDamageReductionPercentage, damageAfterConditionModifiers) =
                ResolveConditionTaggedDamage(context, damageAfterSourceReductions, notes);
        }
        else if (context.ActionKind == CombatActionKind.Heal && impactSucceeded)
        {
            finalHealing = ResolveRestoration(context, resolvedBaseMagnitude, notes);
        }

        var directConditionResults = _conditionApplicationService.Resolve(context, impactSucceeded);
        var interactionEvaluation = _conditionInteractionService.Evaluate(targetActiveConditions, directConditionResults);
        interactionDamageIncreasePercentage = interactionEvaluation.TotalFinalDamageIncreasePercentage;
        notes.AddRange(interactionEvaluation.Notes);

        var allConditionResults = directConditionResults
            .Concat(interactionEvaluation.GeneratedConditionResults)
            .ToArray();

        if (context.ActionKind == CombatActionKind.Damage && impactSucceeded)
        {
            finalDamage = ApplyIncrease(damageAfterConditionModifiers, interactionDamageIncreasePercentage);

            if (finalDamage > 0m)
            {
                proposedResourceChanges.Add(_resourceProjectionService.Project(
                    context.Target,
                    CombatEntityRole.Target,
                    context.TargetResourceType,
                    -finalDamage,
                    CombatResourceChangeReason.Damage));
            }
        }

        if (context.ActionKind == CombatActionKind.Heal && impactSucceeded && finalHealing > 0m)
        {
            var resourceChangeReason = context.TargetResourceType == CharacterResourceType.Hp
                ? CombatResourceChangeReason.Healing
                : CombatResourceChangeReason.ResourceRestore;

            proposedResourceChanges.Add(_resourceProjectionService.Project(
                context.Target,
                CombatEntityRole.Target,
                context.TargetResourceType,
                finalHealing,
                resourceChangeReason));
        }

        var wouldCauseTargetDefeat = proposedResourceChanges.Any(change =>
            change.AffectedEntity == CombatEntityRole.Target &&
            change.ResourceType == CharacterResourceType.Hp &&
            change.WouldTriggerDepletionResolution);

        var outcome = ResolveOutcome(context, impactSucceeded, wasCritical);
        var failureReason = ResolveFailureReason(context, impactSucceeded);

        return new CombatResolutionResult(
            outcome,
            failureReason,
            impactSucceeded,
            wasCritical,
            resolvedBaseMagnitude,
            actionMagnitudeResolution,
            baseDamage,
            criticalBonusDamage,
            damageBeforeMitigation,
            damageMitigatedAmount,
            damageAfterMitigation,
            damageAfterSourceReductions,
            damageAfterConditionModifiers,
            finalDamage,
            finalHealing,
            outgoingDamageIncreasePercentage,
            criticalDamageIncreasePercentage,
            incomingDamageReductionPercentage,
            criticalDamageTakenReductionPercentage,
            conditionDamageIncreasePercentage,
            conditionDamageReductionPercentage,
            interactionDamageIncreasePercentage,
            hitChanceResolution,
            hitCheck,
            criticalChanceResolution,
            criticalCheck,
            mitigationResolution,
            actionResourceEvaluation,
            proposedResourceChanges,
            allConditionResults,
            interactionEvaluation.Activations,
            declaredResourceCosts,
            wouldCauseTargetDefeat,
            notes);
    }

    private static void ValidateContext(CombatEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context.Actor);
        ArgumentNullException.ThrowIfNull(context.Target);

        if (context.MagnitudeProfile is null && context.BaseMagnitude < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(context), context.BaseMagnitude, "Legacy BaseMagnitude fallback cannot be negative.");
        }

        if (context.ActionKind == CombatActionKind.Damage && context.DamageType is null)
        {
            throw new ArgumentException("Damage actions require a damage type.", nameof(context));
        }

        var invalidDeclaredCost = (context.DeclaredResourceCosts ?? Array.Empty<CombatResourceCost>())
            .FirstOrDefault(cost => cost.Amount < 0m);

        if (invalidDeclaredCost is { Amount: < 0m })
        {
            throw new ArgumentOutOfRangeException(
                nameof(context),
                invalidDeclaredCost.Amount,
                $"Declared resource cost for {invalidDeclaredCost.ResourceType} cannot be negative.");
        }
    }

    private static CombatImpactFailureReason ResolveFailureReason(CombatEventContext context, bool impactSucceeded)
    {
        if (impactSucceeded)
        {
            return CombatImpactFailureReason.None;
        }

        return context.RequiresHitCheck
            ? CombatImpactFailureReason.Evaded
            : CombatImpactFailureReason.None;
    }

    private static CombatImpactOutcome ResolveOutcome(CombatEventContext context, bool impactSucceeded, bool wasCritical)
    {
        if (!impactSucceeded)
        {
            return CombatImpactOutcome.Miss;
        }

        return context.ActionKind switch
        {
            CombatActionKind.Heal when context.TargetResourceType != CharacterResourceType.Hp => CombatImpactOutcome.ResourceRestore,
            CombatActionKind.Heal => CombatImpactOutcome.Heal,
            CombatActionKind.Damage when wasCritical => CombatImpactOutcome.Critical,
            CombatActionKind.Damage => CombatImpactOutcome.Hit,
            CombatActionKind.Utility => CombatImpactOutcome.Hit,
            _ => CombatImpactOutcome.None
        };
    }

    private decimal GetOutgoingDamageIncreasePercentage(CombatEventContext context)
    {
        var contexts = GetBaseDamageContexts(context);
        return contexts.Sum(modifierContext =>
            _damageModifierResolver.ResolveForContext(context.Actor.Stats, modifierContext).IncreasePercentage);
    }

    private decimal GetCriticalDamageIncreasePercentage(CombatEventContext context, bool wasCritical)
    {
        if (!wasCritical)
        {
            return 0m;
        }

        return _damageModifierResolver.ResolveForContext(context.Actor.Stats, DamageModifierContext.Critical).IncreasePercentage;
    }

    private decimal GetIncomingDamageReductionPercentage(CombatEventContext context)
    {
        var contexts = GetBaseDamageContexts(context);
        return contexts.Sum(modifierContext =>
            _damageModifierResolver.ResolveForContext(context.Target.Stats, modifierContext).ReductionPercentage);
    }

    private decimal GetCriticalDamageTakenReductionPercentage(CombatEventContext context, bool wasCritical)
    {
        if (!wasCritical)
        {
            return 0m;
        }

        return _damageModifierResolver.ResolveForContext(context.Target.Stats, DamageModifierContext.Critical).ReductionPercentage;
    }

    private MitigationResolution? ResolveMitigation(CombatEventContext context, decimal currentDamage, List<string> notes)
    {
        if (context.DamageType == CombatDamageType.True)
        {
            notes.Add("True damage bypassed defense and resistance mitigation.");
            return null;
        }

        if (currentDamage <= 0m)
        {
            return null;
        }

        var (mitigationStatType, penetrationStatType) = context.DamageType switch
        {
            CombatDamageType.Physical => (StatType.Defense, StatType.DefensePenetration),
            CombatDamageType.Magical => (StatType.MagicResistance, StatType.MagicPenetration),
            _ => throw new ArgumentOutOfRangeException(nameof(context.DamageType), context.DamageType, "Unsupported damage type for mitigation.")
        };

        var rawMitigationInput = context.Target.Stats.Get(mitigationStatType);
        var penetrationPercentage = context.Actor.Stats.Get(penetrationStatType);
        var effectiveMitigationInput = rawMitigationInput * Math.Clamp(1m - penetrationPercentage, 0m, 1m);

        return _mitigationResolver.Resolve(mitigationStatType, effectiveMitigationInput);
    }

    private (decimal IncreasePercentage, decimal ReductionPercentage, decimal DamageAfterConditionModifiers) ResolveConditionTaggedDamage(
        CombatEventContext context,
        decimal damageAfterSourceReductions,
        List<string> notes)
    {
        if (context.DamageConditionType is null)
        {
            return (0m, 0m, damageAfterSourceReductions);
        }

        var actorProfile = _damageModifierResolver.TryResolveForCondition(context.Actor.Stats, context.DamageConditionType.Value);
        var targetProfile = _damageModifierResolver.TryResolveForCondition(context.Target.Stats, context.DamageConditionType.Value);

        if (actorProfile is null && targetProfile is null)
        {
            notes.Add($"No direct condition damage modifier profile is defined for {context.DamageConditionType.Value}; state damage modifiers were skipped.");
            return (0m, 0m, damageAfterSourceReductions);
        }

        var increasePercentage = actorProfile?.IncreasePercentage ?? 0m;
        var reductionPercentage = targetProfile?.ReductionPercentage ?? 0m;
        var damageAfterIncrease = ApplyIncrease(damageAfterSourceReductions, increasePercentage);
        var damageAfterReduction = ApplyReduction(damageAfterIncrease, reductionPercentage);

        return (increasePercentage, reductionPercentage, damageAfterReduction);
    }

    private decimal ResolveRestoration(CombatEventContext context, decimal resolvedBaseMagnitude, List<string> notes)
    {
        if (context.TargetResourceType != CharacterResourceType.Hp)
        {
            notes.Add($"Restorative action targeted {context.TargetResourceType}, so the pipeline used the resolved base magnitude directly. No non-HP restoration modifiers are modeled yet.");
            return resolvedBaseMagnitude;
        }

        var outgoingHealing = ApplyIncrease(
            resolvedBaseMagnitude,
            context.Actor.Stats.Get(StatType.HealingEffectiveness));

        return ApplyIncrease(
            outgoingHealing,
            context.Target.Stats.Get(StatType.HealingReceived));
    }

    private static decimal ApplyIncrease(decimal baseValue, decimal increasePercentage)
    {
        return baseValue * Math.Max(0m, 1m + increasePercentage);
    }

    private static decimal ApplyReduction(decimal baseValue, decimal reductionPercentage)
    {
        return baseValue * Math.Max(0m, 1m - reductionPercentage);
    }

    private static IReadOnlyList<DamageModifierContext> GetBaseDamageContexts(CombatEventContext context)
    {
        var contexts = new List<DamageModifierContext>
        {
            context.ActionSource == CombatActionSource.BasicAttack
                ? DamageModifierContext.BasicAttack
                : DamageModifierContext.Skill
        };

        var classificationContext = context.TargetClassification switch
        {
            CombatTargetClassification.Player => DamageModifierContext.PvP,
            CombatTargetClassification.Monster => DamageModifierContext.Monster,
            CombatTargetClassification.Boss => DamageModifierContext.Boss,
            _ => (DamageModifierContext?)null
        };

        if (classificationContext.HasValue)
        {
            contexts.Add(classificationContext.Value);
        }

        return Array.AsReadOnly(contexts.ToArray());
    }
}

