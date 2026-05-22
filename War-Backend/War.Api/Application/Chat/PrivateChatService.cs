namespace War.Api.Application.Chat;

/// <summary>
/// Mensaje privado entre dos jugadores (no persistido — sólo en memoria).
/// </summary>
public sealed record PrivateMessageDto(
    Guid MessageId,
    string SenderCharacterId,
    string SenderDisplayName,
    string RecipientCharacterId,
    string Content,
    DateTime SentAtUtc);

/// <summary>
/// Chat privado 1-a-1 en memoria entre dos character IDs cualesquiera.
/// No requiere que los personajes estén en el mundo de juego.
/// </summary>
public sealed class PrivateChatService
{
    public const int MaxMessagesPerConversation = 200;
    public const int MaxContentLength = 500;

    private readonly object _lock = new();
    private readonly Dictionary<string, List<PrivateMessageDto>> _conversations = new();

    /// Clave simétrica: independientemente del orden de los IDs, la misma conversación.
    private static string Key(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";

    /// Envía un mensaje y lo almacena en la conversación.
    public PrivateMessageDto Send(
        string senderCharacterId,
        string senderDisplayName,
        string recipientCharacterId,
        string content)
    {
        var trimmed = (content ?? string.Empty).Trim();
        if (trimmed.Length > MaxContentLength) trimmed = trimmed[..MaxContentLength];

        var msg = new PrivateMessageDto(
            Guid.NewGuid(),
            senderCharacterId,
            string.IsNullOrWhiteSpace(senderDisplayName) ? "Guerrero" : senderDisplayName.Trim(),
            recipientCharacterId,
            trimmed,
            DateTime.UtcNow);

        var key = Key(senderCharacterId, recipientCharacterId);
        lock (_lock)
        {
            if (!_conversations.TryGetValue(key, out var list))
            {
                list = new List<PrivateMessageDto>();
                _conversations[key] = list;
            }
            list.Add(msg);
            // Circular buffer
            if (list.Count > MaxMessagesPerConversation)
                list.RemoveAt(0);
        }
        return msg;
    }

    /// Devuelve los últimos <paramref name="limit"/> mensajes de la conversación entre los dos IDs.
    public IReadOnlyList<PrivateMessageDto> GetConversation(
        string myId, string partnerId, int limit = 100, DateTime? sinceUtc = null)
    {
        if (limit <= 0) limit = 100;
        if (limit > MaxMessagesPerConversation) limit = MaxMessagesPerConversation;

        var key = Key(myId, partnerId);
        lock (_lock)
        {
            if (!_conversations.TryGetValue(key, out var list))
                return Array.Empty<PrivateMessageDto>();

            IEnumerable<PrivateMessageDto> query = list;
            if (sinceUtc.HasValue)
                query = query.Where(m => m.SentAtUtc > sinceUtc.Value);
            return query.TakeLast(limit).ToList();
        }
    }
}
