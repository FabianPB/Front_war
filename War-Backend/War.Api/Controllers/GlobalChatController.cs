using Microsoft.AspNetCore.Mvc;
using War.Api.Application.GameWorld;
using War.Api.Application.Marketplace;

namespace War.Api.Controllers;

/// <summary>
/// Chat global vía REST + polling. Diseñado para el cliente Flutter, que
/// usa <c>GET /api/chat/global?sinceUtc=...</c> cada N segundos para refrescar.
/// El cliente Unity (SignalR) usa el ChatHub de proximidad por separado.
/// </summary>
[ApiController]
[Route("api/chat/global")]
public sealed class GlobalChatController : ControllerBase
{
    private readonly GlobalChatService _chat;
    private readonly HubActionRateLimiter _rateLimiter;

    public GlobalChatController(GlobalChatService chat, HubActionRateLimiter rateLimiter)
    {
        _chat = chat;
        _rateLimiter = rateLimiter;
    }

    public sealed record PostMessageRequest(string SenderAccountId, string SenderDisplayName, string Content);

    /// <summary>
    /// Devuelve los últimos <paramref name="limit"/> mensajes. Si se pasa
    /// <paramref name="sinceUtc"/> (ISO8601) sólo devuelve los más nuevos
    /// que ese instante — formato ideal para polling incremental.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<GlobalChatMessageDto>> GetRecent(
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? sinceUtc = null)
    {
        var messages = _chat.GetRecent(limit, sinceUtc);
        return Ok(messages);
    }

    [HttpPost]
    public ActionResult<GlobalChatMessageDto> Post([FromBody] PostMessageRequest body)
    {
        if (body is null
            || string.IsNullOrWhiteSpace(body.SenderAccountId)
            || string.IsNullOrWhiteSpace(body.Content))
            return BadRequest(new { error = "senderAccountId y content requeridos." });

        // Rate limit por cuenta — para no permitir floods desde una sola.
        if (!_rateLimiter.TryAcquire($"chat:{body.SenderAccountId}", out var retry))
            return StatusCode(429, new { error = "Demasiadas solicitudes.", retryAfterMs = retry });

        try
        {
            var msg = _chat.Post(body.SenderAccountId, body.SenderDisplayName, body.Content);
            return Ok(msg);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
