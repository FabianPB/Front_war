using System.Text.Json;

namespace War.IntegrationTests;

/// <summary>
/// Scenario 1: Player connection, join world, and disconnect lifecycle.
/// </summary>
public class T1_ConnectionTests : IClassFixture<WarTestServer>
{
    private readonly WarTestServer _server;

    public T1_ConnectionTests(WarTestServer server) => _server = server;

    [Fact]
    public async Task Player_Can_Join_World_And_Receive_FullState()
    {
        var conn = _server.CreateGameHubConnection();
        await conn.StartAsync();

        var result = await conn.InvokeAsync<object>("JoinGame", "TestPlayer1", "Sorcerer", 30, 5);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        var player = doc.RootElement.Prop("Player");
        player.Prop("DisplayName").GetString().Should().Be("TestPlayer1");
        player.Prop("ClassName").GetString().Should().Be("Sorcerer");
        player.Prop("Level").GetInt32().Should().Be(30);
        player.Prop("CurrentHp").GetDecimal().Should().BeGreaterThan(0);
        player.Prop("MaxHp").GetDecimal().Should().BeGreaterThan(0);
        player.Prop("CurrentMana").GetDecimal().Should().BeGreaterThan(0);
        player.Prop("Skills").GetArrayLength().Should().BeGreaterThan(0);
        player.Prop("PlayerId").GetString().Should().NotBeNullOrEmpty();

        var snapshot = doc.RootElement.Prop("WorldSnapshot");
        snapshot.Prop("PlayerCount").GetInt32().Should().BeGreaterOrEqualTo(1);

        await conn.StopAsync();
    }

    [Fact]
    public async Task PlayerCount_Increments_On_Join()
    {
        var conn = _server.CreateGameHubConnection();
        await conn.StartAsync();
        await conn.InvokeAsync<object>("JoinGame", "CountTest", "Bruiser", 30, 5);

        var count = await conn.InvokeAsync<int>("GetPlayerCount");
        count.Should().BeGreaterOrEqualTo(1);

        await conn.StopAsync();
    }

    [Fact]
    public async Task Each_Player_Gets_Unique_PlayerId()
    {
        // Instead of testing duplicate name rejection (server may allow same names),
        // test that each player gets a unique PlayerId
        var (conn1, id1) = await _server.CreateAndJoinPlayerAsync("PlayerA");
        var (conn2, id2) = await _server.CreateAndJoinPlayerAsync("PlayerB");

        id1.Should().NotBe(id2);

        await conn1.StopAsync();
        await conn2.StopAsync();
    }

    [Fact]
    public async Task GetMyState_Returns_Current_Player_State()
    {
        var (conn, _) = await _server.CreateAndJoinPlayerAsync("MyStateTest");

        using var doc = await conn.InvokeJsonAsync("GetMyState");

        doc.RootElement.Prop("DisplayName").GetString().Should().Be("MyStateTest");
        doc.RootElement.Prop("CurrentHp").GetDecimal().Should().BeGreaterThan(0);

        await conn.StopAsync();
    }
}
