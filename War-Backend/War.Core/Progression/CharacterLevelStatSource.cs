using War.Core.Skills;
using War.Core.Stats;

namespace War.Core.Progression;

public sealed class CharacterLevelStatSource : IStatSource
{
    private readonly ClassLevelGrowthProfile _profile;
    private readonly int _level;

    public CharacterLevelStatSource(
        ClassType classType,
        int level,
        IClassLevelGrowthCatalog? growthCatalog = null)
    {
        if (!CharacterLevelRules.IsSupportedLevel(level))
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                $"Character level must be between {CharacterLevelRules.MinimumLevel} and {CharacterLevelRules.MaximumLevel}.");
        }

        _profile = (growthCatalog ?? ClassLevelGrowthCatalog.Default).GetRequired(classType);
        _level = level;
    }

    public IEnumerable<StatContribution> GetContributions()
    {
        var growthStacks = Math.Max(0, _level - CharacterLevelRules.MinimumLevel);
        if (growthStacks == 0)
        {
            yield break;
        }

        var sourceKey = $"progression.level.{_profile.ClassType.ToString().ToLowerInvariant()}";
        var sourceName = $"Level Growth ({_profile.ClassType})";

        foreach (var entry in _profile.PerLevelStatGains.OrderBy(entry => entry.Key))
        {
            var totalValue = entry.Value * growthStacks;
            if (totalValue == 0m)
            {
                continue;
            }

            yield return new StatContribution
            {
                StatType = entry.Key,
                Value = totalValue,
                SourceName = sourceName,
                SourceKey = sourceKey,
                ContributionType = StatContributionType.Flat
            };
        }
    }
}
