using System.Collections.Concurrent;

namespace War.Api.Application.GameWorld;

/// <summary>
/// Gestión de grupos in-memory (sin persistencia).
///
/// Un grupo es una colección de jugadores que comparten el efecto
/// de ciertas skills (heals, shields, buffs) cuando la habilidad
/// tiene afinidad Ally o Self+Area. Un jugador puede estar en 0 ó 1
/// grupo a la vez.
///
/// Reglas:
///   · Crear un grupo: el líder se convierte en miembro único.
///   · Invitar: solo a jugadores dentro del rango de proximidad
///     (visibilidad default, 15 unidades).
///   · Aceptar invitación: el invitado se une; se cancela cualquier
///     invitación pendiente previa.
///   · Salir: si el que sale es el líder y queda alguien, se promueve
///     al siguiente miembro; si queda vacío, el grupo se disuelve.
///   · Kick: solo el líder puede expulsar.
///   · Desconexión: el jugador se saca del grupo automáticamente.
/// </summary>
public sealed class GroupService
{
    // groupId → group
    private readonly ConcurrentDictionary<string, OnlineGroup> _groups = new();
    // invitationId → invitation
    private readonly ConcurrentDictionary<string, GroupInvitation> _pendingInvitations = new();

    private readonly GameWorldService _world;

    public GroupService(GameWorldService world)
    {
        _world = world;
    }

    // ──────────────────────────────────────────────────────────────
    // QUERY
    // ──────────────────────────────────────────────────────────────

    /// <summary>Devuelve el grupo del jugador (o null si está solo).</summary>
    public OnlineGroup? GetGroup(OnlinePlayer player)
    {
        if (player.GroupId is null) return null;
        return _groups.TryGetValue(player.GroupId, out var group) ? group : null;
    }

    /// <summary>
    /// Devuelve los jugadores del grupo (incluyendo al solicitante) que están
    /// conectados. Si no hay grupo, devuelve una lista con solo el solicitante.
    /// </summary>
    public IReadOnlyList<OnlinePlayer> GetGroupMembersOrSelf(OnlinePlayer player)
    {
        var group = GetGroup(player);
        if (group is null)
        {
            return new[] { player };
        }

        var members = new List<OnlinePlayer>(group.MemberIds.Count);
        foreach (var memberId in group.MemberIds)
        {
            var p = _world.GetPlayerByPlayerId(memberId);
            if (p is not null) members.Add(p);
        }
        // Fallback defensivo: si el grupo quedó vacío por algún motivo,
        // retornar al jugador solo para que sus propias heals le lleguen.
        return members.Count > 0 ? members : new[] { player };
    }

    public IReadOnlyList<GroupInvitation> GetPendingInvitationsFor(string inviteeId)
    {
        return _pendingInvitations.Values
            .Where(i => i.InviteeId == inviteeId)
            .ToArray();
    }

    // ──────────────────────────────────────────────────────────────
    // MUTATION
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un grupo con el jugador como único miembro y líder.
    /// Si el jugador ya está en un grupo, devuelve null y no hace nada.
    /// </summary>
    public OnlineGroup? CreateGroup(OnlinePlayer leader)
    {
        if (leader.GroupId is not null) return null;

        var group = new OnlineGroup
        {
            GroupId = Guid.NewGuid().ToString("N"),
            LeaderId = leader.PlayerId,
            MemberIds = new List<string> { leader.PlayerId }
        };

        _groups[group.GroupId] = group;
        leader.GroupId = group.GroupId;
        return group;
    }

    /// <summary>
    /// Invita a otro jugador al grupo del invitante. El invitado debe estar
    /// dentro del rango de visibilidad (NearbyDiscoveryRadius). Crea el grupo
    /// si el invitante aún no tiene uno.
    ///
    /// Devuelve el id de la invitación creada, o null si falló (fuera de
    /// rango, ya en grupo, invitando a sí mismo, etc.).
    /// </summary>
    public string? InviteToGroup(OnlinePlayer inviter, OnlinePlayer invitee)
    {
        if (inviter.PlayerId == invitee.PlayerId) return null;
        if (invitee.GroupId is not null) return null; // invitee ya está en un grupo

        // Rango: el invitado debe estar dentro del radio de descubrimiento
        if (!_world.AreInRange(inviter, invitee, GameWorldService.NearbyDiscoveryRadius))
            return null;

        // Crear el grupo del invitante si no existe
        var group = GetGroup(inviter) ?? CreateGroup(inviter);
        if (group is null) return null;

        // Evitar invitaciones duplicadas pendientes del mismo grupo
        var existing = _pendingInvitations.Values.FirstOrDefault(i =>
            i.InviteeId == invitee.PlayerId && i.GroupId == group.GroupId);
        if (existing is not null) return existing.InvitationId;

        var invitation = new GroupInvitation
        {
            InvitationId = Guid.NewGuid().ToString("N"),
            GroupId = group.GroupId,
            InviterId = inviter.PlayerId,
            InviterName = inviter.DisplayName,
            InviteeId = invitee.PlayerId,
            CreatedAt = DateTime.UtcNow
        };

        _pendingInvitations[invitation.InvitationId] = invitation;
        return invitation.InvitationId;
    }

    /// <summary>
    /// El invitado acepta una invitación. Lo añade al grupo y limpia la
    /// invitación. Si el invitado ya estaba en otro grupo, primero se
    /// le saca de ese.
    /// </summary>
    public OnlineGroup? AcceptInvitation(OnlinePlayer invitee, string invitationId)
    {
        if (!_pendingInvitations.TryRemove(invitationId, out var invitation)) return null;
        if (invitation.InviteeId != invitee.PlayerId) return null;

        if (!_groups.TryGetValue(invitation.GroupId, out var group)) return null;

        // Si el invitado ya estaba en otro grupo, sacarlo primero.
        if (invitee.GroupId is not null) LeaveGroup(invitee);

        lock (group.MutationLock)
        {
            if (!group.MemberIds.Contains(invitee.PlayerId))
            {
                group.MemberIds.Add(invitee.PlayerId);
            }
        }
        invitee.GroupId = group.GroupId;

        // Limpiar otras invitaciones pendientes del mismo invitee (ya está en un grupo)
        var toRemove = _pendingInvitations
            .Where(kv => kv.Value.InviteeId == invitee.PlayerId)
            .Select(kv => kv.Key)
            .ToArray();
        foreach (var id in toRemove) _pendingInvitations.TryRemove(id, out _);

        return group;
    }

    /// <summary>Rechazar una invitación pendiente sin efectos colaterales.</summary>
    public bool RejectInvitation(OnlinePlayer invitee, string invitationId)
    {
        if (!_pendingInvitations.TryGetValue(invitationId, out var invitation)) return false;
        if (invitation.InviteeId != invitee.PlayerId) return false;
        return _pendingInvitations.TryRemove(invitationId, out _);
    }

    /// <summary>
    /// Saca al jugador de su grupo actual. Si es el líder y queda alguien,
    /// promueve al siguiente miembro. Si el grupo queda vacío, se disuelve.
    /// </summary>
    public OnlineGroup? LeaveGroup(OnlinePlayer player)
    {
        if (player.GroupId is null) return null;
        if (!_groups.TryGetValue(player.GroupId, out var group)) { player.GroupId = null; return null; }

        lock (group.MutationLock)
        {
            group.MemberIds.Remove(player.PlayerId);

            if (group.MemberIds.Count == 0)
            {
                _groups.TryRemove(group.GroupId, out _);
            }
            else if (group.LeaderId == player.PlayerId)
            {
                group.LeaderId = group.MemberIds[0];
            }
        }

        player.GroupId = null;
        return group;
    }

    /// <summary>Solo el líder puede expulsar. Retorna true si se expulsó.</summary>
    public bool KickFromGroup(OnlinePlayer leader, OnlinePlayer target)
    {
        var group = GetGroup(leader);
        if (group is null) return false;
        if (group.LeaderId != leader.PlayerId) return false;
        if (leader.PlayerId == target.PlayerId) return false;
        if (target.GroupId != group.GroupId) return false;

        LeaveGroup(target);
        return true;
    }

    /// <summary>Limpieza al desconectarse — saca al jugador de su grupo si lo tenía.</summary>
    public void CleanupPlayer(OnlinePlayer player)
    {
        if (player.GroupId is not null) LeaveGroup(player);

        // Limpiar invitaciones pendientes emitidas o recibidas por él
        var toRemove = _pendingInvitations
            .Where(kv => kv.Value.InviterId == player.PlayerId || kv.Value.InviteeId == player.PlayerId)
            .Select(kv => kv.Key)
            .ToArray();
        foreach (var id in toRemove) _pendingInvitations.TryRemove(id, out _);
    }
}

// ──────────────────────────────────────────────────────────────
// MODELOS DE GRUPO
// ──────────────────────────────────────────────────────────────

/// <summary>
/// Estado de un grupo en memoria. MemberIds es mutable pero toda mutación
/// se hace bajo MutationLock para evitar carreras entre acceptación de
/// invitaciones y salida de miembros.
/// </summary>
public sealed class OnlineGroup
{
    public required string GroupId { get; init; }
    public string LeaderId { get; set; } = "";
    public List<string> MemberIds { get; init; } = new();
    public readonly object MutationLock = new();
}

/// <summary>Invitación pendiente emitida por un líder a un jugador cercano.</summary>
public sealed class GroupInvitation
{
    public required string InvitationId { get; init; }
    public required string GroupId { get; init; }
    public required string InviterId { get; init; }
    public required string InviterName { get; init; }
    public required string InviteeId { get; init; }
    public DateTime CreatedAt { get; init; }
}

// ──────────────────────────────────────────────────────────────
// DTOs para broadcasting
// ──────────────────────────────────────────────────────────────

public sealed record GroupMemberDto(
    string PlayerId,
    string DisplayName,
    string ClassName,
    bool IsLeader,
    decimal CurrentHp,
    decimal MaxHp);

public sealed record GroupStateDto(
    string GroupId,
    string LeaderId,
    IReadOnlyList<GroupMemberDto> Members);

public sealed record GroupInvitationDto(
    string InvitationId,
    string GroupId,
    string InviterId,
    string InviterName);
