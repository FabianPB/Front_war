namespace War.Core.Progression;

public static class CharacterLevelRules
{
    public const int MinimumLevel = 1;
    public const int MaximumLevel = 80;
    public const long BaseExperienceRequiredForFirstLevelUp = 1000;
    public const decimal CompoundGrowthMultiplier = 1.20m;
    public const decimal DecadeMilestoneMultiplier = 1.50m;

    public static bool IsSupportedLevel(int level)
    {
        return level >= MinimumLevel && level <= MaximumLevel;
    }

    public static bool IsMilestoneTransition(int fromLevel)
    {
        return fromLevel >= MinimumLevel &&
               fromLevel < MaximumLevel &&
               ((fromLevel + 1) % 10 == 0);
    }
}
