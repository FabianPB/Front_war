using War.Core.Social;

namespace War.Api.Application.GameWorld;

/// <summary>
/// Real IProximityProvider implementation that uses GameWorldService positions
/// to compute distances between online players.
/// Replaces the DefaultProximityProvider stub for the online demo.
/// </summary>
public sealed class GameWorldProximityProvider : IProximityProvider
{
    private readonly GameWorldService _world;

    public GameWorldProximityProvider(GameWorldService world)
    {
        _world = world;
    }

    public ProximityCheckResult CheckProximity(Guid characterA, Guid characterB)
    {
        var playerA = _world.GetPlayerByGuid(characterA);
        var playerB = _world.GetPlayerByGuid(characterB);

        if (playerA is null || playerB is null)
            return new ProximityCheckResult(false, 999m, "Uno o ambos jugadores no están en el mundo.");

        var dx = playerA.X - playerB.X;
        var dy = playerA.Y - playerB.Y;
        var distance = (decimal)Math.Sqrt(dx * dx + dy * dy);

        var isInRange = distance <= SocialConfiguration.InteractionRangeMeters;
        return new ProximityCheckResult(isInRange, distance,
            isInRange ? null : "El jugador objetivo no se encuentra dentro del rango de interacción.");
    }

    public IReadOnlyList<Guid> GetNearbyCharacters(Guid characterId)
    {
        var player = _world.GetPlayerByGuid(characterId);
        if (player is null) return [];

        var nearby = _world.GetNearbyPlayersOf(player, (float)SocialConfiguration.NearbyDiscoveryRangeMeters);
        return nearby
            .Where(p => Guid.TryParse(p.PlayerId, out _))
            .Select(p => Guid.Parse(p.PlayerId))
            .ToList();
    }
}
