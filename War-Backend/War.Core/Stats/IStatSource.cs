namespace War.Core.Stats;

public interface IStatSource
{
    IEnumerable<StatContribution> GetContributions();
}