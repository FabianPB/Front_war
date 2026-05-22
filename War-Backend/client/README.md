# WAR · Unity Client (files ready to copy)

This folder contains the Unity scripts that compose the client for the WAR backend. They are NOT a full Unity project — copy them into your Unity project under `Assets/WAR/Scripts/` preserving the folder layout.

```
client/Scripts/
├── Network/
│   ├── Dtos.cs           → mirrors server records exactly
│   ├── GameClient.cs     → /game SignalR hub (world, combat, economy, social)
│   └── ChatClient.cs     → /chat SignalR hub (proximity chat, ?characterId=...)
└── Gameplay/
    ├── CharacterLoader.cs → Addressables loader per class + gender
    └── PlayerController.cs → input forwarder (server-authoritative)
```

## Quick install

1. Unity 2022.3 LTS (or newer) with URP.
2. Window → Package Manager → install:
   - `com.unity.render-pipelines.universal` (URP)
   - `com.unity.inputsystem`
   - `com.unity.addressables`
   - `com.unity.nuget.newtonsoft-json`
3. Install [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity), then via its UI add:
   - `Microsoft.AspNetCore.SignalR.Client` (8.x)
4. Copy `client/Scripts/` → `<your-unity-project>/Assets/WAR/Scripts/`.
5. In your scene, create a GameObject `_Managers/GameClient`, attach `GameClient.cs`, set the server URL (default `http://localhost:5050`).
6. Create `_Managers/CharacterLoader`, attach `CharacterLoader.cs`.
7. Create a Character prefab per class+gender (`Prefab_Char_Sorcerer_M`, `Prefab_Char_Sorcerer_F`, …). Mark them as Addressable with address key `Char_Sorcerer_M`, `Char_Sorcerer_F`, etc.
8. Attach `PlayerController.cs` to a persistent scene object that holds `_client` + `_loader` references.

## Server contract assumptions

These scripts assume the backend contract on branch `Raizon` (post 2026-04-18 gender addition):
- `JoinGame(displayName, className, level?, ascensionLevel?, gender?)` — gender is at the END for backward-compat.
- Events emitted: `PlayerJoined`, `PlayerLeft`, `PlayerMoved`, `MoveResult`, `CombatResult`, `PlayerStateUpdate`, `TargetStateUpdate`, `WalletUpdate`, `InventoryUpdate`, `ItemUpdated`, `ChapelUpdate`, `GroupUpdated`, `ChatMessage`, `SocialNotification`, `Error`, `SystemMessage`.
- `ChatHub` expects `?characterId=<guid>` query param on connection.

If the backend adds fields to DTOs, update `Dtos.cs` in lock-step.

## Rule of gold

**Server-authoritative.** Never decide HP, damage, wallet balance, inventory, skill cooldowns, or combat outcomes locally. The client only:
1. Forwards input (`JoinGame`, `MoveTo`, `BasicAttack`, `UseSkill`, …).
2. Listens for server events and re-renders.

If you feel tempted to write `player.CurrentHp -= damage`, stop. Wait for `PlayerStateUpdate`.
