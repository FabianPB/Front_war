using War.Core.Stats;

namespace War.Core.Combat;

public enum CombatConditionType
{
    Heat,
    Cold,
    Electrified,
    Poison,
    Weaken,
    Blind,
    Stun,
    Freeze,
    Paralyze
}

public enum CombatConditionCategory
{
    State,
    CrowdControl
}

public sealed record CombatConditionDefinition(
    CombatConditionType Type,
    CombatConditionCategory Category,
    string Description,
    StatType ApplyChanceStat,
    StatType EvadeChanceStat,
    bool RequiresHitBeforeApplication,
    bool ChecksEvadeAfterApplyRoll,
    bool DurationAffectedByTenacity,
    string? FutureRuleNote = null);