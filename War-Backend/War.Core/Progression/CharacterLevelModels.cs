namespace War.Core.Progression;

public sealed record CharacterLevelTransition(
    int FromLevel,
    int ToLevel,
    long ExperienceRequired,
    bool IncludesMilestoneBonus,
    decimal CompoundMultiplier,
    decimal AppliedMultiplier,
    string? Note = null);

public sealed record CharacterLevelProgress(
    int Level,
    long CurrentXp,
    long XpToNextLevel,
    long TotalXp)
{
    public bool IsAtMaxLevel => Level >= CharacterLevelRules.MaximumLevel;

    public long RemainingXpToNextLevel => IsAtMaxLevel ? 0 : Math.Max(0L, XpToNextLevel - CurrentXp);
}

public sealed record CharacterExperienceGrantResult(
    CharacterLevelProgress PreviousProgress,
    CharacterLevelProgress UpdatedProgress,
    long GrantedExperience,
    int LevelsGained,
    long ExperienceAppliedWithinLevelBands,
    long OverflowExperienceAtMaxLevel,
    IReadOnlyList<CharacterLevelTransition> AppliedTransitions,
    IReadOnlyList<string>? Notes = null);
