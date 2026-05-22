using Microsoft.EntityFrameworkCore;
using War.Api.Application.SkillAdmin;
using War.Core.Skills;
using War.Infrastructure.Persistence;

namespace War.Api.Application.SkillRuntime;

public enum RuntimeSkillSourceKind
{
    ProgrammedCatalog,
    PublishedPersisted
}

public sealed record RuntimeResolvedSkillDefinition(
    string SkillId,
    SkillDefinition Definition,
    RuntimeSkillSourceKind SourceKind,
    Guid? RecordId = null,
    int? DraftVersion = null,
    int? PublishedVersion = null,
    DateTimeOffset? PublishedAtUtc = null,
    string? PublishedBy = null,
    string? ResolutionNote = null);

public sealed record RuntimeClassSkillCatalogSnapshot(
    ClassType ClassType,
    IReadOnlyList<RuntimeResolvedSkillDefinition> Skills,
    IReadOnlyList<string> Notes);

public sealed record RuntimeSkillCatalogSnapshot(
    SkillCatalog Catalog,
    IReadOnlyList<RuntimeResolvedSkillDefinition> Skills,
    IReadOnlyList<string> Notes);

public interface IProgrammedSkillCatalogSource
{
    SkillCatalog GetCatalog();
}

public sealed class ProgrammedSkillCatalogSource : IProgrammedSkillCatalogSource
{
    public SkillCatalog GetCatalog()
    {
        return SkillCatalogRegistry.Current;
    }
}

public interface IPublishedSkillCatalogSource
{
    Task<IReadOnlyList<RuntimeResolvedSkillDefinition>> GetPublishedSkillsAsync(CancellationToken cancellationToken = default);
}

public sealed class PublishedSkillCatalogSource : IPublishedSkillCatalogSource
{
    private readonly WarDbContext _dbContext;

    public PublishedSkillCatalogSource(WarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RuntimeResolvedSkillDefinition>> GetPublishedSkillsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.AdminSkillRecords
            .AsNoTracking()
            .Where(entity => !entity.IsDeleted && entity.PublishedVersion != null && entity.PublishedDefinitionJson != null)
            .OrderBy(entity => entity.ClassType)
            .ThenBy(entity => entity.Slot)
            .ThenBy(entity => entity.Name)
            .ToListAsync(cancellationToken);

        var entries = new List<RuntimeResolvedSkillDefinition>();

        foreach (var entity in entities)
        {
            var definition = SkillAdminJsonSerializer.Deserialize<SkillDefinition>(entity.PublishedDefinitionJson!);
            if (definition is null)
            {
                continue;
            }

            entries.Add(new RuntimeResolvedSkillDefinition(
                definition.Id,
                definition,
                RuntimeSkillSourceKind.PublishedPersisted,
                entity.RecordId,
                entity.DraftVersion,
                entity.PublishedVersion,
                entity.PublishedAtUtc,
                entity.PublishedBy,
                "Resolved from the published admin skill snapshot."));
        }

        return Array.AsReadOnly(entries.ToArray());
    }
}

public interface ISkillRuntimeCatalogProvider
{
    Task<RuntimeSkillCatalogSnapshot> GetRuntimeCatalogAsync(CancellationToken cancellationToken = default);

    Task<RuntimeClassSkillCatalogSnapshot> GetRuntimeClassCatalogAsync(ClassType classType, CancellationToken cancellationToken = default);

    Task<RuntimeResolvedSkillDefinition?> FindRuntimeSkillAsync(string skillId, CancellationToken cancellationToken = default);

    Task<RuntimeResolvedSkillDefinition> GetRequiredRuntimeSkillAsync(string skillId, CancellationToken cancellationToken = default);
}

public sealed class SkillRuntimeCatalogProvider : ISkillRuntimeCatalogProvider
{
    private readonly IProgrammedSkillCatalogSource _programmedSource;
    private readonly IPublishedSkillCatalogSource _publishedSource;

    public SkillRuntimeCatalogProvider(
        IProgrammedSkillCatalogSource programmedSource,
        IPublishedSkillCatalogSource publishedSource)
    {
        _programmedSource = programmedSource;
        _publishedSource = publishedSource;
    }

    public async Task<RuntimeSkillCatalogSnapshot> GetRuntimeCatalogAsync(CancellationToken cancellationToken = default)
    {
        var programmedCatalog = _programmedSource.GetCatalog();
        var publishedEntries = await _publishedSource.GetPublishedSkillsAsync(cancellationToken);
        return RuntimeSkillCatalogComposer.Compose(programmedCatalog, publishedEntries);
    }

    public async Task<RuntimeClassSkillCatalogSnapshot> GetRuntimeClassCatalogAsync(ClassType classType, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetRuntimeCatalogAsync(cancellationToken);
        var skills = snapshot.Skills
            .Where(skill => skill.Definition.ClassType == classType)
            .OrderBy(skill => skill.Definition.Slot.GetOrder())
            .ToArray();
        var notes = snapshot.Notes
            .Where(note => note.Contains(classType.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return new RuntimeClassSkillCatalogSnapshot(
            classType,
            Array.AsReadOnly(skills),
            Array.AsReadOnly(notes));
    }

    public async Task<RuntimeResolvedSkillDefinition?> FindRuntimeSkillAsync(string skillId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);

        var snapshot = await GetRuntimeCatalogAsync(cancellationToken);
        return snapshot.Skills.FirstOrDefault(skill => string.Equals(skill.SkillId, skillId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<RuntimeResolvedSkillDefinition> GetRequiredRuntimeSkillAsync(string skillId, CancellationToken cancellationToken = default)
    {
        var definition = await FindRuntimeSkillAsync(skillId, cancellationToken);
        return definition ?? throw new KeyNotFoundException($"Skill '{skillId}' is not resolvable from the runtime catalog provider.");
    }
}
