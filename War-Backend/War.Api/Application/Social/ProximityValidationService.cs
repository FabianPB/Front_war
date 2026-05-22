using War.Core.Social;

namespace War.Api.Application.Social;

// Decision: Service wraps IProximityProvider to add server-authority validation.
// The provider gives raw distance data; this service applies game rules.
public sealed class ProximityValidationService
{
    private readonly IProximityProvider _proximityProvider;

    public ProximityValidationService(IProximityProvider proximityProvider)
    {
        _proximityProvider = proximityProvider;
    }

    /// <summary>
    /// Validates that two characters are within interaction range.
    /// This is called before EVERY social interaction initiation. No exceptions.
    /// </summary>
    public ProximityCheckResult ValidateInteractionRange(Guid actorId, Guid targetId)
    {
        ArgumentNullException.ThrowIfNull(actorId);

        // Decision: Self-interaction is always "in range" — allows viewing own profile.
        if (actorId == targetId)
            return new ProximityCheckResult(true, 0m);

        var check = _proximityProvider.CheckProximity(actorId, targetId);

        if (!check.IsWithinRange)
        {
            return new ProximityCheckResult(
                false,
                check.DistanceMeters,
                "El jugador objetivo no se encuentra dentro del rango de interaccion.");
        }

        return check;
    }

    /// <summary>
    /// Returns nearby character IDs for the social discovery panel.
    /// </summary>
    public IReadOnlyList<Guid> GetNearbyCharacters(Guid characterId)
    {
        return _proximityProvider.GetNearbyCharacters(characterId);
    }
}
