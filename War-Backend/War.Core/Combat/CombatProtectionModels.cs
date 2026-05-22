namespace War.Core.Combat;

[Flags]
public enum CombatProtectionBlockType
{
    None = 0,
    Damage = 1 << 0,
    NegativeConditions = 1 << 1,
    CrowdControl = 1 << 2
}

public enum CombatProtectionType
{
    Invulnerability
}

public enum CombatProtectionRefreshPolicy
{
    IgnoreIfAlreadyActive,
    RefreshDuration,
    ReplaceIfLonger
}

public sealed record CombatProtectionState(
    CombatProtectionType Type,
    CombatProtectionBlockType Blocks,
    decimal RemainingDurationSeconds,
    CombatProtectionRefreshPolicy RefreshPolicy = CombatProtectionRefreshPolicy.IgnoreIfAlreadyActive,
    string? SourceKey = null,
    string? Note = null);

public sealed record CombatProtectionGrantIntent(
    CombatProtectionType Type,
    CombatProtectionBlockType Blocks,
    decimal DurationSeconds,
    CombatProtectionRefreshPolicy RefreshPolicy = CombatProtectionRefreshPolicy.IgnoreIfAlreadyActive,
    bool RemovesExistingNegativeEffects = false,
    string? SourceKey = null,
    string? Note = null);

public static class CombatProtectionRules
{
    public static bool BlocksDamage(IReadOnlyCollection<CombatProtectionState>? protections)
    {
        return (protections ?? Array.Empty<CombatProtectionState>())
            .Any(protection => protection.Blocks.HasFlag(CombatProtectionBlockType.Damage));
    }

    public static bool BlocksCondition(
        CombatConditionDefinition definition,
        IReadOnlyCollection<CombatProtectionState>? protections)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var activeProtections = protections ?? Array.Empty<CombatProtectionState>();
        if (activeProtections.Count == 0)
        {
            return false;
        }

        return definition.Category switch
        {
            CombatConditionCategory.CrowdControl => activeProtections.Any(protection =>
                protection.Blocks.HasFlag(CombatProtectionBlockType.CrowdControl) ||
                protection.Blocks.HasFlag(CombatProtectionBlockType.NegativeConditions)),

            _ => activeProtections.Any(protection =>
                protection.Blocks.HasFlag(CombatProtectionBlockType.NegativeConditions))
        };
    }

    public static string DescribeBlockingProtections(IReadOnlyCollection<CombatProtectionState>? protections)
    {
        var activeProtections = protections ?? Array.Empty<CombatProtectionState>();
        if (activeProtections.Count == 0)
        {
            return "No active protections were supplied.";
        }

        var labels = activeProtections
            .Select(protection => protection.Type.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return $"Active protections blocked the incoming payload: {string.Join(", ", labels)}.";
    }
}
