using War.Core.Combat;
using War.Core.Entities;
using War.Core.Skills;
using War.Core.Stats;

namespace War.Core.PowerScore;

public interface IPowerScoreClassUsageAnalyzer
{
    PowerScoreClassUsageAnalysis Analyze(PowerScoreCalculationContext context);
}

public sealed class PowerScoreClassUsageAnalyzer : IPowerScoreClassUsageAnalyzer
{
    private readonly IPowerScorePolicyCatalog _policyCatalog;
    private readonly ISkillAvailabilityEvaluator _skillAvailabilityEvaluator;
    private readonly ISkillAscensionResolver _skillAscensionResolver;

    public PowerScoreClassUsageAnalyzer(
        IPowerScorePolicyCatalog? policyCatalog = null,
        ISkillAvailabilityEvaluator? skillAvailabilityEvaluator = null,
        ISkillAscensionResolver? skillAscensionResolver = null)
    {
        _policyCatalog = policyCatalog ?? PowerScorePolicyCatalog.Default;
        _skillAvailabilityEvaluator = skillAvailabilityEvaluator ?? new SkillAvailabilityEvaluator();
        _skillAscensionResolver = skillAscensionResolver ?? new SkillAscensionResolver();
    }

    public PowerScoreClassUsageAnalysis Analyze(PowerScoreCalculationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Character);

        var character = context.Character;
        var classProfile = context.ClassProfile ?? PowerScoreClassProfileRegistry.GetRequired(character.ClassType);
        var skillCatalog = context.SkillCatalog ?? SkillCatalogRegistry.Current;
        var classSkillCatalog = skillCatalog.ClassCatalogs.FirstOrDefault(catalog => catalog.ClassType == character.ClassType)
            ?? new ClassSkillCatalog(character.ClassType);

        var sources = new List<PowerScoreUsageSourceBreakdown>();
        var statSignals = new Dictionary<StatType, decimal>();
        var notes = new List<string>();

        foreach (var pending in classProfile.PendingData ?? Array.Empty<string>())
        {
            notes.Add($"Pending class data: {pending}");
        }

        notes.AddRange(classProfile.Notes ?? Array.Empty<string>());

        foreach (var basicAttack in classProfile.BasicAttacks ?? Array.Empty<BasicAttackPowerProfile>())
        {
            var breakdown = BuildBasicAttackBreakdown(basicAttack, statSignals);
            sources.Add(breakdown);
        }

        foreach (var skill in classSkillCatalog.Skills)
        {
            var breakdown = BuildSkillBreakdown(character, context.SkillProgress, classProfile, skill, statSignals);
            sources.Add(breakdown);
        }

        var modeledBasicSourceCount = sources.Count(source => source.SourceKind == PowerScoreSourceKind.BasicAttack && source.IsModeled);
        var modeledSkillSourceCount = sources.Count(source => source.SourceKind == PowerScoreSourceKind.Skill && source.IsModeled);
        var availableBasicSourceCount = sources.Count(source => source.SourceKind == PowerScoreSourceKind.BasicAttack && source.IsAvailable);
        var availableSkillSourceCount = sources.Count(source => source.SourceKind == PowerScoreSourceKind.Skill && source.IsAvailable);
        var modeledSourceWeight = sources.Where(source => source.IsModeled).Sum(source => source.BaseWeight);
        var expectedSourceWeight = ResolveExpectedSourceWeight(classProfile, _policyCatalog.Tuning);
        var coverageRatio = expectedSourceWeight <= 0m
            ? 1m
            : Math.Clamp(modeledSourceWeight / expectedSourceWeight, 0m, 1m);
        var coverageBlend = Math.Clamp(
            _policyCatalog.Tuning.MinimumCoverageBlend + ((1m - _policyCatalog.Tuning.MinimumCoverageBlend) * coverageRatio),
            _policyCatalog.Tuning.MinimumCoverageBlend,
            1m);

        if (modeledSkillSourceCount == 0)
        {
            notes.Add($"No modeled skills are currently registered for class {character.ClassType}, so contextual offensive weights remain close to neutral.");
        }

        var statFactors = BuildStatFactors(statSignals, coverageBlend);

        return new PowerScoreClassUsageAnalysis(
            character.ClassType,
            classProfile.ExpectedBasicSourceCount,
            classProfile.ExpectedStandardSkillSourceCount,
            classProfile.ExpectedUltimateSkillSourceCount,
            modeledBasicSourceCount,
            modeledSkillSourceCount,
            availableBasicSourceCount,
            availableSkillSourceCount,
            modeledSourceWeight,
            expectedSourceWeight,
            coverageRatio,
            coverageBlend,
            Array.AsReadOnly(sources.ToArray()),
            statFactors,
            Array.AsReadOnly(notes.ToArray()));
    }

    private PowerScoreUsageSourceBreakdown BuildBasicAttackBreakdown(
        BasicAttackPowerProfile basicAttack,
        IDictionary<StatType, decimal> statSignals)
    {
        var baseWeight = _policyCatalog.Tuning.BasicAttackSourceWeight * Math.Max(0m, basicAttack.UsageWeight);
        var signals = new List<PowerScoreSourceSignal>();
        var notes = new List<string>();
        var scalingComponents = basicAttack.ScalingComponents?.Count > 0
            ? basicAttack.ScalingComponents
            : new[]
            {
                new BasicAttackPowerScalingComponent(
                    basicAttack.ScalingType,
                    1m,
                    $"Basic attack '{basicAttack.Name}' scales with {basicAttack.ScalingType}.")
            };

        foreach (var scalingComponent in scalingComponents)
        {
            var scalingWeight = Math.Max(0m, scalingComponent.SignalWeight);
            if (scalingWeight <= 0m)
            {
                continue;
            }

            AddScalingSignal(
                signals,
                statSignals,
                scalingComponent.ScalingType,
                baseWeight * scalingWeight,
                scalingComponent.Note ?? $"Basic attack '{basicAttack.Name}' scales with {scalingComponent.ScalingType}.");
        }

        if (basicAttack.UsesAttackSpeed)
        {
            AddSignal(signals, statSignals, StatType.AttackSpeed, baseWeight, $"Basic attack '{basicAttack.Name}' converts AttackSpeed into throughput.");
        }

        if (basicAttack.CanCrit)
        {
            AddSignal(signals, statSignals, StatType.CritChance, baseWeight, $"Basic attack '{basicAttack.Name}' can crit.");
            AddSignal(signals, statSignals, StatType.CritDamage, baseWeight, $"Basic attack '{basicAttack.Name}' can crit.");
            AddSignal(signals, statSignals, StatType.CritDamageIncrease, baseWeight, $"Basic attack '{basicAttack.Name}' can crit.");
        }

        if (basicAttack.RequiresHitCheck)
        {
            AddSignal(signals, statSignals, StatType.Accuracy, baseWeight, $"Basic attack '{basicAttack.Name}' requires hit checks.");
        }

        if (basicAttack.DamageType == SkillDamageType.Physical)
        {
            AddSignal(signals, statSignals, StatType.DefensePenetration, baseWeight, $"Basic attack '{basicAttack.Name}' deals physical damage.");
            AddSignal(signals, statSignals, StatType.BasicAttackDamageIncrease, baseWeight, $"Basic attack '{basicAttack.Name}' is a basic-damage source.");
        }
        else if (basicAttack.DamageType == SkillDamageType.Magical)
        {
            AddSignal(signals, statSignals, StatType.MagicPenetration, baseWeight, $"Basic attack '{basicAttack.Name}' deals magical damage.");
            AddSignal(signals, statSignals, StatType.BasicAttackDamageIncrease, baseWeight, $"Basic attack '{basicAttack.Name}' is a basic-damage source.");
        }

        foreach (var condition in basicAttack.AppliedConditions ?? Array.Empty<CombatConditionType>())
        {
            AddConditionSignals(signals, statSignals, condition, baseWeight, $"Basic attack '{basicAttack.Name}' can apply {condition}.");
        }

        notes.AddRange(basicAttack.Notes ?? Array.Empty<string>());

        return new PowerScoreUsageSourceBreakdown(
            basicAttack.Id,
            PowerScoreSourceKind.BasicAttack,
            basicAttack.Name,
            baseWeight,
            baseWeight,
            IsModeled: true,
            IsAvailable: true,
            Signals: Array.AsReadOnly(signals.ToArray()),
            Notes: Array.AsReadOnly(notes.ToArray()));
    }

    private PowerScoreUsageSourceBreakdown BuildSkillBreakdown(
        Character character,
        CharacterSkillProgressCollection? skillProgressCollection,
        ClassPowerScoreProfile classProfile,
        SkillDefinition skill,
        IDictionary<StatType, decimal> statSignals)
    {
        var progress = skillProgressCollection?.GetOrDefault(skill.Id);
        var availability = _skillAvailabilityEvaluator.Evaluate(skill, character, progress);
        var baseWeight = ResolveSkillWeight(skill, classProfile);
        var notes = new List<string>();
        notes.AddRange(availability.Notes ?? Array.Empty<string>());

        if (!availability.IsAvailable)
        {
            return new PowerScoreUsageSourceBreakdown(
                skill.Id,
                PowerScoreSourceKind.Skill,
                skill.Name,
                baseWeight,
                baseWeight,
                IsModeled: true,
                IsAvailable: false,
                Signals: Array.Empty<PowerScoreSourceSignal>(),
                Notes: Array.AsReadOnly(notes.ToArray()));
        }

        var resolvedSkill = _skillAscensionResolver.Resolve(skill, availability.ResolvedAscensionLevel);
        var signals = new List<PowerScoreSourceSignal>();
        var hitMultiplier = Math.Max(1, resolvedSkill.Tuning.MultiHit?.HitCount ?? 1);

        AddSkillCadenceSignals(signals, statSignals, resolvedSkill.Tuning.Cadence, baseWeight, resolvedSkill.Name);
        AddActionSignals(
            signals,
            statSignals,
            resolvedSkill.Tuning.Action,
            resolvedSkill.Tuning.Effects,
            sourceKind: PowerScoreSourceKind.Skill,
            sourceDisplayName: resolvedSkill.Name,
            sourceWeight: baseWeight,
            executionMultiplier: hitMultiplier,
            signalNote: hitMultiplier > 1
                ? $"{resolvedSkill.Name} is modeled as a {hitMultiplier}-hit skill, so its stat leverage reflects repeated executions."
                : null);

        foreach (var triggeredAction in resolvedSkill.Tuning.TriggeredActions ?? Array.Empty<SkillTriggeredActionDefinition>())
        {
            AddActionSignals(
                signals,
                statSignals,
                triggeredAction.Action,
                triggeredAction.Effects,
                sourceKind: PowerScoreSourceKind.SkillTriggeredAction,
                sourceDisplayName: $"{resolvedSkill.Name}:{triggeredAction.ActionKey}",
                sourceWeight: baseWeight * _policyCatalog.Tuning.TriggeredActionWeightScale,
                executionMultiplier: 1,
                signalNote: triggeredAction.Note);
        }

        foreach (var pendingDatum in resolvedSkill.PendingData ?? Array.Empty<SkillPendingDatum>())
        {
            notes.Add($"Pending skill data: {pendingDatum.Description}");
        }

        notes.AddRange(resolvedSkill.ResolutionNotes ?? Array.Empty<string>());
        notes.AddRange(resolvedSkill.SecurityNotes ?? Array.Empty<string>());

        return new PowerScoreUsageSourceBreakdown(
            resolvedSkill.Id,
            PowerScoreSourceKind.Skill,
            resolvedSkill.Name,
            baseWeight,
            baseWeight,
            IsModeled: true,
            IsAvailable: true,
            ResolvedAscensionLevel: resolvedSkill.AscensionLevel,
            Signals: Array.AsReadOnly(signals.ToArray()),
            Notes: Array.AsReadOnly(notes.ToArray()));
    }

    private Dictionary<StatType, PowerScoreClassFactorBreakdown> BuildStatFactors(
        IReadOnlyDictionary<StatType, decimal> statSignals,
        decimal coverageBlend)
    {
        var factors = new Dictionary<StatType, PowerScoreClassFactorBreakdown>();

        foreach (var policy in _policyCatalog.GetAll().Where(policy => policy.UsesClassContextWeight))
        {
            var signalTotal = statSignals.TryGetValue(policy.StatType, out var resolvedSignal)
                ? resolvedSignal
                : 0m;
            var signalRatio = policy.ReferenceSignal <= 0m
                ? 1m
                : Math.Clamp(signalTotal / policy.ReferenceSignal, 0m, 1m);
            var observedWeight = policy.MinimumClassWeight + ((policy.MaximumClassWeight - policy.MinimumClassWeight) * signalRatio);
            var finalWeight = 1m + ((observedWeight - 1m) * coverageBlend);
            var notes = new List<string>
            {
                signalTotal == 0m
                    ? "No modeled class source currently routes meaningful output through this stat."
                    : "Modeled class sources route output through this stat, so its contextual weight moved away from neutral."
            };

            factors[policy.StatType] = new PowerScoreClassFactorBreakdown(
                policy.StatType,
                signalTotal,
                policy.ReferenceSignal,
                signalRatio,
                coverageBlend,
                policy.MinimumClassWeight,
                policy.MaximumClassWeight,
                finalWeight,
                Array.AsReadOnly(notes.ToArray()));
        }

        return factors;
    }

    private void AddSkillCadenceSignals(
        IList<PowerScoreSourceSignal> sourceSignals,
        IDictionary<StatType, decimal> statSignals,
        SkillCadenceProfile cadence,
        decimal baseWeight,
        string sourceDisplayName)
    {
        if (cadence.BaseCooldownSeconds <= 0m)
        {
            return;
        }

        AddSignal(sourceSignals, statSignals, StatType.CooldownReduction, baseWeight, $"Skill '{sourceDisplayName}' contributes to skill-cadence valuation.");
        AddSignal(sourceSignals, statSignals, StatType.SkillRecoveryRate, baseWeight, $"Skill '{sourceDisplayName}' contributes to skill-cadence valuation.");
    }

    private void AddActionSignals(
        IList<PowerScoreSourceSignal> sourceSignals,
        IDictionary<StatType, decimal> statSignals,
        SkillActionDefinition action,
        IReadOnlyList<SkillConditionEffectDefinition>? effects,
        PowerScoreSourceKind sourceKind,
        string sourceDisplayName,
        decimal sourceWeight,
        int executionMultiplier,
        string? signalNote)
    {
        var scaledWeight = Math.Max(0m, sourceWeight) * Math.Max(1, executionMultiplier);
        var scalingIntensity = Math.Abs(action.MagnitudeProfile.ScalingCoefficient);
        var baseIntensity = scalingIntensity > 0m ? scalingIntensity * scaledWeight : scaledWeight;

        if (!string.IsNullOrWhiteSpace(signalNote))
        {
            sourceSignals.Add(new PowerScoreSourceSignal(
                StatType.SkillDamageIncrease,
                0m,
                signalNote));
        }

        AddScalingSignal(sourceSignals, statSignals, action.MagnitudeProfile.ScalingType, scalingIntensity * scaledWeight, $"{sourceDisplayName} scales with {action.MagnitudeProfile.ScalingType}.");

        // Dual scaling: si la skill escala de una stat secundaria, añadir su señal
        // (e.g., Sorcerer toma 10% de PhysicalAttack además de 90% de MagicAttack)
        var secondaryIntensity = Math.Abs(action.MagnitudeProfile.SecondaryScalingCoefficient);
        if (secondaryIntensity > 0m && action.MagnitudeProfile.SecondaryScalingType != SkillScalingType.FixedOnly)
        {
            AddScalingSignal(sourceSignals, statSignals, action.MagnitudeProfile.SecondaryScalingType, secondaryIntensity * scaledWeight, $"{sourceDisplayName} secondary scales with {action.MagnitudeProfile.SecondaryScalingType}.");
        }

        if (action.ActionType == SkillActionType.Damage)
        {
            if (action.CanCrit)
            {
                AddSignal(sourceSignals, statSignals, StatType.CritChance, baseIntensity, $"{sourceDisplayName} can crit.");
                AddSignal(sourceSignals, statSignals, StatType.CritDamage, baseIntensity, $"{sourceDisplayName} can crit.");
                AddSignal(sourceSignals, statSignals, StatType.CritDamageIncrease, baseIntensity, $"{sourceDisplayName} can crit.");
            }

            if (action.RequiresHitCheck)
            {
                AddSignal(sourceSignals, statSignals, StatType.Accuracy, baseIntensity, $"{sourceDisplayName} requires hit checks.");
            }

            switch (sourceKind)
            {
                case PowerScoreSourceKind.BasicAttack:
                    AddSignal(sourceSignals, statSignals, StatType.BasicAttackDamageIncrease, baseIntensity, $"{sourceDisplayName} routes damage through basic attacks.");
                    break;
                default:
                    AddSignal(sourceSignals, statSignals, StatType.SkillDamageIncrease, baseIntensity, $"{sourceDisplayName} routes damage through skills.");
                    break;
            }

            if (action.DamageType == SkillDamageType.Physical)
            {
                AddSignal(sourceSignals, statSignals, StatType.DefensePenetration, baseIntensity, $"{sourceDisplayName} deals physical damage.");
            }
            else if (action.DamageType == SkillDamageType.Magical)
            {
                AddSignal(sourceSignals, statSignals, StatType.MagicPenetration, baseIntensity, $"{sourceDisplayName} deals magical damage.");
            }
        }
        else if (action.ActionType == SkillActionType.Heal)
        {
            AddSignal(sourceSignals, statSignals, StatType.HealingEffectiveness, baseIntensity, $"{sourceDisplayName} produces healing output.");
        }

        foreach (var effect in effects ?? Array.Empty<SkillConditionEffectDefinition>())
        {
            var effectIntensity = Math.Max(0.50m, effect.BaseApplyChance ?? 1m) * scaledWeight;
            AddConditionSignals(sourceSignals, statSignals, effect.Condition, effectIntensity, $"{sourceDisplayName} can apply {effect.Condition}.");
            AddRequiredConditionSignals(sourceSignals, statSignals, effect.RequiredTargetConditions, effectIntensity * 0.50m, $"{sourceDisplayName} requires target setup to apply {effect.Condition}.");
        }

        foreach (var synergy in action.ConditionSynergies ?? Array.Empty<SkillConditionSynergyDefinition>())
        {
            var synergySignal = Math.Max(0.25m, synergy.MagnitudeMultiplier - 1m) * scaledWeight;
            if (synergy.FlatBaseMagnitudeBonus > 0m)
            {
                synergySignal += scaledWeight * 0.25m;
            }

            AddRequiredConditionSignals(sourceSignals, statSignals, [synergy.RequiredTargetCondition], synergySignal, $"{sourceDisplayName} gains extra output when the target is affected by {synergy.RequiredTargetCondition}.");
        }
    }

    private static decimal ResolveExpectedSourceWeight(ClassPowerScoreProfile classProfile, PowerScoreGlobalTuning tuning)
    {
        return (classProfile.ExpectedBasicSourceCount * tuning.BasicAttackSourceWeight)
               + (classProfile.ExpectedStandardSkillSourceCount * tuning.StandardSkillSourceWeight)
               + (classProfile.ExpectedUltimateSkillSourceCount * tuning.UltimateSkillSourceWeight);
    }

    private decimal ResolveSkillWeight(SkillDefinition skill, ClassPowerScoreProfile classProfile)
    {
        if (classProfile.SkillSourceWeightOverrides is not null &&
            classProfile.SkillSourceWeightOverrides.TryGetValue(skill.Id, out var explicitWeight))
        {
            return Math.Max(0m, explicitWeight);
        }

        return skill.IsUltimate
            ? _policyCatalog.Tuning.UltimateSkillSourceWeight
            : _policyCatalog.Tuning.StandardSkillSourceWeight;
    }

    private static void AddRequiredConditionSignals(
        IList<PowerScoreSourceSignal> sourceSignals,
        IDictionary<StatType, decimal> statSignals,
        IReadOnlyList<CombatConditionType>? requiredConditions,
        decimal signal,
        string reason)
    {
        if (signal <= 0m)
        {
            return;
        }

        foreach (var requiredCondition in requiredConditions ?? Array.Empty<CombatConditionType>())
        {
            var definition = CombatConditionCatalog.Get(requiredCondition);
            AddSignal(sourceSignals, statSignals, definition.ApplyChanceStat, signal, reason);

            if (TryGetConditionDamageIncreaseStat(requiredCondition, out var damageIncreaseStat))
            {
                AddSignal(sourceSignals, statSignals, damageIncreaseStat, signal * 0.50m, $"{reason} This also increases the value of damage bonuses tied to {requiredCondition}.");
            }
        }
    }
    private static void AddScalingSignal(
        IList<PowerScoreSourceSignal> sourceSignals,
        IDictionary<StatType, decimal> statSignals,
        SkillScalingType scalingType,
        decimal signal,
        string reason)
    {
        if (signal <= 0m)
        {
            return;
        }

        switch (scalingType)
        {
            case SkillScalingType.PhysicalAttack:
                AddSignal(sourceSignals, statSignals, StatType.PhysicalAttack, signal, reason);
                break;

            case SkillScalingType.MagicAttack:
                AddSignal(sourceSignals, statSignals, StatType.MagicAttack, signal, reason);
                break;
        }
    }

    private static void AddConditionSignals(
        IList<PowerScoreSourceSignal> sourceSignals,
        IDictionary<StatType, decimal> statSignals,
        CombatConditionType condition,
        decimal signal,
        string reason)
    {
        var conditionDefinition = CombatConditionCatalog.Get(condition);
        AddSignal(sourceSignals, statSignals, conditionDefinition.ApplyChanceStat, signal, reason);

        if (TryGetConditionDamageIncreaseStat(condition, out var damageIncreaseStat))
        {
            AddSignal(sourceSignals, statSignals, damageIncreaseStat, signal, $"{reason} This also increases the practical value of {damageIncreaseStat}.");
        }
    }

    private static bool TryGetConditionDamageIncreaseStat(CombatConditionType condition, out StatType statType)
    {
        switch (condition)
        {
            case CombatConditionType.Heat:
                statType = StatType.HeatDamageIncrease;
                return true;

            case CombatConditionType.Cold:
                statType = StatType.ColdDamageIncrease;
                return true;

            case CombatConditionType.Electrified:
                statType = StatType.ElectrifiedDamageIncrease;
                return true;

            case CombatConditionType.Poison:
                statType = StatType.PoisonDamageIncrease;
                return true;

            default:
                statType = default;
                return false;
        }
    }

    private static void AddSignal(
        IList<PowerScoreSourceSignal> sourceSignals,
        IDictionary<StatType, decimal> statSignals,
        StatType statType,
        decimal signal,
        string reason)
    {
        if (signal <= 0m)
        {
            return;
        }

        sourceSignals.Add(new PowerScoreSourceSignal(statType, signal, reason));
        statSignals[statType] = statSignals.TryGetValue(statType, out var currentSignal)
            ? currentSignal + signal
            : signal;
    }
}



