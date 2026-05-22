using War.Core.Skills;

namespace War.Api.Application.SkillRuntime;

internal static class RuntimeSkillCatalogComposer
{
    public static RuntimeSkillCatalogSnapshot Compose(
        SkillCatalog programmedCatalog,
        IReadOnlyList<RuntimeResolvedSkillDefinition> publishedEntries)
    {
        var classSnapshots = new List<RuntimeClassSkillCatalogSnapshot>();
        var notes = new List<string>
        {
            "Runtime resolution policy: published persisted skills override the programmed catalog only when the merged class catalog remains valid.",
            "Draft-only admin edits never enter runtime.",
            "Archived admin skills never enter runtime."
        };

        foreach (var classType in SkillCatalogRules.InitialClasses)
        {
            classSnapshots.Add(ComposeClassCatalog(programmedCatalog, publishedEntries, classType));
        }

        notes.AddRange(classSnapshots.SelectMany(snapshot => snapshot.Notes));

        var runtimeCatalog = new SkillCatalog(classSnapshots
            .Select(snapshot => new ClassSkillCatalog(snapshot.ClassType, snapshot.Skills.Select(skill => skill.Definition))));

        return new RuntimeSkillCatalogSnapshot(
            runtimeCatalog,
            Array.AsReadOnly(classSnapshots.SelectMany(snapshot => snapshot.Skills).ToArray()),
            Array.AsReadOnly(notes.Distinct(StringComparer.Ordinal).ToArray()));
    }

    
    public static IReadOnlyList<SkillValidationIssue> GetMergedClassIssues(
        SkillCatalog programmedCatalog,
        IReadOnlyList<RuntimeResolvedSkillDefinition> publishedEntries,
        ClassType classType)
    {
        var programmedClassCatalog = programmedCatalog.ClassCatalogs.FirstOrDefault(catalog => catalog.ClassType == classType)
            ?? new ClassSkillCatalog(classType);
        var programmedDefinitions = programmedClassCatalog.Skills.ToDictionary(skill => skill.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var publishedEntry in publishedEntries.Where(entry => entry.Definition.ClassType == classType))
        {
            programmedDefinitions[publishedEntry.SkillId] = publishedEntry.Definition;
        }

        var mergedCatalog = new ClassSkillCatalog(classType, programmedDefinitions.Values);
        return mergedCatalog.Validate(requireFullKit: false);
    }
    public static RuntimeClassSkillCatalogSnapshot ComposeClassCatalog(
        SkillCatalog programmedCatalog,
        IReadOnlyList<RuntimeResolvedSkillDefinition> publishedEntries,
        ClassType classType)
    {
        var programmedClassCatalog = programmedCatalog.ClassCatalogs.FirstOrDefault(catalog => catalog.ClassType == classType)
            ?? new ClassSkillCatalog(classType);
        var programmedEntries = programmedClassCatalog.Skills
            .Select(skill => new RuntimeResolvedSkillDefinition(
                skill.Id,
                skill,
                RuntimeSkillSourceKind.ProgrammedCatalog,
                ResolutionNote: "Resolved from the programmed skill catalog."))
            .ToArray();
        var publishedClassEntries = publishedEntries
            .Where(entry => entry.Definition.ClassType == classType)
            .OrderBy(entry => entry.Definition.Slot.GetOrder())
            .ThenBy(entry => entry.Definition.Name, StringComparer.Ordinal)
            .ToArray();

        if (publishedClassEntries.Length == 0)
        {
            return new RuntimeClassSkillCatalogSnapshot(classType, Array.AsReadOnly(programmedEntries), Array.Empty<string>());
        }

        var mergedBySkillId = programmedEntries.ToDictionary(entry => entry.SkillId, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in publishedClassEntries)
        {
            mergedBySkillId[entry.SkillId] = entry;
        }

        var mergedEntries = mergedBySkillId.Values
            .OrderBy(entry => entry.Definition.Slot.GetOrder())
            .ThenBy(entry => entry.Definition.Name, StringComparer.Ordinal)
            .ToArray();
        var mergedCatalog = new ClassSkillCatalog(classType, mergedEntries.Select(entry => entry.Definition));
        var issues = mergedCatalog.Validate(requireFullKit: false);

        if (issues.Count == 0)
        {
            return new RuntimeClassSkillCatalogSnapshot(
                classType,
                Array.AsReadOnly(mergedEntries),
                Array.AsReadOnly(new[]
                {
                    $"Runtime class {classType} resolved {publishedClassEntries.Length} published persisted skill(s) with programmed fallback for missing entries."
                }));
        }

        var issueText = string.Join(" | ", issues.Select(issue => $"[{issue.Code}] {issue.Message}"));
        return new RuntimeClassSkillCatalogSnapshot(
            classType,
            Array.AsReadOnly(programmedEntries),
            Array.AsReadOnly(new[]
            {
                $"Runtime class {classType} ignored published persisted overrides and fell back to the programmed catalog because the merged class catalog was invalid: {issueText}"
            }));
    }
}

