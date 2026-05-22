using System.Collections.Generic;

namespace War.Core.Stats;

public class DummyStatSource : IStatSource
{
    public IEnumerable<StatContribution> GetContributions()
    {
        return new List<StatContribution>
        {
            new StatContribution { StatType = StatType.Accuracy, Value = 25, SourceName = "Test" },
            new StatContribution { StatType = StatType.Accuracy, Value = 10, SourceName = "Test 2" },
            new StatContribution { StatType = StatType.Evasion, Value = 5, SourceName = "Test 3" }
        };
    }
}