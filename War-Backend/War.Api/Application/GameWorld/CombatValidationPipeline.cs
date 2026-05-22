namespace War.Api.Application.GameWorld;

/// <summary>
/// Server-authoritative combat validation pipeline.
/// Runs BEFORE any combat action reaches the engine.
/// Uses early-return pattern for clarity.
/// </summary>
public static class CombatValidationPipeline
{
    public enum CombatActionType { BasicAttack, UseSkill }

    public sealed record ValidationResult(bool Allowed, string? RejectionReason = null);

    private static readonly ValidationResult Ok = new(true);

    /// <summary>
    /// Validates whether a combat action should proceed.
    /// Order: Lockout → RateLimit → CC → Silence → GCD → ActionSpecific
    /// </summary>
    public static ValidationResult Validate(
        OnlinePlayer player,
        CombatActionType actionType,
        OnlinePlayerSkill? skill = null)
    {
        var now = DateTime.UtcNow;
        var tag = $"[{player.DisplayName}] {actionType}";

        // ── 1. Is player in lockout from rate limiting? ──
        if (now < player.LockoutUntil)
        {
            Log(tag, "REJECTED: In lockout");
            return new ValidationResult(false, "Lockout activo");
        }

        // ── 1.5. Cast-time lock: si aún está casteando algo, rechazar en silencio ──
        // Cada skill/básico define su cast time (secreto del sistema, no broadcast).
        // Mientras la animación de casteo está activa, el jugador no puede iniciar
        // otra acción de combate. Se rechaza silenciosamente sin avisar al cliente.
        if (now < player.CastingUntil)
        {
            Log(tag, $"REJECTED: Casting in progress ({(player.CastingUntil - now).TotalMilliseconds:F0}ms restantes)");
            return new ValidationResult(false, "Cast en progreso");
        }

        // ── 2. Register action and check rate limit ──
        CleanOldActions(player, now);
        player.CombatActionLog.Enqueue(now);

        if (player.CombatActionLog.Count > CombatTimingConstants.RateLimitActions)
        {
            // Exceeded rate limit → activate lockout
            var lockoutMs = CombatTimingConstants.LockoutDurationMs;

            // Check for escalation
            if (now - player.LockoutWindowStart < TimeSpan.FromMilliseconds(CombatTimingConstants.LockoutEscalationWindowMs))
            {
                player.LockoutCount++;
                if (player.LockoutCount >= CombatTimingConstants.LockoutEscalationCount)
                {
                    lockoutMs = CombatTimingConstants.LockoutEscalatedDurationMs;
                    Log(tag, $"REJECTED: Rate limit ESCALATED ({lockoutMs}ms)");
                }
            }
            else
            {
                // Reset escalation window
                player.LockoutWindowStart = now;
                player.LockoutCount = 1;
            }

            player.LockoutUntil = now.AddMilliseconds(lockoutMs);
            Log(tag, $"REJECTED: Rate limit exceeded, lockout {lockoutMs}ms");
            return new ValidationResult(false, "Rate limit");
        }

        // ── 3. Hard CC check (Stun, Freeze, Paralyze) ──
        if (player.Conditions.Any(c => c.ConditionType is "Stun" or "Freeze" or "Paralyze"))
        {
            Log(tag, "REJECTED: Hard CC active");
            return new ValidationResult(false, "CC activo");
        }

        // ── 4. Silence check (only blocks skills, not basic attacks) ──
        if (actionType == CombatActionType.UseSkill &&
            player.Conditions.Any(c => c.ConditionType is "Silence"))
        {
            Log(tag, "REJECTED: Silenced");
            return new ValidationResult(false, "Silenciado");
        }

        // ── 5. GCD check — time since last ANY combat action ──
        var gcdMs = CombatTimingConstants.GcdDurationMs;
        var msSinceLastSkill = (now - player.LastSkillUseTime).TotalMilliseconds;
        var msSinceLastBasic = (now - player.LastBasicAttackTime).TotalMilliseconds;

        // GCD blocks if EITHER a skill or basic attack was used too recently
        if (msSinceLastSkill < gcdMs)
        {
            Log(tag, $"REJECTED: GCD from skill ({msSinceLastSkill:F0}ms < {gcdMs}ms)");
            return new ValidationResult(false, "GCD");
        }
        if (msSinceLastBasic < gcdMs)
        {
            Log(tag, $"REJECTED: GCD from basic ({msSinceLastBasic:F0}ms < {gcdMs}ms)");
            return new ValidationResult(false, "GCD");
        }

        // ── 6. Action-specific checks ──
        if (actionType == CombatActionType.BasicAttack)
        {
            // Basic attacks additionally respect attack speed interval
            var msSinceLast = (now - player.LastBasicAttackTime).TotalMilliseconds;
            if (msSinceLast < CombatTimingConstants.BasicAttackIntervalMs)
            {
                Log(tag, $"REJECTED: Attack speed ({msSinceLast:F0}ms < {CombatTimingConstants.BasicAttackIntervalMs}ms)");
                return new ValidationResult(false, "Attack speed");
            }
        }

        if (actionType == CombatActionType.UseSkill && skill is not null)
        {
            // Cooldown already validated in OnlineCombatService.ExecuteSkill
            // but we double-check here as early exit
            if (player.Cooldowns.TryGetValue(skill.SkillId, out var cd) && cd > 0)
            {
                Log(tag, $"REJECTED: Skill on cooldown ({cd:F1}s)");
                return new ValidationResult(false, "Cooldown");
            }

            if (player.CurrentMana < skill.ManaCost)
            {
                Log(tag, $"REJECTED: Insufficient mana ({player.CurrentMana:F0}/{skill.ManaCost})");
                return new ValidationResult(false, "Maná insuficiente");
            }
        }

        // ── All checks passed ──
        Log(tag, "ALLOWED");
        return Ok;
    }

    /// <summary>
    /// Call AFTER a successful combat action to update timing.
    /// </summary>
    public static void RecordAction(OnlinePlayer player, CombatActionType actionType)
    {
        var now = DateTime.UtcNow;
        if (actionType == CombatActionType.BasicAttack)
        {
            player.LastBasicAttackTime = now;
        }
        else
        {
            player.LastSkillUseTime = now;
        }
    }

    private static void CleanOldActions(OnlinePlayer player, DateTime now)
    {
        var windowStart = now.AddMilliseconds(-CombatTimingConstants.RateLimitWindowMs);
        while (player.CombatActionLog.Count > 0 && player.CombatActionLog.Peek() < windowStart)
        {
            player.CombatActionLog.Dequeue();
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private static void Log(string tag, string message)
    {
#pragma warning disable CS0162 // Unreachable code — gated by DebugCombatValidation const
        if (!CombatTimingConstants.DebugCombatValidation) return;
        Console.WriteLine($"[CombatValidation] {tag}: {message}");
#pragma warning restore CS0162
    }
}
