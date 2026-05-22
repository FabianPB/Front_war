using War.Core.Stats;

namespace War.Core.Resources;

public static class CharacterResourceTypeExtensions
{
    public static CharacterResourceDefinition GetDefinition(this CharacterResourceType resourceType)
    {
        return CharacterResourceCatalog.Get(resourceType);
    }

    public static StatType GetMaximumStat(this CharacterResourceType resourceType)
    {
        return resourceType.GetDefinition().MaximumStatType;
    }

    public static bool IsRuntimeResource(this CharacterResourceType resourceType)
    {
        return resourceType.GetDefinition().IsRuntimeResource;
    }

    public static bool IsPersistent(this CharacterResourceType resourceType)
    {
        return resourceType.GetDefinition().IsPersistent;
    }

    public static CharacterResourceConstraint GetConstraints(this CharacterResourceType resourceType)
    {
        return resourceType.GetDefinition().Constraints;
    }

    public static string GetDescription(this CharacterResourceType resourceType)
    {
        return resourceType.GetDefinition().Description;
    }

    public static string? GetFutureRuleNote(this CharacterResourceType resourceType)
    {
        return resourceType.GetDefinition().FutureRuleNote;
    }
}