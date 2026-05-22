namespace War.Core.Combat;

public interface ICombatConditionInteractionService
{
    CombatConditionInteractionEvaluation Evaluate(
        IReadOnlyCollection<CombatConditionType>? existingTargetConditions,
        IReadOnlyCollection<CombatConditionResolution>? directConditionResults);
}

public sealed class CombatConditionInteractionService : ICombatConditionInteractionService
{
    public CombatConditionInteractionEvaluation Evaluate(
        IReadOnlyCollection<CombatConditionType>? existingTargetConditions,
        IReadOnlyCollection<CombatConditionResolution>? directConditionResults)
    {
        var activeConditions = new HashSet<CombatConditionType>(existingTargetConditions ?? Array.Empty<CombatConditionType>());
        var directAppliedConditions = (directConditionResults ?? Array.Empty<CombatConditionResolution>())
            .Where(result =>
                result.Source == CombatConditionApplicationSource.DirectEffect &&
                result.WasApplied)
            .Select(result => result.Condition)
            .ToHashSet();

        if (directAppliedConditions.Count == 0)
        {
            return new CombatConditionInteractionEvaluation(
                Array.Empty<CombatConditionInteractionActivation>(),
                Array.Empty<CombatConditionResolution>(),
                0m,
                Array.Empty<string>());
        }

        var availableAfterApplication = new HashSet<CombatConditionType>(activeConditions);
        availableAfterApplication.UnionWith(directAppliedConditions);

        var activations = new List<CombatConditionInteractionActivation>();
        var generatedConditions = new List<CombatConditionResolution>();
        var notes = new List<string>();

        foreach (var rule in CombatConditionInteractionCatalog.GetDefinedRules())
        {
            if (!rule.RequiredConditions.All(availableAfterApplication.Contains) ||
                !rule.RequiredConditions.Any(directAppliedConditions.Contains))
            {
                continue;
            }

            var activation = new CombatConditionInteractionActivation(
                rule.Key,
                Array.AsReadOnly(rule.RequiredConditions.ToArray()),
                rule.FinalDamageIncreasePercentage ?? 0m,
                rule.AdditionalConditionToApply,
                rule.Description,
                rule.FutureRuleNote);

            activations.Add(activation);

            if (rule.AdditionalConditionToApply is not { } extraCondition)
            {
                continue;
            }

            if (availableAfterApplication.Contains(extraCondition))
            {
                notes.Add($"Interaction '{rule.Key}' recognized {extraCondition} as already present, so no extra condition entry was generated.");
                continue;
            }

            var conditionDefinition = CombatConditionCatalog.Get(extraCondition);
            generatedConditions.Add(new CombatConditionResolution(
                extraCondition,
                conditionDefinition.Category,
                CombatConditionApplicationStatus.AppliedByInteraction,
                CombatConditionApplicationSource.Interaction,
                Note: conditionDefinition.DurationAffectedByTenacity
                    ? $"Applied by interaction '{rule.Key}' without a defined base duration. Runtime duration remains unresolved."
                    : $"Applied by interaction '{rule.Key}'."));
            availableAfterApplication.Add(extraCondition);
        }

        return new CombatConditionInteractionEvaluation(
            Array.AsReadOnly(activations.ToArray()),
            Array.AsReadOnly(generatedConditions.ToArray()),
            activations.Sum(activation => activation.FinalDamageIncreasePercentage),
            Array.AsReadOnly(notes.ToArray()));
    }
}
