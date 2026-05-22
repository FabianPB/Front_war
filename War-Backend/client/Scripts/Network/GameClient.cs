// ═══════════════════════════════════════════════════════════════════════════════
// WAR · Unity SignalR GameClient
// ─────────────────────────────────────────────────────────────────────────────
// Central client for the /game SignalR hub. One connection per Unity session.
// All server events are exposed as typed C# events; UI / Gameplay layers subscribe.
//
// RULE OF GOLD: server-authoritative. NEVER mutate HP, wallet, inventory or
// skill state locally. Only forward inputs and render what the server emits.
//
// Requires:
//   - NuGet: Microsoft.AspNetCore.SignalR.Client 8.x (install via NuGetForUnity)
//   - NuGet: Newtonsoft.Json (via com.unity.nuget.newtonsoft-json)
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;

namespace WAR.Network
{
    [DisallowMultipleComponent]
    public sealed class GameClient : MonoBehaviour
    {
        [Header("Server")]
        [SerializeField] private string _serverUrl = "http://localhost:5050";
        [SerializeField] private string _hubPath = "/game";

        private HubConnection _conn;
        private SynchronizationContext _unityCtx;

        // ═════════ Events raised on the Unity main thread ═════════
        public event Action<PlayerPresenceDto> OnPlayerJoined;
        public event Action<PlayerLeftPayload> OnPlayerLeft;
        public event Action<PlayerMovedPayload> OnPlayerMoved;
        public event Action<MoveResultDto> OnMoveResult;
        public event Action<OnlineCombatResult> OnCombatResult;
        public event Action<PlayerFullStateDto> OnPlayerStateUpdate;
        public event Action<PlayerPresenceDto> OnTargetStateUpdate;
        public event Action<WalletDto> OnWalletUpdate;
        public event Action<InventoryDto> OnInventoryUpdate;
        public event Action<InventoryItemDto> OnItemUpdated;
        public event Action<ChapelStateDto> OnChapelUpdate;
        public event Action<GroupStateDto> OnGroupUpdated;
        public event Action<ChatPayload> OnChatMessage;
        public event Action<SocialNotification> OnSocialNotification;
        public event Action<string> OnError;
        public event Action<string> OnSystemMessage;
        public event Action OnConnected;
        public event Action<Exception> OnDisconnected;

        public bool IsConnected => _conn?.State == HubConnectionState.Connected;
        public string CurrentPlayerId { get; private set; }

        private void Awake()
        {
            _unityCtx = SynchronizationContext.Current;
        }

        // ═════════ Connection lifecycle ═════════

        public async Task ConnectAsync()
        {
            if (_conn is not null)
                throw new InvalidOperationException("GameClient already initialized. Call DisconnectAsync first.");

            var fullUrl = _serverUrl.TrimEnd('/') + _hubPath;
            _conn = new HubConnectionBuilder()
                .WithUrl(fullUrl)
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();

            _conn.Closed += ex =>
            {
                Debug.LogWarning($"[GameClient] Connection closed: {ex?.Message}");
                Dispatch(OnDisconnected, ex);
                return Task.CompletedTask;
            };

            await _conn.StartAsync();
            Debug.Log($"[GameClient] Connected to {fullUrl}");
            Dispatch(OnConnected);
        }

        public async Task DisconnectAsync()
        {
            if (_conn is null) return;
            try { await _conn.StopAsync(); }
            catch (Exception ex) { Debug.LogWarning($"[GameClient] Stop threw: {ex.Message}"); }
            await _conn.DisposeAsync();
            _conn = null;
            CurrentPlayerId = null;
        }

        private async void OnDestroy() => await DisconnectAsync();

        // ═════════ Client → Server invocations ═════════

        public async Task<JoinWorldResultDto> JoinGameAsync(
            string displayName,
            string className,
            CharacterGender gender,
            int? level = null,
            int? ascensionLevel = null)
        {
            RequireConnected();
            var result = await _conn.InvokeAsync<JoinWorldResultDto>(
                "JoinGame", displayName, className, level, ascensionLevel, gender.ToString());
            CurrentPlayerId = result?.Player?.PlayerId;
            return result;
        }

        public Task MoveAsync(string direction) => SendFire("Move", direction);

        public Task MoveToAsync(float x, float y) => SendFire("MoveTo", x, y);

        public Task BasicAttackAsync(string targetPlayerId) => SendFire("BasicAttack", targetPlayerId);

        public Task UseSkillAsync(int skillIndex, string targetPlayerId)
            => SendFire("UseSkill", skillIndex, targetPlayerId);

        public Task<PlayerFullStateDto> GetMyStateAsync()
        {
            RequireConnected();
            return _conn.InvokeAsync<PlayerFullStateDto>("GetMyState");
        }

        public Task<WorldSnapshotDto> GetWorldSnapshotAsync()
        {
            RequireConnected();
            return _conn.InvokeAsync<WorldSnapshotDto>("GetWorldSnapshot");
        }

        public Task<IReadOnlyList<PlayerPresenceDto>> GetNearbyPlayersAsync(float? radius = null)
        {
            RequireConnected();
            return _conn.InvokeAsync<IReadOnlyList<PlayerPresenceDto>>("GetNearbyPlayers", radius);
        }

        public Task<WalletDto> GetWalletAsync()
        {
            RequireConnected();
            return _conn.InvokeAsync<WalletDto>("GetWallet");
        }

        public Task<InventoryDto> GetInventoryAsync()
        {
            RequireConnected();
            return _conn.InvokeAsync<InventoryDto>("GetInventory");
        }

        public Task<ChapelStateDto> GetChapelStateAsync()
        {
            RequireConnected();
            return _conn.InvokeAsync<ChapelStateDto>("GetChapelState");
        }

        public Task<SkillAscensionPreviewDto> GetSkillAscensionPreviewAsync(string skillId)
        {
            RequireConnected();
            return _conn.InvokeAsync<SkillAscensionPreviewDto>("GetSkillAscensionPreview", skillId);
        }

        public Task EquipItemAsync(string itemId) => SendFire("EquipItem", itemId);
        public Task UnequipItemAsync(string itemId) => SendFire("UnequipItem", itemId);
        public Task ExpandInventoryAsync() => SendFire("ExpandInventory");
        public Task DevelopItemAsync(string itemId) => SendFire("DevelopItem", itemId);
        public Task CraftTierUpAsync(string idA, string idB) => SendFire("CraftTierUp", idA, idB);
        public Task UpgradeChapelAsync() => SendFire("UpgradeChapel");
        public Task ConvertCurrencyAsync(string to, long amountToCreate) => SendFire("ConvertCurrency", to, amountToCreate);
        public Task AscendSkillAsync(string skillId) => SendFire("AscendSkill", skillId);
        public Task SendChatAsync(string message) => SendFire("SendChatMessage", message);
        public Task SendFriendRequestAsync(string targetId) => SendFire("SendFriendRequest", targetId);
        public Task AcceptFriendRequestAsync(string requestId) => SendFire("AcceptFriendRequest", requestId);
        public Task BlockPlayerAsync(string targetId) => SendFire("BlockPlayer", targetId);
        public Task CreateGroupAsync() => SendFire("CreateGroup");
        public Task InviteToGroupAsync(string targetId) => SendFire("InviteToGroup", targetId);
        public Task AcceptGroupInviteAsync(string invitationId) => SendFire("AcceptGroupInvite", invitationId);
        public Task KickFromGroupAsync(string targetId) => SendFire("KickFromGroup", targetId);

        // ═════════ Internal helpers ═════════

        private void RegisterHandlers()
        {
            _conn.On<PlayerPresenceDto>("PlayerJoined", p => Dispatch(OnPlayerJoined, p));
            _conn.On<PlayerLeftPayload>("PlayerLeft", p => Dispatch(OnPlayerLeft, p));
            _conn.On<PlayerMovedPayload>("PlayerMoved", p => Dispatch(OnPlayerMoved, p));
            _conn.On<MoveResultDto>("MoveResult", r => Dispatch(OnMoveResult, r));
            _conn.On<OnlineCombatResult>("CombatResult", r => Dispatch(OnCombatResult, r));
            _conn.On<PlayerFullStateDto>("PlayerStateUpdate", s => Dispatch(OnPlayerStateUpdate, s));
            _conn.On<PlayerPresenceDto>("TargetStateUpdate", t => Dispatch(OnTargetStateUpdate, t));
            _conn.On<WalletDto>("WalletUpdate", w => Dispatch(OnWalletUpdate, w));
            _conn.On<InventoryDto>("InventoryUpdate", i => Dispatch(OnInventoryUpdate, i));
            _conn.On<InventoryItemDto>("ItemUpdated", i => Dispatch(OnItemUpdated, i));
            _conn.On<ChapelStateDto>("ChapelUpdate", c => Dispatch(OnChapelUpdate, c));
            _conn.On<GroupStateDto>("GroupUpdated", g => Dispatch(OnGroupUpdated, g));
            _conn.On<ChatPayload>("ChatMessage", c => Dispatch(OnChatMessage, c));
            _conn.On<SocialNotification>("SocialNotification", n => Dispatch(OnSocialNotification, n));
            _conn.On<string>("Error", e => Dispatch(OnError, e));
            _conn.On<string>("SystemMessage", m => Dispatch(OnSystemMessage, m));
        }

        private Task SendFire(string method, params object[] args)
        {
            RequireConnected();
            return _conn.SendCoreAsync(method, args);
        }

        private void RequireConnected()
        {
            if (!IsConnected)
                throw new InvalidOperationException(
                    "GameClient is not connected. Call ConnectAsync first and await it.");
        }

        private void Dispatch(Action ev)
        {
            if (ev is null) return;
            _unityCtx.Post(_ => ev.Invoke(), null);
        }

        private void Dispatch<T>(Action<T> ev, T arg)
        {
            if (ev is null) return;
            _unityCtx.Post(_ => ev.Invoke(arg), null);
        }
    }
}
