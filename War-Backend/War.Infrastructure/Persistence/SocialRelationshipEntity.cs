using War.Core.Social;

namespace War.Infrastructure.Persistence;

// Decision: Single table for both Friend and Block relationships using a discriminator column.
// This simplifies queries like "is there ANY relationship between A and B?" which are frequent during interaction checks.
public sealed class SocialRelationshipEntity
{
    public Guid Id { get; set; }

    // Decision: OwnerId is the character who initiated the relationship (sent the friend request or performed the block).
    public required Guid OwnerId { get; set; }
    public required Guid TargetId { get; set; }
    public required SocialRelationshipType Type { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
}
