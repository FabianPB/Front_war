# WAR · Unity client integration guide

Documento canónico para el cliente Unity que se conecta al backend WAR (branch `Raizon`). Todo el código aquí es server-authoritative — el cliente NUNCA calcula HP, daño, wallet ni ascensión; solo envía input y renderiza lo que el servidor emite.

## 1. Project setup

- **Unity 2022.3 LTS** (o superior).
- **Rendering pipeline**: URP.
- **Scripting backend**: IL2CPP para builds release, Mono OK para desarrollo.
- **API compatibility level**: .NET Standard 2.1.

### Packages (Package Manager)

Instalar vía Window → Package Manager:
- `com.unity.render-pipelines.universal` (URP)
- `com.unity.inputsystem` (New Input System)
- `com.unity.addressables` (para streaming de personajes/props)
- `com.unity.nuget.newtonsoft-json`

### NuGet packages (via NuGetForUnity)

Instala [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) y luego:
- `Microsoft.AspNetCore.SignalR.Client` (última versión 8.x)
- `Microsoft.Extensions.Logging` (dependencia transitiva)

Alternativa: usar `SignalR.Client.Unity` si prefieres un puerto específico para Unity.

## 2. Estructura de carpetas sugerida

```
Assets/WAR/
├── Characters/       (FBX, texturas, materials, prefabs)
├── Weapons/
├── Armor/
├── Environment/
├── UI/
│   ├── Icons/
│   ├── HUD/
│   └── Menus/
├── VFX/
├── Audio/
└── Scripts/
    ├── Network/      (SignalR client, DTOs)
    ├── Gameplay/     (Input, controllers)
    ├── Rendering/    (Animator controllers, shaders)
    └── UI/
```

## 3. Network layer — DTOs

Archivo: `Assets/WAR/Scripts/Network/Dtos.cs`

Los DTOs tienen que coincidir EXACTAMENTE con los records C# del backend en `War.Api/Application/GameWorld/GameWorldModels.cs`. SignalR usa camelCase en JSON por default.

```csharp
using System;
using System.Collections.Generic;

namespace WAR.Network
{
    public enum CharacterGender { Male = 0, Female = 1 }

    [Serializable]
    public class PlayerPresenceDto
    {
        public string PlayerId;
        public string DisplayName;
        public string ClassName;        // "Sorcerer" | "Juramentada" | "Lancero" | "Bruiser"
        public CharacterGender Gender;
        public int Level;
        public float X;
        public float Y;
        public decimal CurrentHp;
        public decimal MaxHp;
        public bool IsDefeated;
    }

    [Serializable]
    public class PlayerFullStateDto
    {
        public string PlayerId;
        public string DisplayName;
        public string ClassName;
        public CharacterGender Gender;
        public int Level;
        public int AscensionLevel;
        public float X;
        public float Y;
        public decimal CurrentHp;
        public decimal MaxHp;
        public decimal CurrentMana;
        public decimal MaxMana;
        public List<SkillSlotDto> Skills;
        public List<ConditionDto> Conditions;
    }

    [Serializable]
    public class SkillSlotDto
    {
        public string SkillId;
        public string Name;
        public decimal ManaCost;
        public decimal BaseCooldownSeconds;
        public float RemainingCooldown;
        public bool IsOnCooldown;
        public string DamageType;
    }

    [Serializable]
    public class ConditionDto
    {
        public string ConditionType;    // "Heat" | "Cold" | "Poison" | etc.
        public string Category;         // "State" | "DoT" | "CrowdControl"
        public float RemainingSeconds;
    }

    [Serializable]
    public class WorldSnapshotDto
    {
        public int PlayerCount;
        public List<PlayerPresenceDto> Players;
    }

    [Serializable]
    public class MoveResultDto
    {
        public float X;
        public float Y;
        public List<PlayerPresenceDto> NearbyPlayers;
    }

    [Serializable]
    public class JoinWorldResultDto
    {
        public PlayerFullStateDto Player;
        public WorldSnapshotDto WorldSnapshot;
    }

    [Serializable]
    public class OnlineCombatResult
    {
        public string ActorPlayerId;
        public string TargetPlayerId;
        public string ActionType;       // "BasicAttack" | "Skill"
        public string Outcome;          // "Hit" | "Miss" | "Blocked" | "InsufficientResources"
        public string BlockedReason;    // nullable
        public DateTime Timestamp;
        public decimal DamageDealt;
        public string SkillId;          // nullable
        public int BasicComboStage;     // 1-6, relevant for basic attacks
    }

    [Serializable]
    public class WalletDto
    {
        public long Copper;
        public long Silver;
        public long Gold;
        public int Energy;
        public int EnergyMax;
    }

    [Serializable]
    public class InventoryItemDto
    {
        public string ItemId;
        public string ItemType;         // "Equipment" | "Material" | "Book" | "Consumable"
        public string DefinitionId;
        public int Quantity;
        public int SlotIndex;
        public int Tier;
        public int DevelopmentLevel;
        public bool IsEquipped;
        public string EquippedSlot;     // nullable
    }

    [Serializable]
    public class InventoryDto
    {
        public int Capacity;
        public int MaxCapacity;
        public int UsedSlots;
        public int FreeSlots;
        public int ExpansionsPurchased;
        public int MaxExpansions;
        public List<InventoryItemDto> Items;
    }

    [Serializable]
    public class ChapelStateDto
    {
        public int Level;
        public int MaxLevel;
        public int? CharacterLevelRequiredForNext;
        public CurrencyCostDto PossessionCaps;
        public long SilverConvDaily;
        public long SilverConvWeekly;
        public long SilverConvMonthly;
        public long GoldConvDaily;
        public long GoldConvWeekly;
        public long GoldConvMonthly;
    }

    [Serializable]
    public class CurrencyCostDto
    {
        public long Copper;
        public long Silver;
        public long Gold;
        public long Energy;
    }

    [Serializable]
    public class GroupStateDto
    {
        public string GroupId;
        public string LeaderId;
        public List<string> MemberIds;
        public DateTime CreatedAt;
    }

    [Serializable]
    public class ChatPayload
    {
        public string SenderId;
        public string SenderName;
        public string RecipientId;
        public string Content;
        public DateTime Timestamp;
    }
}
```

## 4. SignalR client — GameClient

Archivo: `Assets/WAR/Scripts/Network/GameClient.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;

namespace WAR.Network
{
    /// <summary>
    /// Cliente central del GameHub del backend WAR. Mantiene UNA conexión activa.
    /// Todos los eventos del servidor se exponen como C# events que la UI y gameplay consumen.
    /// Server-authoritative: nunca mutamos estado local, solo forwardeamos inputs y reflejamos eventos.
    /// </summary>
    public sealed class GameClient : MonoBehaviour
    {
        [Header("Server")]
        [SerializeField] private string _serverUrl = "http://localhost:5050";
        [SerializeField] private string _hubPath = "/game";

        private HubConnection _conn;

        // ═════════ Eventos emitidos por el servidor ═════════
        public event Action<PlayerPresenceDto> OnPlayerJoined;
        public event Action<string, string> OnPlayerLeft;                    // playerId, displayName
        public event Action<string, float, float> OnPlayerMoved;             // playerId, x, y
        public event Action<MoveResultDto> OnMoveResult;
        public event Action<OnlineCombatResult> OnCombatResult;
        public event Action<PlayerFullStateDto> OnPlayerStateUpdate;
        public event Action<PlayerPresenceDto> OnTargetStateUpdate;
        public event Action<string> OnError;
        public event Action<ChatPayload> OnChatMessage;
        public event Action<WalletDto> OnWalletUpdate;
        public event Action<InventoryDto> OnInventoryUpdate;
        public event Action<InventoryItemDto> OnItemUpdated;
        public event Action<ChapelStateDto> OnChapelUpdate;
        public event Action<GroupStateDto> OnGroupUpdated;
        public event Action<string> OnSystemMessage;

        // ═════════ Connection lifecycle ═════════

        public bool IsConnected => _conn?.State == HubConnectionState.Connected;

        public async Task ConnectAsync()
        {
            if (_conn is not null)
                throw new InvalidOperationException("Already connected.");

            var fullUrl = _serverUrl.TrimEnd('/') + _hubPath;
            _conn = new HubConnectionBuilder()
                .WithUrl(fullUrl)
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();

            _conn.Closed += async ex =>
            {
                Debug.LogWarning($"[GameClient] Connection closed: {ex?.Message}");
                await Task.CompletedTask;
            };

            await _conn.StartAsync();
            Debug.Log("[GameClient] Connected.");
        }

        public async Task DisconnectAsync()
        {
            if (_conn is null) return;
            await _conn.StopAsync();
            await _conn.DisposeAsync();
            _conn = null;
        }

        // ═════════ Client → Server invocations ═════════

        public Task<JoinWorldResultDto> JoinGameAsync(string displayName, string className, CharacterGender gender, int? level = null, int? ascensionLevel = null)
        {
            RequireConnected();
            return _conn.InvokeAsync<JoinWorldResultDto>(
                "JoinGame", displayName, className, level, ascensionLevel, gender.ToString());
        }

        public Task MoveAsync(string direction)
            => Invoke("Move", direction);

        public Task MoveToAsync(float x, float y)
            => Invoke("MoveTo", x, y);

        public Task BasicAttackAsync(string targetPlayerId)
            => Invoke("BasicAttack", targetPlayerId);

        public Task UseSkillAsync(int skillIndex, string targetPlayerId)
            => Invoke("UseSkill", skillIndex, targetPlayerId);

        public Task<PlayerFullStateDto> GetMyStateAsync()
            => _conn.InvokeAsync<PlayerFullStateDto>("GetMyState");

        public Task<WorldSnapshotDto> GetWorldSnapshotAsync()
            => _conn.InvokeAsync<WorldSnapshotDto>("GetWorldSnapshot");

        public Task<IReadOnlyList<PlayerPresenceDto>> GetNearbyPlayersAsync(float? radius = null)
            => _conn.InvokeAsync<IReadOnlyList<PlayerPresenceDto>>("GetNearbyPlayers", radius);

        public Task SendChatAsync(string message)
            => Invoke("SendChatMessage", message);

        public Task EquipItemAsync(string itemId) => Invoke("EquipItem", itemId);
        public Task UnequipItemAsync(string itemId) => Invoke("UnequipItem", itemId);
        public Task ExpandInventoryAsync() => Invoke("ExpandInventory");
        public Task UpgradeChapelAsync() => Invoke("UpgradeChapel");
        public Task ConvertCurrencyAsync(string to, long amountToCreate) => Invoke("ConvertCurrency", to, amountToCreate);
        public Task AscendSkillAsync(string skillId) => Invoke("AscendSkill", skillId);

        // ═════════ Private helpers ═════════

        private void RegisterHandlers()
        {
            _conn.On<PlayerPresenceDto>("PlayerJoined", p => Dispatch(OnPlayerJoined, p));
            _conn.On<string, string>("PlayerLeft", (id, name) => Dispatch(OnPlayerLeft, id, name));
            _conn.On<MoveResultDto>("MoveResult", r => Dispatch(OnMoveResult, r));
            _conn.On<OnlineCombatResult>("CombatResult", r => Dispatch(OnCombatResult, r));
            _conn.On<PlayerFullStateDto>("PlayerStateUpdate", s => Dispatch(OnPlayerStateUpdate, s));
            _conn.On<PlayerPresenceDto>("TargetStateUpdate", p => Dispatch(OnTargetStateUpdate, p));
            _conn.On<string>("Error", e => Dispatch(OnError, e));
            _conn.On<ChatPayload>("ChatMessage", c => Dispatch(OnChatMessage, c));
            _conn.On<WalletDto>("WalletUpdate", w => Dispatch(OnWalletUpdate, w));
            _conn.On<InventoryDto>("InventoryUpdate", i => Dispatch(OnInventoryUpdate, i));
            _conn.On<InventoryItemDto>("ItemUpdated", i => Dispatch(OnItemUpdated, i));
            _conn.On<ChapelStateDto>("ChapelUpdate", c => Dispatch(OnChapelUpdate, c));
            _conn.On<GroupStateDto>("GroupUpdated", g => Dispatch(OnGroupUpdated, g));
            _conn.On<string>("SystemMessage", m => Dispatch(OnSystemMessage, m));

            // PlayerMoved viene con shape { PlayerId, X, Y }
            _conn.On<dynamic>("PlayerMoved", d =>
            {
                string pid = d.PlayerId ?? d.playerId;
                float x = (float)(d.X ?? d.x);
                float y = (float)(d.Y ?? d.y);
                Dispatch(OnPlayerMoved, pid, x, y);
            });
        }

        private Task Invoke(string method, params object[] args)
        {
            RequireConnected();
            return _conn.SendCoreAsync(method, args);
        }

        private void RequireConnected()
        {
            if (!IsConnected)
                throw new InvalidOperationException("GameClient is not connected. Call ConnectAsync first.");
        }

        // Unity eventos deben raisearse en el main thread. Usamos SynchronizationContext.
        private readonly System.Threading.SynchronizationContext _unityCtx = System.Threading.SynchronizationContext.Current;

        private void Dispatch<T>(Action<T> ev, T arg)
        {
            if (ev is null) return;
            _unityCtx.Post(_ => ev.Invoke(arg), null);
        }

        private void Dispatch<T1, T2>(Action<T1, T2> ev, T1 a, T2 b)
        {
            if (ev is null) return;
            _unityCtx.Post(_ => ev.Invoke(a, b), null);
        }

        private void Dispatch<T1, T2, T3>(Action<T1, T2, T3> ev, T1 a, T2 b, T3 c)
        {
            if (ev is null) return;
            _unityCtx.Post(_ => ev.Invoke(a, b, c), null);
        }

        private async void OnDestroy()
        {
            await DisconnectAsync();
        }
    }
}
```

**Uso básico**:

```csharp
public class LoginScene : MonoBehaviour
{
    [SerializeField] private GameClient _client;

    public async void OnClickJoin(string name, string className, CharacterGender gender)
    {
        await _client.ConnectAsync();
        _client.OnPlayerStateUpdate += HandleMyState;
        _client.OnCombatResult += HandleCombat;

        var result = await _client.JoinGameAsync(name, className, gender);
        Debug.Log($"Joined world: {result.WorldSnapshot.PlayerCount} players online");
    }

    private void HandleMyState(PlayerFullStateDto s) { /* update HUD */ }
    private void HandleCombat(OnlineCombatResult r) { /* play VFX + damage number */ }
}
```

## 5. ChatClient (separate hub)

El ChatHub usa una conexión distinta con `?characterId=<guid>` en la query.

```csharp
public sealed class ChatClient : MonoBehaviour
{
    [SerializeField] private string _serverUrl = "http://localhost:5050";
    [SerializeField] private string _hubPath = "/chat";

    private HubConnection _conn;

    public event Action<ChatPayload> OnMessage;

    public async Task ConnectAsync(Guid characterId)
    {
        var url = $"{_serverUrl.TrimEnd('/')}{_hubPath}?characterId={characterId}";
        _conn = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        _conn.On<ChatPayload>("ChatMessage", p => OnMessage?.Invoke(p));

        await _conn.StartAsync();
    }

    public Task SendAsync(Guid recipientCharacterId, string content)
        => _conn.InvokeAsync("SendMessage", recipientCharacterId, content);
}
```

## 6. Character loader — swap por clase + género

Archivo: `Assets/WAR/Scripts/Gameplay/CharacterLoader.cs`

Usa Addressables para cargar el prefab correcto según `ClassName` + `Gender`.

```csharp
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using WAR.Network;

public sealed class CharacterLoader : MonoBehaviour
{
    // Address key: "Char_{ClassName}_{Gender}" → e.g. "Char_Sorcerer_M"
    public AsyncOperationHandle<GameObject> LoadForPlayer(string className, CharacterGender gender, Vector3 pos)
    {
        var key = $"Char_{className}_{(gender == CharacterGender.Male ? "M" : "F")}";
        var handle = Addressables.InstantiateAsync(key, pos, Quaternion.identity);
        return handle;
    }
}
```

Marcar cada prefab `Prefab_Char_Sorcerer_M.prefab` etc. con el Addressables Group correspondiente y asignar la address key igual al nombre.

## 7. Scene minimal: Test_World_01

Hierarchy:
```
Scene: Test_World_01
├── _Managers
│   ├── GameClient (GameObject + GameClient script)
│   ├── ChatClient
│   └── CharacterLoader
├── _World
│   ├── Terrain_50x50
│   └── Props (árboles, rocas, fuente de meditación)
├── _UI
│   ├── LoginCanvas
│   │   ├── NameInputField
│   │   ├── ClassDropdown (Sorcerer/Juramentada/Lancero/Bruiser)
│   │   ├── GenderToggle (Male/Female)
│   │   └── JoinButton
│   └── HUDCanvas
│       ├── HpBar
│       ├── ManaBar
│       ├── WalletPanel
│       └── SkillBar (13 slots)
├── _Camera
│   └── MainCamera (3rd person follow)
└── _Lighting
    └── Directional Light + Sky
```

## 8. Event flow (de alto nivel)

1. `LoginScene.OnClickJoin(name, "Sorcerer", Female)` → `GameClient.ConnectAsync()` → `GameClient.JoinGameAsync()`.
2. Servidor responde con `JoinWorldResultDto` → cliente instancia `CharacterLoader.LoadForPlayer("Sorcerer", Female)` en `(X, Y)`.
3. Servidor emite `PlayerJoined` a otros → cada cliente carga el prefab correspondiente para el nuevo jugador.
4. Usuario pulsa `W` → `GameClient.MoveToAsync(newX, newY)`.
5. Servidor valida + actualiza posición + emite `MoveResult` al mover + `PlayerMoved` a nearby.
6. Usuario pulsa `1` (básico) → `GameClient.BasicAttackAsync(targetId)`.
7. Servidor valida pipeline 7-fase + resuelve combate + emite `CombatResult` al atacante y objetivo + `PlayerStateUpdate` para ambos.
8. Cliente reproduce animación de ataque, VFX y damage number basado en `CombatResult.Outcome` + `DamageDealt`.
9. Si `Outcome == "InsufficientResources"` o `"Blocked"`, mostrar tooltip; no se reproduce animación de hit.

## 9. Reglas de oro (server-authoritative)

- **NUNCA** escribir `player.CurrentHp -= damage` en el cliente. Espera `PlayerStateUpdate`.
- **NUNCA** decidir si un skill está en cooldown localmente — pregunta via `GetMyState` o usa `SkillSlotDto.IsOnCooldown` del último `PlayerStateUpdate`.
- **NUNCA** mostrar wallet actualizado desde el cliente — espera `WalletUpdate`.
- **NUNCA** predecir hit/miss — espera `CombatResult.Outcome`.
- Movimiento: cliente puede predecir visualmente (smooth interp) pero la posición autoritativa viene de `MoveResult` / `PlayerMoved`. Si el server corrige, aplica snap + lerp.

## 10. Troubleshooting

| Síntoma | Causa probable | Fix |
|---|---|---|
| `HubException: 'Sorcerer' is not a valid class name` | `className` no-case-insensitive parse — probablemente whitespace | Trim + capitaliza |
| DTOs llegan con campos null | Mismatch de casing — SignalR serializa camelCase por default | Asegúrate que los campos públicos C# usen PascalCase y Newtonsoft respete policies |
| `InvalidOperationException: not connected` | Intentando invokear antes de `ConnectAsync()` completarse | Await la promesa de Connect antes de invocar |
| Movimiento no se aplica | Rate limit (200ms entre moves) | Lanza moves con debounce client-side |
| `CombatResult` no llega al objetivo | El target se desconectó entre invoke y emit | Manejar silenciosamente |
