using System.Collections.ObjectModel;

namespace War.Core.Progression;

public interface ICharacterLevelProgressionService
{
    CharacterLevelTransition GetTransition(int fromLevel);

    IReadOnlyList<CharacterLevelTransition> GetAllTransitions();

    long GetExperienceRequiredForLevelUp(int currentLevel);

    long GetTotalExperienceRequiredToReachLevel(int targetLevel);

    CharacterLevelProgress CreateProgress(int level, long currentXp = 0, long? totalXp = null);

    CharacterExperienceGrantResult GrantExperience(CharacterLevelProgress progress, long grantedExperience);
}

public sealed class CharacterLevelProgressionService : ICharacterLevelProgressionService
{
    private readonly IReadOnlyDictionary<int, CharacterLevelTransition> _transitionsByFromLevel;
    private readonly IReadOnlyList<CharacterLevelTransition> _transitions;

    public CharacterLevelProgressionService()
    {
        _transitions = BuildTransitions();
        _transitionsByFromLevel = new ReadOnlyDictionary<int, CharacterLevelTransition>(
            _transitions.ToDictionary(transition => transition.FromLevel));
    }

    public static CharacterLevelProgressionService Default { get; } = new();

    public CharacterLevelTransition GetTransition(int fromLevel)
    {
        if (fromLevel == CharacterLevelRules.MaximumLevel)
        {
            return new CharacterLevelTransition(
                CharacterLevelRules.MaximumLevel,
                CharacterLevelRules.MaximumLevel,
                0,
                IncludesMilestoneBonus: false,
                CharacterLevelRules.CompoundGrowthMultiplier,
                AppliedMultiplier: 0m,
                Note: "Max-level characters have no further XP transition.");
        }

        ValidateTransitionLevel(fromLevel);

        return _transitionsByFromLevel[fromLevel];
    }

    public IReadOnlyList<CharacterLevelTransition> GetAllTransitions()
    {
        return _transitions;
    }

    public long GetExperienceRequiredForLevelUp(int currentLevel)
    {
        return GetTransition(currentLevel).ExperienceRequired;
    }

    public long GetTotalExperienceRequiredToReachLevel(int targetLevel)
    {
        ValidateLevel(targetLevel);

        long total = 0;

        for (var level = CharacterLevelRules.MinimumLevel; level < targetLevel; level++)
        {
            total += GetExperienceRequiredForLevelUp(level);
        }

        return total;
    }

    public CharacterLevelProgress CreateProgress(int level, long currentXp = 0, long? totalXp = null)
    {
        ValidateLevel(level);

        if (currentXp < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentXp), currentXp, "Current XP cannot be negative.");
        }

        var xpToNextLevel = GetExperienceRequiredForLevelUp(level);

        if (level < CharacterLevelRules.MaximumLevel && currentXp >= xpToNextLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentXp),
                currentXp,
                $"Current XP for level {level} must be lower than the next-level requirement ({xpToNextLevel}).");
        }

        if (level == CharacterLevelRules.MaximumLevel && currentXp != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentXp), currentXp, "Max-level characters must store current XP as 0 within the current level band.");
        }

        var minimumTotalXp = GetTotalExperienceRequiredToReachLevel(level) + currentXp;
        var resolvedTotalXp = totalXp ?? minimumTotalXp;

        if (resolvedTotalXp < minimumTotalXp)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalXp),
                resolvedTotalXp,
                $"Total XP cannot be lower than the minimum XP implied by level {level} and current XP {currentXp}.");
        }

        return new CharacterLevelProgress(level, currentXp, xpToNextLevel, resolvedTotalXp);
    }

    public CharacterExperienceGrantResult GrantExperience(CharacterLevelProgress progress, long grantedExperience)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (grantedExperience < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(grantedExperience), grantedExperience, "Granted experience cannot be negative.");
        }

        if (grantedExperience == 0)
        {
            return new CharacterExperienceGrantResult(
                progress,
                progress,
                grantedExperience,
                LevelsGained: 0,
                ExperienceAppliedWithinLevelBands: 0,
                OverflowExperienceAtMaxLevel: 0,
                AppliedTransitions: Array.Empty<CharacterLevelTransition>(),
                Notes: ["No experience was granted."]);
        }

        var currentLevel = progress.Level;
        var currentXp = progress.CurrentXp;
        var totalXp = progress.TotalXp + grantedExperience;
        var remainingExperience = grantedExperience;
        var levelsGained = 0;
        var transitions = new List<CharacterLevelTransition>();
        var notes = new List<string>();

        while (remainingExperience > 0 && currentLevel < CharacterLevelRules.MaximumLevel)
        {
            var xpToNextLevel = GetExperienceRequiredForLevelUp(currentLevel);
            var xpNeeded = xpToNextLevel - currentXp;

            if (remainingExperience < xpNeeded)
            {
                currentXp += remainingExperience;
                remainingExperience = 0;
                break;
            }

            remainingExperience -= xpNeeded;
            transitions.Add(GetTransition(currentLevel));
            currentLevel++;
            currentXp = 0;
            levelsGained++;
        }

        if (currentLevel == CharacterLevelRules.MaximumLevel)
        {
            currentXp = 0;

            if (remainingExperience > 0)
            {
                notes.Add("Max level reached; excess earned XP is preserved only in TotalXp for audit/history purposes.");
            }
        }

        var updatedProgress = CreateProgress(currentLevel, currentXp, totalXp);
        var appliedWithinBands = grantedExperience - remainingExperience;

        return new CharacterExperienceGrantResult(
            progress,
            updatedProgress,
            grantedExperience,
            levelsGained,
            appliedWithinBands,
            remainingExperience,
            Array.AsReadOnly(transitions.ToArray()),
            Array.AsReadOnly(notes.ToArray()));
    }

    private static IReadOnlyList<CharacterLevelTransition> BuildTransitions()
    {
        var transitions = new List<CharacterLevelTransition>();
        long previousExperienceRequired = 0;

        for (var fromLevel = CharacterLevelRules.MinimumLevel; fromLevel < CharacterLevelRules.MaximumLevel; fromLevel++)
        {
            var includesMilestoneBonus = CharacterLevelRules.IsMilestoneTransition(fromLevel);
            var appliedMultiplier = fromLevel == CharacterLevelRules.MinimumLevel
                ? 1m
                : CharacterLevelRules.CompoundGrowthMultiplier * (includesMilestoneBonus ? CharacterLevelRules.DecadeMilestoneMultiplier : 1m);
            var experienceRequired = fromLevel == CharacterLevelRules.MinimumLevel
                ? CharacterLevelRules.BaseExperienceRequiredForFirstLevelUp
                : RoundToLong(previousExperienceRequired * appliedMultiplier);
            var note = fromLevel == CharacterLevelRules.MinimumLevel
                ? "Base XP requirement for the first level-up transition."
                : includesMilestoneBonus
                    ? "This transition applied the normal 20% growth plus the explicit 50% decade milestone bonus."
                    : "This transition applied the normal 20% compound growth.";

            transitions.Add(new CharacterLevelTransition(
                fromLevel,
                fromLevel + 1,
                experienceRequired,
                includesMilestoneBonus,
                CharacterLevelRules.CompoundGrowthMultiplier,
                appliedMultiplier,
                note));

            previousExperienceRequired = experienceRequired;
        }

        return Array.AsReadOnly(transitions.ToArray());
    }

    private static long RoundToLong(decimal value)
    {
        return decimal.ToInt64(decimal.Round(value, 0, MidpointRounding.AwayFromZero));
    }

    private static void ValidateLevel(int level)
    {
        if (!CharacterLevelRules.IsSupportedLevel(level))
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                $"Character level must be between {CharacterLevelRules.MinimumLevel} and {CharacterLevelRules.MaximumLevel}.");
        }
    }

    private static void ValidateTransitionLevel(int level)
    {
        if (level < CharacterLevelRules.MinimumLevel || level > CharacterLevelRules.MaximumLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                $"Transition source level must be between {CharacterLevelRules.MinimumLevel} and {CharacterLevelRules.MaximumLevel}.");
        }
    }
}
