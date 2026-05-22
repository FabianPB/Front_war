namespace War.Core.Social;

// Decision: Static configuration class with constants instead of appsettings.json
// because these are game-design values that should be version-controlled and never differ between environments.
public static class SocialConfiguration
{
    // --- Proximity ---

    // Decision: 15 meters matches the typical "conversation distance" in MMORPGs.
    // Close enough to require intentional approach, far enough to not be frustrating.
    public const decimal InteractionRangeMeters = 15m;

    // Decision: Slightly larger range for the "nearby players" list so players approaching
    // appear before they're close enough to interact, giving a natural discovery feel.
    public const decimal NearbyDiscoveryRangeMeters = 25m;

    // --- Chat Rate Limiting ---

    // Decision: 10 messages per 5 seconds prevents spam bots while allowing fast-paced conversation.
    // More generous than typical MMOs because WAR targets mobile where typing is slower anyway.
    public const int MaxMessagesPerWindow = 10;
    public const int RateLimitWindowSeconds = 5;

    // Decision: Cooldown penalty escalates on repeated violations to deter persistent spammers.
    public const int SpamCooldownSeconds = 10;
    public const int RepeatedViolationMultiplier = 2;
    public const int MaxCooldownSeconds = 300; // 5 minute hard cap

    // --- Friends ---

    public const int MaxFriendsPerCharacter = 100;

    // Decision: Pending requests expire after 7 days to prevent indefinite "request limbo".
    public const int FriendRequestExpirationDays = 7;

    // Decision: Limit pending outbound requests to prevent harassment via mass friend-request spam.
    public const int MaxPendingOutboundRequests = 20;

    // --- Block ---

    // Decision: Generous block limit. Players should never feel they can't block enough people.
    public const int MaxBlockedPerCharacter = 500;

    // --- Public Profile ---

    // Decision: Cache profile data briefly to reduce DB load from repeated profile views
    // in crowded areas, but keep it short enough that level-ups reflect quickly.
    public const int ProfileCacheDurationSeconds = 30;

    // --- Client-Side Chat Storage (constants for Unity client reference) ---
    // TODO [Unity Integration]: Use these constants in the Unity chat manager to configure local FIFO storage.

    // Decision: FIFO queue per conversation. Oldest messages drop when limit is reached.
    // 200 messages ≈ 50KB typical, negligible on any modern mobile device.
    public const int MaxLocalMessagesPerConversation = 200;
    public const int MaxLocalConversations = 50;
}
