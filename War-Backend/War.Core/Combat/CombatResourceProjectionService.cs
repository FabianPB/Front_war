using War.Core.Entities;
using War.Core.Resources;

namespace War.Core.Combat;

public interface ICombatResourceProjectionService
{
    CombatResourceChangeProposal Project(
        Character character,
        CombatEntityRole affectedEntity,
        CharacterResourceType resourceType,
        decimal delta,
        CombatResourceChangeReason reason);
}

public sealed class CombatResourceProjectionService : ICombatResourceProjectionService
{
    public CombatResourceChangeProposal Project(
        Character character,
        CombatEntityRole affectedEntity,
        CharacterResourceType resourceType,
        decimal delta,
        CombatResourceChangeReason reason)
    {
        ArgumentNullException.ThrowIfNull(character);

        var definition = resourceType.GetDefinition();
        var constraints = definition.Constraints;
        var previousValue = character.GetCurrentResource(resourceType);
        var maximumValue = character.GetResourceMaximum(resourceType);
        var unclampedResult = previousValue + delta;
        var proposedResult = unclampedResult;
        var wasClampedToZero = false;
        var wasClampedToMaximum = false;

        if (constraints.HasFlag(CharacterResourceConstraint.NonNegative) && proposedResult < 0m)
        {
            proposedResult = 0m;
            wasClampedToZero = true;
        }

        if (delta > 0m && constraints.HasFlag(CharacterResourceConstraint.ClampToMaximumOnGain) && proposedResult > maximumValue)
        {
            proposedResult = maximumValue;
            wasClampedToMaximum = true;
        }

        var wouldTriggerDepletionResolution =
            constraints.HasFlag(CharacterResourceConstraint.ResolveDepletionAtZero) && proposedResult <= 0m;

        return new CombatResourceChangeProposal(
            affectedEntity,
            resourceType,
            reason,
            previousValue,
            delta,
            unclampedResult,
            proposedResult,
            maximumValue,
            wasClampedToZero,
            wasClampedToMaximum,
            wouldTriggerDepletionResolution);
    }
}
