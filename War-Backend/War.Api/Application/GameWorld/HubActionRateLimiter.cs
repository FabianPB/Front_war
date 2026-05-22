using System.Collections.Concurrent;

namespace War.Api.Application.GameWorld;

/// <summary>
/// Lightweight per-connection rate limiter for SignalR hub methods that
/// are not already gated by <see cref="CombatValidationPipeline"/> or the
/// movement cooldown inside <see cref="GameWorldService"/>.
///
/// Use this for hub methods that change state but do not pass through the
/// combat pipeline: friend requests, group/party operations, currency
/// conversion, inventory changes, skill ascension, chapel upgrades, etc.
///
/// Combat and movement keep their dedicated limiters because their windows
/// are tighter and tied to gameplay timing. This limiter is the catch-all
/// safety net against generic spam (scripts, bots).
/// </summary>
public sealed class HubActionRateLimiter
{
    // Defaults are intentionally generous so legitimate UI interactions
    // (rapid clicking, double-tap recovery) are never rejected. Anything
    // beyond this is bot territory.
    public const int DefaultMaxActionsPerWindow = 20;
    public const int DefaultWindowMilliseconds = 1000;
    public const int DefaultCooldownMilliseconds = 1500;

    private readonly int _maxActions;
    private readonly int _windowMs;
    private readonly int _cooldownMs;
    private readonly ConcurrentDictionary<string, ConnectionState> _states = new();

    public HubActionRateLimiter(
        int maxActions = DefaultMaxActionsPerWindow,
        int windowMs = DefaultWindowMilliseconds,
        int cooldownMs = DefaultCooldownMilliseconds)
    {
        if (maxActions <= 0) throw new ArgumentOutOfRangeException(nameof(maxActions));
        if (windowMs <= 0) throw new ArgumentOutOfRangeException(nameof(windowMs));
        if (cooldownMs < 0) throw new ArgumentOutOfRangeException(nameof(cooldownMs));

        _maxActions = maxActions;
        _windowMs = windowMs;
        _cooldownMs = cooldownMs;
    }

    /// <summary>
    /// Returns true if the action is allowed. When false, <paramref name="retryAfterMs"/>
    /// is the time in milliseconds the caller should wait before retrying.
    /// </summary>
    public bool TryAcquire(string connectionId, out int retryAfterMs)
    {
        if (string.IsNullOrEmpty(connectionId))
        {
            retryAfterMs = 0;
            return true; // No connection id → cannot rate-limit, allow.
        }

        var now = DateTime.UtcNow;
        var state = _states.GetOrAdd(connectionId, _ => new ConnectionState());

        lock (state)
        {
            if (state.LockoutUntilUtc.HasValue && now < state.LockoutUntilUtc.Value)
            {
                retryAfterMs = (int)Math.Ceiling((state.LockoutUntilUtc.Value - now).TotalMilliseconds);
                return false;
            }

            // Slide the window: drop timestamps older than the configured window.
            var windowStart = now.AddMilliseconds(-_windowMs);
            while (state.Timestamps.Count > 0 && state.Timestamps.Peek() < windowStart)
            {
                state.Timestamps.Dequeue();
            }

            if (state.Timestamps.Count >= _maxActions)
            {
                // Trip a brief lockout to discourage retries that just spam-fail.
                state.LockoutUntilUtc = now.AddMilliseconds(_cooldownMs);
                retryAfterMs = _cooldownMs;
                return false;
            }

            state.Timestamps.Enqueue(now);
            state.LockoutUntilUtc = null;
            retryAfterMs = 0;
            return true;
        }
    }

    /// <summary>
    /// Drop tracking for a disconnected connection so memory does not grow
    /// unbounded. Call from the hub's OnDisconnectedAsync.
    /// </summary>
    public void Release(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId)) return;
        _states.TryRemove(connectionId, out _);
    }

    private sealed class ConnectionState
    {
        public Queue<DateTime> Timestamps { get; } = new();
        public DateTime? LockoutUntilUtc { get; set; }
    }
}
