using System.Collections.Concurrent;
using War.Api.Application.GameWorld;
using War.Core.Social;

namespace War.Api.Application.Social;

/// <summary>
/// In-memory implementation of ISocialRelationshipService for the online demo.
/// All relationships live in RAM and are cleaned up when players disconnect.
/// Decision: Singleton so state persists across requests during a server session.
/// </summary>
public sealed class InMemorySocialRelationshipService : ISocialRelationshipService
{
    private readonly GameWorldService _world;

    // ── In-memory storage ──
    // Relationships: ownerId → set of (targetId, type)
    private readonly ConcurrentDictionary<Guid, List<InMemoryRelationship>> _relationships = new();
    // Friend requests: requestId → request
    private readonly ConcurrentDictionary<Guid, InMemoryFriendRequest> _requests = new();

    private readonly object _lock = new();

    public InMemorySocialRelationshipService(GameWorldService world)
    {
        _world = world;
    }

    // ────────────────────────────────────────────────────────────────────
    // Friend Requests
    // ────────────────────────────────────────────────────────────────────

    public Task<SocialOperationResult> SendFriendRequestAsync(Guid senderId, Guid targetId)
    {
        lock (_lock)
        {
            if (senderId == targetId)
                return Task.FromResult(SocialOperationResult.Fail("No puedes enviarte una solicitud de amistad a ti mismo."));

            // Check blocks
            if (HasBlock(senderId, targetId))
                return Task.FromResult(SocialOperationResult.Fail("No se puede enviar una solicitud de amistad a este jugador."));

            // Check already friends
            if (HasRelationship(senderId, targetId, SocialRelationshipType.Friend))
                return Task.FromResult(SocialOperationResult.Fail("Ya eres amigo de este jugador."));

            // Check existing pending request
            if (_requests.Values.Any(r =>
                ((r.SenderId == senderId && r.ReceiverId == targetId) ||
                 (r.SenderId == targetId && r.ReceiverId == senderId)) &&
                r.Status == FriendRequestStatus.Pending))
                return Task.FromResult(SocialOperationResult.Fail("Ya existe una solicitud de amistad pendiente con este jugador."));

            // Check limits
            var friendCount = GetRelationships(senderId).Count(r => r.Type == SocialRelationshipType.Friend);
            if (friendCount >= SocialConfiguration.MaxFriendsPerCharacter)
                return Task.FromResult(SocialOperationResult.Fail($"Has alcanzado el límite máximo de {SocialConfiguration.MaxFriendsPerCharacter} amigos."));

            var pendingOutbound = _requests.Values.Count(r => r.SenderId == senderId && r.Status == FriendRequestStatus.Pending);
            if (pendingOutbound >= SocialConfiguration.MaxPendingOutboundRequests)
                return Task.FromResult(SocialOperationResult.Fail($"Has alcanzado el límite máximo de {SocialConfiguration.MaxPendingOutboundRequests} solicitudes pendientes."));

            // Create request
            var request = new InMemoryFriendRequest
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = targetId,
                Status = FriendRequestStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };
            _requests[request.Id] = request;

            return Task.FromResult(SocialOperationResult.Ok());
        }
    }

    public Task<SocialOperationResult> RespondToFriendRequestAsync(Guid receiverId, Guid requestId, bool accept)
    {
        lock (_lock)
        {
            if (!_requests.TryGetValue(requestId, out var request))
                return Task.FromResult(SocialOperationResult.Fail("La solicitud de amistad no existe."));

            if (request.ReceiverId != receiverId)
                return Task.FromResult(SocialOperationResult.Fail("No tienes permiso para responder a esta solicitud."));

            if (request.Status != FriendRequestStatus.Pending)
                return Task.FromResult(SocialOperationResult.Fail("Esta solicitud ya ha sido resuelta."));

            if (accept)
            {
                // Create mutual friendship
                AddRelationship(request.SenderId, request.ReceiverId, SocialRelationshipType.Friend);
                AddRelationship(request.ReceiverId, request.SenderId, SocialRelationshipType.Friend);
                request.Status = FriendRequestStatus.Accepted;
            }
            else
            {
                request.Status = FriendRequestStatus.Rejected;
            }
            request.ResolvedAtUtc = DateTime.UtcNow;

            return Task.FromResult(SocialOperationResult.Ok());
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Friend List & Removal
    // ────────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<FriendListEntryDto>> GetFriendListAsync(Guid characterId)
    {
        lock (_lock)
        {
            var friends = GetRelationships(characterId)
                .Where(r => r.Type == SocialRelationshipType.Friend)
                .Select(r =>
                {
                    var player = _world.GetPlayerByGuid(r.TargetId);
                    return new FriendListEntryDto
                    {
                        CharacterId = r.TargetId,
                        CharacterName = player?.DisplayName ?? "Desconocido",
                        ClassName = player?.ClassName ?? "",
                        Level = player?.Level ?? 0,
                        IsOnline = player is not null
                    };
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<FriendListEntryDto>>(friends);
        }
    }

    public Task<SocialOperationResult> RemoveFriendAsync(Guid characterId, Guid friendId)
    {
        lock (_lock)
        {
            if (characterId == friendId)
                return Task.FromResult(SocialOperationResult.Fail("No puedes eliminarte a ti mismo de tu lista de amigos."));

            var removed1 = RemoveRelationship(characterId, friendId, SocialRelationshipType.Friend);
            var removed2 = RemoveRelationship(friendId, characterId, SocialRelationshipType.Friend);

            if (!removed1 && !removed2)
                return Task.FromResult(SocialOperationResult.Fail("Este jugador no se encuentra en tu lista de amigos."));

            return Task.FromResult(SocialOperationResult.Ok());
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Blocking
    // ────────────────────────────────────────────────────────────────────

    public Task<SocialOperationResult> BlockPlayerAsync(Guid characterId, Guid targetId)
    {
        lock (_lock)
        {
            if (characterId == targetId)
                return Task.FromResult(SocialOperationResult.Fail("No puedes bloquearte a ti mismo."));

            if (HasRelationship(characterId, targetId, SocialRelationshipType.Blocked))
                return Task.FromResult(SocialOperationResult.Fail("Este jugador ya está bloqueado."));

            var blockCount = GetRelationships(characterId).Count(r => r.Type == SocialRelationshipType.Blocked);
            if (blockCount >= SocialConfiguration.MaxBlockedPerCharacter)
                return Task.FromResult(SocialOperationResult.Fail($"Has alcanzado el límite máximo de {SocialConfiguration.MaxBlockedPerCharacter} jugadores bloqueados."));

            // Remove friendships in both directions
            RemoveRelationship(characterId, targetId, SocialRelationshipType.Friend);
            RemoveRelationship(targetId, characterId, SocialRelationshipType.Friend);

            // Cancel pending friend requests between both
            foreach (var req in _requests.Values.Where(r =>
                ((r.SenderId == characterId && r.ReceiverId == targetId) ||
                 (r.SenderId == targetId && r.ReceiverId == characterId)) &&
                r.Status == FriendRequestStatus.Pending))
            {
                req.Status = FriendRequestStatus.Cancelled;
                req.ResolvedAtUtc = DateTime.UtcNow;
            }

            // Create block
            AddRelationship(characterId, targetId, SocialRelationshipType.Blocked);

            return Task.FromResult(SocialOperationResult.Ok());
        }
    }

    public Task<SocialOperationResult> UnblockPlayerAsync(Guid characterId, Guid targetId)
    {
        lock (_lock)
        {
            if (characterId == targetId)
                return Task.FromResult(SocialOperationResult.Fail("No puedes desbloquearte a ti mismo."));

            if (!RemoveRelationship(characterId, targetId, SocialRelationshipType.Blocked))
                return Task.FromResult(SocialOperationResult.Fail("Este jugador no está bloqueado."));

            return Task.FromResult(SocialOperationResult.Ok());
        }
    }

    public Task<IReadOnlyList<BlockListEntryDto>> GetBlockListAsync(Guid characterId)
    {
        lock (_lock)
        {
            var blocked = GetRelationships(characterId)
                .Where(r => r.Type == SocialRelationshipType.Blocked)
                .Select(r =>
                {
                    var player = _world.GetPlayerByGuid(r.TargetId);
                    return new BlockListEntryDto
                    {
                        CharacterId = r.TargetId,
                        CharacterName = player?.DisplayName ?? "Desconocido"
                    };
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<BlockListEntryDto>>(blocked);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Pending Requests
    // ────────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<PendingFriendRequestDto>> GetPendingInboundRequestsAsync(Guid characterId)
    {
        lock (_lock)
        {
            var requests = _requests.Values
                .Where(r => r.ReceiverId == characterId && r.Status == FriendRequestStatus.Pending)
                .Select(r =>
                {
                    var sender = _world.GetPlayerByGuid(r.SenderId);
                    return new PendingFriendRequestDto
                    {
                        RequestId = r.Id,
                        SenderCharacterId = r.SenderId,
                        SenderName = sender?.DisplayName ?? "Desconocido",
                        SenderClassName = sender?.ClassName ?? "",
                        SenderLevel = sender?.Level ?? 0,
                        SentAtUtc = r.CreatedAtUtc
                    };
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<PendingFriendRequestDto>>(requests);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Block Queries
    // ────────────────────────────────────────────────────────────────────

    public Task<bool> IsBlockedAsync(Guid characterA, Guid characterB)
    {
        lock (_lock)
        {
            return Task.FromResult(HasBlock(characterA, characterB));
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Cleanup
    // ────────────────────────────────────────────────────────────────────

    public void CleanupPlayer(Guid characterId)
    {
        lock (_lock)
        {
            // Remove all relationships owned by this player
            _relationships.TryRemove(characterId, out _);

            // Remove this player from others' relationships
            foreach (var kvp in _relationships)
            {
                kvp.Value.RemoveAll(r => r.TargetId == characterId);
            }

            // Remove friend requests involving this player
            var requestsToRemove = _requests.Values
                .Where(r => r.SenderId == characterId || r.ReceiverId == characterId)
                .Select(r => r.Id)
                .ToList();
            foreach (var id in requestsToRemove)
            {
                _requests.TryRemove(id, out _);
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Private Helpers
    // ────────────────────────────────────────────────────────────────────

    private List<InMemoryRelationship> GetRelationships(Guid ownerId)
    {
        return _relationships.GetOrAdd(ownerId, _ => new List<InMemoryRelationship>());
    }

    private bool HasRelationship(Guid ownerId, Guid targetId, SocialRelationshipType type)
    {
        return GetRelationships(ownerId).Any(r => r.TargetId == targetId && r.Type == type);
    }

    private bool HasBlock(Guid a, Guid b)
    {
        return HasRelationship(a, b, SocialRelationshipType.Blocked) ||
               HasRelationship(b, a, SocialRelationshipType.Blocked);
    }

    private void AddRelationship(Guid ownerId, Guid targetId, SocialRelationshipType type)
    {
        var list = GetRelationships(ownerId);
        if (!list.Any(r => r.TargetId == targetId && r.Type == type))
        {
            list.Add(new InMemoryRelationship(targetId, type, DateTime.UtcNow));
        }
    }

    private bool RemoveRelationship(Guid ownerId, Guid targetId, SocialRelationshipType type)
    {
        if (!_relationships.TryGetValue(ownerId, out var list)) return false;
        return list.RemoveAll(r => r.TargetId == targetId && r.Type == type) > 0;
    }

    // ── Inner types ──

    private sealed record InMemoryRelationship(
        Guid TargetId,
        SocialRelationshipType Type,
        DateTime CreatedAtUtc);

    private sealed class InMemoryFriendRequest
    {
        public Guid Id { get; init; }
        public Guid SenderId { get; init; }
        public Guid ReceiverId { get; init; }
        public FriendRequestStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; init; }
        public DateTime? ResolvedAtUtc { get; set; }
    }
}
