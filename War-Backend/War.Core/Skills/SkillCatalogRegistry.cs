using War.Core.Skills.Catalogs;

namespace War.Core.Skills;

public static class SkillCatalogRegistry
{
    public static SkillCatalog Current { get; } = CreateCurrentCatalog();

    public static SkillDefinition GetRequired(string skillId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);

        var definition = Current.ClassCatalogs
            .SelectMany(catalog => catalog.Skills)
            .FirstOrDefault(skill => string.Equals(skill.Id, skillId, StringComparison.OrdinalIgnoreCase));

        return definition ?? throw new KeyNotFoundException($"Skill '{skillId}' is not registered in the current catalog registry.");
    }

    private static SkillCatalog CreateCurrentCatalog()
    {
        var catalog = new SkillCatalog(
        [
            SorcererSkillCatalog.CreateCatalog(),
            JuramentadaSkillCatalog.CreateCatalog(),
            LanceroSkillCatalog.CreateCatalog(),
            BruiserSkillCatalog.CreateCatalog()
        ]);

        var issues = new List<SkillValidationIssue>();
        issues.AddRange(catalog.Validate(requireFullKit: false));
        issues.AddRange(catalog.ClassCatalogs
            .Where(classCatalog => classCatalog.Skills.Count > 0)
            .SelectMany(classCatalog => classCatalog.Validate(requireFullKit: true)));

        if (issues.Count > 0)
        {
            var message = string.Join(Environment.NewLine, issues.Select(issue => $"[{issue.Code}] {issue.Message}"));
            throw new InvalidOperationException($"The current skill catalog registry is invalid:{Environment.NewLine}{message}");
        }

        return catalog;
    }
}
