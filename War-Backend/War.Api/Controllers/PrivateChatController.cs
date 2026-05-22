using Microsoft.AspNetCore.Mvc;
using War.Api.Application.Chat;
using War.Api.Application.GameWorld;

namespace War.Api.Controllers;

/// <summary>
/// Chat privado 1-a-1 vía REST + polling.
/// Diseñado para el cliente Flutter sin necesidad de SignalR.
/// </summary>
[ApiController]
[Route("api/chat/private")]
public sealed class PrivateChatController : ControllerBase
{
    private readonly PrivateChatService _chat;
    private readonly HubActionRateLimiter _rateLimiter;

    public PrivateChatController(PrivateChatService chat, HubActionRateLimiter rateLimiter)
    {
        _chat = chat;
        _rateLimiter = rateLimiter;
    }

    public sealed record SendPrivateMessageRequest(
        string SenderCharacterId,
        string SenderDisplayName,
        string RecipientCharacterId,
        string Content);

    /// <summary>
    /// Envía un mensaje privado a otro jugador.
    /// </summary>
    [HttpPost]
    public IActionResult Send([FromBody] SendPrivateMessageRequest body)
    {
        if (body is null
            || string.IsNullOrWhiteSpace(body.SenderCharacterId)
            || string.IsNullOrWhiteSpace(body.RecipientCharacterId)
            || string.IsNullOrWhiteSpace(body.Content))
            return BadRequest(new { error = "Campos requeridos: senderCharacterId, recipientCharacterId, content." });

        if (body.SenderCharacterId == body.RecipientCharacterId)
            return BadRequest(new { error = "No puedes enviarte mensajes a ti mismo." });

        if (!_rateLimiter.TryAcquire($"pm:{body.SenderCharacterId}", out var retry))
            return StatusCode(429, new { error = "Demasiados mensajes.", retryAfterMs = retry });

        var msg = _chat.Send(
            body.SenderCharacterId,
            body.SenderDisplayName,
            body.RecipientCharacterId,
            body.Content);

        return Ok(msg);
    }

    /// <summary>
    /// Devuelve el historial de conversación entre el caller (X-Character-Id) y partnerId.
    /// Soporta polling incremental con sinceUtc.
    /// </summary>
    [HttpGet("{partnerId}")]
    public IActionResult GetConversation(
        string partnerId,
        [FromQuery] int limit = 100,
        [FromQuery] DateTime? sinceUtc = null)
    {
        var myId = Request.Headers["X-Character-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(myId))
            return BadRequest(new { error = "Header X-Character-Id requerido." });

        var msgs = _chat.GetConversation(myId, partnerId, limit, sinceUtc);
        return Ok(msgs);
    }
}
