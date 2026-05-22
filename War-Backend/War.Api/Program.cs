using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using War.Api.Application.Characters;
using War.Api.Application.Economy;
using War.Api.Application.SkillAdmin;
using War.Api.Application.SkillRuntime;
using War.Api.Application.GameWorld;
using War.Api.Application.Social;
using War.Api.Hubs;
using War.Core.Combat;
using War.Core.PowerScore;
using War.Core.Progression;
using War.Core.Skills;
using War.Core.Social;
using War.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Decision: Only register PostgreSQL DbContext if a connection string is configured.
// In production demo mode (no DB), the online world runs entirely in RAM.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<WarDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Servicios que dependen directamente de WarDbContext.
    // Sin connection string el backend funciona en modo in-memory (sólo GlobalChat y GameWorld).
    builder.Services.AddScoped<IPersistedCharacterRuntimeService, PersistedCharacterRuntimeService>();
    builder.Services.AddScoped<ICharacterSnapshotQueryService, CharacterSnapshotQueryService>();
    builder.Services.AddScoped<IDatabaseSchemaCompatibilityService, DatabaseSchemaCompatibilityService>();
    builder.Services.AddScoped<ISkillAdminCatalogService, SkillAdminCatalogService>();
    builder.Services.AddScoped<IPublishedSkillCatalogSource, PublishedSkillCatalogSource>();
    builder.Services.AddScoped<ISkillRuntimeCatalogProvider, SkillRuntimeCatalogProvider>();
    builder.Services.AddScoped<IWarApiStartupInitializer, WarApiStartupInitializer>();
    builder.Services.AddScoped<SocialRelationshipService>();
    builder.Services.AddScoped<ChatRelayService>();
}

builder.Services.AddSingleton<ICharacterLevelProgressionService>(CharacterLevelProgressionService.Default);
builder.Services.AddSingleton<IClassBasicAttackCatalog>(ClassBasicAttackCatalog.Default);
builder.Services.AddSingleton<IBasicAttackComboResolver>(BasicAttackComboResolver.Default);
builder.Services.AddSingleton<IBasicAttackMagnitudeResolver>(BasicAttackMagnitudeResolver.Default);
builder.Services.AddScoped<ICharacterFinalStatsBuilder, CharacterFinalStatsBuilder>();
builder.Services.AddScoped<IPowerScoreCalculator, PowerScoreCalculator>();
builder.Services.AddScoped<ICharacterProfileSnapshotFactory, CharacterProfileSnapshotFactory>();
builder.Services.AddScoped<ICombatEventResolver, CombatEventResolver>();
builder.Services.AddScoped<IBasicAttackCombatTranslator, BasicAttackCombatTranslator>();
builder.Services.AddScoped<ISkillCombatTranslator, SkillCombatTranslator>();
builder.Services.AddScoped<ISkillAdminOptionsService, SkillAdminOptionsService>();
builder.Services.AddScoped<IProgrammedSkillCatalogSource, ProgrammedSkillCatalogSource>();

// ── Social System Services ──────────────────────────────────────────────────

// Decision: For the online demo, use the real GameWorldProximityProvider that reads positions
// from the GameWorldService singleton. This replaces the stub that always returned "in range".
// NOTE: We register as a factory so it can resolve the GameWorldService singleton.
// Demo mode: permite todas las interacciones sociales sin requerir cliente Unity.
builder.Services.AddSingleton<IProximityProvider>(new DefaultProximityProvider());

// Decision: For the online demo, use the in-memory social service instead of EF Core.
// Switch to SocialRelationshipService for production with a DB.
builder.Services.AddSingleton<ISocialRelationshipService>(sp =>
    new InMemorySocialRelationshipService(sp.GetRequiredService<GameWorldService>()));

// Decision: Singletons for stateful in-memory services that must share state across all requests.
builder.Services.AddSingleton<ChatRateLimiter>();
builder.Services.AddSingleton<HubActionRateLimiter>();
builder.Services.AddSingleton<War.Api.Application.Marketplace.MarketplaceService>();
builder.Services.AddSingleton<War.Api.Application.Marketplace.GlobalChatService>();
builder.Services.AddSingleton<War.Api.Application.Chat.PrivateChatService>();
builder.Services.AddSingleton<ProximityValidationService>();
builder.Services.AddSingleton<PublicProfileService>();

// SocialRelationshipService y ChatRelayService se registran dentro del bloque if(connectionString).

// ── Game World (Multiplayer Online) ─────────────────────────────────────────

// Decision: Singleton — all world state lives in RAM for the demo.
// GameWorldService needs ICharacterFinalStatsBuilder which is scoped, so we resolve it via factory.
builder.Services.AddSingleton<GameWorldService>(sp =>
{
    var statsBuilder = new CharacterFinalStatsBuilder();
    return new GameWorldService(statsBuilder);
});
builder.Services.AddSingleton<OnlineCombatService>();

// Group service: in-memory group management (party play for heals/buffs/shields)
builder.Services.AddSingleton<GroupService>(sp =>
    new GroupService(sp.GetRequiredService<GameWorldService>()));
builder.Services.AddHostedService<WorldTickService>();

// ── Economy (wallets + auditoría + Capilla de Economía) ─────────────────────
// Decision: Singleton — wallet, audit log y Capilla viven en memoria por player.
// Para producción con BD, los tres se migran a implementaciones persistidas respetando la API.
builder.Services.AddSingleton<WalletAuditLog>();
// PlayerChapelService implementa IWalletCapProvider: el wallet consulta la Capilla para los caps
// de posesión dinámicos. Se registra bajo ambas interfaces para que ambas las resuelvan al mismo singleton.
builder.Services.AddSingleton<PlayerChapelService>();
builder.Services.AddSingleton<War.Core.Economy.IWalletCapProvider>(sp => sp.GetRequiredService<PlayerChapelService>());
builder.Services.AddSingleton<PlayerWalletService>();

// Conversión de monedas (cobre↔plata↔oro) con quotas diarias/semanales/mensuales por Capilla.
builder.Services.AddSingleton<ConversionQuotaTracker>();
builder.Services.AddSingleton<CurrencyConversionService>();

// Ascensión de habilidades (orquesta libros del inventario + wallet).
builder.Services.AddSingleton<War.Api.Application.Skills.SkillAscensionService>();

// ── SignalR ──────────────────────────────────────────────────────────────────

builder.Services.AddSignalR();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ── CORS (required for browser clients on different origins / Flutter web) ──

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true); // Decision: Allow all origins for demo. Lock down in production.
    });
});

// Railway/Render/Fly.io provide PORT env variable
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

var app = builder.Build();

// Only run DB-dependent startup if a connection string is configured
if (!string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<IWarApiStartupInitializer>();
    await initializer.InitializeAsync();
}

app.UseCors("AllowAll");

app.MapControllers();
app.MapHub<ChatHub>("/chat");
app.MapHub<GameHub>("/game");

app.Run();

// ────────────────────────────────────────────────────────────────────────────
// Development Stub
// ────────────────────────────────────────────────────────────────────────────

// Decision: Stub implementation that always returns "within range" so the social system can be
// developed and tested without a running Unity game client providing real positions.
// WARNING: This MUST be replaced with the real IProximityProvider before any public deployment.
// The real implementation will receive position updates from Unity clients and perform actual
// distance calculations using Vector3 positions stored in a concurrent spatial data structure.
/// <summary>
/// Development-only proximity provider that treats all characters as within interaction range.
/// </summary>
// Expose the implicit Program class so WebApplicationFactory<Program> works from integration tests.
public partial class Program { }

internal sealed class DefaultProximityProvider : IProximityProvider
{
    public ProximityCheckResult CheckProximity(Guid characterA, Guid characterB)
    {
        // Decision: Return 0 meters distance so all proximity checks pass during development.
        return new ProximityCheckResult(IsWithinRange: true, DistanceMeters: 0m);
    }

    public IReadOnlyList<Guid> GetNearbyCharacters(Guid characterId)
    {
        // Decision: Return empty list because we have no real position data.
        // In production, this would query a spatial index for characters within NearbyDiscoveryRangeMeters.
        return [];
    }
}
