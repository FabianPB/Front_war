using War.Core.Stats;

namespace War.Core.Resources;

[Flags]
public enum CharacterResourceConstraint
{
    None = 0,
    NonNegative = 1 << 0,
    ClampToMaximumOnGain = 1 << 1,
    RejectSpendWhenInsufficient = 1 << 2,
    ResolveDepletionAtZero = 1 << 3,
    PersistentRuntime = 1 << 4
}

public sealed record CharacterResourceDefinition(
    CharacterResourceType Type,
    string Description,
    StatValueKind ValueKind,
    StatValueScale ValueScale,
    StatMeasurementUnit MeasurementUnit,
    StatType MaximumStatType,
    SystemQueryStage QueryStages,
    CharacterResourceConstraint Constraints,
    string? FutureRuleNote = null)
{
    public bool IsRuntimeResource => true;

    public bool IsPersistent => Constraints.HasFlag(CharacterResourceConstraint.PersistentRuntime);
}