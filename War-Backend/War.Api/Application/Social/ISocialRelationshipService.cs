namespace War.Api.Application.Social;

/// <summary>
/// Interface for social relationship operations (friends, blocks, requests).
/// Allows switching between EF Core (production) and in-memory (demo) implementations.
/// </summary>
public interface ISocialRelationshipService
{
    Task<SocialOperationResult> SendFriendRequestAsync(Guid senderId, Guid targetId);
    Task<SocialOperationResult> RespondToFriendRequestAsync(Guid receiverId, Guid requestId, bool accept);
    Task<IReadOnlyList<FriendListEntryDto>> GetFriendListAsync(Guid characterId);
    Task<SocialOperationResult> RemoveFriendAsync(Guid characterId, Guid friendId);
    Task<SocialOperationResult> BlockPlayerAsync(Guid characterId, Guid targetId);
    Task<SocialOperationResult> UnblockPlayerAsync(Guid characterId, Guid targetId);
    Task<IReadOnlyList<BlockListEntryDto>> GetBlockListAsync(Guid characterId);
    Task<IReadOnlyList<PendingFriendRequestDto>> GetPendingInboundRequestsAsync(Guid characterId);
    Task<bool> IsBlockedAsync(Guid characterA, Guid characterB);

    /// <summary>
    /// Cleanup all social data for a player (called on disconnect in demo mode).
    /// </summary>
    void CleanupPlayer(Guid characterId);
}
