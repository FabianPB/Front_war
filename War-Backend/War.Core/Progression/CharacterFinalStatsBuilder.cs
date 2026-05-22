using War.Core.Skills;
using War.Core.Stats;

namespace War.Core.Progression;

public sealed record CharacterFinalStatsBuildContext(
    ClassType ClassType,
    int Level,
    IEnumerable<IStatSource>? AdditionalSources = null);

public sealed record CharacterFinalStatsBuildResult(
    FinalStats FinalStats,
    IReadOnlyList<IStatSource> Sources);

public interface ICharacterFinalStatsBuilder
{
    FinalStats Build(ClassType classType, int level, IEnumerable<IStatSource>? additionalSources = null);

    CharacterFinalStatsBuildResult Build(CharacterFinalStatsBuildContext context);
}

public sealed class CharacterFinalStatsBuilder : ICharacterFinalStatsBuilder
{
    private readonly IClassLevelGrowthCatalog _growthCatalog;
    private readonly StatsCalculator _statsCalculator;

    public CharacterFinalStatsBuilder(
        IClassLevelGrowthCatalog? growthCatalog = null,
        StatsCalculator? statsCalculator = null)
    {
        _growthCatalog = growthCatalog ?? ClassLevelGrowthCatalog.Default;
        _statsCalculator = statsCalculator ?? new StatsCalculator();
    }

    public FinalStats Build(ClassType classType, int level, IEnumerable<IStatSource>? additionalSources = null)
    {
        return Build(new CharacterFinalStatsBuildContext(classType, level, additionalSources)).FinalStats;
    }

    public CharacterFinalStatsBuildResult Build(CharacterFinalStatsBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sources = new List<IStatSource>
        {
            new CharacterLevelStatSource(context.ClassType, context.Level, _growthCatalog)
        };

        if (context.AdditionalSources is not null)
        {
            sources.AddRange(context.AdditionalSources);
        }

        var finalStats = _statsCalculator.Calculate(sources);

        return new CharacterFinalStatsBuildResult(
            finalStats,
            Array.AsReadOnly(sources.ToArray()));
    }
}
