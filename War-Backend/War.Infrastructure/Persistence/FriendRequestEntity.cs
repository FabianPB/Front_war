using War.Core.Social;

namespace War.Infrastructure.Persistence;

// Decision: Separate table for friend requests because they have a lifecycle (Pending->Accepted/Rejected/Cancelled)
// distinct from the resulting SocialRelationship. Accepted requests create a relationship; the request record stays for audit.
public sealed class FriendRequestEntity
{
    public Guid Id { get; set; }
    public required Guid SenderId { get; set; }
    public required Guid ReceiverId { get; set; }
    public required FriendRequestStatus Status { get; set; }
    public required DateTime CreatedAtUtc { get; set; }

    // Decision: Nullable because pending requests haven't been resolved yet.
    public DateTime? ResolvedAtUtc { get; set; }
}
