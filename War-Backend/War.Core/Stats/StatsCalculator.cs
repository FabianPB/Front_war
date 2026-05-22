namespace War.Core.Stats;

public class StatsCalculator
{
    public FinalStats Calculate(IEnumerable<IStatSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        return Calculate(CollectContributions(sources));
    }

    public FinalStats Calculate(IEnumerable<StatContribution> contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);

        var finalStats = new FinalStats();

        foreach (var groupedContributions in GroupByStat(contributions))
        {
            var statContributions = groupedContributions.ToArray();
            var total = Aggregate(statContributions);

            finalStats.Set(groupedContributions.Key, total, statContributions);
        }

        return finalStats;
    }

    private static IEnumerable<StatContribution> CollectContributions(IEnumerable<IStatSource> sources)
    {
        foreach (var source in sources)
        {
            ArgumentNullException.ThrowIfNull(source);

            var contributions = source.GetContributions();
            ArgumentNullException.ThrowIfNull(contributions);

            foreach (var contribution in contributions)
            {
                ArgumentNullException.ThrowIfNull(contribution);

                if (!contribution.StatType.AllowsContributions())
                {
                    throw new InvalidOperationException($"Stat '{contribution.StatType}' does not accept source contributions.");
                }

                yield return contribution;
            }
        }
    }

    private static IEnumerable<IGrouping<StatType, StatContribution>> GroupByStat(IEnumerable<StatContribution> contributions)
    {
        return contributions.GroupBy(contribution => contribution.StatType);
    }

    private static decimal Aggregate(IEnumerable<StatContribution> contributions)
    {
        decimal total = 0m;

        foreach (var contribution in contributions)
        {
            total += contribution.ContributionType switch
            {
                StatContributionType.Flat => contribution.Value,
                _ => throw new NotSupportedException($"Contribution type '{contribution.ContributionType}' is not supported yet.")
            };
        }

        return total;
    }
}