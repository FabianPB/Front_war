using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using War.Core.Social;
using War.Infrastructure.Persistence;

namespace War.Api.Application.Social;

// Decision: Scoped service because it depends on WarDbContext (scoped) for block checks.
// The IHubContext is singleton-safe and can be injected into scoped services without issues.
public sealed class ChatRelayService
{
    private readonly WarDbContext _db;
    private readonly ProximityValidationService _proximity;
    private readonly ChatRateLimiter _rateLimiter;
    private readonly SocialRelationshipService _relationships;
    private readonly IHubContext<War.Api.Hubs.ChatHub> _hubContext;

    public ChatRelayService(
        WarDbContext db,
        ProximityValidationService proximity,
        ChatRateLimiter rateLimiter,
        SocialRelationshipService relationships,
        IHubContext<War.Api.Hubs.ChatHub> hubContext)
    {
        _db = db;
        _proximity = proximity;
        _rateLimiter = rateLimiter;
        _relationships = relationships;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Orchestrates sending a chat message through the full validation pipeline:
    /// 1. Sanitize input
    /// 2. Validate proximity
    /// 3. Check rate limit
    /// 4. Check not blocked
    /// 5. Relay via SignalR
    /// </summary>
    public async Task<SocialOperationResult> SendMessageAsync(Guid senderId, Guid recipientId, string rawContent)
    {
        try
        {
            // 1. Sanitizar el contenido del mensaje.
            var sanitizedContent = InputSanitizer.SanitizeChatMessage(rawContent);
            if (sanitizedContent is null)
                return SocialOperationResult.Fail("El mensaje esta vacio o contiene solo caracteres no permitidos.");

            // 2. No puedes enviarte un mensaje a ti mismo.
            if (senderId == recipientId)
                return SocialOperationResult.Fail("No puedes enviarte un mensaje a ti mismo.");

            // 3. Validar proximidad entre ambos jugadores.
            var proximityCheck = _proximity.ValidateInteractionRange(senderId, recipientId);
            if (!proximityCheck.IsWithinRange)
                return SocialOperationResult.Fail(proximityCheck.DenialReason ?? "Fuera de rango de interaccion.");

            // 4. Verificar limite de velocidad (rate limit).
            if (!_rateLimiter.IsAllowed(senderId, out var cooldownSeconds))
                return SocialOperationResult.Fail(
                    $"Estas enviando mensajes demasiado rapido. Espera {cooldownSeconds} segundos.");

            // 5. Verificar que no exista bloqueo en ninguna direccion.
            var isBlocked = await _relationships.IsBlockedAsync(senderId, recipientId);
            if (isBlocked)
                return SocialOperationResult.Fail("No puedes enviar mensajes a este jugador.");

            // 6. Obtener el nombre del remitente para incluirlo en el mensaje.
            var senderName = await _db.Characters
                .Where(c => c.Id == senderId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();

            if (senderName is null)
                return SocialOperationResult.Fail("Tu personaje no existe.");

            // 7. Construir el mensaje de chat.
            // Decision: ChatMessage is a transient record — it's created, relayed via SignalR, and discarded.
            // The server does NOT persist chat history. Clients store locally if desired.
            var chatMessage = new ChatMessage(
                MessageId: Guid.NewGuid(),
                SenderId: senderId,
                SenderName: senderName,
                Content: sanitizedContent,
                SentAtUtc: DateTime.UtcNow);

            // 8. Enviar via SignalR al destinatario.
            // Decision: Send to a user-group named after the recipient's character ID.
            // The ChatHub is responsible for mapping connected clients to their character ID groups.
            await _hubContext.Clients
                .Group(recipientId.ToString())
                .SendAsync("ReceiveChatMessage", chatMessage);

            return SocialOperationResult.Ok();
        }
        catch (Exception ex)
        {
            return SocialOperationResult.Fail($"Error al enviar el mensaje: {ex.Message}");
        }
    }
}
