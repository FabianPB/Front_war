namespace War.Core.Skills;

public static class SkillCatalogRules
{
    public const int InitialClassCount = 4;
    public const int SkillsPerClass = 13;
    public const int MinimumCharacterLevel = 1;
    public const int MinimumAscensionLevel = 1;
    public const int MaximumAscensionLevel = 10;

    public static IReadOnlyList<ClassType> InitialClasses { get; } = Array.AsReadOnly(
    [
        ClassType.Sorcerer,
        ClassType.Juramentada,
        ClassType.Lancero,
        ClassType.Bruiser
    ]);
}
