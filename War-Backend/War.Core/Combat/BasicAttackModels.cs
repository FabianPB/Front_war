using War.Core.Entities;
using War.Core.Resources;
using War.Core.Skills;
using War.Core.Stats;

namespace War.Core.Combat;

public sealed record BasicAttackMagnitudeCoefficients(
    decimal MagicAttackCoefficient,
    decimal PhysicalAttackCoefficient,
    string? Note = null)
{
    public bool HasMagicContribution => MagicAttackCoefficient > 0m;

    public bool HasPhysicalContribution => PhysicalAttackCoefficient > 0m;
}

public sealed record BasicAttackComboProfile(
    int StageCount,
    decimal ContinuationWindowSeconds,
    decimal SequentialStageMultiplier,
    decimal? FinalStageMultiplierOverrideFromPrevious = null,
    int? FinalStageOverrideStage = null,
    string? FinalStageOverrideNote = null)
{
    public int ResolvedFinalStageOverrideStage => FinalStageOverrideStage ?? StageCount;
}

public sealed record BasicAttackRuntimeState(
    int LastCompletedStage,
    DateTimeOffset? LastCompletedAtUtc)
{
    public static BasicAttackRuntimeState Empty { get; } = new(0, null);

    public bool HasCompletedStage => LastCompletedStage > 0 && LastCompletedAtUtc.HasValue;
}

public sealed record BasicAttackComboStatus(
    int LastCompletedStage,
    DateTimeOffset? LastCompletedAtUtc,
    int NextStage,
    int ComboLength,
    bool IsContinuationWindowActive,
    DateTimeOffset? ContinuationWindowExpiresAtUtc,
    decimal ContinuationWindowSeconds,
    decimal CastTimeSeconds,
    string? Note = null);

public sealed record BasicAttackComboResolution(
    int StageToExecute,
    int ComboLength,
    int? PreviousCompletedStage,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    bool StartedFresh,
    bool ContinuedFromPreviousStage,
    bool ResetBecauseWindowExpired,
    DateTimeOffset? PreviousCompletedAtUtc,
    DateTimeOffset? PreviousContinuationWindowExpiresAtUtc,
    decimal CastTimeSeconds,
    decimal ContinuationWindowSeconds,
    string? Note = null);

public sealed record BasicAttackMagnitudeBreakdown(
    decimal StageMultiplier,
    decimal MagicAttackStatValue,
    decimal PhysicalAttackStatValue,
    decimal MagicAttackContribution,
    decimal PhysicalAttackContribution,
    decimal FinalBaseMagnitude,
    string? Note = null);

public sealed record BasicAttackCombatPlan(
    BasicAttackDefinition Definition,
    BasicAttackComboResolution Combo,
    BasicAttackMagnitudeBreakdown Magnitude,
    CombatEventContext EventContext,
    IReadOnlyList<string> Notes);

public interface IBasicAttackComboResolver
{
    BasicAttackComboStatus Describe(BasicAttackDefinition definition, BasicAttackRuntimeState runtimeState, DateTimeOffset referenceTimeUtc);

    BasicAttackComboResolution Resolve(BasicAttackDefinition definition, BasicAttackRuntimeState runtimeState, DateTimeOffset startedAtUtc);
}

public interface IBasicAttackMagnitudeResolver
{
    BasicAttackMagnitudeBreakdown Resolve(BasicAttackDefinition definition, Character actor, int comboStage);
}

public interface IClassBasicAttackCatalog
{
    BasicAttackDefinition GetRequired(ClassType classType);

    IReadOnlyCollection<BasicAttackDefinition> GetAll();
}

public interface IBasicAttackCombatTranslator
{
    BasicAttackCombatPlan Prepare(
        BasicAttackDefinition definition,
        Character actor,
        Character target,
        BasicAttackRuntimeState runtimeState,
        DateTimeOffset startedAtUtc,
        CombatTargetClassification targetClassification = CombatTargetClassification.None,
        IReadOnlyCollection<CombatConditionType>? targetActiveConditions = null,
        IReadOnlyCollection<CombatProtectionState>? targetActiveProtections = null);
}

public sealed class BasicAttackComboResolver : IBasicAttackComboResolver
{
    public static BasicAttackComboResolver Default { get; } = new();

    public BasicAttackComboStatus Describe(
        BasicAttackDefinition definition,
        BasicAttackRuntimeState runtimeState,
        DateTimeOffset referenceTimeUtc)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var windowExpiresAtUtc = runtimeState.LastCompletedAtUtc?.AddSeconds((double)definition.Combo.ContinuationWindowSeconds);
        var isWindowActive = runtimeState.HasCompletedStage &&
            windowExpiresAtUtc.HasValue &&
            referenceTimeUtc <= windowExpiresAtUtc.Value;
        var nextStage = runtimeState.HasCompletedStage && isWindowActive
            ? WrapStage(runtimeState.LastCompletedStage + 1, definition.Combo.StageCount)
            : 1;
        var note = runtimeState.HasCompletedStage && !isWindowActive
            ? "The continuation window expired, so the next basic attack restarts at stage 1."
            : null;

        return new BasicAttackComboStatus(
            runtimeState.LastCompletedStage,
            runtimeState.LastCompletedAtUtc,
            nextStage,
            definition.Combo.StageCount,
            isWindowActive,
            windowExpiresAtUtc,
            definition.Combo.ContinuationWindowSeconds,
            definition.CastTimeSeconds,
            note);
    }

    public BasicAttackComboResolution Resolve(
        BasicAttackDefinition definition,
        BasicAttackRuntimeState runtimeState,
        DateTimeOffset startedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var status = Describe(definition, runtimeState, startedAtUtc);
        var previousCompletedStage = runtimeState.HasCompletedStage
            ? (int?)runtimeState.LastCompletedStage
            : null;
        var continuedFromPreviousStage = previousCompletedStage.HasValue && status.IsContinuationWindowActive;
        var resetBecauseWindowExpired = previousCompletedStage.HasValue && !status.IsContinuationWindowActive;
        var completedAtUtc = startedAtUtc.AddSeconds((double)definition.CastTimeSeconds);
        var note = resetBecauseWindowExpired
            ? $"The combo restarted because more than {definition.Combo.ContinuationWindowSeconds:0.##}s elapsed between the previous completion and the new cast start."
            : continuedFromPreviousStage
                ? $"The combo continued from stage {previousCompletedStage} into stage {status.NextStage}."
                : "The combo started at stage 1.";

        return new BasicAttackComboResolution(
            status.NextStage,
            definition.Combo.StageCount,
            previousCompletedStage,
            startedAtUtc,
            completedAtUtc,
            StartedFresh: status.NextStage == 1,
            ContinuedFromPreviousStage: continuedFromPreviousStage,
            ResetBecauseWindowExpired: resetBecauseWindowExpired,
            runtimeState.LastCompletedAtUtc,
            status.ContinuationWindowExpiresAtUtc,
            definition.CastTimeSeconds,
            definition.Combo.ContinuationWindowSeconds,
            note);
    }

    private static int WrapStage(int stage, int comboLength)
    {
        if (comboLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(comboLength), comboLength, "Combo length must be positive.");
        }

        if (stage <= comboLength)
        {
            return stage;
        }

        var wrapped = stage % comboLength;
        return wrapped == 0 ? comboLength : wrapped;
    }
}

public sealed class BasicAttackMagnitudeResolver : IBasicAttackMagnitudeResolver
{
    public static BasicAttackMagnitudeResolver Default { get; } = new();

    public BasicAttackMagnitudeBreakdown Resolve(BasicAttackDefinition definition, Character actor, int comboStage)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(actor);

        if (comboStage < 1 || comboStage > definition.Combo.StageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(comboStage), comboStage, $"Combo stage must be between 1 and {definition.Combo.StageCount}.");
        }

        var stageMultiplier = ResolveStageMultiplier(definition.Combo, comboStage);
        var magicAttack = actor.Stats.Get(StatType.MagicAttack);
        var physicalAttack = actor.Stats.Get(StatType.PhysicalAttack);
        var magicContribution = magicAttack * definition.BaseStageCoefficients.MagicAttackCoefficient * stageMultiplier;
        var physicalContribution = physicalAttack * definition.BaseStageCoefficients.PhysicalAttackCoefficient * stageMultiplier;
        var finalMagnitude = magicContribution + physicalContribution;
        var note = comboStage == definition.Combo.ResolvedFinalStageOverrideStage &&
            definition.Combo.FinalStageMultiplierOverrideFromPrevious.HasValue
            ? definition.Combo.FinalStageOverrideNote
            : null;

        return new BasicAttackMagnitudeBreakdown(
            stageMultiplier,
            magicAttack,
            physicalAttack,
            magicContribution,
            physicalContribution,
            finalMagnitude,
            note);
    }

    private static decimal ResolveStageMultiplier(BasicAttackComboProfile combo, int comboStage)
    {
        var multiplier = 1m;

        for (var stage = 2; stage <= comboStage; stage++)
        {
            var stepMultiplier = combo.SequentialStageMultiplier;
            if (combo.FinalStageMultiplierOverrideFromPrevious.HasValue &&
                stage == combo.ResolvedFinalStageOverrideStage)
            {
                stepMultiplier = combo.FinalStageMultiplierOverrideFromPrevious.Value;
            }

            multiplier *= stepMultiplier;
        }

        return multiplier;
    }
}
