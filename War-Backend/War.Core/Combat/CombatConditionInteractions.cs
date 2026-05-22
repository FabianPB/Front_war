namespace War.Core.Combat;

public enum CombatConditionInteractionStatus
{
    Defined,
    PartiallyDefined
}

public sealed record CombatConditionInteractionKey(IReadOnlyList<CombatConditionType> Conditions)
{
    public static CombatConditionInteractionKey From(IEnumerable<CombatConditionType> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        return new CombatConditionInteractionKey(Array.AsReadOnly(
            conditions
                .Distinct()
                .OrderBy(condition => condition)
                .ToArray()));
    }

    public string NormalizedKey => string.Join("+", Conditions.Select(condition => condition.ToString()));

    public bool MatchesExactly(IEnumerable<CombatConditionType> activeConditions)
    {
        return Conditions.SequenceEqual(From(activeConditions).Conditions);
    }

    public bool Contains(CombatConditionType condition)
    {
        return Conditions.Contains(condition);
    }
}

public sealed record CombatConditionInteractionRule(
    string Key,
    IReadOnlyList<CombatConditionType> RequiredConditions,
    decimal? FinalDamageIncreasePercentage,
    CombatConditionType? AdditionalConditionToApply,
    CombatConditionInteractionStatus Status,
    string Description,
    string? FutureRuleNote = null)
{
    public CombatConditionInteractionKey InteractionKey => CombatConditionInteractionKey.From(RequiredConditions);

    public bool IsExecutable => Status == CombatConditionInteractionStatus.Defined;

    public bool MatchesExactly(IEnumerable<CombatConditionType> activeConditions)
    {
        ArgumentNullException.ThrowIfNull(activeConditions);
        return InteractionKey.MatchesExactly(activeConditions);
    }
}

public sealed class CombatConditionInteractionMatrix
{
    private readonly IReadOnlyDictionary<string, CombatConditionInteractionRule> _rulesByNormalizedKey;

    public CombatConditionInteractionMatrix(IEnumerable<CombatConditionInteractionRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var normalizedRules = rules.ToArray();
        AllRules = Array.AsReadOnly(normalizedRules);
        DefinedRules = Array.AsReadOnly(normalizedRules.Where(rule => rule.Status == CombatConditionInteractionStatus.Defined).ToArray());
        PartialRules = Array.AsReadOnly(normalizedRules.Where(rule => rule.Status == CombatConditionInteractionStatus.PartiallyDefined).ToArray());
        _rulesByNormalizedKey = normalizedRules.ToDictionary(rule => rule.InteractionKey.NormalizedKey, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CombatConditionInteractionRule> AllRules { get; }

    public IReadOnlyList<CombatConditionInteractionRule> DefinedRules { get; }

    public IReadOnlyList<CombatConditionInteractionRule> PartialRules { get; }

    public CombatConditionInteractionRule? FindExact(IEnumerable<CombatConditionType> activeConditions, bool includePartialRules = false)
    {
        ArgumentNullException.ThrowIfNull(activeConditions);

        var key = CombatConditionInteractionKey.From(activeConditions).NormalizedKey;
        if (!_rulesByNormalizedKey.TryGetValue(key, out var rule))
        {
            return null;
        }

        return includePartialRules || rule.Status == CombatConditionInteractionStatus.Defined
            ? rule
            : null;
    }

    public IReadOnlyList<CombatConditionInteractionRule> GetRulesFor(CombatConditionType condition, bool includePartialRules = true)
    {
        var source = includePartialRules ? AllRules : DefinedRules;
        return Array.AsReadOnly(source.Where(rule => rule.InteractionKey.Contains(condition)).ToArray());
    }
}

public static class CombatConditionInteractionCatalog
{
    private static readonly CombatConditionInteractionMatrix Matrix = new(
    [
        new CombatConditionInteractionRule(
            "heat-cold-weaken-damage-bonus",
            [CombatConditionType.Cold, CombatConditionType.Heat],
            0.20m,
            CombatConditionType.Weaken,
            CombatConditionInteractionStatus.Defined,
            "Combining Heat with Cold applies Weaken and increases final damage by 20%."),

        new CombatConditionInteractionRule(
            "heat-freeze-damage-bonus",
            [CombatConditionType.Heat, CombatConditionType.Freeze],
            0.50m,
            null,
            CombatConditionInteractionStatus.Defined,
            "Combining Heat with Freeze increases final damage by 50%."),

        new CombatConditionInteractionRule(
            "electrified-cold-damage-bonus",
            [CombatConditionType.Cold, CombatConditionType.Electrified],
            0.30m,
            null,
            CombatConditionInteractionStatus.Defined,
            "Combining Electrified with Cold increases final damage by 30%."),

        new CombatConditionInteractionRule(
            "electrified-freeze-damage-bonus",
            [CombatConditionType.Electrified, CombatConditionType.Freeze],
            0.60m,
            null,
            CombatConditionInteractionStatus.Defined,
            "Combining Electrified with Freeze increases final damage by 60%."),

        new CombatConditionInteractionRule(
            "electrified-similar-to-heat-open-rule",
            [CombatConditionType.Electrified],
            null,
            null,
            CombatConditionInteractionStatus.PartiallyDefined,
            "Electrified is described as having a damage-interaction profile similar to Heat.",
            "Only the Cold (+30%) and Freeze (+60%) pairings are formally defined right now; no other Electrified pairings should be invented until the combat/effects design is closed.")
    ]);

    public static CombatConditionInteractionMatrix Current => Matrix;

    public static IReadOnlyList<CombatConditionInteractionRule> GetAll()
    {
        return Matrix.DefinedRules;
    }

    public static IReadOnlyList<CombatConditionInteractionRule> GetDefinedRules()
    {
        return Matrix.DefinedRules;
    }

    public static IReadOnlyList<CombatConditionInteractionRule> GetPartialRules()
    {
        return Matrix.PartialRules;
    }

    public static IReadOnlyList<CombatConditionInteractionRule> GetAllDocumentedRules()
    {
        return Matrix.AllRules;
    }

    public static IReadOnlyList<CombatConditionInteractionRule> GetRulesFor(CombatConditionType condition, bool includePartialRules = true)
    {
        return Matrix.GetRulesFor(condition, includePartialRules);
    }

    public static CombatConditionInteractionRule? FindDefinedExact(IEnumerable<CombatConditionType> activeConditions)
    {
        return Matrix.FindExact(activeConditions, includePartialRules: false);
    }

    public static CombatConditionInteractionRule? FindDocumentedExact(IEnumerable<CombatConditionType> activeConditions)
    {
        return Matrix.FindExact(activeConditions, includePartialRules: true);
    }
}
