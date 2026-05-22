namespace War.Core.Skills;

public sealed record SkillValidationIssue(
    string Code,
    string Message,
    string? SkillId = null,
    ClassType? ClassType = null,
    SkillSlot? Slot = null);

public sealed class ClassSkillCatalog
{
    public ClassSkillCatalog(ClassType classType, IEnumerable<SkillDefinition>? skills = null)
    {
        ClassType = classType;
        Skills = Array.AsReadOnly((skills ?? Array.Empty<SkillDefinition>())
            .OrderBy(skill => skill.Slot.GetOrder())
            .ToArray());
    }

    public ClassType ClassType { get; }

    public IReadOnlyList<SkillDefinition> Skills { get; }

    public bool IsComplete => Skills.Count == SkillCatalogRules.SkillsPerClass;

    public IReadOnlyList<SkillValidationIssue> Validate(bool requireFullKit = false)
    {
        var issues = new List<SkillValidationIssue>();

        foreach (var skill in Skills)
        {
            issues.AddRange(SkillDefinitionValidator.Validate(skill));

            if (skill.ClassType != ClassType)
            {
                issues.Add(new SkillValidationIssue(
                    "class-skill-mismatch",
                    $"Skill '{skill.Id}' belongs to {skill.ClassType}, but it was registered under {ClassType}.",
                    skill.Id,
                    ClassType,
                    skill.Slot));
            }
        }

        foreach (var duplicateId in Skills
                     .GroupBy(skill => skill.Id, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            issues.Add(new SkillValidationIssue(
                "duplicate-skill-id",
                $"Class catalog for {ClassType} contains duplicate skill id '{duplicateId}'.",
                duplicateId,
                ClassType));
        }

        foreach (var duplicateSlot in Skills
                     .GroupBy(skill => skill.Slot)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            issues.Add(new SkillValidationIssue(
                "duplicate-skill-slot",
                $"Class catalog for {ClassType} contains more than one skill in slot {duplicateSlot}.",
                ClassType: ClassType,
                Slot: duplicateSlot));
        }

        var ultimateCount = Skills.Count(skill => skill.IsUltimate);

        if (ultimateCount > 1)
        {
            issues.Add(new SkillValidationIssue(
                "multiple-ultimates",
                $"Class catalog for {ClassType} contains {ultimateCount} skills marked as ultimate.",
                ClassType: ClassType));
        }

        if (requireFullKit)
        {
            if (Skills.Count != SkillCatalogRules.SkillsPerClass)
            {
                issues.Add(new SkillValidationIssue(
                    "class-kit-size",
                    $"Class catalog for {ClassType} must contain exactly {SkillCatalogRules.SkillsPerClass} skills, but it currently contains {Skills.Count}.",
                    ClassType: ClassType));
            }

            if (ultimateCount != 1)
            {
                issues.Add(new SkillValidationIssue(
                    "missing-ultimate",
                    $"Class catalog for {ClassType} must contain exactly one ultimate skill.",
                    ClassType: ClassType));
            }
        }

        return Array.AsReadOnly(issues.ToArray());
    }
}

public sealed class SkillCatalog
{
    public SkillCatalog(IEnumerable<ClassSkillCatalog>? classCatalogs = null)
    {
        ClassCatalogs = Array.AsReadOnly((classCatalogs ?? Array.Empty<ClassSkillCatalog>())
            .OrderBy(catalog => catalog.ClassType)
            .ToArray());
    }

    public IReadOnlyList<ClassSkillCatalog> ClassCatalogs { get; }

    public static SkillCatalog FromDefinitions(IEnumerable<SkillDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var grouped = definitions
            .GroupBy(definition => definition.ClassType)
            .ToDictionary(
                group => group.Key,
                group => new ClassSkillCatalog(group.Key, group));

        var catalogs = SkillCatalogRules.InitialClasses
            .Select(classType => grouped.TryGetValue(classType, out var catalog)
                ? catalog
                : new ClassSkillCatalog(classType))
            .ToArray();

        return new SkillCatalog(catalogs);
    }

    public IReadOnlyList<SkillValidationIssue> Validate(bool requireFullKit = false)
    {
        var issues = new List<SkillValidationIssue>();

        foreach (var catalog in ClassCatalogs)
        {
            issues.AddRange(catalog.Validate(requireFullKit));
        }

        foreach (var duplicateClassType in ClassCatalogs
                     .GroupBy(catalog => catalog.ClassType)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            issues.Add(new SkillValidationIssue(
                "duplicate-class-catalog",
                $"Skill catalog contains multiple entries for class {duplicateClassType}.",
                ClassType: duplicateClassType));
        }

        if (requireFullKit)
        {
            foreach (var requiredClassType in SkillCatalogRules.InitialClasses)
            {
                if (ClassCatalogs.All(catalog => catalog.ClassType != requiredClassType))
                {
                    issues.Add(new SkillValidationIssue(
                        "missing-class-catalog",
                        $"Skill catalog is missing the class catalog for {requiredClassType}.",
                        ClassType: requiredClassType));
                }
            }
        }

        return Array.AsReadOnly(issues.ToArray());
    }
}

public static class SkillDefinitionValidator
{
    public static IReadOnlyList<SkillValidationIssue> Validate(SkillDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var issues = new List<SkillValidationIssue>();
        var skillId = definition.Id;

        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            issues.Add(new SkillValidationIssue(
                "missing-skill-id",
                "Skill definitions require a non-empty id.",
                ClassType: definition.ClassType,
                Slot: definition.Slot));
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            issues.Add(new SkillValidationIssue(
                "missing-skill-name",
                $"Skill '{definition.Id}' requires a non-empty name.",
                skillId,
                definition.ClassType,
                definition.Slot));
        }

        if (string.IsNullOrWhiteSpace(definition.Description))
        {
            issues.Add(new SkillValidationIssue(
                "missing-skill-description",
                $"Skill '{definition.Id}' requires a non-empty description.",
                skillId,
                definition.ClassType,
                definition.Slot));
        }

        if (definition.UnlockLevel < SkillCatalogRules.MinimumCharacterLevel)
        {
            issues.Add(new SkillValidationIssue(
                "invalid-unlock-level",
                $"Skill '{definition.Id}' must unlock at character level {SkillCatalogRules.MinimumCharacterLevel} or higher.",
                skillId,
                definition.ClassType,
                definition.Slot));
        }

        ValidateTuning(definition.BaseTuning, definition, issues, "base");

        var availableEffectKeys = new HashSet<string>(
            (definition.BaseTuning.Effects ?? Array.Empty<SkillConditionEffectDefinition>())
                .Select(effect => effect.EffectKey),
            StringComparer.OrdinalIgnoreCase);

        var overrides = definition.AscensionOverrides ?? EmptyOverrides;

        foreach (var overrideEntry in overrides.OrderBy(entry => entry.Key))
        {
            var ascensionLevel = overrideEntry.Key;
            var ascensionOverride = overrideEntry.Value;

            if (ascensionLevel <= SkillCatalogRules.MinimumAscensionLevel ||
                ascensionLevel > SkillCatalogRules.MaximumAscensionLevel)
            {
                issues.Add(new SkillValidationIssue(
                    "invalid-ascension-level",
                    $"Skill '{definition.Id}' contains an ascension override for level {ascensionLevel}, but supported levels are 2 to {SkillCatalogRules.MaximumAscensionLevel}.",
                    skillId,
                    definition.ClassType,
                    definition.Slot));
            }

            if (ascensionOverride.AscensionLevel != ascensionLevel)
            {
                issues.Add(new SkillValidationIssue(
                    "ascension-level-mismatch",
                    $"Skill '{definition.Id}' registered an ascension override at key {ascensionLevel}, but the payload declares level {ascensionOverride.AscensionLevel}.",
                    skillId,
                    definition.ClassType,
                    definition.Slot));
            }

            if (ascensionOverride.MagnitudeProfile is not null)
            {
                ValidateMagnitude(ascensionOverride.MagnitudeProfile, definition, issues, $"ascension-{ascensionLevel}");
            }

            if (ascensionOverride.Action is not null)
            {
                ValidateAction(ascensionOverride.Action, definition, issues, $"ascension-{ascensionLevel}");
            }

            if (ascensionOverride.Targeting is not null)
            {
                ValidateTargeting(ascensionOverride.Targeting, definition, issues, $"ascension-{ascensionLevel}");
            }

            if (ascensionOverride.Cadence is not null)
            {
                ValidateCadence(ascensionOverride.Cadence, definition, issues, $"ascension-{ascensionLevel}");
            }

            if (ascensionOverride.ResourceCosts is not null)
            {
                ValidateResourceCosts(ascensionOverride.ResourceCosts, definition, issues, $"ascension-{ascensionLevel}");
            }

            if (ascensionOverride.MultiHit is not null)
            {
                ValidateMultiHit(ascensionOverride.MultiHit, definition, issues, $"ascension-{ascensionLevel}");
            }

            if (ascensionOverride.CastProtections is not null)
            {
                ValidateProtections(ascensionOverride.CastProtections, definition, issues, $"ascension-{ascensionLevel}");
            }

            if (ascensionOverride.TriggeredActions is not null)
            {
                ValidateTriggeredActions(ascensionOverride.TriggeredActions, definition, issues, $"ascension-{ascensionLevel}");
            }

            ValidateAscensionEffects(
                definition,
                ascensionOverride.EffectOverrides,
                ascensionOverride.AddedEffects,
                ascensionOverride.RemovedEffectKeys,
                availableEffectKeys,
                issues,
                $"ascension-{ascensionLevel}");
        }

        return Array.AsReadOnly(issues.ToArray());
    }

    private static void ValidateTuning(
        SkillTuningSnapshot tuning,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        ValidateAction(tuning.Action, definition, issues, phase);
        ValidateTargeting(tuning.Targeting, definition, issues, phase);
        ValidateCadence(tuning.Cadence, definition, issues, phase);
        ValidateResourceCosts(tuning.ResourceCosts ?? Array.Empty<SkillResourceCostDefinition>(), definition, issues, phase);
        ValidateBaseEffects(tuning.Effects ?? Array.Empty<SkillConditionEffectDefinition>(), definition, issues, phase);
        if (tuning.MultiHit is not null)
        {
            ValidateMultiHit(tuning.MultiHit, definition, issues, phase);
        }

        ValidateProtections(tuning.CastProtections ?? Array.Empty<SkillProtectionGrantDefinition>(), definition, issues, phase);
        ValidateTriggeredActions(tuning.TriggeredActions ?? Array.Empty<SkillTriggeredActionDefinition>(), definition, issues, phase);
    }

    private static void ValidateAction(
        SkillActionDefinition action,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        ValidateMagnitude(action.MagnitudeProfile, definition, issues, phase);

        if (action.ActionType == SkillActionType.Damage && action.DamageType is null)
        {
            issues.Add(new SkillValidationIssue(
                "missing-damage-type",
                $"Skill '{definition.Id}' declares damage action data in {phase}, but no damage type was supplied.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        if (action.ActionType != SkillActionType.Damage && action.DamageType is not null)
        {
            issues.Add(new SkillValidationIssue(
                "unexpected-damage-type",
                $"Skill '{definition.Id}' supplied a damage type in {phase}, but only damage actions should define one.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        foreach (var duplicateSynergyKey in (action.ConditionSynergies ?? Array.Empty<SkillConditionSynergyDefinition>())
                     .GroupBy(synergy => synergy.SynergyKey, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            issues.Add(new SkillValidationIssue(
                "duplicate-condition-synergy-key",
                $"Skill '{definition.Id}' contains duplicate condition synergy key '{duplicateSynergyKey}' in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        foreach (var synergy in action.ConditionSynergies ?? Array.Empty<SkillConditionSynergyDefinition>())
        {
            if (string.IsNullOrWhiteSpace(synergy.SynergyKey))
            {
                issues.Add(new SkillValidationIssue(
                    "missing-condition-synergy-key",
                    $"Skill '{definition.Id}' contains a condition synergy without a key in {phase}.",
                    definition.Id,
                    definition.ClassType,
                    definition.Slot));
            }

            if (synergy.MagnitudeMultiplier < 0m)
            {
                issues.Add(new SkillValidationIssue(
                    "negative-condition-synergy-multiplier",
                    $"Skill '{definition.Id}' contains a negative magnitude multiplier for synergy '{synergy.SynergyKey}' in {phase}.",
                    definition.Id,
                    definition.ClassType,
                    definition.Slot));
            }
        }
    }

    private static void ValidateMagnitude(
        SkillMagnitudeProfile magnitude,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        if (magnitude.BaseMagnitude < 0m)
        {
            issues.Add(new SkillValidationIssue(
                "negative-base-magnitude",
                $"Skill '{definition.Id}' has a negative base magnitude in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        if (magnitude.ScalingCoefficient < 0m)
        {
            issues.Add(new SkillValidationIssue(
                "negative-scaling-coefficient",
                $"Skill '{definition.Id}' has a negative scaling coefficient in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        if (magnitude.ScalingType == SkillScalingType.FixedOnly && magnitude.ScalingCoefficient > 0m)
        {
            issues.Add(new SkillValidationIssue(
                "fixed-scaling-mismatch",
                $"Skill '{definition.Id}' uses FixedOnly scaling in {phase} but also declares a positive scaling coefficient.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }
    }

    private static void ValidateTargeting(
        SkillTargetingProfile targeting,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        if (targeting.BaseRangeUnits < 0m)
        {
            issues.Add(new SkillValidationIssue(
                "negative-range",
                $"Skill '{definition.Id}' has a negative base range in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        if (targeting.AreaRadiusUnits < 0m)
        {
            issues.Add(new SkillValidationIssue(
                "negative-area-radius",
                $"Skill '{definition.Id}' has a negative area radius in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        if (targeting.MaxTargets <= 0)
        {
            issues.Add(new SkillValidationIssue(
                "invalid-max-targets",
                $"Skill '{definition.Id}' must target at least one entity in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }
    }

    private static void ValidateCadence(
        SkillCadenceProfile cadence,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        if (cadence.BaseCooldownSeconds < 0m)
        {
            issues.Add(new SkillValidationIssue(
                "negative-cooldown",
                $"Skill '{definition.Id}' has a negative base cooldown in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }
    }

    private static void ValidateResourceCosts(
        IEnumerable<SkillResourceCostDefinition> resourceCosts,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        foreach (var cost in resourceCosts)
        {
            if (cost.Amount < 0m)
            {
                issues.Add(new SkillValidationIssue(
                    "negative-resource-cost",
                    $"Skill '{definition.Id}' has a negative resource cost for {cost.ResourceType} in {phase}.",
                    definition.Id,
                    definition.ClassType,
                    definition.Slot));
            }
        }
    }

    private static void ValidateBaseEffects(
        IReadOnlyList<SkillConditionEffectDefinition> effects,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        foreach (var duplicateKey in effects
                     .GroupBy(effect => effect.EffectKey, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            issues.Add(new SkillValidationIssue(
                "duplicate-effect-key",
                $"Skill '{definition.Id}' contains duplicate effect key '{duplicateKey}' in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        foreach (var effect in effects)
        {
            ValidateEffect(effect, definition, issues, phase);
        }
    }

    private static void ValidateAscensionEffects(
        SkillDefinition definition,
        IReadOnlyList<SkillConditionEffectOverride>? effectOverrides,
        IReadOnlyList<SkillConditionEffectDefinition>? addedEffects,
        IReadOnlyList<string>? removedEffectKeys,
        HashSet<string> availableEffectKeys,
        List<SkillValidationIssue> issues,
        string phase)
    {
        foreach (var removedEffectKey in removedEffectKeys ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(removedEffectKey))
            {
                issues.Add(new SkillValidationIssue(
                    "empty-removed-effect-key",
                    $"Skill '{definition.Id}' contains an empty removed effect key in {phase}.",
                    definition.Id,
                    definition.ClassType,
                    definition.Slot));
                continue;
            }

            if (!availableEffectKeys.Contains(removedEffectKey))
            {
                issues.Add(new SkillValidationIssue(
                    "unknown-removed-effect-key",
                    $"Skill '{definition.Id}' tries to remove unknown effect '{removedEffectKey}' in {phase}.",
                    definition.Id,
                    definition.ClassType,
                    definition.Slot));
                continue;
            }

            availableEffectKeys.Remove(removedEffectKey);
        }

        foreach (var effectOverride in effectOverrides ?? Array.Empty<SkillConditionEffectOverride>())
        {
            if (string.IsNullOrWhiteSpace(effectOverride.EffectKey))
            {
                issues.Add(new SkillValidationIssue(
                    "missing-effect-key",
                    $"Skill '{definition.Id}' contains an effect override without an effect key in {phase}.",
                    definition.Id,
                    definition.ClassType,
                    definition.Slot));
                continue;
            }

            if (!availableEffectKeys.Contains(effectOverride.EffectKey))
            {
                issues.Add(new SkillValidationIssue(
                    "unknown-effect-override",
                    $"Skill '{definition.Id}' tries to override effect '{effectOverride.EffectKey}' in {phase}, but the effect is not available at that ascension stage.",
                    definition.Id,
                    definition.ClassType,
                    definition.Slot));
            }

            ValidateEffectOverride(effectOverride, definition, issues, phase);
        }

        foreach (var duplicateAddedKey in (addedEffects ?? Array.Empty<SkillConditionEffectDefinition>())
                     .GroupBy(effect => effect.EffectKey, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            issues.Add(new SkillValidationIssue(
                "duplicate-effect-key",
                $"Skill '{definition.Id}' contains duplicate added effect key '{duplicateAddedKey}' in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        foreach (var addedEffect in addedEffects ?? Array.Empty<SkillConditionEffectDefinition>())
        {
            ValidateEffect(addedEffect, definition, issues, phase);

            if (!string.IsNullOrWhiteSpace(addedEffect.EffectKey) &&
                !availableEffectKeys.Add(addedEffect.EffectKey))
            {
                issues.Add(new SkillValidationIssue(
                    "duplicate-effect-key",
                    $"Skill '{definition.Id}' tries to add effect '{addedEffect.EffectKey}' in {phase}, but that effect key is already available.",
                    definition.Id,
                    definition.ClassType,
                    definition.Slot));
            }
        }
    }

    private static void ValidateEffect(
        SkillConditionEffectDefinition effect,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        if (string.IsNullOrWhiteSpace(effect.EffectKey))
        {
            issues.Add(new SkillValidationIssue(
                "missing-effect-key",
                $"Skill '{definition.Id}' contains an effect without an effect key in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        if (effect.BaseDurationSeconds < 0m)
        {
            issues.Add(new SkillValidationIssue(
                "negative-effect-duration",
                $"Skill '{definition.Id}' contains a negative effect duration for '{effect.EffectKey}' in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        if (effect.ApplyChanceMultiplier < 0m)
        {
            issues.Add(new SkillValidationIssue(
                "negative-effect-chance-multiplier",
                $"Skill '{definition.Id}' contains a negative apply chance multiplier for '{effect.EffectKey}' in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }
    }

    private static void ValidateEffectOverride(
        SkillConditionEffectOverride effectOverride,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        if (effectOverride.BaseDurationSeconds < 0m)
        {
            issues.Add(new SkillValidationIssue(
                "negative-effect-duration",
                $"Skill '{definition.Id}' contains a negative effect duration override for '{effectOverride.EffectKey}' in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        if (effectOverride.ApplyChanceMultiplier < 0m)
        {
            issues.Add(new SkillValidationIssue(
                "negative-effect-chance-multiplier",
                $"Skill '{definition.Id}' contains a negative apply chance multiplier override for '{effectOverride.EffectKey}' in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }
    }

    private static void ValidateMultiHit(
        SkillMultiHitProfile multiHit,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        if (multiHit.HitCount <= 0)
        {
            issues.Add(new SkillValidationIssue(
                "invalid-hit-count",
                $"Skill '{definition.Id}' must declare a hit count greater than zero in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        if (multiHit.ActiveDurationSeconds < 0m)
        {
            issues.Add(new SkillValidationIssue(
                "negative-active-duration",
                $"Skill '{definition.Id}' has a negative active duration in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }
    }

    private static void ValidateProtections(
        IEnumerable<SkillProtectionGrantDefinition> protections,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        foreach (var duplicateGrantKey in protections
                     .GroupBy(protection => protection.GrantKey, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            issues.Add(new SkillValidationIssue(
                "duplicate-protection-key",
                $"Skill '{definition.Id}' contains duplicate protection key '{duplicateGrantKey}' in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        foreach (var protection in protections)
        {
            if (string.IsNullOrWhiteSpace(protection.GrantKey))
            {
                issues.Add(new SkillValidationIssue(
                    "missing-protection-key",
                    $"Skill '{definition.Id}' contains a cast protection without a key in {phase}.",
                    definition.Id,
                    definition.ClassType,
                    definition.Slot));
            }

            if (protection.BaseDurationSeconds <= 0m)
            {
                issues.Add(new SkillValidationIssue(
                    "invalid-protection-duration",
                    $"Skill '{definition.Id}' contains non-positive protection duration for '{protection.GrantKey}' in {phase}.",
                    definition.Id,
                    definition.ClassType,
                    definition.Slot));
            }
        }
    }

    private static void ValidateTriggeredActions(
        IEnumerable<SkillTriggeredActionDefinition> triggeredActions,
        SkillDefinition definition,
        List<SkillValidationIssue> issues,
        string phase)
    {
        foreach (var duplicateActionKey in triggeredActions
                     .GroupBy(action => action.ActionKey, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            issues.Add(new SkillValidationIssue(
                "duplicate-triggered-action-key",
                $"Skill '{definition.Id}' contains duplicate triggered action key '{duplicateActionKey}' in {phase}.",
                definition.Id,
                definition.ClassType,
                definition.Slot));
        }

        foreach (var triggeredAction in triggeredActions)
        {
            if (string.IsNullOrWhiteSpace(triggeredAction.ActionKey))
            {
                issues.Add(new SkillValidationIssue(
                    "missing-triggered-action-key",
                    $"Skill '{definition.Id}' contains a triggered action without a key in {phase}.",
                    definition.Id,
                    definition.ClassType,
                    definition.Slot));
            }

            ValidateAction(triggeredAction.Action, definition, issues, $"{phase}:{triggeredAction.ActionKey}");
            ValidateBaseEffects(triggeredAction.Effects ?? Array.Empty<SkillConditionEffectDefinition>(), definition, issues, $"{phase}:{triggeredAction.ActionKey}");
        }
    }

    private static readonly IReadOnlyDictionary<int, SkillAscensionOverrides> EmptyOverrides =
        new Dictionary<int, SkillAscensionOverrides>();
}
