using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using War.Core.Social;
using War.Infrastructure.Persistence;

namespace War.Api.Application.Social;

// Decision: Singleton service because the in-memory profile cache must be shared across all requests.
// The DbContext is NOT injected directly — instead we use IServiceScopeFactory to create short-lived scopes
// when we need to query the database, avoiding the singleton-consuming-scoped anti-pattern.
public sealed class PublicProfileService
{
    private readonly ProximityValidationService _proximity;
    private readonly IServiceScopeFactory _scopeFactory;

    // Decision: Simple ConcurrentDictionary cache with timestamps instead of IMemoryCache.
    // This gives us explicit control over eviction logic and avoids IMemoryCache's overhead
    // for what is a very simple use case (single key type, single TTL).
    private readonly ConcurrentDictionary<Guid, CachedProfile> _profileCache = new();

    public PublicProfileService(ProximityValidationService proximity, IServiceScopeFactory scopeFactory)
    {
        _proximity = proximity;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Builds and returns a public profile for the target character.
    /// Validates proximity before allowing profile view (except for viewing your own profile).
    /// </summary>
    public async Task<PublicProfileDto?> GetPublicProfileAsync(Guid viewerId, Guid targetId)
    {
        // Validate proximity (self-view is always allowed by ProximityValidationService).
        var proximityCheck = _proximity.ValidateInteractionRange(viewerId, targetId);
        if (!proximityCheck.IsWithinRange)
            return null;

        // Check cache first.
        if (_profileCache.TryGetValue(targetId, out var cached) &&
            (DateTime.UtcNow - cached.FetchedAtUtc).TotalSeconds < SocialConfiguration.ProfileCacheDurationSeconds)
        {
            return cached.Profile;
        }

        // Cache miss or expired — fetch from database.
        var profile = await BuildProfileFromDatabaseAsync(targetId);
        if (profile is null)
            return null;

        // Update cache.
        _profileCache[targetId] = new CachedProfile(profile, DateTime.UtcNow);

        return profile;
    }

    /// <summary>
    /// Returns basic info for a list of nearby characters (for the social discovery panel).
    /// </summary>
    public async Task<IReadOnlyList<NearbyPlayerDto>> GetNearbyPlayersAsync(Guid characterId)
    {
        var nearbyIds = _proximity.GetNearbyCharacters(characterId);

        if (nearbyIds.Count == 0)
            return [];

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WarDbContext>();

        var players = await db.Characters
            .Where(c => nearbyIds.Contains(c.Id))
            .Select(c => new NearbyPlayerDto
            {
                CharacterId = c.Id,
                CharacterName = c.Name,
                ClassName = c.ClassType.ToString(),
                Level = c.Level
            })
            .ToListAsync();

        return players;
    }

    private async Task<PublicProfileDto?> BuildProfileFromDatabaseAsync(Guid characterId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WarDbContext>();

        var character = await db.Characters.FirstOrDefaultAsync(c => c.Id == characterId);
        if (character is null)
            return null;

        // Decision: Only expose safe, non-sensitive fields. NEVER include inventory,
        // resources, account data, currency, or private stats.
        // Equipment, spirits, and detailed skill data will be populated when those systems exist.
        // For now, return empty collections as placeholders.
        return new PublicProfileDto
        {
            CharacterId = character.Id,
            CharacterName = character.Name,
            ClassName = character.ClassType.ToString(),
            Level = character.Level,
            // Decision: PowerScore computation requires FinalStats which needs the full CharacterFinalStatsBuilder pipeline.
            // For the profile view, we store/cache a simplified version. Set to 0 until integrated with PowerScoreCalculator.
            PowerScore = 0,
            EquippedSkills = [],
            EquippedItems = [],
            BoundSpirits = []
        };
    }

    // Decision: Record struct for cache entries to minimize allocations.
    private readonly record struct CachedProfile(PublicProfileDto Profile, DateTime FetchedAtUtc);
}
