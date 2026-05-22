using System.Collections.ObjectModel;
using War.Core.Stats;

namespace War.Core.Combat;

public static class CombatConditionCatalog
{
    private static readonly IReadOnlyDictionary<CombatConditionType, CombatConditionDefinition> Definitions =
        new ReadOnlyDictionary<CombatConditionType, CombatConditionDefinition>(
            Enum.GetValues<CombatConditionType>().ToDictionary(conditionType => conditionType, CreateDefinition));

    private static readonly IReadOnlyCollection<CombatConditionDefinition> AllDefinitions = Definitions.Values.ToArray();

    public static CombatConditionDefinition Get(CombatConditionType conditionType)
    {
        return Definitions.TryGetValue(conditionType, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(conditionType), conditionType, "Unknown combat condition type.");
    }

    public static IReadOnlyCollection<CombatConditionDefinition> GetAll()
    {
        return AllDefinitions;
    }

    private static CombatConditionDefinition CreateDefinition(CombatConditionType conditionType)
    {
        return conditionType switch
        {
            CombatConditionType.Heat => State(conditionType, "General heat state. It is distinct from crowd-control effects and can participate in damage-synergy interactions.", StatType.HeatApplyChance, StatType.HeatEvadeChance),
            CombatConditionType.Cold => State(conditionType, "General cold state. It is not the same effect as Freeze and can participate in damage-synergy interactions.", StatType.ColdApplyChance, StatType.ColdEvadeChance),
            CombatConditionType.Electrified => State(conditionType, "General electrified state used by future lightning interactions and status-driven damage rules.", StatType.ElectrifiedApplyChance, StatType.ElectrifiedEvadeChance),
            CombatConditionType.Poison => State(conditionType, "General poison state used by future damage-over-time or poison-tagged damage systems.", StatType.PoisonApplyChance, StatType.PoisonEvadeChance),
            CombatConditionType.Weaken => CrowdControl(conditionType, "Crowd-control debuff that future combat may interpret as a weakening incapacity or vulnerability state.", StatType.WeakenApplyChance, StatType.WeakenEvadeChance),
            CombatConditionType.Blind => CrowdControl(conditionType, "Crowd-control debuff representing blindness and future accuracy disruption.", StatType.BlindApplyChance, StatType.BlindEvadeChance),
            CombatConditionType.Stun => CrowdControl(conditionType, "Crowd-control debuff representing a stun/incapacitation window.", StatType.StunApplyChance, StatType.StunEvadeChance),
            CombatConditionType.Freeze => CrowdControl(conditionType, "Crowd-control freeze effect. It is distinct from the general Cold state.", StatType.FreezeApplyChance, StatType.FreezeEvadeChance),
            CombatConditionType.Paralyze => CrowdControl(conditionType, "Crowd-control debuff representing paralysis or mobility/action lock.", StatType.ParalyzeApplyChance, StatType.ParalyzeEvadeChance),
            _ => throw new ArgumentOutOfRangeException(nameof(conditionType), conditionType, "Unknown combat condition type.")
        };
    }

    private static CombatConditionDefinition State(
        CombatConditionType conditionType,
        string description,
        StatType applyChanceStat,
        StatType evadeChanceStat)
    {
        return new CombatConditionDefinition(
            conditionType,
            CombatConditionCategory.State,
            description,
            applyChanceStat,
            evadeChanceStat,
            RequiresHitBeforeApplication: true,
            ChecksEvadeAfterApplyRoll: true,
            DurationAffectedByTenacity: false);
    }

    private static CombatConditionDefinition CrowdControl(
        CombatConditionType conditionType,
        string description,
        StatType applyChanceStat,
        StatType evadeChanceStat)
    {
        return new CombatConditionDefinition(
            conditionType,
            CombatConditionCategory.CrowdControl,
            description,
            applyChanceStat,
            evadeChanceStat,
            RequiresHitBeforeApplication: true,
            ChecksEvadeAfterApplyRoll: true,
            DurationAffectedByTenacity: true,
            FutureRuleNote: "Tenacity reduces only the resulting duration, not the initial application chance.");
    }
}