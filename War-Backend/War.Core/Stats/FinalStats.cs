using System.Collections.ObjectModel;

namespace War.Core.Stats;

public class FinalStats
{
    private readonly Dictionary<StatType, decimal> _values = new();
    private readonly Dictionary<StatType, IReadOnlyList<StatContribution>> _contributions = new();
    private readonly IReadOnlyDictionary<StatType, decimal> _readOnlyValues;
    private readonly IReadOnlyDictionary<StatType, IReadOnlyList<StatContribution>> _readOnlyContributions;

    public FinalStats()
    {
        _readOnlyValues = new ReadOnlyDictionary<StatType, decimal>(_values);
        _readOnlyContributions = new ReadOnlyDictionary<StatType, IReadOnlyList<StatContribution>>(_contributions);
    }

    public decimal this[StatType statType] => Get(statType);

    public void Set(StatType statType, decimal value)
    {
        _values[statType] = value;
        _contributions.Remove(statType);
    }

    public void Set(StatType statType, decimal value, IEnumerable<StatContribution> contributions)
    {
        Set(statType, value);
        SetContributions(statType, contributions);
    }

    public void SetContributions(StatType statType, IEnumerable<StatContribution> contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);

        _contributions[statType] = Array.AsReadOnly(contributions.ToArray());
    }

    public decimal Get(StatType statType)
    {
        return _values.TryGetValue(statType, out var value) ? value : 0m;
    }

    public IReadOnlyList<StatContribution> GetContributions(StatType statType)
    {
        return _contributions.TryGetValue(statType, out var contributions)
            ? contributions
            : Array.Empty<StatContribution>();
    }

    public IReadOnlyDictionary<StatType, decimal> GetAll()
    {
        return _readOnlyValues;
    }

    public IReadOnlyDictionary<StatType, IReadOnlyList<StatContribution>> GetAllContributions()
    {
        return _readOnlyContributions;
    }
}