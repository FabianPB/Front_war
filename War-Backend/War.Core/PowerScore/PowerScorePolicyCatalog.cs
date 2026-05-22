using System.Collections.ObjectModel;
using War.Core.Combat;
using War.Core.Stats;

namespace War.Core.PowerScore;

public interface IPowerScorePolicyCatalog
{
    PowerScoreGlobalTuning Tuning { get; }

    PowerScoreStatPolicy Get(StatType statType);

    IReadOnlyCollection<PowerScoreStatPolicy> GetAll();

    IReadOnlyList<PowerScoreStatAuditEntry> Audit();
}

public sealed class PowerScorePolicyCatalog : IPowerScorePolicyCatalog
{
    private readonly IReadOnlyDictionary<StatType, PowerScoreStatPolicy> _policies;
    private readonly IReadOnlyList<PowerScoreStatAuditEntry> _auditEntries;

    public PowerScorePolicyCatalog(PowerScoreGlobalTuning? tuning = null)
    {
        Tuning = tuning ?? new PowerScoreGlobalTuning();
        _policies = new ReadOnlyDictionary<StatType, PowerScoreStatPolicy>(
            Enum.GetValues<StatType>().ToDictionary(statType => statType, CreatePolicy));
        _auditEntries = Array.AsReadOnly(_policies.Values
            .OrderBy(policy => policy.Category)
            .ThenBy(policy => policy.StatType)
            .Select(policy => new PowerScoreStatAuditEntry(
                policy.StatType,
                policy.Category,
                policy.Inclusion,
                policy.UsesClassContextWeight,
                policy.Reason,
                policy.PendingReason))
            .ToArray());
    }

    public static PowerScorePolicyCatalog Default { get; } = new();

    public PowerScoreGlobalTuning Tuning { get; }

    public PowerScoreStatPolicy Get(StatType statType)
    {
        return _policies.TryGetValue(statType, out var policy)
            ? policy
            : throw new ArgumentOutOfRangeException(nameof(statType), statType, "No power-score policy exists for the requested stat.");
    }

    public IReadOnlyCollection<PowerScoreStatPolicy> GetAll()
    {
        return _policies.Values.ToArray();
    }

    public IReadOnlyList<PowerScoreStatAuditEntry> Audit()
    {
        return _auditEntries;
    }

    private static PowerScoreStatPolicy CreatePolicy(StatType statType)
    {
        return statType switch
        {
            StatType.MaxHp => Direct(
                statType,
                PowerScoreCategory.Defensive,
                unitValue: 4.0m,
                referenceValue: 100m,
                reason: "MaxHp is a permanent survivability pool and should contribute uniformly across classes."),

            StatType.MaxMana => Indirect(
                statType,
                PowerScoreCategory.Recovery,
                unitValue: 1.5m,
                referenceValue: 100m,
                reason: "MaxMana is structurally useful, but its conversion to current power is incomplete until real skill-cost data exists for each kit.",
                pendingReason: "Most live skill definitions still lack authoritative mana or energy costs."),

            StatType.HpRegen => Direct(
                statType,
                PowerScoreCategory.Recovery,
                unitValue: 8m,
                referenceValue: 1m,
                reason: "HpRegen contributes to sustained combat durability regardless of class."),

            StatType.ManaRegen => Indirect(
                statType,
                PowerScoreCategory.Recovery,
                unitValue: 5m,
                referenceValue: 1m,
                reason: "ManaRegen is real build power, but its practical value is still under-informed until skill costs and encounter pacing are filled in.",
                pendingReason: "The current skill catalog still has many placeholder resource costs."),

            StatType.UltimateChargeMax => Excluded(
                statType,
                PowerScoreCategory.Utility,
                reason: "UltimateChargeMax is a runtime storage ceiling, not stable structural output by itself. It should not inflate permanent power before charge generation and spending semantics are modeled."),

            StatType.PhysicalAttack => ContextualOffense(
                statType,
                unitValue: 9m,
                referenceValue: 10m,
                minWeight: 0.55m,
                maxWeight: 1.60m,
                referenceSignal: 40m,
                reason: "PhysicalAttack should scale with how strongly the class converts physical coefficients in basics and skills."),

            StatType.MagicAttack => ContextualOffense(
                statType,
                unitValue: 9m,
                referenceValue: 10m,
                minWeight: 0.55m,
                maxWeight: 1.60m,
                referenceSignal: 40m,
                reason: "MagicAttack should scale with how strongly the class converts magical coefficients in basics and skills."),

            StatType.AttackSpeed => ContextualOffense(
                statType,
                unitValue: 6m,
                referenceValue: 1m,
                minWeight: 0.40m,
                maxWeight: 1.35m,
                referenceSignal: 6m,
                reason: "AttackSpeed should matter mostly for classes whose basic attacks are modeled as a real throughput source."),

            StatType.CritChance => ContextualOffense(
                statType,
                unitValue: 55m,
                referenceValue: 1m,
                minWeight: 0.70m,
                maxWeight: 1.35m,
                referenceSignal: 30m,
                transform: PowerScoreValueTransform.EffectiveCritChanceAgainstReference,
                reason: "CritChance should scale with how often the class can route real output through crit-capable sources."),

            StatType.CritDamage => ContextualOffense(
                statType,
                unitValue: 6m,
                referenceValue: 10m,
                minWeight: 0.70m,
                maxWeight: 1.35m,
                referenceSignal: 30m,
                reason: "CritDamage only matters when the class has meaningful crit-capable damage sources."),

            StatType.Accuracy => ContextualOffense(
                statType,
                unitValue: 70m,
                referenceValue: 1m,
                minWeight: 0.65m,
                maxWeight: 1.25m,
                referenceSignal: 30m,
                transform: PowerScoreValueTransform.EffectiveHitChanceAgainstReference,
                reason: "Accuracy should follow how much of the kit relies on hit-checked offensive actions."),

            StatType.CriticalEvasion => Direct(
                statType,
                PowerScoreCategory.Defensive,
                unitValue: 55m,
                referenceValue: 1m,
                transform: PowerScoreValueTransform.DirectFraction,
                reason: "CriticalEvasion reduces exposure to critical spikes and should count broadly across classes."),

            StatType.DefensePenetration => ContextualOffense(
                statType,
                unitValue: 32m,
                referenceValue: 1m,
                minWeight: 0.65m,
                maxWeight: 1.35m,
                referenceSignal: 28m,
                transform: PowerScoreValueTransform.DirectFraction,
                reason: "DefensePenetration should matter most for classes that actually route damage through physical sources."),

            StatType.MagicPenetration => ContextualOffense(
                statType,
                unitValue: 32m,
                referenceValue: 1m,
                minWeight: 0.65m,
                maxWeight: 1.35m,
                referenceSignal: 28m,
                transform: PowerScoreValueTransform.DirectFraction,
                reason: "MagicPenetration should matter most for classes that actually route damage through magical sources."),

            StatType.AttackRange => Indirect(
                statType,
                PowerScoreCategory.Utility,
                unitValue: 4m,
                referenceValue: 5m,
                reason: "AttackRange is structurally useful spatial leverage, but it is still secondary compared with core combat throughput stats."),

            StatType.Defense => Direct(
                statType,
                PowerScoreCategory.Defensive,
                unitValue: 120m,
                referenceValue: 1m,
                transform: PowerScoreValueTransform.MitigationRatio,
                reason: "Defense contributes through real mitigation efficiency and should be valued uniformly."),

            StatType.MagicResistance => Direct(
                statType,
                PowerScoreCategory.Defensive,
                unitValue: 120m,
                referenceValue: 1m,
                transform: PowerScoreValueTransform.MitigationRatio,
                reason: "MagicResistance contributes through real mitigation efficiency and should be valued uniformly."),

            StatType.Evasion => Direct(
                statType,
                PowerScoreCategory.Defensive,
                unitValue: 70m,
                referenceValue: 1m,
                transform: PowerScoreValueTransform.HitAvoidanceAgainstReference,
                reason: "Evasion is broad survivability because it reduces how often incoming hit-checked actions connect."),

            StatType.Tenacity => Direct(
                statType,
                PowerScoreCategory.Defensive,
                unitValue: 70m,
                referenceValue: 1m,
                transform: PowerScoreValueTransform.CrowdControlResistanceRatio,
                reason: "Tenacity is universally defensive because it shortens hostile crowd-control duration regardless of class."),

            StatType.CooldownReduction => ContextualUtility(
                statType,
                unitValue: 42m,
                minWeight: 0.75m,
                maxWeight: 1.35m,
                referenceSignal: 12m,
                reason: "CooldownReduction should matter more for classes whose available kit meaningfully routes value through skills."),

            StatType.SkillRecoveryRate => ContextualUtility(
                statType,
                unitValue: 42m,
                minWeight: 0.75m,
                maxWeight: 1.35m,
                referenceSignal: 12m,
                transform: PowerScoreValueTransform.RecoveryAccelerationFromRate,
                reason: "SkillRecoveryRate should matter more for classes whose available kit is skill-centric."),

            StatType.HealingEffectiveness => ContextualRecovery(
                statType,
                unitValue: 40m,
                minWeight: 0.60m,
                maxWeight: 1.35m,
                referenceSignal: 10m,
                reason: "HealingEffectiveness should scale with how much of the class kit produces healing output."),

            StatType.HealingReceived => Direct(
                statType,
                PowerScoreCategory.Recovery,
                unitValue: 35m,
                referenceValue: 1m,
                transform: PowerScoreValueTransform.DirectFraction,
                reason: "HealingReceived is broadly defensive because any build benefits from more efficient incoming healing."),

            StatType.MoveSpeed => Direct(
                statType,
                PowerScoreCategory.Utility,
                unitValue: 8m,
                referenceValue: 5m,
                reason: "MoveSpeed is general utility and mobility leverage that should contribute across classes."),

            StatType.ExpGain => Progression(statType, "ExpGain improves account or character progression throughput but should remain supplemental to combat power."),
            StatType.DropRate => Progression(statType, "DropRate improves farming efficiency but should remain supplemental to combat power."),
            StatType.DropQuality => Progression(statType, "DropQuality improves reward efficiency but should remain supplemental to combat power."),
            StatType.GatheringSpeed => Progression(statType, "GatheringSpeed is non-combat progression value and should stay low-weight."),
            StatType.MeditationSpeed => Progression(statType, "MeditationSpeed is non-combat progression value and should stay low-weight."),

            StatType.HeatApplyChance => StatusApply(statType, CombatConditionType.Heat),
            StatType.ColdApplyChance => StatusApply(statType, CombatConditionType.Cold),
            StatType.ElectrifiedApplyChance => StatusApply(statType, CombatConditionType.Electrified),
            StatType.PoisonApplyChance => StatusApply(statType, CombatConditionType.Poison),
            StatType.WeakenApplyChance => StatusApply(statType, CombatConditionType.Weaken),
            StatType.BlindApplyChance => StatusApply(statType, CombatConditionType.Blind),
            StatType.StunApplyChance => StatusApply(statType, CombatConditionType.Stun),
            StatType.FreezeApplyChance => StatusApply(statType, CombatConditionType.Freeze),
            StatType.ParalyzeApplyChance => StatusApply(statType, CombatConditionType.Paralyze),

            StatType.HeatEvadeChance => StatusResistance(statType, CombatConditionType.Heat),
            StatType.ColdEvadeChance => StatusResistance(statType, CombatConditionType.Cold),
            StatType.ElectrifiedEvadeChance => StatusResistance(statType, CombatConditionType.Electrified),
            StatType.PoisonEvadeChance => StatusResistance(statType, CombatConditionType.Poison),
            StatType.WeakenEvadeChance => StatusResistance(statType, CombatConditionType.Weaken),
            StatType.BlindEvadeChance => StatusResistance(statType, CombatConditionType.Blind),
            StatType.StunEvadeChance => StatusResistance(statType, CombatConditionType.Stun),
            StatType.FreezeEvadeChance => StatusResistance(statType, CombatConditionType.Freeze),
            StatType.ParalyzeEvadeChance => StatusResistance(statType, CombatConditionType.Paralyze),

            StatType.BasicAttackDamageIncrease => ContextualOffense(
                statType,
                unitValue: 35m,
                referenceValue: 1m,
                minWeight: 0.55m,
                maxWeight: 1.35m,
                referenceSignal: 10m,
                transform: PowerScoreValueTransform.DirectFraction,
                reason: "BasicAttackDamageIncrease should matter according to how much the class actually uses basics for output."),

            StatType.SkillDamageIncrease => ContextualOffense(
                statType,
                unitValue: 35m,
                referenceValue: 1m,
                minWeight: 0.65m,
                maxWeight: 1.40m,
                referenceSignal: 16m,
                transform: PowerScoreValueTransform.DirectFraction,
                reason: "SkillDamageIncrease should track how much output the class routes through skills."),

            StatType.CritDamageIncrease => ContextualOffense(
                statType,
                unitValue: 28m,
                referenceValue: 1m,
                minWeight: 0.65m,
                maxWeight: 1.30m,
                referenceSignal: 18m,
                transform: PowerScoreValueTransform.DirectFraction,
                reason: "CritDamageIncrease only matters when the class has meaningful crit-capable damage sources."),

            StatType.MonsterDamageIncrease => SituationalOffense(statType, "MonsterDamageIncrease is a valuable PvE specialization stat, but it should not count like always-on throughput."),
            StatType.BossDamageIncrease => SituationalOffense(statType, "BossDamageIncrease is a valuable PvE specialization stat, but it should not count like always-on throughput."),
            StatType.PvPDamageIncrease => SituationalOffense(statType, "PvPDamageIncrease is valuable in PvP-only contexts and should remain supplemental in a general-purpose power score."),
            StatType.HeatDamageIncrease => StatusDamage(statType, CombatConditionType.Heat),
            StatType.ColdDamageIncrease => StatusDamage(statType, CombatConditionType.Cold),
            StatType.ElectrifiedDamageIncrease => StatusDamage(statType, CombatConditionType.Electrified),
            StatType.PoisonDamageIncrease => StatusDamage(statType, CombatConditionType.Poison),

            StatType.BasicAttackDamageReduction => SituationalDefense(statType, "BasicAttackDamageReduction is useful but only against one incoming damage context."),
            StatType.SkillDamageReduction => SituationalDefense(statType, "SkillDamageReduction is useful but only against one incoming damage context."),
            StatType.CritDamageTakenReduction => SituationalDefense(statType, "CritDamageTakenReduction is useful but only against critical damage windows."),
            StatType.MonsterDamageReduction => SituationalDefense(statType, "MonsterDamageReduction is useful but PvE-contextual."),
            StatType.BossDamageReduction => SituationalDefense(statType, "BossDamageReduction is useful but PvE-contextual."),
            StatType.PvPDamageReduction => SituationalDefense(statType, "PvPDamageReduction is useful but PvP-contextual."),
            StatType.HeatDamageReduction => StatusDefense(statType, CombatConditionType.Heat),
            StatType.ColdDamageReduction => StatusDefense(statType, CombatConditionType.Cold),
            StatType.ElectrifiedDamageReduction => StatusDefense(statType, CombatConditionType.Electrified),
            StatType.PoisonDamageReduction => StatusDefense(statType, CombatConditionType.Poison),

            _ => throw new ArgumentOutOfRangeException(nameof(statType), statType, "Unknown stat type.")
        };
    }

    private static PowerScoreStatPolicy Direct(
        StatType statType,
        PowerScoreCategory category,
        decimal unitValue,
        decimal referenceValue,
        string reason,
        PowerScoreValueTransform transform = PowerScoreValueTransform.RatioToReference,
        decimal categoryAdjustment = 1m,
        string? pendingReason = null)
    {
        return new PowerScoreStatPolicy(
            statType,
            category,
            PowerScoreStatInclusion.Direct,
            unitValue,
            transform,
            referenceValue,
            UsesClassContextWeight: false,
            CategoryAdjustment: categoryAdjustment,
            Reason: reason,
            PendingReason: pendingReason);
    }

    private static PowerScoreStatPolicy Indirect(
        StatType statType,
        PowerScoreCategory category,
        decimal unitValue,
        decimal referenceValue,
        string reason,
        PowerScoreValueTransform transform = PowerScoreValueTransform.RatioToReference,
        decimal categoryAdjustment = 0.75m,
        string? pendingReason = null)
    {
        return new PowerScoreStatPolicy(
            statType,
            category,
            PowerScoreStatInclusion.IndirectLowWeight,
            unitValue,
            transform,
            referenceValue,
            UsesClassContextWeight: false,
            CategoryAdjustment: categoryAdjustment,
            Reason: reason,
            PendingReason: pendingReason);
    }

    private static PowerScoreStatPolicy Excluded(
        StatType statType,
        PowerScoreCategory category,
        string reason)
    {
        return new PowerScoreStatPolicy(
            statType,
            category,
            PowerScoreStatInclusion.Excluded,
            UnitValue: 0m,
            PowerScoreValueTransform.RatioToReference,
            Reason: reason);
    }

    private static PowerScoreStatPolicy ContextualOffense(
        StatType statType,
        decimal unitValue,
        decimal referenceValue,
        decimal minWeight,
        decimal maxWeight,
        decimal referenceSignal,
        string reason,
        PowerScoreValueTransform transform = PowerScoreValueTransform.RatioToReference)
    {
        return new PowerScoreStatPolicy(
            statType,
            PowerScoreCategory.Offensive,
            PowerScoreStatInclusion.Direct,
            unitValue,
            transform,
            referenceValue,
            UsesClassContextWeight: true,
            MinimumClassWeight: minWeight,
            MaximumClassWeight: maxWeight,
            ReferenceSignal: referenceSignal,
            Reason: reason);
    }

    private static PowerScoreStatPolicy ContextualUtility(
        StatType statType,
        decimal unitValue,
        decimal minWeight,
        decimal maxWeight,
        decimal referenceSignal,
        string reason,
        PowerScoreValueTransform transform = PowerScoreValueTransform.DirectFraction)
    {
        return new PowerScoreStatPolicy(
            statType,
            PowerScoreCategory.Utility,
            PowerScoreStatInclusion.Direct,
            unitValue,
            transform,
            1m,
            UsesClassContextWeight: true,
            MinimumClassWeight: minWeight,
            MaximumClassWeight: maxWeight,
            ReferenceSignal: referenceSignal,
            Reason: reason);
    }

    private static PowerScoreStatPolicy ContextualRecovery(
        StatType statType,
        decimal unitValue,
        decimal minWeight,
        decimal maxWeight,
        decimal referenceSignal,
        string reason)
    {
        return new PowerScoreStatPolicy(
            statType,
            PowerScoreCategory.Recovery,
            PowerScoreStatInclusion.Direct,
            unitValue,
            PowerScoreValueTransform.DirectFraction,
            1m,
            UsesClassContextWeight: true,
            MinimumClassWeight: minWeight,
            MaximumClassWeight: maxWeight,
            ReferenceSignal: referenceSignal,
            Reason: reason);
    }

    private static PowerScoreStatPolicy Progression(StatType statType, string reason)
    {
        return new PowerScoreStatPolicy(
            statType,
            PowerScoreCategory.Progression,
            PowerScoreStatInclusion.IndirectLowWeight,
            UnitValue: 18m,
            PowerScoreValueTransform.DirectFraction,
            ReferenceValue: 1m,
            UsesClassContextWeight: false,
            CategoryAdjustment: 0.50m,
            Reason: reason);
    }

    private static PowerScoreStatPolicy StatusApply(StatType statType, CombatConditionType condition)
    {
        return new PowerScoreStatPolicy(
            statType,
            PowerScoreCategory.Status,
            PowerScoreStatInclusion.Direct,
            UnitValue: CombatConditionCatalog.Get(condition).Category == CombatConditionCategory.CrowdControl ? 45m : 30m,
            PowerScoreValueTransform.EffectiveStatusApplyChanceAgainstReference,
            ReferenceValue: 1m,
            UsesClassContextWeight: true,
            MinimumClassWeight: 0.50m,
            MaximumClassWeight: 1.35m,
            ReferenceSignal: CombatConditionCatalog.Get(condition).Category == CombatConditionCategory.CrowdControl ? 8m : 6m,
            Reason: $"{statType} should count according to whether the class kit can realistically apply {condition}.");
    }

    private static PowerScoreStatPolicy StatusResistance(StatType statType, CombatConditionType condition)
    {
        return new PowerScoreStatPolicy(
            statType,
            PowerScoreCategory.Status,
            PowerScoreStatInclusion.Direct,
            UnitValue: CombatConditionCatalog.Get(condition).Category == CombatConditionCategory.CrowdControl ? 40m : 25m,
            PowerScoreValueTransform.EffectiveStatusResistanceAgainstReference,
            ReferenceValue: 1m,
            UsesClassContextWeight: false,
            Reason: $"{statType} is broad defensive resilience against hostile {condition} application.");
    }

    private static PowerScoreStatPolicy StatusDamage(StatType statType, CombatConditionType condition)
    {
        return new PowerScoreStatPolicy(
            statType,
            PowerScoreCategory.Status,
            PowerScoreStatInclusion.Direct,
            UnitValue: 24m,
            PowerScoreValueTransform.DirectFraction,
            ReferenceValue: 1m,
            UsesClassContextWeight: true,
            MinimumClassWeight: 0.45m,
            MaximumClassWeight: 1.25m,
            ReferenceSignal: 5m,
            Reason: $"{statType} should matter according to whether the class kit can actually establish {condition} for follow-up damage.");
    }

    private static PowerScoreStatPolicy StatusDefense(StatType statType, CombatConditionType condition)
    {
        return new PowerScoreStatPolicy(
            statType,
            PowerScoreCategory.Status,
            PowerScoreStatInclusion.IndirectLowWeight,
            UnitValue: 20m,
            PowerScoreValueTransform.DirectFraction,
            ReferenceValue: 1m,
            UsesClassContextWeight: false,
            CategoryAdjustment: 0.80m,
            Reason: $"{statType} is defensive resilience against {condition}-tagged damage and should count, but less than broad unconditional mitigation.");
    }

    private static PowerScoreStatPolicy SituationalOffense(StatType statType, string reason)
    {
        return new PowerScoreStatPolicy(
            statType,
            PowerScoreCategory.Offensive,
            PowerScoreStatInclusion.IndirectLowWeight,
            UnitValue: 22m,
            PowerScoreValueTransform.DirectFraction,
            ReferenceValue: 1m,
            UsesClassContextWeight: false,
            CategoryAdjustment: 0.65m,
            Reason: reason);
    }

    private static PowerScoreStatPolicy SituationalDefense(StatType statType, string reason)
    {
        return new PowerScoreStatPolicy(
            statType,
            PowerScoreCategory.Defensive,
            PowerScoreStatInclusion.IndirectLowWeight,
            UnitValue: 22m,
            PowerScoreValueTransform.DirectFraction,
            ReferenceValue: 1m,
            UsesClassContextWeight: false,
            CategoryAdjustment: 0.70m,
            Reason: reason);
    }
}
