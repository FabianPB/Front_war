using System.Text.Json;

namespace War.IntegrationTests;

/// <summary>
/// Helper utilities for reading SignalR responses in integration tests.
/// </summary>
internal static class Helpers
{
    // Default JSON options with case-insensitive property matching
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Invokes a hub method and deserializes the result as a JsonDocument for ad-hoc property access.
    /// Uses case-insensitive re-serialization to normalize property names.
    /// </summary>
    public static async Task<JsonDocument> InvokeJsonAsync(this HubConnection conn, string method, params object[] args)
    {
        var result = await conn.InvokeCoreAsync<object>(method, args);
        var json = JsonSerializer.Serialize(result);
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// Gets a property from a JsonElement, trying both PascalCase and camelCase.
    /// </summary>
    public static JsonElement Prop(this JsonElement el, string name)
    {
        if (el.TryGetProperty(name, out var val))
            return val;

        // Try camelCase
        var camel = char.ToLowerInvariant(name[0]) + name[1..];
        if (el.TryGetProperty(camel, out val))
            return val;

        // Try PascalCase
        var pascal = char.ToUpperInvariant(name[0]) + name[1..];
        if (el.TryGetProperty(pascal, out val))
            return val;

        throw new KeyNotFoundException($"Property '{name}' not found (tried camelCase/PascalCase) in: {el}");
    }
}
