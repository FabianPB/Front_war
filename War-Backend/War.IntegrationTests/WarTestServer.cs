using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace War.IntegrationTests;

/// <summary>
/// Shared test server factory. Boots the real War.Api in-memory (no DB, no external dependencies).
/// </summary>
public class WarTestServer : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production"); // uses appsettings.Production.json → no DB
    }

    /// <summary>
    /// Creates a SignalR HubConnection pointing at the /game hub on the test server.
    /// </summary>
    public HubConnection CreateGameHubConnection()
    {
        var server = Server; // ensure server is started
        var handler = server.CreateHandler();

        return new HubConnectionBuilder()
            .WithUrl($"{server.BaseAddress}game", options =>
            {
                options.HttpMessageHandlerFactory = _ => handler;
            })
            .Build();
    }

    /// <summary>
    /// Convenience: creates a connection, starts it, joins the game, and returns (connection, playerId).
    /// SignalR uses camelCase JSON by default.
    /// </summary>
    public async Task<(HubConnection Connection, string PlayerId)> CreateAndJoinPlayerAsync(
        string displayName, string className = "Sorcerer", int level = 30, int ascensionLevel = 5)
    {
        var conn = CreateGameHubConnection();
        await conn.StartAsync();

        var result = await conn.InvokeAsync<object>("JoinGame", displayName, className, level, ascensionLevel);

        // JoinGame returns JoinWorldResultDto — SignalR serializes with camelCase
        var json = System.Text.Json.JsonSerializer.Serialize(result);
        using var doc = System.Text.Json.JsonDocument.Parse(json);

        // Try both PascalCase and camelCase (depends on SignalR protocol configuration)
        var root = doc.RootElement;
        string playerId;

        if (root.TryGetProperty("player", out var playerEl))
        {
            playerId = playerEl.GetProperty("playerId").GetString()!;
        }
        else if (root.TryGetProperty("Player", out var playerPascal))
        {
            playerId = playerPascal.GetProperty("PlayerId").GetString()!;
        }
        else
        {
            // Dump for debugging
            throw new InvalidOperationException($"Unexpected JoinGame response structure: {json}");
        }

        return (conn, playerId);
    }
}
