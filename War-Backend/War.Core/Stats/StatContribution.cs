namespace War.Core.Stats;

public class StatContribution
{
    public StatType StatType { get; init; }
    public decimal Value { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string? SourceKey { get; init; }
    public StatContributionType ContributionType { get; init; } = StatContributionType.Flat;
}