using System.Collections.Concurrent;
using War.Core.Social;

namespace War.Api.Application.Social;

// Decision: In-memory rate limiter because chat rate data is ephemeral — it doesn't need to survive server restarts.
// A Redis-backed limiter would be appropriate for multi-server deployments but is overengineering for the current architecture.
public sealed class ChatRateLimiter
{
    // Decision: ConcurrentDictionary for thread-safe access from multiple SignalR connections.
    private readonly ConcurrentDictionary<Guid, CharacterRateState> _states = new();

    public bool IsAllowed(Guid characterId, out int cooldownRemainingSeconds)
    {
        var now = DateTime.UtcNow;
        var state = _states.GetOrAdd(characterId, _ => new CharacterRateState());

        lock (state)
        {
            // Check if character is in cooldown penalty
            if (state.CooldownUntilUtc.HasValue && now < state.CooldownUntilUtc.Value)
            {
                cooldownRemainingSeconds = (int)Math.Ceiling((state.CooldownUntilUtc.Value - now).TotalSeconds);
                return false;
            }

            // Clear expired timestamps from the sliding window
            var windowStart = now.AddSeconds(-SocialConfiguration.RateLimitWindowSeconds);
            while (state.MessageTimestamps.Count > 0 && state.MessageTimestamps.Peek() < windowStart)
                state.MessageTimestamps.Dequeue();

            // Check if within rate limit
            if (state.MessageTimestamps.Count >= SocialConfiguration.MaxMessagesPerWindow)
            {
                // Apply escalating cooldown
                state.ConsecutiveViolations++;
                var cooldownSeconds = Math.Min(
                    SocialConfiguration.SpamCooldownSeconds * (int)Math.Pow(SocialConfiguration.RepeatedViolationMultiplier, state.ConsecutiveViolations - 1),
                    SocialConfiguration.MaxCooldownSeconds);
                state.CooldownUntilUtc = now.AddSeconds(cooldownSeconds);
                cooldownRemainingSeconds = cooldownSeconds;
                return false;
            }

            // Record this message timestamp
            state.MessageTimestamps.Enqueue(now);
            state.ConsecutiveViolations = 0; // Reset on successful send
            state.CooldownUntilUtc = null;
            cooldownRemainingSeconds = 0;
            return true;
        }
    }

    // Decision: Periodic cleanup to prevent memory leaks from disconnected players.
    // Called on a timer or during hub disconnect.
    public void RemoveState(Guid characterId)
    {
        _states.TryRemove(characterId, out _);
    }

    private sealed class CharacterRateState
    {
        public Queue<DateTime> MessageTimestamps { get; } = new();
        public int ConsecutiveViolations { get; set; }
        public DateTime? CooldownUntilUtc { get; set; }
    }
}
