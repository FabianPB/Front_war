using System.Collections.ObjectModel;
using War.Core.Stats;

namespace War.Core.Resources;

public static class CharacterResourceCatalog
{
    private static readonly IReadOnlyDictionary<CharacterResourceType, CharacterResourceDefinition> Definitions =
        new ReadOnlyDictionary<CharacterResourceType, CharacterResourceDefinition>(
            Enum.GetValues<CharacterResourceType>().ToDictionary(resourceType => resourceType, CreateDefinition));

    private static readonly IReadOnlyCollection<CharacterResourceDefinition> AllDefinitions = Definitions.Values.ToArray();

    public static CharacterResourceDefinition Get(CharacterResourceType resourceType)
    {
        return Definitions.TryGetValue(resourceType, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, "Unknown character resource type.");
    }

    public static IReadOnlyCollection<CharacterResourceDefinition> GetAll()
    {
        return AllDefinitions;
    }

    private static CharacterResourceDefinition CreateDefinition(CharacterResourceType resourceType)
    {
        return resourceType switch
        {
            CharacterResourceType.Hp => new CharacterResourceDefinition(
                resourceType,
                "Current health persisted at runtime and modified by damage and healing.",
                StatValueKind.RuntimeResource,
                StatValueScale.RawDecimal,
                StatMeasurementUnit.ResourcePoints,
                StatType.MaxHp,
                SystemQueryStage.ResourceValidation | SystemQueryStage.DamageApplication | SystemQueryStage.HealingApplication | SystemQueryStage.Persistence,
                CharacterResourceConstraint.NonNegative | CharacterResourceConstraint.ClampToMaximumOnGain | CharacterResourceConstraint.ResolveDepletionAtZero | CharacterResourceConstraint.PersistentRuntime,
                "Future combat will clamp healing against MaxHp and treat zero as the death threshold."),

            CharacterResourceType.Mana => new CharacterResourceDefinition(
                resourceType,
                "Current mana persisted at runtime and modified by spending and restoration.",
                StatValueKind.RuntimeResource,
                StatValueScale.RawDecimal,
                StatMeasurementUnit.ResourcePoints,
                StatType.MaxMana,
                SystemQueryStage.ResourceValidation | SystemQueryStage.ResourceConsumption | SystemQueryStage.ResourceRecovery | SystemQueryStage.Persistence,
                CharacterResourceConstraint.NonNegative | CharacterResourceConstraint.ClampToMaximumOnGain | CharacterResourceConstraint.RejectSpendWhenInsufficient | CharacterResourceConstraint.PersistentRuntime,
                "Future skill execution must refuse costs that exceed CurrentMana and clamp restoration against MaxMana."),

            CharacterResourceType.UltimateCharge => new CharacterResourceDefinition(
                resourceType,
                "Current ultimate charge persisted at runtime and modified by future gain/spend rules.",
                StatValueKind.RuntimeResource,
                StatValueScale.RawDecimal,
                StatMeasurementUnit.ResourcePoints,
                StatType.UltimateChargeMax,
                SystemQueryStage.ResourceValidation | SystemQueryStage.ResourceConsumption | SystemQueryStage.ResourceRecovery | SystemQueryStage.Persistence,
                CharacterResourceConstraint.NonNegative | CharacterResourceConstraint.ClampToMaximumOnGain | CharacterResourceConstraint.RejectSpendWhenInsufficient | CharacterResourceConstraint.PersistentRuntime,
                "Future ultimate systems must clamp charge against UltimateChargeMax and refuse spends that exceed the current charge."),

            _ => throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, "Unknown character resource type.")
        };
    }
}
