using System.Collections.ObjectModel;
using War.Core.Combat;

namespace War.Core.Stats;

public static class StatCatalog
{
    private static readonly IReadOnlyDictionary<StatType, StatDefinition> Definitions =
        new ReadOnlyDictionary<StatType, StatDefinition>(
            Enum.GetValues<StatType>().ToDictionary(statType => statType, CreateDefinition));

    private static readonly IReadOnlyCollection<StatDefinition> AllDefinitions = Definitions.Values.ToArray();

    public static StatDefinition Get(StatType statType)
    {
        return Definitions.TryGetValue(statType, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(statType), statType, "Unknown stat type.");
    }

    public static IReadOnlyCollection<StatDefinition> GetAll()
    {
        return AllDefinitions;
    }

    private static StatDefinition CreateDefinition(StatType statType)
    {
        return statType switch
        {
            StatType.MaxHp => ResourceMaximum(
                statType,
                "Maximum health pool available to the character in the current build state.",
                SystemQueryStage.DamageApplication | SystemQueryStage.HealingApplication,
                "Future runtime healing must clamp CurrentHp against MaxHp."),

            StatType.MaxMana => ResourceMaximum(
                statType,
                "Maximum mana pool available to the character in the current build state.",
                SystemQueryStage.ResourceConsumption | SystemQueryStage.ResourceRecovery,
                "Future mana restoration must clamp CurrentMana against MaxMana."),

            StatType.UltimateChargeMax => Define(
                statType,
                "Maximum runtime capacity for the ultimate charge resource.",
                StatCategory.CombatResource,
                StatValueKind.Flat,
                StatValueScale.RawDecimal,
                StatMeasurementUnit.ResourcePoints,
                StatBehaviorFamily.ResourceMaximum,
                StatUsageKind.Direct,
                StatResolutionKind.None,
                SystemQueryStage.BuildAggregation | SystemQueryStage.ResourceValidation | SystemQueryStage.ResourceRecovery | SystemQueryStage.ResourceConsumption,
                StatInfluence.ResourceMaximum,
                StatConstraint.NonNegative | StatConstraint.UsedAsRuntimeMaximum,
                isMaximum: true,
                futureRuleNote: "Future ultimate gain/spend flows must clamp UltimateCharge against UltimateChargeMax."),

            StatType.HpRegen => ResourceRecovery(
                statType,
                "Health recovered per second before future combat/runtime modifiers.",
                futureRuleNote: "The combat loop will later decide tick cadence and whether regen can be paused or reduced by effects."),

            StatType.ManaRegen => ResourceRecovery(
                statType,
                "Mana recovered per second before future combat/runtime modifiers.",
                futureRuleNote: "The resource system will later decide how mana regeneration interacts with combat state and skill pressure."),

            StatType.PhysicalAttack => DirectCombatValue(
                statType,
                "Base physical damage contribution added by the current build.",
                StatCategory.Offensive,
                StatInfluence.DamageOutput,
                SystemQueryStage.DamageModification),

            StatType.MagicAttack => DirectCombatValue(
                statType,
                "Base magical damage contribution added by the current build.",
                StatCategory.Offensive,
                StatInfluence.DamageOutput,
                SystemQueryStage.DamageModification),

            StatType.AttackSpeed => Define(
                statType,
                "Attack cadence expressed as attacks per second, not as a percentage modifier.",
                StatCategory.Offensive,
                StatValueKind.Rate,
                StatValueScale.RawDecimal,
                StatMeasurementUnit.AttacksPerSecond,
                StatBehaviorFamily.DirectValue,
                StatUsageKind.Direct,
                StatResolutionKind.None,
                SystemQueryStage.BuildAggregation | SystemQueryStage.AttackTiming,
                StatInfluence.AttackCadence,
                StatConstraint.NonNegative),

            StatType.CritChance => Define(
                statType,
                "Chance for an attack or skill to become critical before critical evasion is applied.",
                StatCategory.Offensive,
                StatValueKind.Percentage,
                StatValueScale.NormalizedFraction,
                StatMeasurementUnit.Probability,
                StatBehaviorFamily.OpposedCriticalChance,
                StatUsageKind.ResolvedBeforeUse,
                StatResolutionKind.CriticalChanceSuppression,
                SystemQueryStage.BuildAggregation | SystemQueryStage.CriticalCheck,
                StatInfluence.CriticalOutcome,
                StatConstraint.NonNegative | StatConstraint.RequiresOpposedResolution,
                opposedByStatType: StatType.CriticalEvasion),

            StatType.CritDamage => Define(
                statType,
                "Additional net damage added on a successful critical outcome. It is not a multiplicative crit modifier.",
                StatCategory.Offensive,
                StatValueKind.Flat,
                StatValueScale.RawDecimal,
                StatMeasurementUnit.Points,
                StatBehaviorFamily.DirectValue,
                StatUsageKind.Direct,
                StatResolutionKind.None,
                SystemQueryStage.BuildAggregation | SystemQueryStage.DamageModification,
                StatInfluence.DamageOutput,
                StatConstraint.NonNegative),

            StatType.Accuracy => Define(
                statType,
                "Raw hit-capability input opposed by Evasion and converted through a non-linear hit-chance curve.",
                StatCategory.Offensive,
                StatValueKind.Flat,
                StatValueScale.RawDecimal,
                StatMeasurementUnit.Points,
                StatBehaviorFamily.OpposedHitChance,
                StatUsageKind.ResolvedBeforeUse,
                StatResolutionKind.OpposedHitChanceCurve,
                SystemQueryStage.BuildAggregation | SystemQueryStage.HitCheck,
                StatInfluence.HitChance,
                StatConstraint.NonNegative | StatConstraint.NeverUseLinearly | StatConstraint.RequiresOpposedResolution | StatConstraint.ProvisionalBalanceFormula,
                opposedByStatType: StatType.Evasion,
                futureRuleNote: "The hit curve is intentionally technical and configurable until final combat balance is closed."),

            StatType.CriticalEvasion => Define(
                statType,
                "Direct critical-chance suppression expressed on the same normalized probability scale as CritChance. It is subtracted from the attacker's crit chance and never reduces non-critical base damage.",
                StatCategory.Defensive,
                StatValueKind.Percentage,
                StatValueScale.NormalizedFraction,
                StatMeasurementUnit.Probability,
                StatBehaviorFamily.OpposedCriticalChance,
                StatUsageKind.ResolvedBeforeUse,
                StatResolutionKind.CriticalChanceSuppression,
                SystemQueryStage.BuildAggregation | SystemQueryStage.CriticalCheck,
                StatInfluence.CriticalOutcome,
                StatConstraint.NonNegative | StatConstraint.RequiresOpposedResolution,
                opposedByStatType: StatType.CritChance),

            StatType.DefensePenetration => Define(
                statType,
                "Percentage of physical mitigation input ignored before defense is converted into final mitigation.",
                StatCategory.Offensive,
                StatValueKind.Percentage,
                StatValueScale.NormalizedFraction,
                StatMeasurementUnit.Percent,
                StatBehaviorFamily.MitigationBypass,
                StatUsageKind.Contextual,
                StatResolutionKind.None,
                SystemQueryStage.BuildAggregation | SystemQueryStage.DamageMitigation,
                StatInfluence.DamageOutput,
                StatConstraint.NonNegative),

            StatType.MagicPenetration => Define(
                statType,
                "Percentage of magical mitigation input ignored before magic resistance is converted into final mitigation.",
                StatCategory.Offensive,
                StatValueKind.Percentage,
                StatValueScale.NormalizedFraction,
                StatMeasurementUnit.Percent,
                StatBehaviorFamily.MitigationBypass,
                StatUsageKind.Contextual,
                StatResolutionKind.None,
                SystemQueryStage.BuildAggregation | SystemQueryStage.DamageMitigation,
                StatInfluence.DamageOutput,
                StatConstraint.NonNegative),

            StatType.AttackRange => Define(
                statType,
                "Spatial attack reach expressed in game-world units.",
                StatCategory.Offensive,
                StatValueKind.Spatial,
                StatValueScale.RawDecimal,
                StatMeasurementUnit.GameUnits,
                StatBehaviorFamily.MobilityModifier,
                StatUsageKind.Direct,
                StatResolutionKind.None,
                SystemQueryStage.BuildAggregation | SystemQueryStage.SpatialEvaluation,
                StatInfluence.SpatialReach,
                StatConstraint.NonNegative),

            StatType.Defense => ConvertedMitigation(
                statType,
                "Raw physical mitigation input that must be converted through an asymptotic mitigation curve.",
                futureRuleNote: "The default technical curve approaches a 90% mitigation ceiling without ever reaching it; final tuning remains open."),

            StatType.MagicResistance => ConvertedMitigation(
                statType,
                "Raw magical mitigation input that must be converted through an asymptotic mitigation curve.",
                futureRuleNote: "The default technical curve approaches a 90% mitigation ceiling without ever reaching it; final tuning remains open."),

            StatType.Evasion => Define(
                statType,
                "Raw avoidance input opposed by Accuracy and converted through a non-linear hit-chance curve.",
                StatCategory.Defensive,
                StatValueKind.Flat,
                StatValueScale.RawDecimal,
                StatMeasurementUnit.Points,
                StatBehaviorFamily.OpposedHitChance,
                StatUsageKind.ResolvedBeforeUse,
                StatResolutionKind.OpposedHitChanceCurve,
                SystemQueryStage.BuildAggregation | SystemQueryStage.HitCheck,
                StatInfluence.HitChance,
                StatConstraint.NonNegative | StatConstraint.NeverUseLinearly | StatConstraint.RequiresOpposedResolution | StatConstraint.ProvisionalBalanceFormula,
                opposedByStatType: StatType.Accuracy,
                futureRuleNote: "The hit curve is intentionally technical and configurable until final combat balance is closed."),

            StatType.Tenacity => Define(
                statType,
                "Percentage reduction applied to crowd-control duration only. It never prevents the initial application of a CC.",
                StatCategory.Defensive,
                StatValueKind.Percentage,
                StatValueScale.NormalizedFraction,
                StatMeasurementUnit.Percent,
                StatBehaviorFamily.CrowdControlDuration,
                StatUsageKind.ResolvedBeforeUse,
                StatResolutionKind.CrowdControlDurationReduction,
                SystemQueryStage.BuildAggregation | SystemQueryStage.CrowdControlDuration,
                StatInfluence.CrowdControlDuration,
                StatConstraint.NonNegative,
                futureRuleNote: "The current tenacity duration resolver is intentionally configurable and provisional until final crowd-control balance is closed."),

            StatType.CooldownReduction => SkillCadenceModifier(
                statType,
                "Percentage reduction applied to future skill cooldown durations.",
                StatInfluence.CooldownFlow),

            StatType.SkillRecoveryRate => SkillCadenceModifier(
                statType,
                "Percentage modifier affecting how quickly skills recover their ready state.",
                StatInfluence.SkillCadence),

            StatType.HealingEffectiveness => Define(
                statType,
                "Percentage modifier applied to outgoing healing produced by the character.",
                StatCategory.Healing,
                StatValueKind.Percentage,
                StatValueScale.NormalizedFraction,
                StatMeasurementUnit.Percent,
                StatBehaviorFamily.HealingModifier,
                StatUsageKind.Contextual,
                StatResolutionKind.None,
                SystemQueryStage.BuildAggregation | SystemQueryStage.HealingApplication,
                StatInfluence.HealingOutput,
                StatConstraint.NonNegative),

            StatType.HealingReceived => Define(
                statType,
                "Percentage modifier applied to incoming healing received by the character.",
                StatCategory.Healing,
                StatValueKind.Percentage,
                StatValueScale.NormalizedFraction,
                StatMeasurementUnit.Percent,
                StatBehaviorFamily.HealingModifier,
                StatUsageKind.Contextual,
                StatResolutionKind.None,
                SystemQueryStage.BuildAggregation | SystemQueryStage.HealingApplication,
                StatInfluence.HealingTaken,
                StatConstraint.NonNegative),

            StatType.MoveSpeed => Define(
                statType,
                "Movement velocity expressed in world units per second.",
                StatCategory.Utility,
                StatValueKind.Rate,
                StatValueScale.RawDecimal,
                StatMeasurementUnit.UnitsPerSecond,
                StatBehaviorFamily.MobilityModifier,
                StatUsageKind.Direct,
                StatResolutionKind.None,
                SystemQueryStage.BuildAggregation | SystemQueryStage.SpatialEvaluation,
                StatInfluence.Movement,
                StatConstraint.NonNegative),

            StatType.ExpGain => ProgressionModifier(statType, "Percentage modifier to experience gains."),
            StatType.DropRate => ProgressionModifier(statType, "Percentage modifier to drop probability or reward frequency."),
            StatType.DropQuality => ProgressionModifier(statType, "Percentage modifier that biases future drop quality resolution."),
            StatType.GatheringSpeed => ProgressionModifier(statType, "Percentage modifier to future gathering throughput or gather-time reduction."),
            StatType.MeditationSpeed => ProgressionModifier(statType, "Percentage modifier to future meditation throughput or meditation-time reduction."),

            StatType.HeatApplyChance => StatusApplyChance(statType, CombatConditionType.Heat, StatType.HeatEvadeChance),
            StatType.ColdApplyChance => StatusApplyChance(statType, CombatConditionType.Cold, StatType.ColdEvadeChance),
            StatType.ElectrifiedApplyChance => StatusApplyChance(statType, CombatConditionType.Electrified, StatType.ElectrifiedEvadeChance),
            StatType.PoisonApplyChance => StatusApplyChance(statType, CombatConditionType.Poison, StatType.PoisonEvadeChance),
            StatType.WeakenApplyChance => StatusApplyChance(statType, CombatConditionType.Weaken, StatType.WeakenEvadeChance),
            StatType.BlindApplyChance => StatusApplyChance(statType, CombatConditionType.Blind, StatType.BlindEvadeChance),
            StatType.StunApplyChance => StatusApplyChance(statType, CombatConditionType.Stun, StatType.StunEvadeChance),
            StatType.FreezeApplyChance => StatusApplyChance(statType, CombatConditionType.Freeze, StatType.FreezeEvadeChance),
            StatType.ParalyzeApplyChance => StatusApplyChance(statType, CombatConditionType.Paralyze, StatType.ParalyzeEvadeChance),

            StatType.HeatEvadeChance => StatusEvadeChance(statType, CombatConditionType.Heat, StatType.HeatApplyChance),
            StatType.ColdEvadeChance => StatusEvadeChance(statType, CombatConditionType.Cold, StatType.ColdApplyChance),
            StatType.ElectrifiedEvadeChance => StatusEvadeChance(statType, CombatConditionType.Electrified, StatType.ElectrifiedApplyChance),
            StatType.PoisonEvadeChance => StatusEvadeChance(statType, CombatConditionType.Poison, StatType.PoisonApplyChance),
            StatType.WeakenEvadeChance => StatusEvadeChance(statType, CombatConditionType.Weaken, StatType.WeakenApplyChance),
            StatType.BlindEvadeChance => StatusEvadeChance(statType, CombatConditionType.Blind, StatType.BlindApplyChance),
            StatType.StunEvadeChance => StatusEvadeChance(statType, CombatConditionType.Stun, StatType.StunApplyChance),
            StatType.FreezeEvadeChance => StatusEvadeChance(statType, CombatConditionType.Freeze, StatType.FreezeApplyChance),
            StatType.ParalyzeEvadeChance => StatusEvadeChance(statType, CombatConditionType.Paralyze, StatType.ParalyzeApplyChance),

            StatType.BasicAttackDamageIncrease => DamageIncrease(statType, "Percentage bonus applied to damage caused by basic attacks.", DamageModifierContext.BasicAttack),
            StatType.SkillDamageIncrease => DamageIncrease(statType, "Percentage bonus applied to damage caused by skills.", DamageModifierContext.Skill),
            StatType.CritDamageIncrease => DamageIncrease(statType, "Percentage bonus applied to the critical portion of damage when a crit succeeds.", DamageModifierContext.Critical),
            StatType.MonsterDamageIncrease => DamageIncrease(statType, "Percentage bonus applied when the damage target is a monster.", DamageModifierContext.Monster),
            StatType.BossDamageIncrease => DamageIncrease(statType, "Percentage bonus applied when the damage target is a boss.", DamageModifierContext.Boss),
            StatType.PvPDamageIncrease => DamageIncrease(statType, "Percentage bonus applied in PvP damage contexts.", DamageModifierContext.PvP),
            StatType.HeatDamageIncrease => ConditionDamageIncrease(statType, CombatConditionType.Heat),
            StatType.ColdDamageIncrease => ConditionDamageIncrease(statType, CombatConditionType.Cold),
            StatType.ElectrifiedDamageIncrease => ConditionDamageIncrease(statType, CombatConditionType.Electrified),
            StatType.PoisonDamageIncrease => ConditionDamageIncrease(statType, CombatConditionType.Poison),

            StatType.BasicAttackDamageReduction => DamageReduction(statType, "Percentage reduction against incoming basic-attack damage.", DamageModifierContext.BasicAttack),
            StatType.SkillDamageReduction => DamageReduction(statType, "Percentage reduction against incoming skill damage.", DamageModifierContext.Skill),
            StatType.CritDamageTakenReduction => DamageReduction(statType, "Percentage reduction against the critical portion of incoming damage.", DamageModifierContext.Critical),
            StatType.MonsterDamageReduction => DamageReduction(statType, "Percentage reduction against damage received from monsters.", DamageModifierContext.Monster),
            StatType.BossDamageReduction => DamageReduction(statType, "Percentage reduction against damage received from bosses.", DamageModifierContext.Boss),
            StatType.PvPDamageReduction => DamageReduction(statType, "Percentage reduction against PvP damage.", DamageModifierContext.PvP),
            StatType.HeatDamageReduction => ConditionDamageReduction(statType, CombatConditionType.Heat),
            StatType.ColdDamageReduction => ConditionDamageReduction(statType, CombatConditionType.Cold),
            StatType.ElectrifiedDamageReduction => ConditionDamageReduction(statType, CombatConditionType.Electrified),
            StatType.PoisonDamageReduction => ConditionDamageReduction(statType, CombatConditionType.Poison),

            _ => throw new ArgumentOutOfRangeException(nameof(statType), statType, "Unknown stat type.")
        };
    }

    private static StatDefinition ResourceMaximum(
        StatType statType,
        string description,
        SystemQueryStage extraStages,
        string? futureRuleNote = null)
    {
        return Define(
            statType,
            description,
            StatCategory.CombatResource,
            StatValueKind.Flat,
            StatValueScale.RawDecimal,
            StatMeasurementUnit.ResourcePoints,
            StatBehaviorFamily.ResourceMaximum,
            StatUsageKind.Direct,
            StatResolutionKind.None,
            SystemQueryStage.BuildAggregation | SystemQueryStage.ResourceValidation | extraStages,
            StatInfluence.ResourceMaximum,
            StatConstraint.NonNegative | StatConstraint.UsedAsRuntimeMaximum,
            isMaximum: true,
            futureRuleNote: futureRuleNote);
    }

    private static StatDefinition ResourceRecovery(
        StatType statType,
        string description,
        string? futureRuleNote = null)
    {
        return Define(
            statType,
            description,
            StatCategory.CombatResource,
            StatValueKind.Rate,
            StatValueScale.RawDecimal,
            StatMeasurementUnit.ResourcePointsPerSecond,
            StatBehaviorFamily.ResourceRecovery,
            StatUsageKind.Direct,
            StatResolutionKind.None,
            SystemQueryStage.BuildAggregation | SystemQueryStage.ResourceRecovery,
            StatInfluence.ResourceRecovery,
            StatConstraint.NonNegative,
            futureRuleNote: futureRuleNote);
    }

    private static StatDefinition DirectCombatValue(
        StatType statType,
        string description,
        StatCategory category,
        StatInfluence influences,
        SystemQueryStage queryStages)
    {
        return Define(
            statType,
            description,
            category,
            StatValueKind.Flat,
            StatValueScale.RawDecimal,
            StatMeasurementUnit.Points,
            StatBehaviorFamily.DirectValue,
            StatUsageKind.Direct,
            StatResolutionKind.None,
            SystemQueryStage.BuildAggregation | queryStages,
            influences,
            StatConstraint.NonNegative);
    }

    private static StatDefinition ConvertedMitigation(
        StatType statType,
        string description,
        string? futureRuleNote = null)
    {
        return Define(
            statType,
            description,
            StatCategory.Defensive,
            StatValueKind.Flat,
            StatValueScale.RawDecimal,
            StatMeasurementUnit.Points,
            StatBehaviorFamily.ConvertedMitigation,
            StatUsageKind.ResolvedBeforeUse,
            StatResolutionKind.AsymptoticMitigationCurve,
            SystemQueryStage.BuildAggregation | SystemQueryStage.DamageMitigation,
            StatInfluence.DamageTaken,
            StatConstraint.NonNegative | StatConstraint.NeverUseLinearly | StatConstraint.ProvisionalBalanceFormula,
            futureRuleNote: futureRuleNote);
    }

    private static StatDefinition SkillCadenceModifier(
        StatType statType,
        string description,
        StatInfluence influences)
    {
        return Define(
            statType,
            description,
            StatCategory.Recovery,
            StatValueKind.Percentage,
            StatValueScale.NormalizedFraction,
            StatMeasurementUnit.Percent,
            StatBehaviorFamily.SkillCadenceModifier,
            StatUsageKind.Contextual,
            StatResolutionKind.None,
            SystemQueryStage.BuildAggregation | SystemQueryStage.SkillTiming,
            influences,
            StatConstraint.NonNegative);
    }

    private static StatDefinition ProgressionModifier(StatType statType, string description)
    {
        return Define(
            statType,
            description,
            StatCategory.Progression,
            StatValueKind.Percentage,
            StatValueScale.NormalizedFraction,
            StatMeasurementUnit.Percent,
            StatBehaviorFamily.ProgressionModifier,
            StatUsageKind.Contextual,
            StatResolutionKind.None,
            SystemQueryStage.BuildAggregation | SystemQueryStage.ProgressionEvaluation,
            StatInfluence.ProgressionRewards,
            StatConstraint.NonNegative,
            isCombatStat: false);
    }

    private static StatDefinition StatusApplyChance(
        StatType statType,
        CombatConditionType relatedCondition,
        StatType opposedByStatType)
    {
        return Define(
            statType,
            $"Chance to attempt applying {relatedCondition} after a hit has already connected.",
            StatCategory.StatusApplication,
            StatValueKind.Percentage,
            StatValueScale.NormalizedFraction,
            StatMeasurementUnit.Probability,
            StatBehaviorFamily.StatusApplyChance,
            StatUsageKind.Contextual,
            StatResolutionKind.StatusChanceLookup,
            SystemQueryStage.BuildAggregation | SystemQueryStage.StatusApplication,
            StatInfluence.StatusApplication,
            StatConstraint.NonNegative | StatConstraint.EvaluateHitBeforeStatusChecks | StatConstraint.EvaluateApplyBeforeEvade,
            opposedByStatType: opposedByStatType,
            relatedCondition: relatedCondition,
            futureRuleNote: "Status application is evaluated only after hit confirmation and before the target's status-evade chance is checked.");
    }

    private static StatDefinition StatusEvadeChance(
        StatType statType,
        CombatConditionType relatedCondition,
        StatType opposedByStatType)
    {
        return Define(
            statType,
            $"Chance to evade {relatedCondition} after the incoming hit landed and the attacker passed the application roll.",
            StatCategory.StatusEvasion,
            StatValueKind.Percentage,
            StatValueScale.NormalizedFraction,
            StatMeasurementUnit.Probability,
            StatBehaviorFamily.StatusEvadeChance,
            StatUsageKind.Contextual,
            StatResolutionKind.StatusChanceLookup,
            SystemQueryStage.BuildAggregation | SystemQueryStage.StatusApplication,
            StatInfluence.StatusResistance,
            StatConstraint.NonNegative | StatConstraint.EvaluateHitBeforeStatusChecks | StatConstraint.EvaluateApplyBeforeEvade,
            opposedByStatType: opposedByStatType,
            relatedCondition: relatedCondition,
            futureRuleNote: "Status evasion is evaluated after hit confirmation and after the attacker's status-apply roll succeeds.");
    }

    private static StatDefinition DamageIncrease(
        StatType statType,
        string description,
        DamageModifierContext context)
    {
        return Define(
            statType,
            description,
            StatCategory.DamageIncrease,
            StatValueKind.Percentage,
            StatValueScale.NormalizedFraction,
            StatMeasurementUnit.Percent,
            StatBehaviorFamily.DamageModifier,
            StatUsageKind.Contextual,
            StatResolutionKind.DamageModifierLookup,
            SystemQueryStage.BuildAggregation | SystemQueryStage.DamageModification,
            StatInfluence.DamageOutput,
            StatConstraint.NonNegative,
            damageModifierContext: context);
    }

    private static StatDefinition DamageReduction(
        StatType statType,
        string description,
        DamageModifierContext context)
    {
        return Define(
            statType,
            description,
            StatCategory.DamageReduction,
            StatValueKind.Percentage,
            StatValueScale.NormalizedFraction,
            StatMeasurementUnit.Percent,
            StatBehaviorFamily.DamageModifier,
            StatUsageKind.Contextual,
            StatResolutionKind.DamageModifierLookup,
            SystemQueryStage.BuildAggregation | SystemQueryStage.DamageModification,
            StatInfluence.DamageTaken,
            StatConstraint.NonNegative,
            damageModifierContext: context);
    }

    private static StatDefinition ConditionDamageIncrease(StatType statType, CombatConditionType relatedCondition)
    {
        return Define(
            statType,
            $"Percentage bonus applied when damage is tagged as {relatedCondition} damage.",
            StatCategory.DamageIncrease,
            StatValueKind.Percentage,
            StatValueScale.NormalizedFraction,
            StatMeasurementUnit.Percent,
            StatBehaviorFamily.DamageModifier,
            StatUsageKind.Contextual,
            StatResolutionKind.DamageModifierLookup,
            SystemQueryStage.BuildAggregation | SystemQueryStage.DamageModification,
            StatInfluence.DamageOutput,
            StatConstraint.NonNegative,
            damageModifierContext: MapConditionToDamageContext(relatedCondition),
            relatedCondition: relatedCondition);
    }

    private static StatDefinition ConditionDamageReduction(StatType statType, CombatConditionType relatedCondition)
    {
        return Define(
            statType,
            $"Percentage reduction applied against incoming {relatedCondition} damage.",
            StatCategory.DamageReduction,
            StatValueKind.Percentage,
            StatValueScale.NormalizedFraction,
            StatMeasurementUnit.Percent,
            StatBehaviorFamily.DamageModifier,
            StatUsageKind.Contextual,
            StatResolutionKind.DamageModifierLookup,
            SystemQueryStage.BuildAggregation | SystemQueryStage.DamageModification,
            StatInfluence.DamageTaken,
            StatConstraint.NonNegative,
            damageModifierContext: MapConditionToDamageContext(relatedCondition),
            relatedCondition: relatedCondition);
    }

    private static DamageModifierContext MapConditionToDamageContext(CombatConditionType relatedCondition)
    {
        return relatedCondition switch
        {
            CombatConditionType.Heat => DamageModifierContext.Heat,
            CombatConditionType.Cold => DamageModifierContext.Cold,
            CombatConditionType.Electrified => DamageModifierContext.Electrified,
            CombatConditionType.Poison => DamageModifierContext.Poison,
            _ => throw new ArgumentOutOfRangeException(nameof(relatedCondition), relatedCondition, "No damage-modifier context is defined for this condition.")
        };
    }

    private static StatDefinition Define(
        StatType statType,
        string description,
        StatCategory category,
        StatValueKind valueKind,
        StatValueScale valueScale,
        StatMeasurementUnit measurementUnit,
        StatBehaviorFamily behaviorFamily,
        StatUsageKind usageKind,
        StatResolutionKind resolutionKind,
        SystemQueryStage queryStages,
        StatInfluence influences,
        StatConstraint constraints,
        bool isMaximum = false,
        bool allowsContributions = true,
        bool isCombatStat = true,
        StatType? opposedByStatType = null,
        DamageModifierContext? damageModifierContext = null,
        CombatConditionType? relatedCondition = null,
        string? futureRuleNote = null)
    {
        return new StatDefinition(
            statType,
            description,
            category,
            valueKind,
            valueScale,
            measurementUnit,
            behaviorFamily,
            usageKind,
            resolutionKind,
            queryStages,
            influences,
            constraints,
            isMaximum,
            allowsContributions,
            isCombatStat,
            opposedByStatType,
            damageModifierContext,
            relatedCondition,
            futureRuleNote);
    }
}



