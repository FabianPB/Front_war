namespace War.Core.Social;

// Decision: Enum flags allow a single relationship query to check multiple states efficiently.
[Flags]
public enum SocialRelationshipType
{
    None = 0,
    Friend = 1,
    Blocked = 2
}

public enum FriendRequestStatus
{
    Pending,
    Accepted,
    Rejected,
    Cancelled
}

// Decision: Record structs for lightweight value types that will be created frequently during proximity checks.
public readonly record struct ProximityCheckResult(
    bool IsWithinRange,
    decimal DistanceMeters,
    string? DenialReason = null);

// Decision: Immutable record for public profile data. Only exposes safe, non-sensitive fields.
// NEVER include: inventory, resources, account data, currency, private stats.
public sealed record PublicPlayerProfile(
    Guid CharacterId,
    string CharacterName,
    string ClassName,        // Localized class name in Spanish
    int Level,
    int PowerScore,
    IReadOnlyList<PublicSkillSummary> EquippedSkills,
    IReadOnlyList<PublicEquipmentSummary> EquippedItems,
    IReadOnlyList<PublicSpiritSummary> BoundSpirits);

public sealed record PublicSkillSummary(
    string SkillId,
    string SkillName,
    int AscensionLevel);

public sealed record PublicEquipmentSummary(
    string SlotName,
    string ItemName,
    int EnhancementLevel);

public sealed record PublicSpiritSummary(
    string SpiritId,
    string SpiritName,
    int Level);

// Decision: Separate record for social relationship state to decouple from persistence entity.
public sealed record SocialRelationship(
    Guid OwnerId,
    Guid TargetId,
    SocialRelationshipType Type,
    DateTime CreatedAtUtc);

public sealed record FriendRequest(
    Guid Id,
    Guid SenderId,
    Guid ReceiverId,
    FriendRequestStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc = null);

// Decision: Chat messages are NOT stored server-side. This record exists only for real-time relay.
// The server creates it, sends it via SignalR, and discards it. Client stores locally if desired.
public sealed record ChatMessage(
    Guid MessageId,
    Guid SenderId,
    string SenderName,
    string Content,
    DateTime SentAtUtc);

// Decision: Interface allows Unity to provide world-position data without the server needing Unity dependencies.
// The server defines WHAT it needs; the client (or a test stub) provides HOW.
// TODO [Unity Integration]: Implement IProximityProvider using Unity's Transform system to provide real character positions.
public interface IProximityProvider
{
    /// <summary>
    /// Checks whether two characters are within interaction range.
    /// The server calls this during every social interaction initiation to enforce proximity rules.
    /// </summary>
    ProximityCheckResult CheckProximity(Guid characterA, Guid characterB);

    /// <summary>
    /// Returns all character IDs within interaction range of the specified character.
    /// Used to populate the nearby-players list in the social UI.
    /// </summary>
    IReadOnlyList<Guid> GetNearbyCharacters(Guid characterId);
}
