using Microsoft.AspNetCore.SignalR;
using War.Api.Application.Economy;
using War.Api.Application.GameWorld;
using War.Api.Application.Skills;
using War.Api.Application.Social;
using War.Core.Economy;
using War.Core.Equipment;
using War.Core.Skills;
using War.Core.Skills.Ascension;
using War.Core.Skills.Books;
using War.Core.Skills.Catalogs;

namespace War.Api.Hubs;

// Decision: Separate hub from ChatHub — different lifecycle and concerns.
// GameHub handles world presence, movement, combat, social, and multiplayer state.
public sealed class GameHub : Hub
{
    private readonly GameWorldService _worldService;
    private readonly OnlineCombatService _combatService;
    private readonly ISocialRelationshipService _socialService;
    private readonly GroupService _groupService;
    private readonly PlayerWalletService _wallets;
    private readonly PlayerChapelService _chapels;
    private readonly CurrencyConversionService _conversion;
    private readonly SkillAscensionService _ascension;
    private readonly HubActionRateLimiter _hubRateLimiter;

    public GameHub(
        GameWorldService worldService,
        OnlineCombatService combatService,
        ISocialRelationshipService socialService,
        GroupService groupService,
        PlayerWalletService wallets,
        PlayerChapelService chapels,
        CurrencyConversionService conversion,
        SkillAscensionService ascension,
        HubActionRateLimiter hubRateLimiter)
    {
        _worldService = worldService;
        _combatService = combatService;
        _socialService = socialService;
        _groupService = groupService;
        _wallets = wallets;
        _chapels = chapels;
        _conversion = conversion;
        _ascension = ascension;
        _hubRateLimiter = hubRateLimiter;
    }

    /// <summary>
    /// Generic catch-all rate limit for hub methods that are not gated by the
    /// combat pipeline or the movement cooldown. Combat and movement keep their
    /// dedicated limiters because their windows are tighter. Returns true if the
    /// action should proceed; when false, the caller has already been notified
    /// via "Error".
    /// </summary>
    private async Task<bool> EnforceHubRateLimitAsync()
    {
        if (_hubRateLimiter.TryAcquire(Context.ConnectionId, out var retryAfterMs))
        {
            return true;
        }

        await Clients.Caller.SendAsync(
            "Error",
            $"Demasiadas solicitudes. Inténtalo en {Math.Max(retryAfterMs, 100)} ms.");
        return false;
    }

    // ──────────────────────────────────────────────────────────────
    // CONNECTION LIFECYCLE
    // ──────────────────────────────────────────────────────────────

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var player = _worldService.LeaveWorld(Context.ConnectionId);
        if (player is not null)
        {
            // Cleanup social and group state
            if (Guid.TryParse(player.PlayerId, out var playerId))
                _socialService.CleanupPlayer(playerId);
            _groupService.CleanupPlayer(player);

            await Clients.Others.SendAsync("PlayerLeft", new
            {
                player.PlayerId,
                player.DisplayName
            });
        }

        // Release rate limiter state so memory does not grow per connection.
        _hubRateLimiter.Release(Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    // ──────────────────────────────────────────────────────────────
    // JOIN / LEAVE
    // ──────────────────────────────────────────────────────────────

    /// Client calls this after connecting to enter the world.
    /// <param name="displayName">Visible name (sanitized to 20 chars, no control characters).</param>
    /// <param name="className">"Sorcerer" | "Juramentada" | "Lancero" | "Bruiser" (case-insensitive).</param>
    /// <param name="level">Optional 1..50. Defaults to 30.</param>
    /// <param name="ascensionLevel">Optional 1..10. Defaults to 5.</param>
    /// <param name="gender">Optional "Male"/"Female" (English) or "Hombre"/"Mujer"/"Masculino"/"Femenino" (Spanish). Null or unrecognized → Male.</param>
    public async Task<JoinWorldResultDto> JoinGame(string displayName, string className, int? level = null, int? ascensionLevel = null, string? gender = null)
    {
        try
        {
            var player = _worldService.JoinWorld(Context.ConnectionId, displayName, className, level, ascensionLevel, gender);

            await Groups.AddToGroupAsync(Context.ConnectionId, "world");

            var presence = GameWorldService.ToPresenceDto(player);
            await Clients.Others.SendAsync("PlayerJoined", presence);

            var fullState = GameWorldService.ToFullStateDto(player);
            var worldSnapshot = _worldService.GetWorldSnapshot();

            return new JoinWorldResultDto(fullState, worldSnapshot);
        }
        catch (ArgumentException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // MOVEMENT
    // ──────────────────────────────────────────────────────────────

    /// Move in a cardinal direction: "up", "down", "left", "right"
    public async Task Move(string direction)
    {
        var player = _worldService.MovePlayer(Context.ConnectionId, direction);
        if (player is null) return; // Rate-limited or not found

        var nearby = _worldService.GetNearbyPlayersOf(player);
        await Clients.Caller.SendAsync("MoveResult", GameWorldService.ToMoveResultDto(player, nearby));
        await Clients.Others.SendAsync("PlayerMoved", new
        {
            player.PlayerId,
            player.X,
            player.Y
        });
    }

    /// Move to exact coordinates (click-to-move)
    public async Task MoveTo(float x, float y)
    {
        var player = _worldService.MovePlayerTo(Context.ConnectionId, x, y);
        if (player is null) return;

        var nearby = _worldService.GetNearbyPlayersOf(player);
        await Clients.Caller.SendAsync("MoveResult", GameWorldService.ToMoveResultDto(player, nearby));
        await Clients.Others.SendAsync("PlayerMoved", new
        {
            player.PlayerId,
            player.X,
            player.Y
        });
    }

    // ──────────────────────────────────────────────────────────────
    // COMBAT
    // ──────────────────────────────────────────────────────────────

    /// Use a skill against a target player.
    public async Task UseSkill(int skillIndex, string targetPlayerId)
    {
        var attacker = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (attacker is null) { await Clients.Caller.SendAsync("Error", "No estás en el mundo."); return; }

        if (skillIndex < 0 || skillIndex >= attacker.Skills.Count)
        { await Clients.Caller.SendAsync("Error", "Índice de habilidad inválido."); return; }

        var skill = attacker.Skills[skillIndex];

        // ── Server-authoritative validation pipeline ──
        var validation = CombatValidationPipeline.Validate(attacker, CombatValidationPipeline.CombatActionType.UseSkill, skill);
        if (!validation.Allowed)
        {
            // Devolvemos un CombatResult "Blocked" con la razón para que el HUD de Unity
            // pueda reaccionar (mostrar mensaje, efecto de input negado, etc.). El motor NO se toca.
            await Clients.Caller.SendAsync("CombatResult", new
            {
                ActorPlayerId = attacker.PlayerId,
                TargetPlayerId = targetPlayerId,
                ActionType = "Skill",
                ActionName = skill.Name,
                Outcome = "Blocked",
                BlockedReason = validation.RejectionReason ?? "Bloqueado",
                Timestamp = DateTime.UtcNow
            });
            return;
        }

        // ── Resolve targets by skill affinity/pattern ──
        // The client-sent targetPlayerId is a hint. The SERVER decides the real targets
        // based on skill.Action/Targeting. Self skills ignore the client target.
        var skillDef = LookupSkillDefinition(skill.SkillId);
        var targetResolution = ResolveSkillTargets(attacker, targetPlayerId, skillDef);

        if (targetResolution.Error is not null)
        {
            await Clients.Caller.SendAsync("Error", targetResolution.Error);
            return;
        }

        // For enemy-targeted skills we still need a single primary target for the
        // range check and auto-approach. Healing/ally skills skip the range check
        // because grupo targeting is position-independent in this iteration.
        if (targetResolution.PrimaryEnemy is OnlinePlayer primaryEnemy)
        {
            var skillRange = _worldService.GetSkillRange(skill, attacker);
            if (!_worldService.AreInRange(attacker, primaryEnemy, skillRange))
            {
                var detectionRange = skillRange * GameWorldService.DetectionRangeMultiplier;
                if (_worldService.AreInRange(attacker, primaryEnemy, detectionRange))
                {
                    _worldService.MoveToward(attacker, primaryEnemy, skillRange * 0.9f);
                    await Clients.Caller.SendAsync("MoveResult", new { x = attacker.X, y = attacker.Y });
                    var nearbyForMove = _worldService.GetNearbyPlayersOf(attacker);
                    foreach (var np in nearbyForMove)
                    {
                        await Clients.Client(np.ConnectionId).SendAsync("PlayerMoved", new { attacker.PlayerId, attacker.X, attacker.Y });
                    }
                    await Clients.Caller.SendAsync("Error", "Acercándose al objetivo...");
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Objetivo fuera de rango de detección.");
                }
                return;
            }
        }

        // ── Execute through combat engine ──
        IReadOnlyList<OnlineCombatService.OnlineCombatResult> results;
        OnlinePlayer broadcastReceiver; // quién recibe el PlayerStateUpdate principal del "target"

        if (targetResolution.IsMultiTarget)
        {
            // Group heal / ally buff / area ally: resolver por cada target
            lock (attacker.CombatLock)
            {
                results = _combatService.ExecuteSkillOnMultipleTargets(attacker, targetResolution.Targets, skill);
            }
            broadcastReceiver = attacker; // para fan-out sin un target único
        }
        else
        {
            var single = targetResolution.Targets[0];
            lock (attacker.CombatLock)
            {
                results = new[] { _combatService.ExecuteSkill(attacker, single, skill) };
            }
            broadcastReceiver = single;
        }

        // Record timing AFTER successful execution
        CombatValidationPipeline.RecordAction(attacker, CombatValidationPipeline.CombatActionType.UseSkill);
        attacker.LastMoveTime = DateTime.UtcNow;

        // ── Cast lock: bloquea la siguiente acción durante el tiempo de casteo ──
        // Secreto del sistema — el cliente no ve este valor, solo lo siente
        // como una ventana en la que sus siguientes inputs son rechazados.
        var castSeconds = GetSkillCastTimeSeconds(skill, skillDef);
        attacker.CastingUntil = DateTime.UtcNow.AddSeconds((double)castSeconds);

        // Fan-out broadcasting por cada resultado
        foreach (var result in results)
        {
            var affected = targetResolution.Targets.FirstOrDefault(t => t.PlayerId == result.TargetPlayerId) ?? broadcastReceiver;
            await BroadcastCombatResult(attacker, affected, result);
        }
    }

    /// Basic attack against a target player.
    public async Task BasicAttack(string targetPlayerId)
    {
        var attacker = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        var target = _worldService.GetPlayerByPlayerId(targetPlayerId);

        if (attacker is null) { await Clients.Caller.SendAsync("Error", "No estás en el mundo."); return; }
        if (target is null) { await Clients.Caller.SendAsync("Error", "Objetivo no encontrado."); return; }
        if (attacker.PlayerId == target.PlayerId) { await Clients.Caller.SendAsync("Error", "No puedes atacarte a ti mismo."); return; }

        // ── Server-authoritative validation pipeline ──
        var validation = CombatValidationPipeline.Validate(attacker, CombatValidationPipeline.CombatActionType.BasicAttack);
        if (!validation.Allowed) return; // Silently ignored

        // ── Range check with auto-approach ──
        var basicRange = _worldService.GetBasicAttackRange(attacker);
        if (!_worldService.AreInRange(attacker, target, basicRange))
        {
            var detectionRange = basicRange * GameWorldService.DetectionRangeMultiplier;
            if (_worldService.AreInRange(attacker, target, detectionRange))
            {
                _worldService.MoveToward(attacker, target, basicRange * 0.9f);
                await Clients.Caller.SendAsync("MoveResult", new { x = attacker.X, y = attacker.Y });
                var nearbyForMove = _worldService.GetNearbyPlayersOf(attacker);
                foreach (var np in nearbyForMove)
                {
                    await Clients.Client(np.ConnectionId).SendAsync("PlayerMoved", new { attacker.PlayerId, attacker.X, attacker.Y });
                }
                await Clients.Caller.SendAsync("Error", "Acercándose al objetivo...");
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Objetivo fuera de rango de detección.");
            }
            return;
        }

        // ── Execute through combat engine (combo handled internally) ──
        OnlineCombatService.OnlineCombatResult result;
        lock (attacker.CombatLock)
        {
            result = _combatService.ExecuteBasicAttack(attacker, target);
        }

        // Timing update is the pipeline's responsibility (single writer)
        CombatValidationPipeline.RecordAction(attacker, CombatValidationPipeline.CombatActionType.BasicAttack);
        attacker.LastMoveTime = DateTime.UtcNow;

        // ── Cast lock de ataque básico ──
        // Cada clase tiene su propio cast time definido en el catálogo de
        // basic attacks. Secreto del sistema, no se broadcast al cliente.
        var basicCastSeconds = GetBasicAttackCastTimeSeconds(attacker);
        attacker.CastingUntil = DateTime.UtcNow.AddSeconds((double)basicCastSeconds);

        await BroadcastCombatResult(attacker, target, result);
    }

    /// Shared helper to broadcast combat results to attacker, target, and nearby players.
    private async Task BroadcastCombatResult(OnlinePlayer attacker, OnlinePlayer target, OnlineCombatService.OnlineCombatResult result)
    {
        await Clients.Caller.SendAsync("CombatResult", result);
        if (target.ConnectionId != attacker.ConnectionId)
        {
            await Clients.Client(target.ConnectionId).SendAsync("CombatResult", result);
        }

        await Clients.Caller.SendAsync("PlayerStateUpdate", GameWorldService.ToFullStateDto(attacker));
        await Clients.Client(target.ConnectionId).SendAsync("PlayerStateUpdate", GameWorldService.ToFullStateDto(target));

        var nearby = _worldService.GetNearbyPlayersOf(target);
        var targetPresence = GameWorldService.ToPresenceDto(target);
        foreach (var nearbyPlayer in nearby)
        {
            if (nearbyPlayer.ConnectionId != attacker.ConnectionId)
            {
                await Clients.Client(nearbyPlayer.ConnectionId).SendAsync("TargetStateUpdate", targetPresence);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // QUERIES
    // ──────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<PlayerPresenceDto>> GetNearbyPlayers(float? radius = null)
    {
        var nearby = _worldService.GetNearbyPlayers(
            Context.ConnectionId,
            radius ?? GameWorldService.DefaultVisibilityRadius);

        IReadOnlyList<PlayerPresenceDto> result = nearby.Select(GameWorldService.ToPresenceDto).ToArray();
        return Task.FromResult(result);
    }

    public Task<WorldSnapshotDto> GetWorldSnapshot()
    {
        return Task.FromResult(_worldService.GetWorldSnapshot());
    }

    public Task<PlayerFullStateDto?> GetMyState()
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) return Task.FromResult<PlayerFullStateDto?>(null);
        return Task.FromResult<PlayerFullStateDto?>(GameWorldService.ToFullStateDto(player));
    }

    public Task<int> GetPlayerCount()
    {
        return Task.FromResult(_worldService.PlayerCount);
    }

    /// Find the nearest living player and return their presence data.
    public Task<PlayerPresenceDto?> FindNearestTarget()
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) return Task.FromResult<PlayerPresenceDto?>(null);

        var nearest = _worldService.FindNearestPlayer(player);
        if (nearest is null) return Task.FromResult<PlayerPresenceDto?>(null);

        return Task.FromResult<PlayerPresenceDto?>(GameWorldService.ToPresenceDto(nearest));
    }

    /// Get the full skill catalog for the player's class, with computed damage values.
    public Task<object?> GetSkillCatalog()
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) return Task.FromResult<object?>(null);

        var classCatalog = SkillCatalogRegistry.Current.ClassCatalogs
            .FirstOrDefault(c => c.ClassType == player.ClassType);

        if (classCatalog is null) return Task.FromResult<object?>(null);

        var basicAttack = War.Core.Combat.ClassBasicAttackCatalog.Default.GetRequired(player.ClassType);

        var skills = classCatalog.Skills.Select(def => {
            var magnitude = def.BaseTuning.Action.MagnitudeProfile;
            var scalingStat = magnitude.ScalingType switch {
                SkillScalingType.PhysicalAttack => "PhysicalAttack",
                SkillScalingType.MagicAttack => "MagicAttack",
                _ => "None"
            };
            var playerStatValue = scalingStat != "None" && player.Stats.TryGetValue(scalingStat, out var sv) ? sv : 0m;
            var baseDmg = magnitude.BaseMagnitude + magnitude.ScalingCoefficient * playerStatValue;

            // Mana cost
            var manaCost = 0m;
            if (def.BaseTuning.ResourceCosts is { } costs) {
                var mc = costs.FirstOrDefault(c => c.ResourceType == War.Core.Resources.CharacterResourceType.Mana);
                if (mc is not null) manaCost = mc.Amount;
            }

            // Effects
            var effects = (def.BaseTuning.Effects ?? Array.Empty<SkillConditionEffectDefinition>()).Select(e => new {
                Condition = e.Condition.ToString(),
                Duration = e.BaseDurationSeconds,
                Chance = e.ApplyChanceMultiplier
            }).ToArray();

            // Build ascension timeline
            var ascensions = new List<object>();
            if (def.AscensionOverrides is not null) {
                foreach (var (level, ovr) in def.AscensionOverrides.OrderBy(kv => kv.Key)) {
                    var changes = new List<string>();

                    if (ovr.MagnitudeProfile is not null) {
                        var newDmg = ovr.MagnitudeProfile.BaseMagnitude + ovr.MagnitudeProfile.ScalingCoefficient * playerStatValue;
                        changes.Add($"Daño: {Math.Round(newDmg, 0)}");
                    }
                    if (ovr.Targeting is not null) {
                        changes.Add($"Rango: {ovr.Targeting.BaseRangeUnits}m");
                        if (ovr.Targeting.AreaRadiusUnits.HasValue)
                            changes.Add($"Radio: {ovr.Targeting.AreaRadiusUnits}m");
                        if (ovr.Targeting.MaxTargets > 1)
                            changes.Add($"Objetivos: {ovr.Targeting.MaxTargets}");
                    }
                    if (ovr.Cadence is not null)
                        changes.Add($"CD: {ovr.Cadence.BaseCooldownSeconds}s");
                    if (ovr.ResourceCosts is { Count: > 0 }) {
                        var mc = ovr.ResourceCosts.FirstOrDefault(c => c.ResourceType == War.Core.Resources.CharacterResourceType.Mana);
                        if (mc is not null) changes.Add($"Maná: {mc.Amount}");
                    }
                    if (ovr.AddedEffects is { Count: > 0 }) {
                        foreach (var e in ovr.AddedEffects)
                            changes.Add($"+Efecto: {e.Condition}");
                    }
                    if (ovr.EffectOverrides is { Count: > 0 }) {
                        foreach (var e in ovr.EffectOverrides)
                            changes.Add($"Efecto mejorado: {e.EffectKey}");
                    }
                    if (ovr.MultiHit is not null)
                        changes.Add($"Multi-golpe: {ovr.MultiHit.HitCount}x en {ovr.MultiHit.ActiveDurationSeconds}s");
                    if (ovr.CastProtections is { Count: > 0 })
                        changes.Add("+ Protección al lanzar");
                    if (ovr.TriggeredActions is { Count: > 0 })
                        changes.Add("+ Acción activada");
                    if (!string.IsNullOrEmpty(ovr.Note))
                        changes.Add(ovr.Note);

                    ascensions.Add(new {
                        Level = level,
                        Changes = changes
                    });
                }
            }

            return new {
                Id = def.Id,
                Name = def.Name,
                Description = def.Description,
                IsUltimate = def.IsUltimate,
                UnlockLevel = def.UnlockLevel,
                Slot = def.Slot.ToString(),
                Elements = def.Elements?.Select(e => e.ToString()).ToArray() ?? Array.Empty<string>(),
                Roles = def.Roles?.Select(r => r.ToString()).ToArray() ?? Array.Empty<string>(),
                // Combat data
                ActionType = def.BaseTuning.Action.ActionType.ToString(),
                DamageType = def.BaseTuning.Action.DamageType?.ToString() ?? "None",
                ScalingStat = scalingStat,
                BaseMagnitude = magnitude.BaseMagnitude,
                ScalingCoefficient = magnitude.ScalingCoefficient,
                ComputedDamage = Math.Round(baseDmg, 0),
                CanCrit = def.BaseTuning.Action.CanCrit,
                RequiresHitCheck = def.BaseTuning.Action.RequiresHitCheck,
                // Targeting
                TargetPattern = def.BaseTuning.Targeting.Pattern.ToString(),
                TargetAffinity = def.BaseTuning.Targeting.Affinity.ToString(),
                Range = def.BaseTuning.Targeting.BaseRangeUnits,
                AreaRadius = def.BaseTuning.Targeting.AreaRadiusUnits,
                MaxTargets = def.BaseTuning.Targeting.MaxTargets,
                // Cadence
                Cooldown = def.BaseTuning.Cadence.BaseCooldownSeconds,
                ManaCost = manaCost,
                // Effects
                Effects = effects,
                // Multi-hit
                MultiHit = def.BaseTuning.MultiHit is not null ? new {
                    HitCount = def.BaseTuning.MultiHit.HitCount,
                    Duration = def.BaseTuning.MultiHit.ActiveDurationSeconds
                } : null,
                // Ascensions
                Ascensions = ascensions
            };
        }).ToArray();

        return Task.FromResult<object?>(new {
            ClassName = player.ClassName,
            ClassType = player.ClassType.ToString(),
            PlayerLevel = player.Level,
            PlayerAscension = player.AscensionLevel,
            BasicAttack = new {
                Name = basicAttack.Name,
                DamageType = basicAttack.DamageType.ToString(),
                RangeMeters = basicAttack.RangeMeters,
                CastTime = basicAttack.CastTimeSeconds
            },
            Skills = skills
        });
    }

    // ──────────────────────────────────────────────────────────────
    // CHAT
    // ──────────────────────────────────────────────────────────────

    /// Envía un mensaje de chat sólo a los jugadores dentro del rango de interacción
    /// (chat local por proximidad). También se entrega al emisor para confirmación visual.
    public async Task SendChatMessage(string message)
    {
        var sender = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (sender is null) return;

        message = message.Trim();
        if (string.IsNullOrWhiteSpace(message) || message.Length > 200) return;
        message = new string(message.Where(c => !char.IsControl(c)).ToArray());

        var chatPayload = new
        {
            sender.PlayerId,
            sender.DisplayName,
            sender.ClassName,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        // Entrega al emisor siempre (confirmación local).
        await Clients.Caller.SendAsync("ChatMessage", chatPayload);

        // Entrega sólo a jugadores dentro del rango de descubrimiento/interacción.
        // GetNearbyPlayersOf filtra por proximidad y excluye al propio emisor.
        var nearby = _worldService.GetNearbyPlayersOf(sender);
        foreach (var np in nearby)
        {
            await Clients.Client(np.ConnectionId).SendAsync("ChatMessage", chatPayload);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // SOCIAL — FRIENDS, BLOCKS, PROFILE
    // ──────────────────────────────────────────────────────────────

    /// Send a friend request to another player by their PlayerId.
    public async Task SendFriendRequest(string targetPlayerId)
    {
        if (!await EnforceHubRateLimitAsync()) return;
        var myPlayer = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (myPlayer is null) { await Clients.Caller.SendAsync("Error", "No estás en el mundo."); return; }

        if (!Guid.TryParse(myPlayer.PlayerId, out var myGuid) || !Guid.TryParse(targetPlayerId, out var targetGuid))
        { await Clients.Caller.SendAsync("Error", "ID de jugador inválido."); return; }

        var result = await _socialService.SendFriendRequestAsync(myGuid, targetGuid);
        if (result.Success)
        {
            await Clients.Caller.SendAsync("SocialNotification", new { Type = "FriendRequestSent", TargetPlayerId = targetPlayerId });

            // Notify the target
            var targetPlayer = _worldService.GetPlayerByPlayerId(targetPlayerId);
            if (targetPlayer is not null)
            {
                await Clients.Client(targetPlayer.ConnectionId).SendAsync("SocialNotification", new
                {
                    Type = "FriendRequestReceived",
                    SenderPlayerId = myPlayer.PlayerId,
                    SenderName = myPlayer.DisplayName,
                    SenderClassName = myPlayer.ClassName,
                    SenderLevel = myPlayer.Level
                });
            }
        }
        else
        {
            await Clients.Caller.SendAsync("SocialError", result.ErrorMessage);
        }
    }

    /// Respond to a friend request (accept/reject).
    public async Task RespondFriendRequest(string requestId, bool accept)
    {
        var myPlayer = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (myPlayer is null) return;

        if (!Guid.TryParse(myPlayer.PlayerId, out var myGuid) || !Guid.TryParse(requestId, out var reqGuid))
        { await Clients.Caller.SendAsync("SocialError", "ID inválido."); return; }

        var result = await _socialService.RespondToFriendRequestAsync(myGuid, reqGuid, accept);
        if (result.Success)
        {
            var action = accept ? "aceptada" : "rechazada";
            await Clients.Caller.SendAsync("SocialNotification", new { Type = "FriendRequestResponse", Action = action });
        }
        else
        {
            await Clients.Caller.SendAsync("SocialError", result.ErrorMessage);
        }
    }

    /// Block a player.
    public async Task BlockPlayer(string targetPlayerId)
    {
        var myPlayer = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (myPlayer is null) return;

        if (!Guid.TryParse(myPlayer.PlayerId, out var myGuid) || !Guid.TryParse(targetPlayerId, out var targetGuid))
        { await Clients.Caller.SendAsync("SocialError", "ID inválido."); return; }

        var result = await _socialService.BlockPlayerAsync(myGuid, targetGuid);
        if (result.Success)
        {
            var target = _worldService.GetPlayerByPlayerId(targetPlayerId);
            await Clients.Caller.SendAsync("SocialNotification", new
            {
                Type = "PlayerBlocked",
                TargetPlayerId = targetPlayerId,
                TargetName = target?.DisplayName ?? "Jugador"
            });
        }
        else
        {
            await Clients.Caller.SendAsync("SocialError", result.ErrorMessage);
        }
    }

    /// Unblock a player.
    public async Task UnblockPlayer(string targetPlayerId)
    {
        var myPlayer = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (myPlayer is null) return;

        if (!Guid.TryParse(myPlayer.PlayerId, out var myGuid) || !Guid.TryParse(targetPlayerId, out var targetGuid))
        { await Clients.Caller.SendAsync("SocialError", "ID inválido."); return; }

        var result = await _socialService.UnblockPlayerAsync(myGuid, targetGuid);
        if (result.Success)
            await Clients.Caller.SendAsync("SocialNotification", new { Type = "PlayerUnblocked", TargetPlayerId = targetPlayerId });
        else
            await Clients.Caller.SendAsync("SocialError", result.ErrorMessage);
    }

    /// Get friend list.
    public async Task<IReadOnlyList<FriendListEntryDto>> GetFriendList()
    {
        var myPlayer = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (myPlayer is null) return Array.Empty<FriendListEntryDto>();

        if (!Guid.TryParse(myPlayer.PlayerId, out var myGuid))
            return Array.Empty<FriendListEntryDto>();

        return await _socialService.GetFriendListAsync(myGuid);
    }

    /// Get block list.
    public async Task<IReadOnlyList<BlockListEntryDto>> GetBlockList()
    {
        var myPlayer = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (myPlayer is null) return Array.Empty<BlockListEntryDto>();

        if (!Guid.TryParse(myPlayer.PlayerId, out var myGuid))
            return Array.Empty<BlockListEntryDto>();

        return await _socialService.GetBlockListAsync(myGuid);
    }

    /// Get pending friend requests.
    public async Task<IReadOnlyList<PendingFriendRequestDto>> GetPendingFriendRequests()
    {
        var myPlayer = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (myPlayer is null) return Array.Empty<PendingFriendRequestDto>();

        if (!Guid.TryParse(myPlayer.PlayerId, out var myGuid))
            return Array.Empty<PendingFriendRequestDto>();

        return await _socialService.GetPendingInboundRequestsAsync(myGuid);
    }

    /// Get public profile of another player.
    public Task<object?> GetPlayerProfile(string targetPlayerId)
    {
        var target = _worldService.GetPlayerByPlayerId(targetPlayerId);
        if (target is null) return Task.FromResult<object?>(null);

        // Build a public profile from the in-memory player data
        var profile = new
        {
            target.PlayerId,
            target.DisplayName,
            target.ClassName,
            target.Level,
            target.AscensionLevel,
            CurrentHp = target.CurrentHp,
            MaxHp = target.MaxHp,
            SkillCount = target.Skills.Count,
            Skills = target.Skills.Select(s => new { s.Name, s.DamageType, s.ManaCost }).ToArray(),
            Conditions = target.Conditions.Select(c => new { c.ConditionType, c.RemainingSeconds }).ToArray()
        };

        return Task.FromResult<object?>(profile);
    }

    // ──────────────────────────────────────────────────────────────
    // TARGET RESOLUTION (server-authoritative)
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Resuelve los targets reales de una skill basado en su Affinity y Pattern.
    /// El cliente envía un targetPlayerId como HINT pero el servidor decide:
    ///
    ///   Affinity.Self                 → [caster]
    ///   Affinity.Ally + Pattern.Self  → [caster] + grupo (si tiene)
    ///   Affinity.Ally + Pattern.Area  → todo el grupo (si tiene), o [caster]
    ///   Affinity.Ally + Single        → aliado específico si está en grupo
    ///   Affinity.Enemy                → target enemigo (rechaza self-target)
    ///   Affinity.Any                  → lo que el cliente mande (default)
    ///
    /// Para skills desconocidas en el catálogo, se usa el fallback razonable
    /// basado en el DamageType de la skill online player slot.
    /// </summary>
    private SkillTargetResolution ResolveSkillTargets(
        OnlinePlayer caster,
        string targetPlayerId,
        SkillDefinition? skillDef)
    {
        // Fallback: skill no en catálogo → asumir single-target enemy
        if (skillDef is null)
        {
            var t = _worldService.GetPlayerByPlayerId(targetPlayerId);
            if (t is null) return SkillTargetResolution.Fail("Objetivo no encontrado.");
            if (t.PlayerId == caster.PlayerId) return SkillTargetResolution.Fail("No puedes atacarte a ti mismo.");
            return SkillTargetResolution.SingleEnemy(t);
        }

        var affinity = skillDef.BaseTuning.Targeting.Affinity;
        var pattern = skillDef.BaseTuning.Targeting.Pattern;

        // ── Self-affinity: siempre sobre el caster ──
        if (affinity == SkillTargetAffinity.Self)
        {
            return SkillTargetResolution.SelfOnly(caster);
        }

        // ── Ally-affinity ──
        if (affinity == SkillTargetAffinity.Ally)
        {
            // Pattern.Self + Ally → aura sobre caster + grupo
            // Pattern.Area + Ally → grupo completo
            if (pattern == SkillTargetingPattern.Self || pattern == SkillTargetingPattern.Area)
            {
                var members = _groupService.GetGroupMembersOrSelf(caster);
                return SkillTargetResolution.Multi(members);
            }
            // Pattern.SingleTarget + Ally → aliado específico (debe estar en el grupo)
            var hint = _worldService.GetPlayerByPlayerId(targetPlayerId);
            if (hint is null || hint.PlayerId == caster.PlayerId)
                return SkillTargetResolution.SelfOnly(caster); // fallback a self
            var casterGroup = _groupService.GetGroup(caster);
            if (casterGroup is not null && casterGroup.MemberIds.Contains(hint.PlayerId))
                return SkillTargetResolution.Multi(new[] { hint });
            // No son del mismo grupo → fallback a self
            return SkillTargetResolution.SelfOnly(caster);
        }

        // ── Enemy-affinity ──
        if (affinity == SkillTargetAffinity.Enemy)
        {
            var enemy = _worldService.GetPlayerByPlayerId(targetPlayerId);
            if (enemy is null) return SkillTargetResolution.Fail("Objetivo no encontrado.");
            if (enemy.PlayerId == caster.PlayerId) return SkillTargetResolution.Fail("No puedes atacarte a ti mismo.");
            // NOTE: Area/Line/Cone AoE sobre enemigos por proximidad queda pendiente
            // (requiere iteración sobre jugadores dentro del AreaRadiusUnits del target).
            // Por ahora resolvemos solo el primario.
            return SkillTargetResolution.SingleEnemy(enemy);
        }

        // ── Any-affinity: confiar en el hint del cliente ──
        var anyTarget = _worldService.GetPlayerByPlayerId(targetPlayerId) ?? caster;
        return SkillTargetResolution.Multi(new[] { anyTarget });
    }

    private static SkillDefinition? LookupSkillDefinition(string skillId)
    {
        try
        {
            return SkillCatalogRegistry.Current.ClassCatalogs
                .SelectMany(c => c.Skills)
                .FirstOrDefault(s => string.Equals(s.Id, skillId, StringComparison.OrdinalIgnoreCase));
        }
        catch { return null; }
    }

    /// <summary>
    /// Cast time de una skill. Secreto del sistema, no se broadcast al cliente.
    /// Se lee del catálogo (SkillCadenceProfile.CastTimeSeconds) donde cada skill
    /// define su propio valor individual. Si la skill no está en el catálogo
    /// (fallback), usa el default de CombatFormulaConstants.
    /// </summary>
    private static decimal GetSkillCastTimeSeconds(OnlinePlayerSkill skill, SkillDefinition? skillDef)
    {
        if (skillDef is not null)
        {
            return skillDef.BaseTuning.Cadence.CastTimeSeconds;
        }
        // Fallback cuando la skill no está en el catálogo
        return CombatFormulaConstants.DefaultSkillCastTimeSeconds;
    }

    /// <summary>
    /// Cast time del ataque básico de la clase del jugador, leído del
    /// catálogo oficial de basic attacks. Secreto del sistema.
    /// </summary>
    private static decimal GetBasicAttackCastTimeSeconds(OnlinePlayer attacker)
    {
        try
        {
            var catalog = War.Core.Combat.ClassBasicAttackCatalog.Default;
            var definition = catalog.GetRequired(attacker.ClassType);
            return definition.CastTimeSeconds;
        }
        catch
        {
            // Fallback seguro si la clase no estuviera en el catálogo
            return CombatFormulaConstants.DefaultSkillCastTimeSeconds;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // GROUP METHODS
    // ──────────────────────────────────────────────────────────────

    /// Crea un grupo nuevo con el caller como líder.
    public async Task<GroupStateDto?> CreateGroup()
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) return null;

        var group = _groupService.CreateGroup(player);
        if (group is null)
        {
            await Clients.Caller.SendAsync("Error", "Ya estás en un grupo.");
            return null;
        }

        return BuildGroupStateDto(group);
    }

    /// Invita a otro jugador cercano al grupo.
    public async Task InviteToGroup(string targetPlayerId)
    {
        if (!await EnforceHubRateLimitAsync()) return;
        var inviter = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        var invitee = _worldService.GetPlayerByPlayerId(targetPlayerId);
        if (inviter is null || invitee is null) { await Clients.Caller.SendAsync("Error", "Jugador no encontrado."); return; }

        var invitationId = _groupService.InviteToGroup(inviter, invitee);
        if (invitationId is null)
        {
            await Clients.Caller.SendAsync("Error", "No se pudo enviar la invitación (fuera de rango, ya en grupo, o inválido).");
            return;
        }

        // Notificar al invitado
        await Clients.Client(invitee.ConnectionId).SendAsync("GroupInvitation", new GroupInvitationDto(
            invitationId,
            inviter.GroupId ?? "",
            inviter.PlayerId,
            inviter.DisplayName));

        await Clients.Caller.SendAsync("SystemMessage", $"Invitación enviada a {invitee.DisplayName}.");
    }

    /// Acepta una invitación pendiente.
    public async Task<GroupStateDto?> AcceptGroupInvite(string invitationId)
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) return null;

        var group = _groupService.AcceptInvitation(player, invitationId);
        if (group is null)
        {
            await Clients.Caller.SendAsync("Error", "Invitación inválida o expirada.");
            return null;
        }

        var dto = BuildGroupStateDto(group);

        // Notificar a todos los miembros que el grupo cambió
        foreach (var memberId in group.MemberIds)
        {
            var m = _worldService.GetPlayerByPlayerId(memberId);
            if (m is not null) await Clients.Client(m.ConnectionId).SendAsync("GroupUpdated", dto);
        }

        return dto;
    }

    /// Rechaza una invitación.
    public async Task RejectGroupInvite(string invitationId)
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) return;
        _groupService.RejectInvitation(player, invitationId);
        await Clients.Caller.SendAsync("SystemMessage", "Invitación rechazada.");
    }

    /// Salir del grupo actual.
    public async Task LeaveGroup()
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) return;

        var group = _groupService.LeaveGroup(player);
        if (group is null) return;

        // Notificar al que salió
        await Clients.Caller.SendAsync("GroupUpdated", (GroupStateDto?)null);

        // Notificar a los miembros restantes
        var remaining = BuildGroupStateDto(group);
        foreach (var memberId in group.MemberIds)
        {
            var m = _worldService.GetPlayerByPlayerId(memberId);
            if (m is not null) await Clients.Client(m.ConnectionId).SendAsync("GroupUpdated", remaining);
        }
    }

    /// Expulsar a un miembro del grupo (solo líder).
    public async Task KickFromGroup(string targetPlayerId)
    {
        var leader = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        var target = _worldService.GetPlayerByPlayerId(targetPlayerId);
        if (leader is null || target is null) return;

        var success = _groupService.KickFromGroup(leader, target);
        if (!success)
        {
            await Clients.Caller.SendAsync("Error", "No puedes expulsar a ese jugador.");
            return;
        }

        await Clients.Client(target.ConnectionId).SendAsync("GroupUpdated", (GroupStateDto?)null);

        var group = _groupService.GetGroup(leader);
        if (group is not null)
        {
            var dto = BuildGroupStateDto(group);
            foreach (var memberId in group.MemberIds)
            {
                var m = _worldService.GetPlayerByPlayerId(memberId);
                if (m is not null) await Clients.Client(m.ConnectionId).SendAsync("GroupUpdated", dto);
            }
        }
    }

    /// Obtener el estado actual del grupo del caller.
    public Task<GroupStateDto?> GetMyGroup()
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) return Task.FromResult<GroupStateDto?>(null);
        var group = _groupService.GetGroup(player);
        if (group is null) return Task.FromResult<GroupStateDto?>(null);
        return Task.FromResult<GroupStateDto?>(BuildGroupStateDto(group));
    }

    private GroupStateDto BuildGroupStateDto(OnlineGroup group)
    {
        var members = new List<GroupMemberDto>();
        foreach (var memberId in group.MemberIds)
        {
            var m = _worldService.GetPlayerByPlayerId(memberId);
            if (m is not null)
            {
                members.Add(new GroupMemberDto(
                    m.PlayerId,
                    m.DisplayName,
                    m.ClassName,
                    IsLeader: m.PlayerId == group.LeaderId,
                    m.CurrentHp,
                    m.MaxHp));
            }
        }
        return new GroupStateDto(group.GroupId, group.LeaderId, members);
    }

    // ──────────────────────────────────────────────────────────────
    // WALLET  (Oro / Plata / Cobre / Energía)
    // ──────────────────────────────────────────────────────────────

    /// Devuelve los saldos actuales del wallet del jugador conectado.
    public WalletDto GetWallet()
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) throw new HubException("No estás en el mundo.");
        if (!Guid.TryParse(player.PlayerId, out var pid)) throw new HubException("ID inválido.");

        var wallet = _wallets.GetOrCreate(pid);
        return new WalletDto(
            wallet.Copper,
            wallet.Silver,
            wallet.Gold,
            (int)wallet.Energy,
            (int)wallet.GetCap(CurrencyType.Energy));
    }

    /// Histórico de transacciones del jugador (más reciente primero).
    public IReadOnlyList<WalletTransactionDto> GetWalletHistory(int limit = 50)
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) throw new HubException("No estás en el mundo.");
        if (!Guid.TryParse(player.PlayerId, out var pid)) throw new HubException("ID inválido.");

        var txs = _wallets.GetHistory(pid, Math.Clamp(limit, 1, 200));
        return txs.Select(ToWalletTxDto).ToArray();
    }

    // ──────────────────────────────────────────────────────────────
    // INVENTARIO
    // ──────────────────────────────────────────────────────────────

    /// Devuelve el estado completo del inventario del jugador.
    public InventoryDto GetInventory()
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) throw new HubException("No estás en el mundo.");
        return ToInventoryDto(player.Inventory);
    }

    /// Intenta comprar la siguiente expansión del inventario (+50 slots).
    /// Valida capacidad, cobra al wallet y, si todo ok, expande.
    public async Task ExpandInventory()
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) { await Clients.Caller.SendAsync("Error", "No estás en el mundo."); return; }
        if (!Guid.TryParse(player.PlayerId, out var pid)) { await Clients.Caller.SendAsync("Error", "ID inválido."); return; }

        var inv = player.Inventory;
        if (!InventoryExpansionCostCalculator.CanExpandFurther(inv.ExpansionsPurchased))
        { await Clients.Caller.SendAsync("Error", "Inventario al tope de expansiones."); return; }

        var cost = InventoryExpansionCostCalculator.GetNextExpansionCost(inv.ExpansionsPurchased);
        var spend = _wallets.Spend(
            playerId: pid,
            cost: cost,
            source: TransactionSource.InventoryExpansion,
            description: $"Expansión #{inv.ExpansionsPurchased + 1} (+{InventoryExpansionCostCalculator.ExpansionBatchSize} slots)");

        if (!spend.Success)
        { await Clients.Caller.SendAsync("Error", $"Expansión rechazada: {spend.ErrorMessage}"); return; }

        lock (inv)
        {
            inv.Expand();
        }

        await Clients.Caller.SendAsync("InventoryUpdate", ToInventoryDto(inv));
        await Clients.Caller.SendAsync("WalletUpdate", BuildWalletDto(pid));
    }

    /// Equipa un ítem del inventario en su slot correspondiente.
    public async Task EquipItem(string itemId)
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) { await Clients.Caller.SendAsync("Error", "No estás en el mundo."); return; }

        var item = player.Inventory.GetById(itemId);
        if (item is null) { await Clients.Caller.SendAsync("Error", "Ítem no encontrado."); return; }
        if (item.ItemType != InventoryItemType.Equipment) { await Clients.Caller.SendAsync("Error", "Solo equipos."); return; }

        var def = EquipmentCatalog.Get(item.DefinitionId);
        lock (player.Inventory)
        {
            // SwapEquipment es atómico: si hay otro en el slot, lo desequipa automáticamente.
            if (!player.Inventory.SwapEquipment(itemId, def.Slot))
            {
                _ = Clients.Caller.SendAsync("Error", "No se pudo equipar.");
                return;
            }
        }

        await Clients.Caller.SendAsync("InventoryUpdate", ToInventoryDto(player.Inventory));
    }

    /// Desequipa un ítem (se queda en el inventario).
    public async Task UnequipItem(string itemId)
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) { await Clients.Caller.SendAsync("Error", "No estás en el mundo."); return; }

        lock (player.Inventory)
        {
            if (!player.Inventory.UnequipItem(itemId))
            {
                _ = Clients.Caller.SendAsync("Error", "No se pudo desequipar.");
                return;
            }
        }

        await Clients.Caller.SendAsync("InventoryUpdate", ToInventoryDto(player.Inventory));
    }

    // ──────────────────────────────────────────────────────────────
    // CRAFTEO: DESARROLLO + TIER-UP
    // ──────────────────────────────────────────────────────────────

    /// Preview de costes para una pieza (qué cuesta subir desarrollo / tier-up).
    public CraftCostPreviewDto GetCraftCostPreview(string itemId)
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) throw new HubException("No estás en el mundo.");
        var item = player.Inventory.GetById(itemId);
        if (item is null || item.ItemType != InventoryItemType.Equipment)
            throw new HubException("Ítem no válido.");

        var def = EquipmentCatalog.Get(item.DefinitionId);
        var preview = CraftingCostCalculator.BuildPreview(item.Tier, def.Rarity, item.DevelopmentLevel);
        return new CraftCostPreviewDto(
            itemId,
            item.Tier,
            def.Rarity.ToString(),
            item.DevelopmentLevel,
            preview.NextDevelopmentCost is null ? null : ToCostDto(preview.NextDevelopmentCost),
            preview.TierUpCost is null ? null : ToCostDto(preview.TierUpCost));
    }

    /// Sube el desarrollo de una pieza en +1 (cobra al wallet).
    public async Task DevelopItem(string itemId)
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) { await Clients.Caller.SendAsync("Error", "No estás en el mundo."); return; }
        if (!Guid.TryParse(player.PlayerId, out var pid)) { await Clients.Caller.SendAsync("Error", "ID inválido."); return; }

        var item = player.Inventory.GetById(itemId);
        if (item is null || item.ItemType != InventoryItemType.Equipment)
        { await Clients.Caller.SendAsync("Error", "Ítem no válido."); return; }

        if (item.DevelopmentLevel >= 30)
        { await Clients.Caller.SendAsync("Error", "Desarrollo al máximo."); return; }

        var def = EquipmentCatalog.Get(item.DefinitionId);
        var cost = CraftingCostCalculator.ComputeDevelopmentCost(item.Tier, def.Rarity, item.DevelopmentLevel);

        var spend = _wallets.Spend(
            playerId: pid,
            cost: cost,
            source: TransactionSource.CraftingDevelopment,
            description: $"Desarrollo {item.DevelopmentLevel}→{item.DevelopmentLevel + 1} [{def.Name} T{item.Tier}]",
            relatedEntityId: Guid.TryParse(item.ItemId, out var eid) ? eid : null);

        if (!spend.Success)
        { await Clients.Caller.SendAsync("Error", $"Desarrollo rechazado: {spend.ErrorMessage}"); return; }

        lock (player.Inventory)
        {
            item.DevelopmentLevel++;
            item.LastModifiedAt = DateTime.UtcNow;
        }

        await Clients.Caller.SendAsync("ItemUpdated", ToItemDto(item));
        await Clients.Caller.SendAsync("WalletUpdate", BuildWalletDto(pid));
    }

    /// Consume DOS piezas del mismo tier/rareza para producir UNA del tier siguiente (dev 1).
    public async Task CraftTierUp(string itemIdA, string itemIdB)
    {
        if (!await EnforceHubRateLimitAsync()) return;
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) { await Clients.Caller.SendAsync("Error", "No estás en el mundo."); return; }
        if (!Guid.TryParse(player.PlayerId, out var pid)) { await Clients.Caller.SendAsync("Error", "ID inválido."); return; }

        var inv = player.Inventory;
        var a = inv.GetById(itemIdA);
        var b = inv.GetById(itemIdB);
        if (a is null || b is null) { await Clients.Caller.SendAsync("Error", "Ítems no encontrados."); return; }
        if (a.ItemId == b.ItemId) { await Clients.Caller.SendAsync("Error", "No puedes usar el mismo ítem dos veces."); return; }
        if (a.IsEquipped || b.IsEquipped) { await Clients.Caller.SendAsync("Error", "No puedes usar ítems equipados."); return; }
        if (a.ItemType != InventoryItemType.Equipment || b.ItemType != InventoryItemType.Equipment)
        { await Clients.Caller.SendAsync("Error", "Solo equipos."); return; }

        var defA = EquipmentCatalog.Get(a.DefinitionId);
        var defB = EquipmentCatalog.Get(b.DefinitionId);
        if (defA.Rarity != defB.Rarity) { await Clients.Caller.SendAsync("Error", "Las piezas deben tener la misma rareza."); return; }
        if (a.Tier != b.Tier) { await Clients.Caller.SendAsync("Error", "Las piezas deben tener el mismo tier."); return; }
        if (a.Tier >= 4) { await Clients.Caller.SendAsync("Error", "Tier ya está al máximo."); return; }
        if (defA.Slot != defB.Slot) { await Clients.Caller.SendAsync("Error", "Las piezas deben ser del mismo slot."); return; }

        var targetTier = a.Tier + 1;
        var cost = CraftingCostCalculator.ComputeTierUpCost(targetTier, defA.Rarity);

        var spend = _wallets.Spend(
            playerId: pid,
            cost: cost,
            source: TransactionSource.CraftingTierUp,
            description: $"TierUp T{a.Tier}→T{targetTier} [{defA.Name}]",
            relatedEntityId: Guid.TryParse(a.ItemId, out var eid) ? eid : null);

        if (!spend.Success)
        { await Clients.Caller.SendAsync("Error", $"Crafteo rechazado: {spend.ErrorMessage}"); return; }

        lock (inv)
        {
            // Atomic: consumimos A y B, mutamos a A como la nueva pieza (Tier+1, dev 1).
            inv.ForceRemoveItem(b.ItemId);
            a.Tier = targetTier;
            a.DevelopmentLevel = 1;
            a.LastModifiedAt = DateTime.UtcNow;
        }

        await Clients.Caller.SendAsync("InventoryUpdate", ToInventoryDto(inv));
        await Clients.Caller.SendAsync("WalletUpdate", BuildWalletDto(pid));
    }

    // ──────────────────────────────────────────────────────────────
    // HELPERS DE SERIALIZACIÓN
    // ──────────────────────────────────────────────────────────────

    private WalletDto BuildWalletDto(Guid pid)
    {
        var w = _wallets.GetOrCreate(pid);
        return new WalletDto(w.Copper, w.Silver, w.Gold, (int)w.Energy, (int)w.GetCap(CurrencyType.Energy));
    }

    private static WalletTransactionDto ToWalletTxDto(WalletTransaction tx) =>
        new(
            tx.Id.ToString("N"),
            tx.Timestamp,
            tx.Currency.ToString(),
            tx.Direction.ToString(),
            tx.Amount,
            tx.Source.ToString(),
            tx.Description,
            tx.BalanceBefore,
            tx.BalanceAfter);

    private static InventoryDto ToInventoryDto(PlayerInventory inv)
    {
        var items = inv.AllItems.Select(ToItemDto).ToArray();
        return new InventoryDto(
            Capacity: inv.Capacity,
            MaxCapacity: InventoryExpansionCostCalculator.MaxCapacity,
            UsedSlots: inv.UsedSlots,
            FreeSlots: inv.FreeSlots,
            ExpansionsPurchased: inv.ExpansionsPurchased,
            MaxExpansions: InventoryExpansionCostCalculator.MaxExpansions,
            Items: items);
    }

    private static InventoryItemDto ToItemDto(InventoryItem i) =>
        new(
            i.ItemId,
            i.ItemType.ToString(),
            i.DefinitionId,
            i.Quantity,
            i.SlotIndex,
            i.Tier,
            i.DevelopmentLevel,
            i.IsEquipped,
            i.EquippedSlot?.ToString());

    private static CurrencyCostDto ToCostDto(CurrencyCost c) =>
        new(c.Copper, c.Silver, c.Gold, c.Energy);

    // ──────────────────────────────────────────────────────────────
    // CAPILLA DE ECONOMÍA
    // ──────────────────────────────────────────────────────────────

    /// Estado de la Capilla del jugador (nivel, caps de posesión, límites de conversión).
    public ChapelStateDto GetChapelState()
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) throw new HubException("No estás en el mundo.");
        if (!Guid.TryParse(player.PlayerId, out var pid)) throw new HubException("ID inválido.");

        var chapel = _chapels.GetOrCreate(pid);
        var caps = chapel.GetPossessionCaps();
        var limits = chapel.GetConversionLimits();
        var nextRequiredCharLevel = chapel.Level < War.Core.Chapel.EconomyChapelRules.MaxLevel
            ? War.Core.Chapel.EconomyChapelRules.CharacterLevelRequiredFor(chapel.Level + 1)
            : (int?)null;

        return new ChapelStateDto(
            Level: chapel.Level,
            MaxLevel: War.Core.Chapel.EconomyChapelRules.MaxLevel,
            CharacterLevelRequiredForNext: nextRequiredCharLevel,
            PossessionCaps: new CurrencyCostDto(caps.Copper, caps.Silver, caps.Gold, caps.Energy),
            SilverConvDaily: limits.SilverDaily,
            SilverConvWeekly: limits.SilverWeekly,
            SilverConvMonthly: limits.SilverMonthly,
            GoldConvDaily: limits.GoldDaily,
            GoldConvWeekly: limits.GoldWeekly,
            GoldConvMonthly: limits.GoldMonthly);
    }

    /// Sube la Capilla un nivel (requiere nivel de personaje suficiente).
    /// Por ahora NO cobra recursos de upgrade — el usuario cuadra los costes en el siguiente paso.
    public async Task UpgradeChapel()
    {
        if (!await EnforceHubRateLimitAsync()) return;
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) { await Clients.Caller.SendAsync("Error", "No estás en el mundo."); return; }
        if (!Guid.TryParse(player.PlayerId, out var pid)) { await Clients.Caller.SendAsync("Error", "ID inválido."); return; }

        var result = _chapels.TryUpgrade(pid, player.Level);
        if (!result.Success)
        {
            await Clients.Caller.SendAsync("Error", $"Capilla no pudo subir: {result.ErrorMessage}");
            return;
        }

        await Clients.Caller.SendAsync("ChapelUpgraded", new
        {
            PreviousLevel = result.PreviousLevel,
            NewLevel = result.NewLevel
        });
        await Clients.Caller.SendAsync("ChapelUpdate", GetChapelState());
    }

    // ──────────────────────────────────────────────────────────────
    // CONVERSIÓN DE MONEDA
    // ──────────────────────────────────────────────────────────────

    /// Cuánta moneda de conversión le queda al jugador en cada ventana (día/semana/mes).
    public ConversionQuotasDto GetConversionQuotas()
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) throw new HubException("No estás en el mundo.");
        if (!Guid.TryParse(player.PlayerId, out var pid)) throw new HubException("ID inválido.");

        var limits = _chapels.GetConversionLimits(pid);
        var snap = _conversion.GetQuotaSnapshot(pid);
        return new ConversionQuotasDto(
            SilverUsedToday: snap.SilverToday, SilverLimitDaily: limits.SilverDaily,
            SilverUsedWeek:  snap.SilverThisWeek, SilverLimitWeekly: limits.SilverWeekly,
            SilverUsedMonth: snap.SilverThisMonth, SilverLimitMonthly: limits.SilverMonthly,
            GoldUsedToday: snap.GoldToday, GoldLimitDaily: limits.GoldDaily,
            GoldUsedWeek:  snap.GoldThisWeek, GoldLimitWeekly: limits.GoldWeekly,
            GoldUsedMonth: snap.GoldThisMonth, GoldLimitMonthly: limits.GoldMonthly);
    }

    /// Convierte cobre→plata o plata→oro. <paramref name="to"/> es la moneda a crear; amount es lo que se CREA.
    public async Task ConvertCurrency(string to, long amountToCreate)
    {
        if (!await EnforceHubRateLimitAsync()) return;
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) { await Clients.Caller.SendAsync("Error", "No estás en el mundo."); return; }
        if (!Guid.TryParse(player.PlayerId, out var pid)) { await Clients.Caller.SendAsync("Error", "ID inválido."); return; }

        CurrencyType target;
        CurrencyType source;
        switch (to.ToLowerInvariant())
        {
            case "silver": target = CurrencyType.Silver; source = CurrencyType.Copper; break;
            case "gold":   target = CurrencyType.Gold;   source = CurrencyType.Silver; break;
            default:
                await Clients.Caller.SendAsync("Error", "Moneda de destino inválida (silver|gold).");
                return;
        }

        var result = _conversion.Convert(pid, source, target, amountToCreate);
        if (!result.Success)
        {
            await Clients.Caller.SendAsync("Error", $"Conversión rechazada: {result.ErrorMessage}");
            return;
        }

        await Clients.Caller.SendAsync("ConversionApplied", new
        {
            From = result.From.ToString(),
            To = result.To.ToString(),
            Consumed = result.Consumed,
            Created = result.Created
        });
        await Clients.Caller.SendAsync("WalletUpdate", BuildWalletDto(pid));
        await Clients.Caller.SendAsync("ConversionQuotasUpdate", GetConversionQuotas());
    }

    // ──────────────────────────────────────────────────────────────
    // ASCENSIÓN DE HABILIDADES
    // ──────────────────────────────────────────────────────────────

    /// Preview de coste del siguiente paso de ascensión de una skill.
    public SkillAscensionPreviewDto? GetSkillAscensionPreview(string skillId)
    {
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) throw new HubException("No estás en el mundo.");

        var preview = _ascension.PreviewNextStep(player, skillId);
        if (preview is null) return null;
        return new SkillAscensionPreviewDto(
            SkillId: preview.SkillId,
            CurrentLevel: preview.CurrentLevel,
            IsMaxed: preview.NextStepCost is null,
            NextStepBookDefinitionId: preview.BookDefinitionId,
            NextStepBookCount: preview.NextStepCost?.Books.Count ?? 0,
            NextStepBookRarity: preview.NextStepCost?.Books.Rarity.ToString(),
            NextStepCost: preview.NextStepCost is null
                ? null
                : new CurrencyCostDto(
                    preview.NextStepCost.Copper,
                    preview.NextStepCost.Silver,
                    preview.NextStepCost.Gold,
                    preview.NextStepCost.Energy));
    }

    /// Aplica un paso de ascensión de una skill. Cobra libros + moneda + energía atómicamente.
    public async Task AscendSkill(string skillId)
    {
        if (!await EnforceHubRateLimitAsync()) return;
        var player = _worldService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null) { await Clients.Caller.SendAsync("Error", "No estás en el mundo."); return; }
        if (!Guid.TryParse(player.PlayerId, out var pid)) { await Clients.Caller.SendAsync("Error", "ID inválido."); return; }

        var result = _ascension.AscendSkill(player, skillId);
        if (!result.Success)
        {
            await Clients.Caller.SendAsync("Error", $"Ascensión rechazada: {result.ErrorMessage}");
            return;
        }

        await Clients.Caller.SendAsync("SkillAscended", new
        {
            result.SkillId,
            result.PreviousLevel,
            result.NewLevel,
            BookDefinitionId = result.BookDefinitionId,
            BookCount = result.CostApplied?.Books.Count ?? 0
        });
        await Clients.Caller.SendAsync("InventoryUpdate", ToInventoryDto(player.Inventory));
        await Clients.Caller.SendAsync("WalletUpdate", BuildWalletDto(pid));
    }
}

// DTO específico del preview de crafteo (no está en GameWorldModels por ser solo del hub).
public sealed record CraftCostPreviewDto(
    string ItemId,
    int Tier,
    string Rarity,
    int CurrentDevelopmentLevel,
    CurrencyCostDto? NextDevelopmentCost,
    CurrencyCostDto? TierUpCost);

// ──────────────────────────────────────────────────────────────
// TARGET RESOLUTION RESULT
// ──────────────────────────────────────────────────────────────

/// <summary>
/// Resultado de la resolución de targets de una skill.
/// Puede ser: un enemigo único (daño), una lista de aliados (heal/buff),
/// el caster solo (self-skill), o un fallo (error message).
/// </summary>
internal sealed record SkillTargetResolution(
    IReadOnlyList<OnlinePlayer> Targets,
    OnlinePlayer? PrimaryEnemy,
    bool IsMultiTarget,
    string? Error)
{
    public static SkillTargetResolution SelfOnly(OnlinePlayer caster) =>
        new(new[] { caster }, null, false, null);

    public static SkillTargetResolution SingleEnemy(OnlinePlayer enemy) =>
        new(new[] { enemy }, enemy, false, null);

    public static SkillTargetResolution Multi(IReadOnlyList<OnlinePlayer> targets) =>
        new(targets, null, true, null);

    public static SkillTargetResolution Fail(string error) =>
        new(Array.Empty<OnlinePlayer>(), null, false, error);
}
