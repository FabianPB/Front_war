using War.Core.Combat;
using War.Core.Resources;

namespace War.Core.Skills;

public enum SkillActionType
{
    Damage,
    Heal,
    Utility
}

public enum SkillDamageType
{
    Physical,
    Magical,
    True
}

public enum SkillScalingType
{
    FixedOnly,
    PhysicalAttack,
    MagicAttack,
    TargetMissingHp
}

public enum SkillElementType
{
    Arcane,
    Fire,
    Lightning,
    Ice,
    Poison,
    Neutral
}

public enum SkillCombatRole
{
    Poke,
    Burst,
    Control,
    Area,
    Pressure,
    Detonation,
    Chain,
    MultiHit,
    Ultimate
}

public enum SkillTargetingPattern
{
    Self,
    SingleTarget,
    Area,
    Line,
    Cone,
    GroundPoint
}

public enum SkillTargetAffinity
{
    Self,
    Ally,
    Enemy,
    Any
}

public enum SkillExecutionTriggerPhase
{
    OnCast,
    DuringActiveWindow,
    OnCompletion
}

public enum SkillTriggeredActionTargetSelector
{
    SelectedTarget,
    Self
}

public enum SkillHitDistributionMode
{
    EvenlyDistributed
}

public enum SkillAscensionMaterialType
{
    UniversalRefinedBook,
    SkillSpecificSpecialBook,
    SkillSpecificEpicBook,
    SkillSpecificLegendaryBook
}

public sealed record SkillPendingDatum(
    string Key,
    string Description,
    bool BlocksExactCombatSimulation = false);

/// <summary>
/// Perfil de magnitud de daño/curación de una habilidad. Soporta escalado dual
/// de dos estadísticas del personaje (primaria + secundaria) para permitir que
/// cada clase tome daño de ambas stats en proporciones distintas.
///
/// Ejemplo: Sorcerer con coeficiente 1.55 → Primary: MagicAttack × 1.395,
///          Secondary: PhysicalAttack × 0.155 → Total: 90% mágico + 10% físico.
/// </summary>
public sealed record SkillMagnitudeProfile(
    decimal BaseMagnitude,
    SkillScalingType ScalingType = SkillScalingType.FixedOnly,
    decimal ScalingCoefficient = 0m,
    // Escalado secundario: permite que una fracción del daño venga de otra stat.
    // Default FixedOnly + 0 = sin contribución secundaria (retrocompatible).
    SkillScalingType SecondaryScalingType = SkillScalingType.FixedOnly,
    decimal SecondaryScalingCoefficient = 0m,
    string? ConfigurationName = null);

public sealed record SkillResourceCostDefinition(
    CharacterResourceType ResourceType,
    decimal Amount,
    bool AbortIfInsufficient = true);

public sealed record SkillCadenceProfile(
    decimal BaseCooldownSeconds = 0m,
    bool AffectedByCooldownReduction = true,
    bool AffectedBySkillRecoveryRate = true,
    // Tiempo de casteo en segundos. Secreto del sistema — el cliente
    // nunca lo ve; solo lo siente como una ventana en la que sus siguientes
    // inputs son rechazados. Default conservador de 0.30s.
    decimal CastTimeSeconds = 0.30m);

public sealed record SkillTargetingProfile(
    SkillTargetingPattern Pattern,
    SkillTargetAffinity Affinity,
    decimal BaseRangeUnits,
    decimal? AreaRadiusUnits = null,
    int MaxTargets = 1,
    bool RequiresTargetSelection = true,
    string? Note = null);

public sealed record SkillConditionEffectDefinition(
    string EffectKey,
    CombatConditionType Condition,
    decimal? BaseDurationSeconds = null,
    decimal? BaseApplyChance = null,
    decimal ApplyChanceFlatBonus = 0m,
    decimal ApplyChanceMultiplier = 1m,
    IReadOnlyList<CombatConditionType>? RequiredTargetConditions = null,
    string? Note = null);

public sealed record SkillConditionEffectOverride(
    string EffectKey,
    decimal? BaseDurationSeconds = null,
    decimal? BaseApplyChance = null,
    decimal? ApplyChanceFlatBonus = null,
    decimal? ApplyChanceMultiplier = null,
    string? Note = null);

public sealed record SkillConditionSynergyDefinition(
    string SynergyKey,
    CombatConditionType RequiredTargetCondition,
    decimal MagnitudeMultiplier = 1m,
    decimal FlatBaseMagnitudeBonus = 0m,
    string? Note = null);

public sealed record SkillMultiHitProfile(
    int HitCount,
    decimal ActiveDurationSeconds,
    SkillHitDistributionMode Distribution = SkillHitDistributionMode.EvenlyDistributed,
    bool EffectsResolvePerHit = true,
    string? Note = null)
{
    public decimal HitIntervalSeconds => HitCount <= 0 ? 0m : ActiveDurationSeconds / HitCount;
}

public sealed record SkillProtectionGrantDefinition(
    string GrantKey,
    CombatProtectionType ProtectionType,
    CombatProtectionBlockType Blocks,
    decimal BaseDurationSeconds,
    CombatProtectionRefreshPolicy RefreshPolicy = CombatProtectionRefreshPolicy.IgnoreIfAlreadyActive,
    bool RemovesExistingNegativeEffects = false,
    string? Note = null);

public sealed record SkillTriggeredActionDefinition(
    string ActionKey,
    SkillExecutionTriggerPhase TriggerPhase,
    SkillActionDefinition Action,
    SkillTriggeredActionTargetSelector TargetSelector = SkillTriggeredActionTargetSelector.SelectedTarget,
    IReadOnlyList<SkillConditionEffectDefinition>? Effects = null,
    string? Note = null);

public sealed record SkillAscensionMaterialCost(
    SkillAscensionMaterialType MaterialType,
    int Quantity,
    string? ItemKey = null,
    string? Note = null);

public sealed record SkillAscensionUpgradeCost(
    IReadOnlyList<SkillAscensionMaterialCost>? Materials = null,
    string? PendingReason = null)
{
    public bool HasPendingData => !string.IsNullOrWhiteSpace(PendingReason);
}

public sealed record SkillActionDefinition(
    SkillActionType ActionType,
    SkillMagnitudeProfile MagnitudeProfile,
    SkillDamageType? DamageType = null,
    CharacterResourceType TargetResourceType = CharacterResourceType.Hp,
    bool RequiresHitCheck = false,
    bool CanCrit = false,
    CombatConditionType? DamageConditionType = null,
    IReadOnlyList<SkillConditionSynergyDefinition>? ConditionSynergies = null);

public sealed record SkillTuningSnapshot(
    SkillActionDefinition Action,
    SkillTargetingProfile Targeting,
    SkillCadenceProfile Cadence,
    IReadOnlyList<SkillResourceCostDefinition>? ResourceCosts = null,
    IReadOnlyList<SkillConditionEffectDefinition>? Effects = null,
    SkillMultiHitProfile? MultiHit = null,
    IReadOnlyList<SkillProtectionGrantDefinition>? CastProtections = null,
    IReadOnlyList<SkillTriggeredActionDefinition>? TriggeredActions = null);

public sealed record SkillAscensionOverrides(
    int AscensionLevel,
    SkillActionDefinition? Action = null,
    SkillMagnitudeProfile? MagnitudeProfile = null,
    SkillTargetingProfile? Targeting = null,
    SkillCadenceProfile? Cadence = null,
    IReadOnlyList<SkillResourceCostDefinition>? ResourceCosts = null,
    IReadOnlyList<SkillConditionEffectOverride>? EffectOverrides = null,
    IReadOnlyList<SkillConditionEffectDefinition>? AddedEffects = null,
    IReadOnlyList<string>? RemovedEffectKeys = null,
    SkillMultiHitProfile? MultiHit = null,
    IReadOnlyList<SkillProtectionGrantDefinition>? CastProtections = null,
    IReadOnlyList<SkillTriggeredActionDefinition>? TriggeredActions = null,
    SkillAscensionUpgradeCost? UpgradeCost = null,
    string? Note = null);

public sealed record SkillDefinition(
    string Id,
    string Name,
    string Description,
    ClassType ClassType,
    SkillSlot Slot,
    bool IsUltimate,
    int UnlockLevel,
    SkillTuningSnapshot BaseTuning,
    IReadOnlyDictionary<int, SkillAscensionOverrides>? AscensionOverrides = null,
    IReadOnlyList<SkillElementType>? Elements = null,
    IReadOnlyList<SkillCombatRole>? Roles = null,
    string? Notes = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    IReadOnlyList<SkillPendingDatum>? PendingData = null,
    IReadOnlyList<string>? SecurityNotes = null);

public sealed record ResolvedSkillDefinition(
    string Id,
    string Name,
    string Description,
    ClassType ClassType,
    SkillSlot Slot,
    bool IsUltimate,
    int UnlockLevel,
    int AscensionLevel,
    SkillTuningSnapshot Tuning,
    IReadOnlyList<SkillElementType>? Elements = null,
    IReadOnlyList<SkillCombatRole>? Roles = null,
    string? Notes = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    IReadOnlyList<string>? ResolutionNotes = null,
    IReadOnlyList<SkillPendingDatum>? PendingData = null,
    IReadOnlyList<string>? SecurityNotes = null);
