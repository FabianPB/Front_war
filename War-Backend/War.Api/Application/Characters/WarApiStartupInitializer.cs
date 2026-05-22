using Microsoft.EntityFrameworkCore;
using War.Api.Application.SkillAdmin;
using War.Infrastructure.Persistence;

namespace War.Api.Application.Characters;

public interface IWarApiStartupInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Orquestador de arranque: aplica migraciones de BD e importa el catálogo de skills admin.
/// </summary>
public sealed class WarApiStartupInitializer : IWarApiStartupInitializer
{
    private readonly WarDbContext _dbContext;
    private readonly IDatabaseSchemaCompatibilityService _databaseSchemaCompatibilityService;
    private readonly ISkillAdminCatalogService _skillAdminCatalogService;
    private readonly ILogger<WarApiStartupInitializer> _logger;

    public WarApiStartupInitializer(
        WarDbContext dbContext,
        IDatabaseSchemaCompatibilityService databaseSchemaCompatibilityService,
        ISkillAdminCatalogService skillAdminCatalogService,
        ILogger<WarApiStartupInitializer> logger)
    {
        _dbContext = dbContext;
        _databaseSchemaCompatibilityService = databaseSchemaCompatibilityService;
        _skillAdminCatalogService = skillAdminCatalogService;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Applying database migrations for WAR API startup.");

        var migrations = _dbContext.Database.GetMigrations().ToArray();
        if (migrations.Length > 0 && await _databaseSchemaCompatibilityService.NeedsLegacyBaselineAsync(migrations[0], cancellationToken))
        {
            await _databaseSchemaCompatibilityService.ApplyLegacyBaselineAsync(migrations[0], cancellationToken);
        }

        await _dbContext.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Ensuring admin skill catalog imports are available.");
        await _skillAdminCatalogService.EnsureCatalogImportedAsync(cancellationToken);
    }
}
