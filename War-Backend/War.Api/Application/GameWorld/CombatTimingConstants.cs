namespace War.Api.Application.GameWorld;

/// <summary>
/// All combat timing constants in ONE place.
/// Values aligned with War.Core BasicAttackCatalog where applicable.
/// </summary>
public static class CombatTimingConstants
{
    // ── Basic Attack Timing ──
    /// <summary>Minimum ms between basic attacks (attack speed).</summary>
    public const int BasicAttackIntervalMs = 800;

    // ── Global Cooldown ──
    /// <summary>GCD after any combat action (skill or basic attack). Blocks all combat for this duration.</summary>
    public const int GcdDurationMs = 800;

    // ── Combo System (from BasicAttackCatalog) ──
    /// <summary>Number of combo stages before reset.</summary>
    public const int ComboStageCount = 6;
    /// <summary>Seconds without attacking before combo resets to stage 1.</summary>
    public const double ComboWindowSeconds = 2.0;
    /// <summary>Damage multiplier per combo stage (1.5% cumulative).</summary>
    public const decimal ComboStageMultiplier = 1.015m;

    // ── Anti-Spam Rate Limiting ──
    /// <summary>Max combat actions allowed within the rate limit window.</summary>
    public const int RateLimitActions = 5;
    /// <summary>Rate limit window in ms.</summary>
    public const int RateLimitWindowMs = 2000;
    /// <summary>Lockout duration in ms when rate limit is exceeded.</summary>
    public const int LockoutDurationMs = 3000;
    /// <summary>Number of lockouts within escalation window to trigger escalation.</summary>
    public const int LockoutEscalationCount = 3;
    /// <summary>Escalation window in ms.</summary>
    public const int LockoutEscalationWindowMs = 60000;
    /// <summary>Escalated lockout duration in ms.</summary>
    public const int LockoutEscalatedDurationMs = 10000;

    // ── Debug ──
    /// <summary>When true, logs each combat validation step to console.</summary>
    public const bool DebugCombatValidation = false;
}
