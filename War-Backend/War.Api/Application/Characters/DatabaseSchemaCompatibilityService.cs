using System.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using War.Core.Progression;
using War.Infrastructure.Persistence;

namespace War.Api.Application.Characters;

public interface IDatabaseSchemaCompatibilityService
{
    Task<bool> NeedsLegacyBaselineAsync(string initialMigrationId, CancellationToken cancellationToken = default);

    Task ApplyLegacyBaselineAsync(string initialMigrationId, CancellationToken cancellationToken = default);
}

public sealed class DatabaseSchemaCompatibilityService : IDatabaseSchemaCompatibilityService
{
    private readonly WarDbContext _dbContext;
    private readonly ILogger<DatabaseSchemaCompatibilityService> _logger;

    public DatabaseSchemaCompatibilityService(
        WarDbContext dbContext,
        ILogger<DatabaseSchemaCompatibilityService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> NeedsLegacyBaselineAsync(string initialMigrationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(initialMigrationId))
        {
            throw new ArgumentException("The initial migration id is required.", nameof(initialMigrationId));
        }

        if (await MigrationExistsAsync(initialMigrationId, cancellationToken))
        {
            return false;
        }

        return await TableExistsAsync("characters", cancellationToken)
            || await TableExistsAsync("character_skill_progress", cancellationToken);
    }

    public async Task ApplyLegacyBaselineAsync(string initialMigrationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(initialMigrationId))
        {
            throw new ArgumentException("The initial migration id is required.", nameof(initialMigrationId));
        }

        var productVersion = ResolveEfProductVersion();

        _logger.LogInformation(
            "Legacy tables were detected without migration history. Applying compatibility baseline for migration {MigrationId}.",
            initialMigrationId);

        var sql = $$"""
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'characters'
          AND column_name = 'Id')
       AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'characters'
          AND column_name = 'id') THEN
        EXECUTE 'ALTER TABLE characters RENAME COLUMN "Id" TO id';
    END IF;
END
$$;

CREATE TABLE IF NOT EXISTS characters (
    id uuid NOT NULL,
    name character varying(128) NOT NULL,
    class_type character varying(64) NOT NULL,
    level integer NOT NULL,
    current_xp bigint NOT NULL,
    xp_to_next_level bigint NOT NULL,
    total_xp bigint NOT NULL,
    current_hp numeric(18,2) NOT NULL,
    current_mana numeric(18,2) NOT NULL,
    ultimate_charge numeric(18,2) NOT NULL,
    last_basic_combo_stage integer NOT NULL DEFAULT 0,
    last_basic_combo_completed_at_utc timestamp with time zone NULL,
    CONSTRAINT "PK_characters" PRIMARY KEY (id)
);

ALTER TABLE characters ADD COLUMN IF NOT EXISTS name character varying(128) NOT NULL DEFAULT 'Legacy Character';
ALTER TABLE characters ADD COLUMN IF NOT EXISTS class_type character varying(64) NOT NULL DEFAULT 'Sorcerer';
ALTER TABLE characters ADD COLUMN IF NOT EXISTS level integer NOT NULL DEFAULT 1;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS current_xp bigint NOT NULL DEFAULT 0;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS xp_to_next_level bigint NOT NULL DEFAULT {{CharacterLevelRules.BaseExperienceRequiredForFirstLevelUp}};
ALTER TABLE characters ADD COLUMN IF NOT EXISTS total_xp bigint NOT NULL DEFAULT 0;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS current_hp numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS current_mana numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS ultimate_charge numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS last_basic_combo_stage integer NOT NULL DEFAULT 0;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS last_basic_combo_completed_at_utc timestamp with time zone NULL;

ALTER TABLE characters ALTER COLUMN name DROP DEFAULT;
ALTER TABLE characters ALTER COLUMN class_type DROP DEFAULT;
ALTER TABLE characters ALTER COLUMN level DROP DEFAULT;
ALTER TABLE characters ALTER COLUMN current_xp DROP DEFAULT;
ALTER TABLE characters ALTER COLUMN xp_to_next_level DROP DEFAULT;
ALTER TABLE characters ALTER COLUMN total_xp DROP DEFAULT;
ALTER TABLE characters ALTER COLUMN current_hp DROP DEFAULT;
ALTER TABLE characters ALTER COLUMN current_mana DROP DEFAULT;
ALTER TABLE characters ALTER COLUMN ultimate_charge DROP DEFAULT;
ALTER TABLE characters ALTER COLUMN last_basic_combo_stage DROP DEFAULT;

CREATE TABLE IF NOT EXISTS character_skill_progress (
    character_id uuid NOT NULL,
    skill_id character varying(128) NOT NULL,
    is_unlocked boolean NOT NULL,
    current_ascension_level integer NOT NULL,
    unlocked_at_character_level integer NULL,
    CONSTRAINT "PK_character_skill_progress" PRIMARY KEY (character_id, skill_id)
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_character_skill_progress_characters_character_id') THEN
        ALTER TABLE character_skill_progress
            ADD CONSTRAINT "FK_character_skill_progress_characters_character_id"
            FOREIGN KEY (character_id)
            REFERENCES characters(id)
            ON DELETE CASCADE;
    END IF;
END
$$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '{{initialMigrationId}}', '{{productVersion}}'
WHERE NOT EXISTS (
    SELECT 1
    FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '{{initialMigrationId}}');
""";

        await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private async Task<bool> MigrationExistsAsync(string migrationId, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync("__EFMigrationsHistory", cancellationToken))
        {
            return false;
        }

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
SELECT EXISTS (
    SELECT 1
    FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = @migrationId);
""";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "migrationId";
            parameter.Value = migrationId;
            command.Parameters.Add(parameter);

            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            return scalar is bool exists && exists;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
SELECT EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = 'public'
      AND table_name = @tableName);
""";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            return scalar is bool exists && exists;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static string ResolveEfProductVersion()
    {
        var informationalVersion = typeof(DbContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return "8.0.11";
        }

        var plusIndex = informationalVersion.IndexOf('+');
        return plusIndex >= 0
            ? informationalVersion[..plusIndex]
            : informationalVersion;
    }
}
