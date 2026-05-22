namespace War.IntegrationTests;

/// <summary>
/// Scenario 2: Movement, position sync, and world boundaries.
/// </summary>
public class T2_MovementTests : IClassFixture<WarTestServer>
{
    private readonly WarTestServer _server;

    public T2_MovementTests(WarTestServer server) => _server = server;

    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("left")]
    [InlineData("right")]
    public async Task Move_In_Cardinal_Direction_Updates_Position(string direction)
    {
        var (conn, _) = await _server.CreateAndJoinPlayerAsync($"Mover_{direction}");

        using var initial = await conn.InvokeJsonAsync("GetMyState");
        var startX = initial.RootElement.Prop("X").GetSingle();
        var startY = initial.RootElement.Prop("Y").GetSingle();

        await conn.InvokeCoreAsync("Move", new object[] { direction });
        await Task.Delay(250);

        using var after = await conn.InvokeJsonAsync("GetMyState");
        var endX = after.RootElement.Prop("X").GetSingle();
        var endY = after.RootElement.Prop("Y").GetSingle();

        var moved = (Math.Abs(endX - startX) > 0.01f) || (Math.Abs(endY - startY) > 0.01f);
        moved.Should().BeTrue($"position should change after moving {direction}");

        await conn.StopAsync();
    }

    [Fact]
    public async Task MoveTo_Updates_Position_To_Target()
    {
        var (conn, _) = await _server.CreateAndJoinPlayerAsync("MoveToTest");

        await conn.InvokeCoreAsync("MoveTo", new object[] { 25.0f, 30.0f });
        await Task.Delay(250);

        using var after = await conn.InvokeJsonAsync("GetMyState");
        var x = after.RootElement.Prop("X").GetSingle();
        var y = after.RootElement.Prop("Y").GetSingle();

        x.Should().BeApproximately(25.0f, 0.5f);
        y.Should().BeApproximately(30.0f, 0.5f);

        await conn.StopAsync();
    }

    [Fact]
    public async Task Position_Stays_Within_World_Bounds()
    {
        var (conn, _) = await _server.CreateAndJoinPlayerAsync("BoundsTest");

        await conn.InvokeCoreAsync("MoveTo", new object[] { 999.0f, 999.0f });
        await Task.Delay(250);

        using var state = await conn.InvokeJsonAsync("GetMyState");
        var x = state.RootElement.Prop("X").GetSingle();
        var y = state.RootElement.Prop("Y").GetSingle();

        x.Should().BeLessThanOrEqualTo(99f);
        y.Should().BeLessThanOrEqualTo(99f);

        await conn.StopAsync();
    }
}
