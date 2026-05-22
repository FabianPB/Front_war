using War.Core.Entities;
using War.Core.PowerScore;
using War.Core.Resources;
using War.Core.Skills;
using War.Core.Stats;

namespace War.Core.Progression;

public sealed record LevelPowerScoreDeltaEntry(
    int FromLevel,
    int ToLevel,
    IReadOnlyDictionary<ClassType, decimal> DeltaByClass,
    decimal AverageDelta,
    decimal MinimumDelta,
    decimal MaximumDelta,
    decimal Spread,
    decimal SpreadRatio);

public sealed record ClassPowerScoreDeltaSummary(
    ClassType ClassType,
    decimal AverageDelta,
    decimal MinimumDelta,
    decimal MaximumDelta,
    decimal TotalGrowthAcrossAuditedRange,
    decimal AverageCoverageRatio,
    decimal AverageCoverageBlend,
    IReadOnlyList<string>? Notes = null);

public sealed record LevelProgressionFairnessAuditResult(
    int FromLevel,
    int ToLevel,
    IReadOnlyList<LevelPowerScoreDeltaEntry> Deltas,
    IReadOnlyList<ClassPowerScoreDeltaSummary> ClassSummaries,
    decimal AverageSpreadRatio,
    decimal MaximumSpreadRatio,
    IReadOnlyList<string>? Notes = null);

public interface ILevelProgressionFairnessAuditService
{
    LevelProgressionFairnessAuditResult Audit(
        IEnumerable<ClassType>? classTypes = null,
        int fromLevel = CharacterLevelRules.MinimumLevel,
        int toLevel = CharacterLevelRules.MaximumLevel);
}

public sealed class LevelProgressionFairnessAuditService : ILevelProgressionFairnessAuditService
{
    private readonly ICharacterFinalStatsBuilder _finalStatsBuilder;
    private readonly IPowerScoreCalculator _powerScoreCalculator;

    public LevelProgressionFairnessAuditService(
        ICharacterFinalStatsBuilder? finalStatsBuilder = null,
        IPowerScoreCalculator? powerScoreCalculator = null)
    {
        _finalStatsBuilder = finalStatsBuilder ?? new CharacterFinalStatsBuilder();
        _powerScoreCalculator = powerScoreCalculator ?? new PowerScoreCalculator();
    }

    public LevelProgressionFairnessAuditResult Audit(
        IEnumerable<ClassType>? classTypes = null,
        int fromLevel = CharacterLevelRules.MinimumLevel,
        int toLevel = CharacterLevelRules.MaximumLevel)
    {
        ValidateRange(fromLevel, toLevel);

        var classes = (classTypes ?? SkillCatalogRules.InitialClasses)
            .Distinct()
            .OrderBy(classType => classType)
            .ToArray();
        var notes = new List<string>
        {
            "Fairness is evaluated with the current referential Power Score only. It does not change combat output.",
            "Because most skill catalogs and all basic-attack profiles are still incomplete, coverage-blended class weights intentionally stay conservative."
        };
        var scoresByClass = new Dictionary<ClassType, Dictionary<int, (decimal Score, PowerScoreResult Result)>>();

        foreach (var classType in classes)
        {
            var perLevelScores = new Dictionary<int, (decimal Score, PowerScoreResult Result)>();

            for (var level = fromLevel; level <= toLevel; level++)
            {
                perLevelScores[level] = CalculateScoreAtLevel(classType, level);
            }

            scoresByClass[classType] = perLevelScores;
        }

        var deltaEntries = new List<LevelPowerScoreDeltaEntry>();

        for (var level = fromLevel; level < toLevel; level++)
        {
            var deltaByClass = new Dictionary<ClassType, decimal>();

            foreach (var classType in classes)
            {
                var currentScore = scoresByClass[classType][level].Score;
                var nextScore = scoresByClass[classType][level + 1].Score;
                deltaByClass[classType] = nextScore - currentScore;
            }

            var minimumDelta = deltaByClass.Values.Min();
            var maximumDelta = deltaByClass.Values.Max();
            var averageDelta = deltaByClass.Values.Average();
            var spread = maximumDelta - minimumDelta;
            var spreadRatio = averageDelta == 0m ? 0m : spread / averageDelta;

            deltaEntries.Add(new LevelPowerScoreDeltaEntry(
                level,
                level + 1,
                deltaByClass,
                averageDelta,
                minimumDelta,
                maximumDelta,
                spread,
                spreadRatio));
        }

        var classSummaries = classes
            .Select(classType =>
            {
                var deltas = deltaEntries.Select(entry => entry.DeltaByClass[classType]).ToArray();
                var usageResults = scoresByClass[classType].Values.Select(entry => entry.Result.UsageAnalysis).ToArray();
                var summaryNotes = new List<string>();

                if (usageResults.Any(result => result.CoverageRatio < 1m))
                {
                    summaryNotes.Add("Current Power Score coverage is incomplete for this class, so contextual weights are partially blended back toward neutral.");
                }

                return new ClassPowerScoreDeltaSummary(
                    classType,
                    deltas.Average(),
                    deltas.Min(),
                    deltas.Max(),
                    scoresByClass[classType][toLevel].Score - scoresByClass[classType][fromLevel].Score,
                    usageResults.Average(result => result.CoverageRatio),
                    usageResults.Average(result => result.CoverageBlend),
                    Array.AsReadOnly(summaryNotes.ToArray()));
            })
            .ToArray();

        var averageSpreadRatio = deltaEntries.Count == 0 ? 0m : deltaEntries.Average(entry => entry.SpreadRatio);
        var maximumSpreadRatio = deltaEntries.Count == 0 ? 0m : deltaEntries.Max(entry => entry.SpreadRatio);

        if (maximumSpreadRatio > 0.20m)
        {
            notes.Add("The audited Power Score delta spread exceeds 20% in at least one sampled level transition. Review class growth or finish missing class kit metadata before final balance lock.");
        }

        return new LevelProgressionFairnessAuditResult(
            fromLevel,
            toLevel,
            Array.AsReadOnly(deltaEntries.ToArray()),
            Array.AsReadOnly(classSummaries),
            averageSpreadRatio,
            maximumSpreadRatio,
            Array.AsReadOnly(notes.ToArray()));
    }

    private (decimal Score, PowerScoreResult Result) CalculateScoreAtLevel(ClassType classType, int level)
    {
        var finalStats = _finalStatsBuilder.Build(classType, level);
        var resources = new CharacterResources(
            finalStats.Get(StatType.MaxHp),
            finalStats.Get(StatType.MaxMana),
            0m);
        var character = new Character(Guid.Empty, finalStats, resources, classType, level);
        var result = _powerScoreCalculator.Calculate(new PowerScoreCalculationContext(
            character,
            SkillProgress: null,
            SkillCatalog: SkillCatalogRegistry.Current,
            ClassProfile: PowerScoreClassProfileRegistry.GetRequired(classType)));

        return (result.TotalScore, result);
    }

    private static void ValidateRange(int fromLevel, int toLevel)
    {
        if (!CharacterLevelRules.IsSupportedLevel(fromLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(fromLevel), fromLevel, $"From level must be between {CharacterLevelRules.MinimumLevel} and {CharacterLevelRules.MaximumLevel}.");
        }

        if (!CharacterLevelRules.IsSupportedLevel(toLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(toLevel), toLevel, $"To level must be between {CharacterLevelRules.MinimumLevel} and {CharacterLevelRules.MaximumLevel}.");
        }

        if (toLevel <= fromLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(toLevel), toLevel, "To level must be greater than from level.");
        }
    }
}
