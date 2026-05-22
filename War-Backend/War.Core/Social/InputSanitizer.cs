using System.Text;
using System.Text.RegularExpressions;

namespace War.Core.Social;

// Decision: Static utility class because sanitization is stateless and deterministic.
// Every piece of user-provided text MUST pass through this before processing or relay.
public static partial class InputSanitizer
{
    // Decision: Generous but safe limit. Prevents memory abuse while allowing normal conversation.
    public const int MaxChatMessageLength = 500;
    public const int MaxPlayerNameLength = 30;

    // Decision: Compiled regex via source generators for zero-allocation matching at runtime.
    [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagPattern();

    [GeneratedRegex(@"[\u200B-\u200F\u202A-\u202E\u2060-\u2064\uFEFF]", RegexOptions.Compiled)]
    private static partial Regex InvisibleUnicodePattern();

    [GeneratedRegex(@"[\u202A-\u202E\u2066-\u2069]", RegexOptions.Compiled)]
    private static partial Regex RtlOverridePattern();

    [GeneratedRegex(@"\s{3,}", RegexOptions.Compiled)]
    private static partial Regex ExcessiveWhitespacePattern();

    /// <summary>
    /// Sanitizes a chat message for safe relay. Strips dangerous content, enforces length limits.
    /// Returns null if the message is empty after sanitization (caller should discard silently).
    /// </summary>
    public static string? SanitizeChatMessage(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            return null;

        var sanitized = rawInput;

        // Step 1: Strip HTML tags to prevent XSS injection via chat relay.
        sanitized = HtmlTagPattern().Replace(sanitized, string.Empty);

        // Step 2: Remove invisible Unicode characters that could hide malicious content or confuse rendering.
        sanitized = InvisibleUnicodePattern().Replace(sanitized, string.Empty);

        // Step 3: Remove RTL override characters that could reverse text direction to deceive readers.
        sanitized = RtlOverridePattern().Replace(sanitized, string.Empty);

        // Step 4: Collapse excessive whitespace (3+ consecutive spaces/tabs) to a single space.
        sanitized = ExcessiveWhitespacePattern().Replace(sanitized, " ");

        // Step 5: Trim and enforce maximum length.
        sanitized = sanitized.Trim();
        if (sanitized.Length > MaxChatMessageLength)
            sanitized = sanitized[..MaxChatMessageLength];

        // Decision: Return null instead of empty to give callers a clear "nothing to send" signal.
        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
    }

    /// <summary>
    /// Validates and sanitizes a player name for display. Same pipeline as chat but with a stricter length limit.
    /// </summary>
    public static string? SanitizeDisplayName(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            return null;

        var sanitized = rawInput;
        sanitized = HtmlTagPattern().Replace(sanitized, string.Empty);
        sanitized = InvisibleUnicodePattern().Replace(sanitized, string.Empty);
        sanitized = RtlOverridePattern().Replace(sanitized, string.Empty);
        sanitized = sanitized.Trim();

        if (sanitized.Length > MaxPlayerNameLength)
            sanitized = sanitized[..MaxPlayerNameLength];

        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
    }
}
