namespace War.Core.Stats;

public enum StatType
{
    // Combat resources and maxima
    MaxHp,
    MaxMana,
    HpRegen,
    ManaRegen,
    UltimateChargeMax,

    // Offensive
    PhysicalAttack,
    MagicAttack,
    AttackSpeed,
    CritChance,
    CritDamage,
    Accuracy,
    CriticalEvasion,
    DefensePenetration,
    MagicPenetration,
    AttackRange,

    // Defensive
    Defense,
    MagicResistance,
    Evasion,
    Tenacity,

    // Recovery and skill cadence
    CooldownReduction,
    SkillRecoveryRate,

    // Healing and support
    HealingEffectiveness,
    HealingReceived,

    // Movement and utility
    MoveSpeed,

    // Progression and farming
    ExpGain,
    DropRate,
    DropQuality,
    GatheringSpeed,
    MeditationSpeed,

    // Status application
    HeatApplyChance,
    ColdApplyChance,
    ElectrifiedApplyChance,
    PoisonApplyChance,
    WeakenApplyChance,
    BlindApplyChance,
    StunApplyChance,
    FreezeApplyChance,
    ParalyzeApplyChance,

    // Status evasion
    HeatEvadeChance,
    ColdEvadeChance,
    ElectrifiedEvadeChance,
    PoisonEvadeChance,
    WeakenEvadeChance,
    BlindEvadeChance,
    StunEvadeChance,
    FreezeEvadeChance,
    ParalyzeEvadeChance,

    // Damage increases
    BasicAttackDamageIncrease,
    SkillDamageIncrease,
    CritDamageIncrease,
    MonsterDamageIncrease,
    BossDamageIncrease,
    PvPDamageIncrease,
    HeatDamageIncrease,
    ColdDamageIncrease,
    ElectrifiedDamageIncrease,
    PoisonDamageIncrease,

    // Damage reductions
    BasicAttackDamageReduction,
    SkillDamageReduction,
    CritDamageTakenReduction,
    MonsterDamageReduction,
    BossDamageReduction,
    PvPDamageReduction,
    HeatDamageReduction,
    ColdDamageReduction,
    ElectrifiedDamageReduction,
    PoisonDamageReduction
}