using War.Core.Resources;
using War.Core.Skills;
using War.Core.Stats;

namespace War.Core.Entities;

public class Character
{
    public Guid Id { get; private set; }

    public ClassType ClassType { get; private set; }

    public int Level { get; private set; }

    public FinalStats Stats { get; private set; }

    public CharacterResources Resources { get; private set; }

    public Character(
        Guid id,
        FinalStats stats,
        CharacterResources resources,
        ClassType classType = ClassType.Sorcerer,
        int level = SkillCatalogRules.MinimumCharacterLevel)
    {
        ArgumentNullException.ThrowIfNull(stats);
        ArgumentNullException.ThrowIfNull(resources);

        if (level < SkillCatalogRules.MinimumCharacterLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                $"Character level must be at least {SkillCatalogRules.MinimumCharacterLevel}.");
        }

        Id = id;
        ClassType = classType;
        Level = level;
        Stats = stats;
        Resources = resources;
    }

    public decimal GetResourceMaximum(CharacterResourceType resourceType)
    {
        return Stats.Get(resourceType.GetMaximumStat());
    }

    public decimal GetCurrentResource(CharacterResourceType resourceType)
    {
        return Resources.Get(resourceType);
    }

    public bool HasAvailableResource(CharacterResourceType resourceType, decimal requiredAmount)
    {
        return Resources.HasAvailable(resourceType, requiredAmount);
    }
}
