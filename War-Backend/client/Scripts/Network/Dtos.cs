// ═══════════════════════════════════════════════════════════════════════════════
// WAR · Unity client DTOs
// ─────────────────────────────────────────────────────────────────────────────
// Mirrors the server records in War.Api/Application/GameWorld/GameWorldModels.cs.
// SignalR serializes with camelCase on the wire, but Newtonsoft.Json with the
// default settings binds PascalCase fields correctly to camelCase JSON.
// Keep this file in sync with the server — if a field is added/removed/renamed,
// update both. DO NOT mutate these DTOs in gameplay code; treat as read-only
// snapshots from the server.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;

namespace WAR.Network
{
    // ─── Identity ────────────────────────────────────────────────────────────
    public enum CharacterGender
    {
        Male = 0,
        Female = 1,
    }

    // ─── World presence / state ──────────────────────────────────────────────

    /// <summary>Lightweight DTO broadcast to all nearby players.</summary>
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

    /// <summary>Full state sent only to the owning player.</summary>
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
        public string DamageType;       // "Physical" | "Magic" | "True"
    }

    [Serializable]
    public class ConditionDto
    {
        public string ConditionType;    // "Heat" | "Cold" | "Poison" | "Bleed" | ...
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

    // ─── Server-to-client event payloads ─────────────────────────────────────

    /// <summary>Emitted when another player leaves the world.</summary>
    [Serializable]
    public class PlayerLeftPayload
    {
        public string PlayerId;
        public string DisplayName;
    }

    /// <summary>Emitted when another player moves (broadcast to nearby).</summary>
    [Serializable]
    public class PlayerMovedPayload
    {
        public string PlayerId;
        public float X;
        public float Y;
    }

    // ─── Combat ──────────────────────────────────────────────────────────────

    [Serializable]
    public class OnlineCombatResult
    {
        public string ActorPlayerId;
        public string TargetPlayerId;
        public string ActionType;       // "BasicAttack" | "Skill"
        public string Outcome;          // "Hit" | "Miss" | "Blocked" | "InsufficientResources" | "OutOfRange"
        public string BlockedReason;    // nullable
        public DateTime Timestamp;
        public decimal DamageDealt;
        public string SkillId;          // nullable, present when ActionType == "Skill"
        public int BasicComboStage;     // 1-6, relevant when ActionType == "BasicAttack"
        public bool IsCritical;
    }

    // ─── Wallet / inventory / chapel ─────────────────────────────────────────

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
    public class WalletTransactionDto
    {
        public string Id;
        public DateTime Timestamp;
        public string Currency;         // "Copper" | "Silver" | "Gold" | "Energy"
        public string Direction;        // "Credit" | "Debit"
        public long Amount;
        public string Source;           // TransactionSource enum name
        public string Description;
        public long BalanceBefore;
        public long BalanceAfter;
    }

    [Serializable]
    public class InventoryItemDto
    {
        public string ItemId;           // unique per-instance id
        public string ItemType;         // "Equipment" | "Material" | "Book" | "Consumable"
        public string DefinitionId;     // e.g. "weapon.sorcerer.common.offensive"
        public int Quantity;
        public int SlotIndex;
        public int Tier;                // 1..4
        public int DevelopmentLevel;    // 1..30
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
    public class CurrencyCostDto
    {
        public long Copper;
        public long Silver;
        public long Gold;
        public long Energy;
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
    public class ConversionQuotasDto
    {
        public long SilverUsedToday, SilverLimitDaily;
        public long SilverUsedWeek, SilverLimitWeekly;
        public long SilverUsedMonth, SilverLimitMonthly;
        public long GoldUsedToday, GoldLimitDaily;
        public long GoldUsedWeek, GoldLimitWeekly;
        public long GoldUsedMonth, GoldLimitMonthly;
    }

    // ─── Skill ascension ─────────────────────────────────────────────────────

    [Serializable]
    public class SkillAscensionPreviewDto
    {
        public string SkillId;
        public int CurrentLevel;
        public bool IsMaxed;
        public string NextStepBookDefinitionId; // nullable
        public int NextStepBookCount;
        public string NextStepBookRarity;       // nullable "Common" | "Special" | "Epic" | "Legendary"
        public CurrencyCostDto NextStepCost;    // nullable
    }

    // ─── Social / group ──────────────────────────────────────────────────────

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
        public string RecipientId;      // nullable for local-broadcast
        public string Content;
        public DateTime Timestamp;
    }

    [Serializable]
    public class SocialNotification
    {
        public string Kind;             // "FriendRequestReceived" | "FriendRequestAccepted" | "Blocked" | ...
        public string SourceId;
        public string SourceName;
        public DateTime Timestamp;
    }
}
