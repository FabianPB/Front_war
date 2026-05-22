using System.Collections.Concurrent;
using War.Core.Characters;
using War.Core.Progression;
using War.Core.Skills;
using War.Core.Skills.Catalogs;
using War.Core.Stats;

namespace War.Api.Application.GameWorld;

// Decision: Singleton — all world state lives in RAM for the demo.
// No database persistence. When the server restarts, the world is empty.
public sealed class GameWorldService
{
    // ── World constants ──
    public const int WorldMinCoord = 0;
    public const int WorldMaxCoord = 99;
    public const float DefaultVisibilityRadius = 15f;
    public const float NearbyDiscoveryRadius = 25f;
    public const int DefaultLevel = 30;
    public const int DefaultAscensionLevel = 5;
    private const int MoveRateLimitMs = 200;

    // ── State ──
    private readonly ConcurrentDictionary<string, OnlinePlayer> _playersByConnectionId = new();
    private readonly ConcurrentDictionary<string, OnlinePlayer> _playersByPlayerId = new();
    private readonly Random _rng = new();

    // ── Dependencies (resolved once at construction via DI) ──
    private readonly ICharacterFinalStatsBuilder _statsBuilder;

    public GameWorldService(ICharacterFinalStatsBuilder statsBuilder)
    {
        _statsBuilder = statsBuilder;
    }

    // ──────────────────────────────────────────────────────────────
    // JOIN / LEAVE
    // ──────────────────────────────────────────────────────────────

    public OnlinePlayer JoinWorld(string connectionId, string displayName, string className, int? level = null, int? ascensionLevel = null, string? gender = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(className);

        // Decision: Sanitize display name (max 20 chars, strip control chars)
        displayName = SanitizeDisplayName(displayName);

        var classType = ParseClassType(className);
        // Decision: Gender parsing is tolerant — never throws, defaults to Male for unknown/null.
        // The client is trusted to send "Male"/"Female" (or Spanish aliases); server validates + stores.
        var genderValue = CharacterGenderParser.ParseOrDefault(gender);
        var playerLevel = Math.Clamp(level ?? DefaultLevel, 1, 50);
        var playerAscension = Math.Clamp(ascensionLevel ?? DefaultAscensionLevel, 1, 10);

        var playerId = Guid.NewGuid().ToString();

        // Build real stats using the existing CharacterFinalStatsBuilder
        var finalStats = _statsBuilder.Build(classType, playerLevel);

        var maxHp = finalStats[StatType.MaxHp];
        var maxMana = finalStats[StatType.MaxMana];

        // Build stats dictionary for the player
        var statsDict = new Dictionary<string, decimal>();
        foreach (var kvp in finalStats.GetAll())
        {
            statsDict[kvp.Key.ToString()] = kvp.Value;
        }

        // Load skills from the catalog
        var skills = LoadSkillsForClass(classType);

        // Random spawn position
        var spawnX = (float)(_rng.NextDouble() * (WorldMaxCoord - 10) + 5);  // 5–94 to avoid edges
        var spawnY = (float)(_rng.NextDouble() * (WorldMaxCoord - 10) + 5);

        var player = new OnlinePlayer
        {
            ConnectionId = connectionId,
            PlayerId = playerId,
            DisplayName = displayName,
            ClassName = className,
            ClassType = classType,
            Gender = genderValue,
            Level = playerLevel,
            AscensionLevel = playerAscension,
            X = spawnX,
            Y = spawnY,
            CurrentHp = maxHp,
            MaxHp = maxHp,
            CurrentMana = maxMana,
            MaxMana = maxMana,
            Stats = statsDict,
            Skills = skills,
            Cooldowns = new Dictionary<string, float>(),
            Conditions = new List<OnlinePlayerCondition>(),
            LastMoveTime = DateTime.MinValue,
            ConnectedAt = DateTime.UtcNow
        };

        _playersByConnectionId[connectionId] = player;
        _playersByPlayerId[playerId] = player;

        return player;
    }

    public OnlinePlayer? LeaveWorld(string connectionId)
    {
        if (_playersByConnectionId.TryRemove(connectionId, out var player))
        {
            _playersByPlayerId.TryRemove(player.PlayerId, out _);
            return player;
        }
        return null;
    }

    // ──────────────────────────────────────────────────────────────
    // MOVEMENT
    // ──────────────────────────────────────────────────────────────

    /// Returns the player after movement, or null if rate-limited/not found.
    public OnlinePlayer? MovePlayer(string connectionId, string direction)
    {
        if (!_playersByConnectionId.TryGetValue(connectionId, out var player))
            return null;

        // Rate limit: max 1 move per 200ms
        var now = DateTime.UtcNow;
        if ((now - player.LastMoveTime).TotalMilliseconds < MoveRateLimitMs)
            return null; // Silently drop — client sends every 250ms, this gives margin

        float dx = 0, dy = 0;
        switch (direction.ToLowerInvariant())
        {
            case "up":    dy = -1; break;
            case "down":  dy = 1;  break;
            case "left":  dx = -1; break;
            case "right": dx = 1;  break;
            default: return null; // Invalid direction
        }

        // Clamp to world boundaries
        player.X = Math.Clamp(player.X + dx, WorldMinCoord, WorldMaxCoord);
        player.Y = Math.Clamp(player.Y + dy, WorldMinCoord, WorldMaxCoord);
        player.LastMoveTime = now;

        return player;
    }

    /// Move to exact coordinates (for click-to-move or teleport)
    public OnlinePlayer? MovePlayerTo(string connectionId, float x, float y)
    {
        if (!_playersByConnectionId.TryGetValue(connectionId, out var player))
            return null;

        var now = DateTime.UtcNow;
        if ((now - player.LastMoveTime).TotalMilliseconds < MoveRateLimitMs)
            return null;

        player.X = Math.Clamp(x, WorldMinCoord, WorldMaxCoord);
        player.Y = Math.Clamp(y, WorldMinCoord, WorldMaxCoord);
        player.LastMoveTime = now;

        return player;
    }

    // ──────────────────────────────────────────────────────────────
    // QUERIES
    // ──────────────────────────────────────────────────────────────

    public IReadOnlyList<OnlinePlayer> GetNearbyPlayers(string connectionId, float radius = DefaultVisibilityRadius)
    {
        if (!_playersByConnectionId.TryGetValue(connectionId, out var self))
            return Array.Empty<OnlinePlayer>();

        var radiusSq = radius * radius;
        return _playersByConnectionId.Values
            .Where(p => p.ConnectionId != connectionId && DistanceSquared(self, p) <= radiusSq)
            .ToArray();
    }

    public IReadOnlyList<OnlinePlayer> GetNearbyPlayersOf(OnlinePlayer self, float radius = DefaultVisibilityRadius)
    {
        var radiusSq = radius * radius;
        return _playersByConnectionId.Values
            .Where(p => p.ConnectionId != self.ConnectionId && DistanceSquared(self, p) <= radiusSq)
            .ToArray();
    }

    public WorldSnapshotDto GetWorldSnapshot()
    {
        var players = _playersByConnectionId.Values
            .Select(ToPresenceDto)
            .ToArray();

        return new WorldSnapshotDto(players.Length, Array.AsReadOnly(players));
    }

    public OnlinePlayer? GetPlayerByConnectionId(string connectionId)
    {
        return _playersByConnectionId.TryGetValue(connectionId, out var player) ? player : null;
    }

    public OnlinePlayer? GetPlayerByPlayerId(string playerId)
    {
        return _playersByPlayerId.TryGetValue(playerId, out var player) ? player : null;
    }

    public int PlayerCount => _playersByConnectionId.Count;

    public IEnumerable<OnlinePlayer> AllPlayers => _playersByConnectionId.Values;

    /// Lookup player by Guid (social system uses Guid-based IDs).
    /// PlayerId is stored as string GUID, so we parse and compare.
    public OnlinePlayer? GetPlayerByGuid(Guid id)
    {
        return GetPlayerByPlayerId(id.ToString());
    }

    // ──────────────────────────────────────────────────────────────
    // COMBAT SUPPORT
    // ──────────────────────────────────────────────────────────────

    /// Validate that attacker and target are in combat range (20 units).
    public const float CombatRange = 20f;

    public bool AreInRange(OnlinePlayer a, OnlinePlayer b, float range = CombatRange)
    {
        return DistanceSquared(a, b) <= range * range;
    }

    /// Get the effective basic attack range for a class.
    /// Uses the catalog value directly so each class is bounded to its archetype:
    ///   Sorcerer 14m (mage), Lancero 4m (polearm), Juramentada 3m (paladin melee), Bruiser 2.5m (heavy melee).
    /// A small server-side tolerance (+0.5m) covers minor latency/desync so legitimate
    /// edge-of-range strikes are not rejected, without making melee feel ranged.
    public float GetBasicAttackRange(OnlinePlayer player)
    {
        var catalog = War.Core.Combat.ClassBasicAttackCatalog.Default;
        var definition = catalog.GetRequired(player.ClassType);
        var rangeMeters = definition.RangeMeters ?? 3m;
        return (float)rangeMeters + LatencyRangeToleranceMeters;
    }

    /// Get effective skill range using the catalog as the single source of truth.
    /// For skills with BaseRangeUnits == 0 (self-centered AoE, Self pattern,
    /// or design-pending), we fall back to the player's basic attack range —
    /// NOT to CombatRange — so melee classes cannot use a "self-centered" skill
    /// as if it had 20m reach.
    public float GetSkillRange(OnlinePlayerSkill skill, OnlinePlayer? caster = null)
    {
        var skillDef = SkillCatalogRegistry.Current.ClassCatalogs
            .SelectMany(c => c.Skills)
            .FirstOrDefault(s => string.Equals(s.Id, skill.SkillId, StringComparison.OrdinalIgnoreCase));

        if (skillDef is null)
        {
            // Unknown skill: be strict. Use caster's basic attack range if available.
            return caster is not null
                ? GetBasicAttackRange(caster)
                : MeleeFallbackRangeMeters + LatencyRangeToleranceMeters;
        }

        var targeting = skillDef.BaseTuning.Targeting;
        var range = targeting.BaseRangeUnits;
        if (range > 0)
        {
            return (float)range + LatencyRangeToleranceMeters;
        }

        // Range == 0: self-centered AoE, Self pattern, Area pattern centered on caster,
        // or design-pending skill. For self/area patterns the engagement happens around
        // the caster, so the gate is "is the target inside the AoE radius?" not
        // "is the target inside cast range?". The radius is the right gate.
        if (targeting.AreaRadiusUnits is decimal radius && radius > 0)
        {
            return (float)radius + LatencyRangeToleranceMeters;
        }

        // True self-only or pending: fall back to the caster's basic attack range
        // so melee classes stay melee. Never the world-wide 20m default.
        return caster is not null
            ? GetBasicAttackRange(caster)
            : MeleeFallbackRangeMeters + LatencyRangeToleranceMeters;
    }

    /// Server-side range tolerance to absorb network jitter (~50-150ms typical).
    /// Player moves at most ~1m/100ms, so +0.5m covers a frame of desync without
    /// making melee feel ranged.
    public const float LatencyRangeToleranceMeters = 0.5f;

    /// Conservative fallback for unknown/pending skills when no caster is available.
    /// Aligns with the most restrictive class (Bruiser 2.5m).
    public const float MeleeFallbackRangeMeters = 2.5f;

    /// Find the nearest other player to the given player.
    public OnlinePlayer? FindNearestPlayer(OnlinePlayer self)
    {
        OnlinePlayer? nearest = null;
        float nearestDistSq = float.MaxValue;

        foreach (var p in _playersByConnectionId.Values)
        {
            if (p.ConnectionId == self.ConnectionId) continue;
            if (p.CurrentHp <= 0) continue; // Skip defeated
            var distSq = DistanceSquared(self, p);
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = p;
            }
        }
        return nearest;
    }

    /// Remove players idle for more than the specified duration.
    public IReadOnlyList<OnlinePlayer> RemoveInactivePlayers(TimeSpan maxIdleTime)
    {
        var now = DateTime.UtcNow;
        var removed = new List<OnlinePlayer>();

        foreach (var player in _playersByConnectionId.Values.ToArray())
        {
            var lastActivity = player.LastMoveTime > player.ConnectedAt
                ? player.LastMoveTime
                : player.ConnectedAt;

            if ((now - lastActivity) > maxIdleTime)
            {
                if (_playersByConnectionId.TryRemove(player.ConnectionId, out _))
                {
                    _playersByPlayerId.TryRemove(player.PlayerId, out _);
                    removed.Add(player);
                }
            }
        }

        return removed;
    }

    // ──────────────────────────────────────────────────────────────
    // DTO BUILDERS
    // ──────────────────────────────────────────────────────────────

    public static PlayerPresenceDto ToPresenceDto(OnlinePlayer player)
    {
        return new PlayerPresenceDto(
            PlayerId: player.PlayerId,
            DisplayName: player.DisplayName,
            ClassName: player.ClassName,
            Gender: player.Gender,
            Level: player.Level,
            X: player.X,
            Y: player.Y,
            CurrentHp: player.CurrentHp,
            MaxHp: player.MaxHp,
            IsDefeated: player.CurrentHp <= 0);
    }

    public static PlayerFullStateDto ToFullStateDto(OnlinePlayer player)
    {
        return new PlayerFullStateDto(
            PlayerId: player.PlayerId,
            DisplayName: player.DisplayName,
            ClassName: player.ClassName,
            Gender: player.Gender,
            Level: player.Level,
            AscensionLevel: player.AscensionLevel,
            X: player.X,
            Y: player.Y,
            CurrentHp: player.CurrentHp,
            MaxHp: player.MaxHp,
            CurrentMana: player.CurrentMana,
            MaxMana: player.MaxMana,
            Skills: player.Skills.Select(s => new SkillSlotDto(
                SkillId: s.SkillId,
                Name: s.Name,
                ManaCost: s.ManaCost,
                BaseCooldownSeconds: s.BaseCooldownSeconds,
                RemainingCooldown: player.Cooldowns.TryGetValue(s.SkillId, out var cd) ? cd : 0f,
                IsOnCooldown: player.Cooldowns.TryGetValue(s.SkillId, out var cd2) && cd2 > 0,
                DamageType: s.DamageType)).ToArray(),
            Conditions: player.Conditions.Select(c => new ConditionDto(
                ConditionType: c.ConditionType,
                Category: c.Category,
                RemainingSeconds: c.RemainingSeconds)).ToArray());
    }

    public static MoveResultDto ToMoveResultDto(OnlinePlayer player, IReadOnlyList<OnlinePlayer> nearby)
    {
        return new MoveResultDto(
            X: player.X,
            Y: player.Y,
            NearbyPlayers: nearby.Select(ToPresenceDto).ToArray());
    }

    // ──────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ──────────────────────────────────────────────────────────────

    /// Move player toward target until within the specified range.
    /// Returns true if movement occurred, false if already in range or can't move.
    public bool MoveToward(OnlinePlayer attacker, OnlinePlayer target, float desiredRange)
    {
        var dx = target.X - attacker.X;
        var dy = target.Y - attacker.Y;
        var dist = (float)Math.Sqrt(dx * dx + dy * dy);

        if (dist <= desiredRange) return false; // Already in range

        // Normalize direction and move 1 unit toward target
        var nx = dx / dist;
        var ny = dy / dist;
        attacker.X = Math.Clamp(attacker.X + nx, WorldMinCoord, WorldMaxCoord);
        attacker.Y = Math.Clamp(attacker.Y + ny, WorldMinCoord, WorldMaxCoord);
        attacker.LastMoveTime = DateTime.UtcNow;
        return true;
    }

    /// Detection range multiplier — how far a player can "see" a target to auto-approach.
    public const float DetectionRangeMultiplier = 2.5f;

    private static float DistanceSquared(OnlinePlayer a, OnlinePlayer b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static ClassType ParseClassType(string className)
    {
        return className.ToLowerInvariant() switch
        {
            "sorcerer" => ClassType.Sorcerer,
            "juramentada" => ClassType.Juramentada,
            "lancero" => ClassType.Lancero,
            "bruiser" => ClassType.Bruiser,
            // Decision: Also accept "warrior" as alias for Bruiser (user spec uses both)
            "warrior" => ClassType.Bruiser,
            _ => throw new ArgumentException($"Clase no válida: '{className}'. Opciones: Sorcerer, Juramentada, Lancero, Bruiser.")
        };
    }

    private static string SanitizeDisplayName(string name)
    {
        // Strip control characters, trim, limit to 20 chars
        var sanitized = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (sanitized.Length > 20) sanitized = sanitized[..20];
        if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "Jugador";
        return sanitized;
    }

    // Decision: Load skill metadata from the existing catalog registry.
    // We read the skill definitions that are programmed in the catalogs (not from DB).
    // This gives each player all 13 skills for their class with real mana costs and cooldowns.
    private static List<OnlinePlayerSkill> LoadSkillsForClass(ClassType classType)
    {
        var classCatalog = SkillCatalogRegistry.Current.ClassCatalogs
            .FirstOrDefault(c => c.ClassType == classType);

        if (classCatalog is null)
            return new List<OnlinePlayerSkill>();

        var skills = new List<OnlinePlayerSkill>();
        foreach (var definition in classCatalog.Skills)
        {
            var manaCost = 0m;
            if (definition.BaseTuning.ResourceCosts is { } costs)
            {
                var manaCostDef = costs.FirstOrDefault(c => c.ResourceType == War.Core.Resources.CharacterResourceType.Mana);
                if (manaCostDef is not null)
                    manaCost = manaCostDef.Amount;
            }

            var damageType = definition.BaseTuning.Action.DamageType?.ToString() ?? "None";

            skills.Add(new OnlinePlayerSkill(
                SkillId: definition.Id,
                Name: definition.Name,
                ManaCost: manaCost,
                BaseCooldownSeconds: definition.BaseTuning.Cadence.BaseCooldownSeconds,
                DamageType: damageType));
        }

        return skills;
    }
}
