// ═══════════════════════════════════════════════════════════════════════════════
// WAR · Unity SignalR ChatClient
// ─────────────────────────────────────────────────────────────────────────────
// Separate hub from GameHub. The chat is proximity-local, rate-limited server-side
// (10 msgs / 5 s, 30 s penalty). The characterId is passed as query param per the
// backend contract (War.Api/Hubs/ChatHub.cs — OnConnectedAsync).
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;

namespace WAR.Network
{
    [DisallowMultipleComponent]
    public sealed class ChatClient : MonoBehaviour
    {
        [Header("Server")]
        [SerializeField] private string _serverUrl = "http://localhost:5050";
        [SerializeField] private string _hubPath = "/chat";

        private HubConnection _conn;
        private SynchronizationContext _unityCtx;

        public event Action<ChatPayload> OnMessage;
        public event Action<string> OnError;
        public event Action<Exception> OnDisconnected;

        public bool IsConnected => _conn?.State == HubConnectionState.Connected;

        private void Awake() => _unityCtx = SynchronizationContext.Current;

        public async Task ConnectAsync(Guid characterId)
        {
            if (_conn is not null)
                throw new InvalidOperationException("ChatClient already initialized.");

            var url = $"{_serverUrl.TrimEnd('/')}{_hubPath}?characterId={characterId}";
            _conn = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            _conn.On<ChatPayload>("ChatMessage", p => Dispatch(OnMessage, p));
            _conn.On<string>("Error", e => Dispatch(OnError, e));

            _conn.Closed += ex =>
            {
                Debug.LogWarning($"[ChatClient] Closed: {ex?.Message}");
                Dispatch(OnDisconnected, ex);
                return Task.CompletedTask;
            };

            await _conn.StartAsync();
            Debug.Log($"[ChatClient] Connected for characterId {characterId}");
        }

        public Task SendAsync(Guid recipientCharacterId, string content)
        {
            if (!IsConnected)
                throw new InvalidOperationException("ChatClient is not connected.");
            return _conn.InvokeAsync("SendMessage", recipientCharacterId, content);
        }

        public async Task DisconnectAsync()
        {
            if (_conn is null) return;
            try { await _conn.StopAsync(); }
            catch (Exception ex) { Debug.LogWarning($"[ChatClient] Stop threw: {ex.Message}"); }
            await _conn.DisposeAsync();
            _conn = null;
        }

        private async void OnDestroy() => await DisconnectAsync();

        private void Dispatch<T>(Action<T> ev, T arg)
        {
            if (ev is null) return;
            _unityCtx.Post(_ => ev.Invoke(arg), null);
        }
    }
}
