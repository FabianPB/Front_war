using War.Core.Characters;
using War.Core.Equipment;
using War.Core.Skills;

namespace War.Api.Application.GameWorld;

// ── Core models for the online multiplayer world (all in-memory, no DB persistence) ──

public sealed class OnlinePlayer
{
    public required string ConnectionId { get; set; }
    public required string PlayerId { get; set; }       // GUID generated on connect
    public required string DisplayName { get; set; }
    public required string ClassName { get; set; }       // "Sorcerer" | "Juramentada" | "Lancero" | "Bruiser"
    public required ClassType ClassType { get; set; }
    public required CharacterGender Gender { get; set; }
    public int Level { get; set; } = 30;
    public int AscensionLevel { get; set; } = 5;

    // ── World position (0–99 grid) ──
    public float X { get; set; }
    public float Y { get; set; }

    // ── Resources ──
    public decimal CurrentHp { get; set; }
    public decimal MaxHp { get; set; }
    public decimal CurrentMana { get; set; }
    public decimal MaxMana { get; set; }

    // ── Stats from CharacterFinalStatsBuilder ──
    public Dictionary<string, decimal> Stats { get; set; } = new();

    // ── Skills loaded from catalog ──
    public List<OnlinePlayerSkill> Skills { get; set; } = new();

    // ── Cooldowns: skillId → remaining seconds ──
    public Dictionary<string, float> Cooldowns { get; set; } = new();

    // ── Active conditions/buffs ──
    public List<OnlinePlayerCondition> Conditions { get; set; } = new();

    // ── Timestamps ──
    public DateTime LastMoveTime { get; set; }
    public DateTime ConnectedAt { get; set; }

    // ── Combat timing (server-authoritative) ──
    public DateTime LastBasicAttackTime { get; set; }
    public DateTime LastSkillUseTime { get; set; }
    public int ComboStep { get; set; }               // 0-5 (combo stages 1-6)

    // ── Cast lock (secreto del sistema, nunca se broadcast) ──
    // Mientras now < CastingUntil, el jugador no puede iniciar otra acción
    // de combate. Cada skill y ataque básico define su cast time internamente.
    public DateTime CastingUntil { get; set; }

    // ── Anti-spam rate limiting ──
    public Queue<DateTime> CombatActionLog { get; set; } = new();
    public DateTime LockoutUntil { get; set; }
    public int LockoutCount { get; set; }
    public DateTime LockoutWindowStart { get; set; }

    // ── Group membership (null = solo) ──
    public string? GroupId { get; set; }

    // ── Combat lock (per-player critical section) ──
    // Usado para serializar mutaciones concurrentes entre GameHub (thread de
    // conexión SignalR) y WorldTickService (background thread) sobre los
    // mismos Cooldowns/Conditions/CurrentHp/CurrentMana.
    public object CombatLock { get; } = new();

    // ── Inventario (fuente de verdad de los objetos del jugador) ──
    // Arranca en 80 casillas, expandible hasta 280 en lotes de 50 con recursos del juego.
    // Los equipos "equipados" son ítems marcados dentro de este mismo inventario.
    public PlayerInventory Inventory { get; } = new();

    // ── Ascensión por skill (0..10 por cada SkillId) ──
    // El nivel global del personaje (AscensionLevel) es legado; el nivel REAL por cada habilidad
    // vive aquí. Al conectarse, el GameWorldService lo inicializa leyendo del catálogo programado.
    public Dictionary<string, int> SkillAscension { get; } = new();
}

public sealed record OnlinePlayerSkill(
    string SkillId,
    string Name,
    decimal ManaCost,
    decimal BaseCooldownSeconds,
    string DamageType);

public sealed record OnlinePlayerCondition(
    string ConditionType,
    string Category,           // "State" | "CrowdControl"
    float RemainingSeconds,
    DateTime AppliedAt);

// ── DTOs sent over SignalR ──

/// Lightweight DTO broadcast to other players (no private data).
public sealed record PlayerPresenceDto(
    string PlayerId,
    string DisplayName,
    string ClassName,
    CharacterGender Gender,
    int Level,
    float X,
    float Y,
    decimal CurrentHp,
    decimal MaxHp,
    bool IsDefeated);

/// Full state sent only to the owning player.
public sealed record PlayerFullStateDto(
    string PlayerId,
    string DisplayName,
    string ClassName,
    CharacterGender Gender,
    int Level,
    int AscensionLevel,
    float X,
    float Y,
    decimal CurrentHp,
    decimal MaxHp,
    decimal CurrentMana,
    decimal MaxMana,
    IReadOnlyList<SkillSlotDto> Skills,
    IReadOnlyList<ConditionDto> Conditions);

public sealed record SkillSlotDto(
    string SkillId,
    string Name,
    decimal ManaCost,
    decimal BaseCooldownSeconds,
    float RemainingCooldown,
    bool IsOnCooldown,
    string DamageType);

public sealed record ConditionDto(
    string ConditionType,
    string Category,
    float RemainingSeconds);

/// Snapshot of all players for the minimap.
public sealed record WorldSnapshotDto(
    int PlayerCount,
    IReadOnlyList<PlayerPresenceDto> Players);

/// Movement result sent back to the mover.
public sealed record MoveResultDto(
    float X,
    float Y,
    IReadOnlyList<PlayerPresenceDto> NearbyPlayers);

/// Join result with full player state.
public sealed record JoinWorldResultDto(
    PlayerFullStateDto Player,
    WorldSnapshotDto WorldSnapshot);

// ── Wallet / Inventario DTOs ─────────────────────────────────────────────────

/// DTO de wallet para broadcast al cliente (saldos + cap de energía + cap de capacidad).
public sealed record WalletDto(
    long Copper,
    long Silver,
    long Gold,
    int Energy,
    int EnergyMax);

/// DTO de transacción monetaria para el cliente / audit UI.
public sealed record WalletTransactionDto(
    string Id,
    DateTime Timestamp,
    string Currency,
    string Direction,          // "Credit" | "Debit"
    long Amount,
    string Source,             // TransactionSource
    string Description,
    long BalanceBefore,
    long BalanceAfter);

/// DTO de un ítem del inventario.
public sealed record InventoryItemDto(
    string ItemId,
    string ItemType,
    string DefinitionId,
    int Quantity,
    int SlotIndex,
    int Tier,
    int DevelopmentLevel,
    bool IsEquipped,
    string? EquippedSlot);

/// DTO agregado del inventario del jugador (listado + capacidad + expansiones).
public sealed record InventoryDto(
    int Capacity,
    int MaxCapacity,
    int UsedSlots,
    int FreeSlots,
    int ExpansionsPurchased,
    int MaxExpansions,
    IReadOnlyList<InventoryItemDto> Items);

/// DTO de coste agregado para UI (crafteo/expansión/ascensión).
public sealed record CurrencyCostDto(
    long Copper,
    long Silver,
    long Gold,
    long Energy);

// ── Capilla de Economía ─────────────────────────────────────────────────────

public sealed record ChapelStateDto(
    int Level,
    int MaxLevel,
    int? CharacterLevelRequiredForNext,
    CurrencyCostDto PossessionCaps,
    long SilverConvDaily,
    long SilverConvWeekly,
    long SilverConvMonthly,
    long GoldConvDaily,
    long GoldConvWeekly,
    long GoldConvMonthly);

public sealed record ConversionQuotasDto(
    long SilverUsedToday, long SilverLimitDaily,
    long SilverUsedWeek,  long SilverLimitWeekly,
    long SilverUsedMonth, long SilverLimitMonthly,
    long GoldUsedToday,   long GoldLimitDaily,
    long GoldUsedWeek,    long GoldLimitWeekly,
    long GoldUsedMonth,   long GoldLimitMonthly);

// ── Ascensión de habilidades ────────────────────────────────────────────────

public sealed record SkillAscensionPreviewDto(
    string SkillId,
    int CurrentLevel,
    bool IsMaxed,
    string? NextStepBookDefinitionId,
    int NextStepBookCount,
    string? NextStepBookRarity,
    CurrencyCostDto? NextStepCost);
