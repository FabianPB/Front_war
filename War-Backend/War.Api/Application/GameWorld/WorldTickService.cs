using Microsoft.AspNetCore.SignalR;
using War.Api.Hubs;

namespace War.Api.Application.GameWorld;

/// <summary>
/// Background service that ticks the game world every second:
/// - Decrements condition durations, removes expired conditions
/// - Applies DoT damage (Poison, Heat)
/// - Regenerates mana (2% of max per tick)
/// - Decrements skill cooldowns
/// - Broadcasts state updates to players whose state changed
/// - Removes players idle for 5+ minutes
/// </summary>
public sealed class WorldTickService : BackgroundService
{
    private const int TickIntervalMs = 1000;
    private const float ManaRegenPercent = 0.02f;    // 2% max mana per tick
    private const float DotDamagePercent = 0.015f;   // 1.5% max HP per tick for DoTs
    private static readonly TimeSpan MaxIdleTime = TimeSpan.FromMinutes(5);

    private readonly GameWorldService _worldService;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<WorldTickService> _logger;

    public WorldTickService(
        GameWorldService worldService,
        IHubContext<GameHub> hubContext,
        ILogger<WorldTickService> logger)
    {
        _worldService = worldService;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorldTickService started. Tick interval: {Interval}ms", TickIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TickIntervalMs, stoppingToken);
                await TickWorld();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WorldTickService tick");
            }
        }

        _logger.LogInformation("WorldTickService stopped.");
    }

    private async Task TickWorld()
    {
        var playersWithChanges = new List<OnlinePlayer>();

        foreach (var player in _worldService.AllPlayers)
        {
            var changed = false;

            // Skip defeated players
            if (player.CurrentHp <= 0) continue;

            // Per-player lock: evita carreras con las mutaciones del hub sobre
            // Conditions/Cooldowns/CurrentHp/CurrentMana. El cuerpo del tick es
            // corto, así que el lock no penaliza throughput.
            lock (player.CombatLock)
            {
                // ── 1. Decrement condition durations ──
                for (int i = player.Conditions.Count - 1; i >= 0; i--)
                {
                    var condition = player.Conditions[i];
                    var newRemaining = condition.RemainingSeconds - (TickIntervalMs / 1000f);

                    if (newRemaining <= 0)
                    {
                        player.Conditions.RemoveAt(i);
                        changed = true;
                    }
                    else
                    {
                        player.Conditions[i] = condition with { RemainingSeconds = newRemaining };
                        changed = true;
                    }
                }

                // ── 2. Apply DoT damage (Poison, Heat) ──
                var hasPoison = player.Conditions.Any(c => c.ConditionType == "Poison");
                var hasHeat = player.Conditions.Any(c => c.ConditionType == "Heat");

                if (hasPoison || hasHeat)
                {
                    var dotDamage = player.MaxHp * (decimal)DotDamagePercent;
                    if (hasPoison && hasHeat) dotDamage *= CombatFormulaConstants.StackedDotMultiplier;
                    player.CurrentHp = Math.Max(0, player.CurrentHp - dotDamage);
                    changed = true;
                }

                // ── 3. Regenerate mana ──
                if (player.CurrentMana < player.MaxMana)
                {
                    var regenAmount = player.MaxMana * (decimal)ManaRegenPercent;
                    player.CurrentMana = Math.Min(player.MaxMana, player.CurrentMana + regenAmount);
                    changed = true;
                }

                // ── 4. Decrement cooldowns ──
                var cooldownKeys = player.Cooldowns.Keys.ToArray();
                foreach (var key in cooldownKeys)
                {
                    var newCd = player.Cooldowns[key] - (TickIntervalMs / 1000f);
                    if (newCd <= 0)
                        player.Cooldowns.Remove(key);
                    else
                        player.Cooldowns[key] = newCd;
                    changed = true;
                }
            } // end lock

            if (changed)
                playersWithChanges.Add(player);
        }

        // ── 5. Broadcast state updates to changed players ──
        foreach (var player in playersWithChanges)
        {
            try
            {
                var stateDto = GameWorldService.ToFullStateDto(player);
                await _hubContext.Clients.Client(player.ConnectionId)
                    .SendAsync("PlayerStateUpdate", stateDto);
            }
            catch
            {
                // Connection may have been lost — ignore
            }
        }

        // ── 6. Remove inactive players ──
        var removed = _worldService.RemoveInactivePlayers(MaxIdleTime);
        foreach (var player in removed)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("PlayerLeft", new
                {
                    player.PlayerId,
                    player.DisplayName
                });
            }
            catch { /* ignore */ }
        }
    }
}
