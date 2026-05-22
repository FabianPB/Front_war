using System.Text.Json;

namespace War.IntegrationTests;

/// <summary>
/// Scenario 8: Multiplayer sync — world snapshot, player joined/left events, nearby players.
/// </summary>
public class T8_MultiplayerSyncTests : IClassFixture<WarTestServer>
{
    private readonly WarTestServer _server;

    public T8_MultiplayerSyncTests(WarTestServer server) => _server = server;

    [Fact]
    public async Task WorldSnapshot_Contains_All_Connected_Players()
    {
        var (p1, _) = await _server.CreateAndJoinPlayerAsync("WorldSnap_A");
        var (p2, _) = await _server.CreateAndJoinPlayerAsync("WorldSnap_B");

        var result = await p1.InvokeAsync<object>("GetWorldSnapshot");
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        var count = doc.RootElement.Prop("PlayerCount").GetInt32();
        count.Should().BeGreaterOrEqualTo(2);

        var players = doc.RootElement.Prop("Players");
        var names = new List<string>();
        foreach (var p in players.EnumerateArray())
        {
            names.Add(p.Prop("DisplayName").GetString()!);
        }
        names.Should().Contain("WorldSnap_A");
        names.Should().Contain("WorldSnap_B");

        await p1.StopAsync();
        await p2.StopAsync();
    }

    [Fact]
    public async Task PlayerJoined_Event_Is_Broadcast_To_Others()
    {
        var (existing, _) = await _server.CreateAndJoinPlayerAsync("ExistingPlayer");

        var joinedTcs = new TaskCompletionSource<string>();
        existing.On<object>("PlayerJoined", presence =>
        {
            var json = JsonSerializer.Serialize(presence);
            using var doc = JsonDocument.Parse(json);
            joinedTcs.TrySetResult(doc.RootElement.Prop("DisplayName").GetString()!);
        });

        var (newPlayer, _) = await _server.CreateAndJoinPlayerAsync("NewJoiner");

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => joinedTcs.TrySetCanceled());
        var joinedName = await joinedTcs.Task;
        joinedName.Should().Be("NewJoiner");

        await existing.StopAsync();
        await newPlayer.StopAsync();
    }

    [Fact]
    public async Task PlayerLeft_Event_Is_Broadcast_On_Disconnect()
    {
        var (watcher, _) = await _server.CreateAndJoinPlayerAsync("Watcher");
        var (leaver, leaverId) = await _server.CreateAndJoinPlayerAsync("Leaver");

        // PlayerLeft sends anonymous object { PlayerId, DisplayName }
        var leftTcs = new TaskCompletionSource<string>();
        watcher.On<object>("PlayerLeft", payload =>
        {
            var json = JsonSerializer.Serialize(payload);
            using var doc = JsonDocument.Parse(json);
            leftTcs.TrySetResult(doc.RootElement.Prop("PlayerId").GetString()!);
        });

        await leaver.StopAsync();
        await Task.Delay(500); // Give time for disconnect to propagate

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => leftTcs.TrySetCanceled());
        var leftId = await leftTcs.Task;
        leftId.Should().Be(leaverId);

        await watcher.StopAsync();
    }

    [Fact]
    public async Task GetNearbyPlayers_Returns_Close_Players_Only()
    {
        var (center, _) = await _server.CreateAndJoinPlayerAsync("Center");
        var (near, _) = await _server.CreateAndJoinPlayerAsync("NearPlayer");
        var (far, _) = await _server.CreateAndJoinPlayerAsync("FarPlayer");

        await center.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);
        await near.InvokeCoreAsync("MoveTo", new object[] { 52.0f, 50.0f });
        await Task.Delay(250);
        await far.InvokeCoreAsync("MoveTo", new object[] { 0.0f, 0.0f });
        await Task.Delay(250);

        var result = await center.InvokeCoreAsync<object>("GetNearbyPlayers", new object[] { 15.0f });
        var json = JsonSerializer.Serialize(result);

        json.Should().Contain("NearPlayer");
        json.Should().NotContain("FarPlayer");

        await center.StopAsync();
        await near.StopAsync();
        await far.StopAsync();
    }

    [Fact]
    public async Task GetPlayerProfile_Returns_Target_Info()
    {
        var (viewer, _) = await _server.CreateAndJoinPlayerAsync("Viewer");
        var (target, targetId) = await _server.CreateAndJoinPlayerAsync("ProfileTarget", "Juramentada");

        var result = await viewer.InvokeAsync<object>("GetPlayerProfile", targetId);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.Prop("DisplayName").GetString().Should().Be("ProfileTarget");
        doc.RootElement.Prop("ClassName").GetString().Should().Be("Juramentada");

        await viewer.StopAsync();
        await target.StopAsync();
    }
}
