using War.Core.Resources;

namespace War.Core.Combat;

public interface ICombatActionResourceService
{
    CombatActionResourceEvaluation Evaluate(CombatEventContext context);
}

public sealed class CombatActionResourceService : ICombatActionResourceService
{
    private readonly ICombatResourceProjectionService _resourceProjectionService;

    public CombatActionResourceService(ICombatResourceProjectionService? resourceProjectionService = null)
    {
        _resourceProjectionService = resourceProjectionService ?? new CombatResourceProjectionService();
    }

    public CombatActionResourceEvaluation Evaluate(CombatEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var declaredCosts = context.DeclaredResourceCosts ?? Array.Empty<CombatResourceCost>();
        var notes = new List<string>();

        if (declaredCosts.Count == 0)
        {
            return CombatActionResourceEvaluation.Empty;
        }

        var normalizedCosts = NormalizeCosts(declaredCosts, notes);

        if (normalizedCosts.Count == 0)
        {
            notes.Add("Declared resource costs did not contain any positive amounts, so no spend validation was required.");
            return new CombatActionResourceEvaluation(declaredCosts, Array.Empty<CombatActionResourceCostResolution>(), false, notes);
        }

        var resolutions = new List<CombatActionResourceCostResolution>();
        var wasAbortedByInsufficientResources = false;

        foreach (var cost in normalizedCosts)
        {
            var definition = cost.ResourceType.GetDefinition();
            var availableAmount = context.Actor.GetCurrentResource(cost.ResourceType);
            var resourceDefinitionRejectsInsufficientSpend =
                definition.Constraints.HasFlag(CharacterResourceConstraint.RejectSpendWhenInsufficient);

            if (availableAmount < cost.Amount)
            {
                var rejectedResolution = new CombatActionResourceCostResolution(
                    cost.ResourceType,
                    cost.Amount,
                    availableAmount,
                    cost.AbortIfInsufficient,
                    resourceDefinitionRejectsInsufficientSpend,
                    CombatActionResourceCostStatus.RejectedInsufficientResource,
                    Note: $"Action required {cost.Amount} {cost.ResourceType}, but only {availableAmount} was available.");

                resolutions.Add(rejectedResolution);
                wasAbortedByInsufficientResources |= rejectedResolution.CausesActionAbort;
                continue;
            }

            var projectedSpend = _resourceProjectionService.Project(
                context.Actor,
                CombatEntityRole.Actor,
                cost.ResourceType,
                -cost.Amount,
                CombatResourceChangeReason.ResourceCost);

            resolutions.Add(new CombatActionResourceCostResolution(
                cost.ResourceType,
                cost.Amount,
                availableAmount,
                cost.AbortIfInsufficient,
                resourceDefinitionRejectsInsufficientSpend,
                CombatActionResourceCostStatus.Approved,
                projectedSpend,
                $"Projected spend for {cost.ResourceType} was approved by resource validation."));
        }

        if (wasAbortedByInsufficientResources)
        {
            notes.Add("Action resource validation aborted the combat event before hit, damage, healing, or condition resolution.");
        }

        return new CombatActionResourceEvaluation(declaredCosts, resolutions, wasAbortedByInsufficientResources, notes);
    }

    private static IReadOnlyList<CombatResourceCost> NormalizeCosts(
        IReadOnlyCollection<CombatResourceCost> declaredCosts,
        List<string> notes)
    {
        var positiveCosts = declaredCosts
            .Where(cost => cost.Amount > 0m)
            .ToArray();

        var zeroValuedCosts = declaredCosts.Count - positiveCosts.Length;
        if (zeroValuedCosts > 0)
        {
            notes.Add("Zero-valued resource costs were ignored during action-cost validation.");
        }

        var groupedCosts = positiveCosts
            .GroupBy(cost => cost.ResourceType)
            .ToArray();

        if (groupedCosts.Any(group => group.Count() > 1))
        {
            notes.Add("Duplicate resource-cost entries were aggregated by resource type before validation.");
        }

        return Array.AsReadOnly(groupedCosts
            .Select(group => new CombatResourceCost(
                group.Key,
                group.Sum(cost => cost.Amount),
                group.Any(cost => cost.AbortIfInsufficient)))
            .ToArray());
    }
}
