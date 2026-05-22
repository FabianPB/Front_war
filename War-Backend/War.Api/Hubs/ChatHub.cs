using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using War.Api.Application.Social;
using War.Api.Localization;

namespace War.Api.Hubs;

// Decision: The hub is thin — it handles connection lifecycle and delegates all business logic to services.
// This keeps the hub testable and prevents SignalR-specific code from leaking into business rules.
public sealed class ChatHub : Hub
{
    // Decision: Static dictionary because Hub instances are transient (one per invocation).
    // Connection mapping must survive across hub method calls.
    private static readonly ConcurrentDictionary<Guid, string> CharacterConnections = new();

    private readonly ChatRelayService _chatRelayService;
    private readonly ChatRateLimiter _rateLimiter;

    public ChatHub(ChatRelayService chatRelayService, ChatRateLimiter rateLimiter)
    {
        _chatRelayService = chatRelayService;
        _rateLimiter = rateLimiter;
    }

    // TODO [Unity Integration]: The Unity client must send characterId as a query parameter when connecting:
    // connection = new HubConnectionBuilder().WithUrl($"https://server/chat?characterId={characterId}").Build();
    public override async Task OnConnectedAsync()
    {
        try
        {
            var characterIdStr = Context.GetHttpContext()?.Request.Query["characterId"].FirstOrDefault();
            if (Guid.TryParse(characterIdStr, out var characterId))
            {
                CharacterConnections[characterId] = Context.ConnectionId;
                // Decision: Store characterId in connection items for easy retrieval during message sends.
                Context.Items["CharacterId"] = characterId;

                // Decision: Add the connection to a group named after the character's ID.
                // ChatRelayService sends messages to Groups (not individual connections) so that
                // future multi-device support is possible without changing the relay service.
                await Groups.AddToGroupAsync(Context.ConnectionId, characterId.ToString());
            }

            await base.OnConnectedAsync();
        }
        catch (Exception)
        {
            // Decision: Swallow connection errors to prevent the hub from crashing.
            // The client will retry automatically via SignalR's reconnection logic.
            await base.OnConnectedAsync();
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            if (Context.Items.TryGetValue("CharacterId", out var idObj) && idObj is Guid characterId)
            {
                CharacterConnections.TryRemove(characterId, out _);
                _rateLimiter.RemoveState(characterId);

                // Decision: Remove from group on disconnect to prevent stale group memberships.
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, characterId.ToString());
            }
        }
        catch (Exception)
        {
            // Decision: Never let cleanup errors break the disconnect flow.
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Decision: Hub method receives primitive parameters to match what the Unity SignalR client sends.
    // Model validation and business rules happen inside ChatRelayService.
    public async Task SendMessage(Guid recipientCharacterId, string content)
    {
        try
        {
            if (!Context.Items.TryGetValue("CharacterId", out var idObj) || idObj is not Guid senderId)
            {
                await Clients.Caller.SendAsync("Error", UiStrings.ChatSenderNotIdentified);
                return;
            }

            var result = await _chatRelayService.SendMessageAsync(senderId, recipientCharacterId, content);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("Error", result.ErrorMessage);
            }
        }
        catch (Exception)
        {
            // Decision: Never expose internal exception details to clients.
            await Clients.Caller.SendAsync("Error", UiStrings.ChatInternalError);
        }
    }

    /// Used by other services to check if a character is currently connected.
    public static string? GetConnectionId(Guid characterId)
    {
        return CharacterConnections.TryGetValue(characterId, out var connectionId) ? connectionId : null;
    }
}
