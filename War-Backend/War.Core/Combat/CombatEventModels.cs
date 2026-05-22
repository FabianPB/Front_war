using War.Core.Entities;
using War.Core.Resources;
using War.Core.Stats;

namespace War.Core.Combat;

public enum CombatActionKind
{
    Damage,
    Heal,
    Utility
}

public enum CombatActionSource
{
    BasicAttack,
    Skill
}

public enum CombatDamageType
{
    Physical,
    Magical,
    True
}

public enum CombatTargetClassification
{
    None,
    Player,
    Monster,
    Boss
}

public enum CombatEntityRole
{
    Actor,
    Target
}

public enum CombatImpactOutcome
{
    None,
    Aborted,
    Miss,
    Hit,
    Critical,
    Heal,
    Blocked,
    ResourceRestore
}

public enum CombatImpactFailureReason
{
    None,
    Evaded,
    InsufficientResources,
    TargetProtected
}

public enum CombatResourceChangeReason
{
    Damage,
    Healing,
    ResourceCost,
    ResourceRestore
}

public enum CombatConditionApplicationStatus
{
    Applied,
    AppliedByInteraction,
    Evaded,
    FailedApplyChance,
    SkippedByConditionRequirement,
    BlockedByProtection,
    SkippedBecauseImpactFailed
}

public enum CombatConditionApplicationSource
{
    DirectEffect,
    Interaction
}

public enum CombatActionResourceCostStatus
{
    Approved,
    RejectedInsufficientResource
}

public sealed record CombatResourceCost(
    CharacterResourceType ResourceType,
    decimal Amount,
    bool AbortIfInsufficient = true);

public sealed record CombatConditionApplicationChanceProfile(
    decimal? BaseApplyChance = null,
    decimal FlatApplyChanceBonus = 0m,
    decimal ApplyChanceMultiplier = 1m);

public sealed record CombatConditionApplicationIntent(
    CombatConditionType Condition,
    decimal? BaseDurationSeconds = null,
    CombatConditionApplicationChanceProfile? ChanceProfile = null,
    IReadOnlyList<CombatConditionType>? RequiredTargetConditions = null,
    string? EffectKey = null,
    string? Note = null);

public sealed record CombatEventContext(
    Character Actor,
    Character Target,
    CombatActionKind ActionKind,
    CombatActionSource ActionSource,
    decimal BaseMagnitude,
    CombatDamageType? DamageType = null,
    bool CanCrit = false,
    bool RequiresHitCheck = false,
    CharacterResourceType TargetResourceType = CharacterResourceType.Hp,
    CombatConditionType? DamageConditionType = null,
    CombatTargetClassification TargetClassification = CombatTargetClassification.None,
    IReadOnlyCollection<CombatConditionType>? TargetActiveConditions = null,
    IReadOnlyCollection<CombatProtectionState>? TargetActiveProtections = null,
    IReadOnlyCollection<CombatConditionApplicationIntent>? PotentialEffects = null,
    IReadOnlyCollection<CombatResourceCost>? DeclaredResourceCosts = null,
    CombatActionMagnitudeProfile? MagnitudeProfile = null,
    string? ActionName = null);

public sealed record CombatProbabilityCheckResult(
    decimal RequestedChance,
    decimal EffectiveChance,
    decimal Roll,
    bool Succeeded,
    bool WasChanceClamped,
    string? Note = null);

public sealed record CombatActionResourceCostResolution(
    CharacterResourceType ResourceType,
    decimal RequestedAmount,
    decimal AvailableAmount,
    bool AbortIfInsufficient,
    bool ResourceDefinitionRejectsInsufficientSpend,
    CombatActionResourceCostStatus Status,
    CombatResourceChangeProposal? ProjectedChange = null,
    string? Note = null)
{
    public bool WasApproved => Status == CombatActionResourceCostStatus.Approved;

    public bool WasRejected => !WasApproved;

    public bool RequiresSufficiencyToExecute =>
        AbortIfInsufficient || ResourceDefinitionRejectsInsufficientSpend;

    public bool CausesActionAbort =>
        WasRejected && RequiresSufficiencyToExecute;
}

public sealed class CombatActionResourceEvaluation
{
    public static CombatActionResourceEvaluation Empty { get; } =
        new(Array.Empty<CombatResourceCost>(), Array.Empty<CombatActionResourceCostResolution>(), false, Array.Empty<string>());

    public CombatActionResourceEvaluation(
        IEnumerable<CombatResourceCost>? declaredCosts,
        IEnumerable<CombatActionResourceCostResolution>? costResolutions,
        bool wasAbortedByInsufficientResources,
        IEnumerable<string>? notes)
    {
        DeclaredCosts = Array.AsReadOnly((declaredCosts ?? Array.Empty<CombatResourceCost>()).ToArray());
        CostResolutions = Array.AsReadOnly((costResolutions ?? Array.Empty<CombatActionResourceCostResolution>()).ToArray());
        WasAbortedByInsufficientResources = wasAbortedByInsufficientResources;
        Notes = Array.AsReadOnly((notes ?? Array.Empty<string>()).ToArray());
    }

    public IReadOnlyList<CombatResourceCost> DeclaredCosts { get; }

    public IReadOnlyList<CombatActionResourceCostResolution> CostResolutions { get; }

    public bool WasAbortedByInsufficientResources { get; }

    public IReadOnlyList<string> Notes { get; }

    public bool HasDeclaredCosts => DeclaredCosts.Count > 0;

    public bool IsActionViable => !WasAbortedByInsufficientResources;

    public IReadOnlyList<CombatActionResourceCostResolution> ApprovedCosts =>
        Array.AsReadOnly(CostResolutions.Where(resolution => resolution.WasApproved).ToArray());

    public IReadOnlyList<CombatActionResourceCostResolution> RejectedCosts =>
        Array.AsReadOnly(CostResolutions.Where(resolution => resolution.WasRejected).ToArray());

    public IReadOnlyList<CombatResourceChangeProposal> ApprovedProjectedChanges =>
        Array.AsReadOnly(CostResolutions
            .Where(resolution => resolution.ProjectedChange is not null)
            .Select(resolution => resolution.ProjectedChange!)
            .ToArray());
}

public sealed record CombatConditionResolution(
    CombatConditionType Condition,
    CombatConditionCategory Category,
    CombatConditionApplicationStatus Status,
    CombatConditionApplicationSource Source,
    CombatProbabilityCheckResult? ApplyCheck = null,
    CombatProbabilityCheckResult? EvadeCheck = null,
    decimal? BaseDurationSeconds = null,
    decimal? EffectiveDurationSeconds = null,
    CrowdControlDurationResolution? DurationResolution = null,
    IReadOnlyList<CombatConditionType>? RequiredTargetConditions = null,
    string? EffectKey = null,
    string? Note = null)
{
    public bool WasApplied =>
        Status is CombatConditionApplicationStatus.Applied or CombatConditionApplicationStatus.AppliedByInteraction;

    public bool WasAttempted =>
        Status is not CombatConditionApplicationStatus.SkippedBecauseImpactFailed and not CombatConditionApplicationStatus.SkippedByConditionRequirement;
}

public sealed record CombatConditionInteractionActivation(
    string Key,
    IReadOnlyList<CombatConditionType> RequiredConditions,
    decimal FinalDamageIncreasePercentage,
    CombatConditionType? AdditionalConditionApplied,
    string Description,
    string? FutureRuleNote = null);

public sealed record CombatConditionInteractionEvaluation(
    IReadOnlyList<CombatConditionInteractionActivation> Activations,
    IReadOnlyList<CombatConditionResolution> GeneratedConditionResults,
    decimal TotalFinalDamageIncreasePercentage,
    IReadOnlyList<string> Notes);

public sealed record CombatResourceChangeProposal(
    CombatEntityRole AffectedEntity,
    CharacterResourceType ResourceType,
    CombatResourceChangeReason Reason,
    decimal PreviousValue,
    decimal Delta,
    decimal UnclampedResult,
    decimal ProposedResult,
    decimal? MaximumValue,
    bool WasClampedToZero,
    bool WasClampedToMaximum,
    bool WouldTriggerDepletionResolution);

public sealed class CombatResolutionResult
{
    public CombatResolutionResult(
        CombatImpactOutcome outcome,
        CombatImpactFailureReason failureReason,
        bool wasHit,
        bool wasCritical,
        decimal baseMagnitude,
        CombatActionMagnitudeResolution actionMagnitudeResolution,
        decimal baseDamage,
        decimal criticalBonusDamage,
        decimal damageBeforeMitigation,
        decimal damageMitigatedAmount,
        decimal damageAfterMitigation,
        decimal damageAfterSourceReductions,
        decimal damageAfterConditionModifiers,
        decimal finalDamage,
        decimal finalHealing,
        decimal outgoingDamageIncreasePercentage,
        decimal criticalDamageIncreasePercentage,
        decimal incomingDamageReductionPercentage,
        decimal criticalDamageTakenReductionPercentage,
        decimal conditionDamageIncreasePercentage,
        decimal conditionDamageReductionPercentage,
        decimal interactionDamageIncreasePercentage,
        HitChanceResolution? hitChanceResolution,
        CombatProbabilityCheckResult? hitCheck,
        CriticalChanceResolution? criticalChanceResolution,
        CombatProbabilityCheckResult? criticalCheck,
        MitigationResolution? mitigationResolution,
        CombatActionResourceEvaluation? actionResourceEvaluation,
        IEnumerable<CombatResourceChangeProposal>? proposedResourceChanges,
        IEnumerable<CombatConditionResolution>? conditionResults,
        IEnumerable<CombatConditionInteractionActivation>? triggeredInteractions,
        IEnumerable<CombatResourceCost>? declaredResourceCosts,
        bool wouldCauseTargetDefeat,
        IEnumerable<string>? notes)
    {
        Outcome = outcome;
        FailureReason = failureReason;
        WasHit = wasHit;
        WasCritical = wasCritical;
        BaseMagnitude = baseMagnitude;
        ActionMagnitudeResolution = actionMagnitudeResolution;
        BaseDamage = baseDamage;
        CriticalBonusDamage = criticalBonusDamage;
        DamageBeforeMitigation = damageBeforeMitigation;
        DamageMitigatedAmount = damageMitigatedAmount;
        DamageAfterMitigation = damageAfterMitigation;
        DamageAfterSourceReductions = damageAfterSourceReductions;
        DamageAfterConditionModifiers = damageAfterConditionModifiers;
        FinalDamage = finalDamage;
        FinalHealing = finalHealing;
        OutgoingDamageIncreasePercentage = outgoingDamageIncreasePercentage;
        CriticalDamageIncreasePercentage = criticalDamageIncreasePercentage;
        IncomingDamageReductionPercentage = incomingDamageReductionPercentage;
        CriticalDamageTakenReductionPercentage = criticalDamageTakenReductionPercentage;
        ConditionDamageIncreasePercentage = conditionDamageIncreasePercentage;
        ConditionDamageReductionPercentage = conditionDamageReductionPercentage;
        InteractionDamageIncreasePercentage = interactionDamageIncreasePercentage;
        HitChanceResolution = hitChanceResolution;
        HitCheck = hitCheck;
        CriticalChanceResolution = criticalChanceResolution;
        CriticalCheck = criticalCheck;
        MitigationResolution = mitigationResolution;
        ActionResourceEvaluation = actionResourceEvaluation ?? CombatActionResourceEvaluation.Empty;
        ProposedResourceChanges = Array.AsReadOnly((proposedResourceChanges ?? Array.Empty<CombatResourceChangeProposal>()).ToArray());
        ConditionResults = Array.AsReadOnly((conditionResults ?? Array.Empty<CombatConditionResolution>()).ToArray());
        TriggeredInteractions = Array.AsReadOnly((triggeredInteractions ?? Array.Empty<CombatConditionInteractionActivation>()).ToArray());
        DeclaredResourceCosts = Array.AsReadOnly((declaredResourceCosts ?? Array.Empty<CombatResourceCost>()).ToArray());
        WouldCauseTargetDefeat = wouldCauseTargetDefeat;
        Notes = Array.AsReadOnly((notes ?? Array.Empty<string>()).ToArray());
    }

    public CombatImpactOutcome Outcome { get; }

    public CombatImpactFailureReason FailureReason { get; }

    public bool WasHit { get; }

    public bool WasCritical { get; }

    public decimal BaseMagnitude { get; }

    public CombatActionMagnitudeResolution ActionMagnitudeResolution { get; }

    public bool HasActionScaling => ActionMagnitudeResolution.HasScaling;

    public bool UsedLegacyBaseMagnitudeFallback => ActionMagnitudeResolution.UsedLegacyBaseMagnitudeFallback;

    public decimal FixedBaseMagnitudeComponent => ActionMagnitudeResolution.FixedBaseMagnitude;

    public StatType? ScalingStatType => ActionMagnitudeResolution.ScalingStatType;

    public decimal ScalingStatValue => ActionMagnitudeResolution.ScalingStatValue;

    public decimal ScalingCoefficient => ActionMagnitudeResolution.ScalingCoefficient;

    public decimal ScaledBaseMagnitudeContribution => ActionMagnitudeResolution.ScaledContribution;

    public decimal BaseDamage { get; }

    public decimal CriticalBonusDamage { get; }

    public decimal DamageBeforeMitigation { get; }

    public decimal DamageMitigatedAmount { get; }

    public decimal DamageAfterMitigation { get; }

    public decimal DamageAfterSourceReductions { get; }

    public decimal DamageAfterConditionModifiers { get; }

    public decimal FinalDamage { get; }

    public decimal FinalHealing { get; }

    public decimal FinalRestoration => FinalHealing;

    public decimal OutgoingDamageIncreasePercentage { get; }

    public decimal CriticalDamageIncreasePercentage { get; }

    public decimal IncomingDamageReductionPercentage { get; }

    public decimal CriticalDamageTakenReductionPercentage { get; }

    public decimal ConditionDamageIncreasePercentage { get; }

    public decimal ConditionDamageReductionPercentage { get; }

    public decimal InteractionDamageIncreasePercentage { get; }

    public HitChanceResolution? HitChanceResolution { get; }

    public CombatProbabilityCheckResult? HitCheck { get; }

    public CriticalChanceResolution? CriticalChanceResolution { get; }

    public CombatProbabilityCheckResult? CriticalCheck { get; }

    public MitigationResolution? MitigationResolution { get; }

    public CombatActionResourceEvaluation ActionResourceEvaluation { get; }

    public IReadOnlyList<CombatResourceChangeProposal> ProposedResourceChanges { get; }

    public IReadOnlyList<CombatResourceChangeProposal> ImpactResourceChanges => ProposedResourceChanges;

    public IReadOnlyList<CombatConditionResolution> ConditionResults { get; }

    public IReadOnlyList<CombatConditionInteractionActivation> TriggeredInteractions { get; }

    public IReadOnlyList<CombatResourceCost> DeclaredResourceCosts { get; }

    public bool WouldCauseTargetDefeat { get; }

    public IReadOnlyList<string> Notes { get; }

    public bool WasAbortedByResourceCost => ActionResourceEvaluation.WasAbortedByInsufficientResources;

    public bool WasBlockedByProtection => FailureReason == CombatImpactFailureReason.TargetProtected;

    public IReadOnlyList<CombatActionResourceCostResolution> ApprovedActionCosts =>
        ActionResourceEvaluation.ApprovedCosts;

    public IReadOnlyList<CombatActionResourceCostResolution> RejectedActionCosts =>
        ActionResourceEvaluation.RejectedCosts;

    public IReadOnlyList<CombatResourceChangeProposal> ApprovedActionCostChanges =>
        ActionResourceEvaluation.ApprovedProjectedChanges;

    public IReadOnlyList<CombatConditionResolution> AttemptedEffects =>
        Array.AsReadOnly(ConditionResults.Where(result => result.WasAttempted).ToArray());

    public IReadOnlyList<CombatConditionResolution> AppliedEffects =>
        Array.AsReadOnly(ConditionResults.Where(result => result.WasApplied).ToArray());

    public IReadOnlyList<CombatConditionResolution> EvadedEffects =>
        Array.AsReadOnly(ConditionResults.Where(result => result.Status == CombatConditionApplicationStatus.Evaded).ToArray());

    public IReadOnlyList<CombatConditionResolution> FailedEffectsByChance =>
        Array.AsReadOnly(ConditionResults.Where(result => result.Status == CombatConditionApplicationStatus.FailedApplyChance).ToArray());
}







