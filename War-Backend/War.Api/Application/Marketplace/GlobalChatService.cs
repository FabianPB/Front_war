using System.Collections.Concurrent;

namespace War.Api.Application.Marketplace;

/// <summary>
/// Chat global GLOBAL del mundo — distinto del ChatHub de proximidad.
/// Mantiene un buffer circular de los últimos N mensajes para que clientes
/// que se conectan tarde puedan ver historial reciente.
///
/// Diseño: thread-safe, in-memory, sin persistencia. Para presentación.
/// </summary>
public sealed class GlobalChatService
{
    public const int MaxBufferedMessages = 200;
    public const int MaxContentLength = 280;

    // ConcurrentQueue para writes baratos; convertimos a List al leer.
    private readonly ConcurrentQueue<GlobalChatMessageDto> _messages = new();
    private readonly object _lock = new();

    public GlobalChatMessageDto Post(string senderAccountId, string senderDisplayName, string content)
    {
        if (string.IsNullOrWhiteSpace(senderAccountId))
            throw new ArgumentException("senderAccountId vacío.", nameof(senderAccountId));
        var trimmed = (content ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Mensaje vacío.", nameof(content));
        if (trimmed.Length > MaxContentLength)
            trimmed = trimmed.Substring(0, MaxContentLength);

        var name = string.IsNullOrWhiteSpace(senderDisplayName) ? "Guerrero" : senderDisplayName.Trim();

        var msg = new GlobalChatMessageDto(
            Guid.NewGuid(),
            senderAccountId,
            name,
            trimmed,
            DateTime.UtcNow);

        lock (_lock)
        {
            _messages.Enqueue(msg);
            // Trim si excede buffer
            while (_messages.Count > MaxBufferedMessages && _messages.TryDequeue(out _)) { }
        }

        return msg;
    }

    /// <summary>
    /// Devuelve los últimos <paramref name="limit"/> mensajes, ordenados
    /// cronológicamente (antiguo primero). Si <paramref name="sinceUtc"/>
    /// se especifica, sólo devuelve los más nuevos que ese instante.
    /// </summary>
    public IReadOnlyList<GlobalChatMessageDto> GetRecent(int limit = 100, DateTime? sinceUtc = null)
    {
        if (limit <= 0) limit = 100;
        if (limit > MaxBufferedMessages) limit = MaxBufferedMessages;

        var snapshot = _messages.ToArray();
        IEnumerable<GlobalChatMessageDto> query = snapshot;
        if (sinceUtc.HasValue)
            query = query.Where(m => m.SentAtUtc > sinceUtc.Value);
        return query
            .OrderBy(m => m.SentAtUtc)
            .TakeLast(limit)
            .ToList();
    }
}
