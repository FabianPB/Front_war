using Microsoft.EntityFrameworkCore;
using War.Core.Social;
using War.Infrastructure.Persistence;

namespace War.Api.Application.Social;

// Decision: Scoped service because it depends on WarDbContext (also scoped).
// Each HTTP request / SignalR invocation gets its own instance with its own DbContext.
public sealed class SocialRelationshipService
{
    private readonly WarDbContext _db;
    private readonly ProximityValidationService _proximity;

    public SocialRelationshipService(WarDbContext db, ProximityValidationService proximity)
    {
        _db = db;
        _proximity = proximity;
    }

    // ────────────────────────────────────────────────────────────────────
    // Friend Requests
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a friend request from <paramref name="senderId"/> to <paramref name="targetId"/>.
    /// Validates proximity, self-targeting, existing relationships, and outbound limits.
    /// </summary>
    public async Task<SocialOperationResult> SendFriendRequestAsync(Guid senderId, Guid targetId)
    {
        try
        {
            // 1. No puedes enviarte una solicitud a ti mismo.
            if (senderId == targetId)
                return SocialOperationResult.Fail("No puedes enviarte una solicitud de amistad a ti mismo.");

            // 2. Validar proximidad.
            var proximityCheck = _proximity.ValidateInteractionRange(senderId, targetId);
            if (!proximityCheck.IsWithinRange)
                return SocialOperationResult.Fail(proximityCheck.DenialReason ?? "Fuera de rango de interaccion.");

            // 3. Verificar que el objetivo existe.
            var targetExists = await _db.Characters.AnyAsync(c => c.Id == targetId);
            if (!targetExists)
                return SocialOperationResult.Fail("El personaje objetivo no existe.");

            // 4. Verificar que no exista bloqueo en ninguna direccion.
            var hasBlock = await _db.SocialRelationships.AnyAsync(r =>
                ((r.OwnerId == senderId && r.TargetId == targetId) ||
                 (r.OwnerId == targetId && r.TargetId == senderId)) &&
                r.Type == SocialRelationshipType.Blocked);
            if (hasBlock)
                return SocialOperationResult.Fail("No se puede enviar una solicitud de amistad a este jugador.");

            // 5. Verificar que no sean ya amigos.
            var alreadyFriends = await _db.SocialRelationships.AnyAsync(r =>
                r.OwnerId == senderId && r.TargetId == targetId &&
                r.Type == SocialRelationshipType.Friend);
            if (alreadyFriends)
                return SocialOperationResult.Fail("Ya eres amigo de este jugador.");

            // 6. Verificar que no exista una solicitud pendiente entre ambos.
            var hasPendingRequest = await _db.FriendRequests.AnyAsync(r =>
                ((r.SenderId == senderId && r.ReceiverId == targetId) ||
                 (r.SenderId == targetId && r.ReceiverId == senderId)) &&
                r.Status == FriendRequestStatus.Pending);
            if (hasPendingRequest)
                return SocialOperationResult.Fail("Ya existe una solicitud de amistad pendiente con este jugador.");

            // 7. Verificar limite de amigos del remitente.
            var senderFriendCount = await _db.SocialRelationships.CountAsync(r =>
                r.OwnerId == senderId && r.Type == SocialRelationshipType.Friend);
            if (senderFriendCount >= SocialConfiguration.MaxFriendsPerCharacter)
                return SocialOperationResult.Fail($"Has alcanzado el limite maximo de {SocialConfiguration.MaxFriendsPerCharacter} amigos.");

            // 8. Verificar limite de solicitudes salientes pendientes.
            var pendingOutbound = await _db.FriendRequests.CountAsync(r =>
                r.SenderId == senderId && r.Status == FriendRequestStatus.Pending);
            if (pendingOutbound >= SocialConfiguration.MaxPendingOutboundRequests)
                return SocialOperationResult.Fail($"Has alcanzado el limite maximo de {SocialConfiguration.MaxPendingOutboundRequests} solicitudes pendientes.");

            // 9. Crear la solicitud.
            var request = new FriendRequestEntity
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = targetId,
                Status = FriendRequestStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.FriendRequests.Add(request);
            await _db.SaveChangesAsync();

            return SocialOperationResult.Ok();
        }
        catch (Exception ex)
        {
            return SocialOperationResult.Fail($"Error al enviar la solicitud de amistad: {ex.Message}");
        }
    }

    /// <summary>
    /// Accepts or rejects a friend request. Only the receiver can respond.
    /// Accepting creates mutual Friend relationships.
    /// </summary>
    public async Task<SocialOperationResult> RespondToFriendRequestAsync(Guid receiverId, Guid requestId, bool accept)
    {
        try
        {
            var request = await _db.FriendRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request is null)
                return SocialOperationResult.Fail("La solicitud de amistad no existe.");

            if (request.ReceiverId != receiverId)
                return SocialOperationResult.Fail("No tienes permiso para responder a esta solicitud.");

            if (request.Status != FriendRequestStatus.Pending)
                return SocialOperationResult.Fail("Esta solicitud ya ha sido resuelta.");

            // Decision: Check expiration based on SocialConfiguration.
            var expirationDate = request.CreatedAtUtc.AddDays(SocialConfiguration.FriendRequestExpirationDays);
            if (DateTime.UtcNow > expirationDate)
            {
                request.Status = FriendRequestStatus.Cancelled;
                request.ResolvedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return SocialOperationResult.Fail("Esta solicitud de amistad ha expirado.");
            }

            var now = DateTime.UtcNow;

            if (accept)
            {
                // Verificar limite de amigos de ambas partes antes de aceptar.
                var senderFriendCount = await _db.SocialRelationships.CountAsync(r =>
                    r.OwnerId == request.SenderId && r.Type == SocialRelationshipType.Friend);
                var receiverFriendCount = await _db.SocialRelationships.CountAsync(r =>
                    r.OwnerId == receiverId && r.Type == SocialRelationshipType.Friend);

                if (senderFriendCount >= SocialConfiguration.MaxFriendsPerCharacter)
                    return SocialOperationResult.Fail("El remitente ha alcanzado su limite maximo de amigos.");
                if (receiverFriendCount >= SocialConfiguration.MaxFriendsPerCharacter)
                    return SocialOperationResult.Fail($"Has alcanzado el limite maximo de {SocialConfiguration.MaxFriendsPerCharacter} amigos.");

                // Decision: Create MUTUAL Friend relationships. Both sides get a row so that
                // querying "my friends" is a simple OwnerId filter without OR conditions.
                _db.SocialRelationships.Add(new SocialRelationshipEntity
                {
                    Id = Guid.NewGuid(),
                    OwnerId = request.SenderId,
                    TargetId = receiverId,
                    Type = SocialRelationshipType.Friend,
                    CreatedAtUtc = now
                });
                _db.SocialRelationships.Add(new SocialRelationshipEntity
                {
                    Id = Guid.NewGuid(),
                    OwnerId = receiverId,
                    TargetId = request.SenderId,
                    Type = SocialRelationshipType.Friend,
                    CreatedAtUtc = now
                });

                request.Status = FriendRequestStatus.Accepted;
            }
            else
            {
                request.Status = FriendRequestStatus.Rejected;
            }

            request.ResolvedAtUtc = now;
            await _db.SaveChangesAsync();

            return SocialOperationResult.Ok();
        }
        catch (Exception ex)
        {
            return SocialOperationResult.Fail($"Error al responder la solicitud de amistad: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Friend List & Removal
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the friend list for a character, including basic character info.
    /// </summary>
    public async Task<IReadOnlyList<FriendListEntryDto>> GetFriendListAsync(Guid characterId)
    {
        var friends = await _db.SocialRelationships
            .Where(r => r.OwnerId == characterId && r.Type == SocialRelationshipType.Friend)
            .Join(_db.Characters,
                r => r.TargetId,
                c => c.Id,
                (r, c) => new FriendListEntryDto
                {
                    CharacterId = c.Id,
                    CharacterName = c.Name,
                    ClassName = c.ClassType.ToString(),
                    Level = c.Level,
                    // Decision: IsOnline will be populated by the caller (e.g., SignalR connection tracker).
                    // The service layer doesn't track presence — that's a transport concern.
                    IsOnline = false
                })
            .ToListAsync();

        return friends;
    }

    /// <summary>
    /// Removes a mutual friendship between two characters.
    /// </summary>
    public async Task<SocialOperationResult> RemoveFriendAsync(Guid characterId, Guid friendId)
    {
        try
        {
            if (characterId == friendId)
                return SocialOperationResult.Fail("No puedes eliminarte a ti mismo de tu lista de amigos.");

            // Decision: Remove BOTH sides of the mutual relationship in a single operation.
            var relationships = await _db.SocialRelationships
                .Where(r =>
                    ((r.OwnerId == characterId && r.TargetId == friendId) ||
                     (r.OwnerId == friendId && r.TargetId == characterId)) &&
                    r.Type == SocialRelationshipType.Friend)
                .ToListAsync();

            if (relationships.Count == 0)
                return SocialOperationResult.Fail("Este jugador no se encuentra en tu lista de amigos.");

            _db.SocialRelationships.RemoveRange(relationships);
            await _db.SaveChangesAsync();

            return SocialOperationResult.Ok();
        }
        catch (Exception ex)
        {
            return SocialOperationResult.Fail($"Error al eliminar amigo: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Blocking
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Blocks a target character. Removes any existing friendship.
    /// Validates proximity, self-targeting, and block limits.
    /// </summary>
    public async Task<SocialOperationResult> BlockPlayerAsync(Guid characterId, Guid targetId)
    {
        try
        {
            if (characterId == targetId)
                return SocialOperationResult.Fail("No puedes bloquearte a ti mismo.");

            // Verificar proximidad.
            var proximityCheck = _proximity.ValidateInteractionRange(characterId, targetId);
            if (!proximityCheck.IsWithinRange)
                return SocialOperationResult.Fail(proximityCheck.DenialReason ?? "Fuera de rango de interaccion.");

            // Verificar que el objetivo existe.
            var targetExists = await _db.Characters.AnyAsync(c => c.Id == targetId);
            if (!targetExists)
                return SocialOperationResult.Fail("El personaje objetivo no existe.");

            // Verificar si ya esta bloqueado.
            var alreadyBlocked = await _db.SocialRelationships.AnyAsync(r =>
                r.OwnerId == characterId && r.TargetId == targetId &&
                r.Type == SocialRelationshipType.Blocked);
            if (alreadyBlocked)
                return SocialOperationResult.Fail("Este jugador ya esta bloqueado.");

            // Verificar limite de bloqueos.
            var blockCount = await _db.SocialRelationships.CountAsync(r =>
                r.OwnerId == characterId && r.Type == SocialRelationshipType.Blocked);
            if (blockCount >= SocialConfiguration.MaxBlockedPerCharacter)
                return SocialOperationResult.Fail($"Has alcanzado el limite maximo de {SocialConfiguration.MaxBlockedPerCharacter} jugadores bloqueados.");

            // Decision: Blocking removes any existing friendship in BOTH directions.
            var existingFriendships = await _db.SocialRelationships
                .Where(r =>
                    ((r.OwnerId == characterId && r.TargetId == targetId) ||
                     (r.OwnerId == targetId && r.TargetId == characterId)) &&
                    r.Type == SocialRelationshipType.Friend)
                .ToListAsync();
            _db.SocialRelationships.RemoveRange(existingFriendships);

            // Decision: Also cancel any pending friend requests between the two.
            var pendingRequests = await _db.FriendRequests
                .Where(r =>
                    ((r.SenderId == characterId && r.ReceiverId == targetId) ||
                     (r.SenderId == targetId && r.ReceiverId == characterId)) &&
                    r.Status == FriendRequestStatus.Pending)
                .ToListAsync();
            foreach (var req in pendingRequests)
            {
                req.Status = FriendRequestStatus.Cancelled;
                req.ResolvedAtUtc = DateTime.UtcNow;
            }

            // Create the block relationship.
            _db.SocialRelationships.Add(new SocialRelationshipEntity
            {
                Id = Guid.NewGuid(),
                OwnerId = characterId,
                TargetId = targetId,
                Type = SocialRelationshipType.Blocked,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return SocialOperationResult.Ok();
        }
        catch (Exception ex)
        {
            return SocialOperationResult.Fail($"Error al bloquear jugador: {ex.Message}");
        }
    }

    /// <summary>
    /// Unblocks a previously blocked character.
    /// </summary>
    public async Task<SocialOperationResult> UnblockPlayerAsync(Guid characterId, Guid targetId)
    {
        try
        {
            if (characterId == targetId)
                return SocialOperationResult.Fail("No puedes desbloquearte a ti mismo.");

            var blockRelationship = await _db.SocialRelationships.FirstOrDefaultAsync(r =>
                r.OwnerId == characterId && r.TargetId == targetId &&
                r.Type == SocialRelationshipType.Blocked);

            if (blockRelationship is null)
                return SocialOperationResult.Fail("Este jugador no esta bloqueado.");

            _db.SocialRelationships.Remove(blockRelationship);
            await _db.SaveChangesAsync();

            return SocialOperationResult.Ok();
        }
        catch (Exception ex)
        {
            return SocialOperationResult.Fail($"Error al desbloquear jugador: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns the block list for a character.
    /// </summary>
    public async Task<IReadOnlyList<BlockListEntryDto>> GetBlockListAsync(Guid characterId)
    {
        var blocked = await _db.SocialRelationships
            .Where(r => r.OwnerId == characterId && r.Type == SocialRelationshipType.Blocked)
            .Join(_db.Characters,
                r => r.TargetId,
                c => c.Id,
                (r, c) => new BlockListEntryDto
                {
                    CharacterId = c.Id,
                    CharacterName = c.Name
                })
            .ToListAsync();

        return blocked;
    }

    // ────────────────────────────────────────────────────────────────────
    // Pending Requests
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns pending inbound friend requests for a character.
    /// </summary>
    public async Task<IReadOnlyList<PendingFriendRequestDto>> GetPendingInboundRequestsAsync(Guid characterId)
    {
        var cutoff = DateTime.UtcNow.AddDays(-SocialConfiguration.FriendRequestExpirationDays);

        var requests = await _db.FriendRequests
            .Where(r => r.ReceiverId == characterId &&
                        r.Status == FriendRequestStatus.Pending &&
                        r.CreatedAtUtc > cutoff)
            .Join(_db.Characters,
                r => r.SenderId,
                c => c.Id,
                (r, c) => new PendingFriendRequestDto
                {
                    RequestId = r.Id,
                    SenderCharacterId = c.Id,
                    SenderName = c.Name,
                    SenderClassName = c.ClassType.ToString(),
                    SenderLevel = c.Level,
                    SentAtUtc = r.CreatedAtUtc
                })
            .ToListAsync();

        return requests;
    }

    // ────────────────────────────────────────────────────────────────────
    // Relationship Queries (used by other services)
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks if either character has blocked the other.
    /// Used by ChatRelayService and other social features.
    /// </summary>
    public async Task<bool> IsBlockedAsync(Guid characterA, Guid characterB)
    {
        return await _db.SocialRelationships.AnyAsync(r =>
            ((r.OwnerId == characterA && r.TargetId == characterB) ||
             (r.OwnerId == characterB && r.TargetId == characterA)) &&
            r.Type == SocialRelationshipType.Blocked);
    }
}
