using System.Text.Json;

namespace War.IntegrationTests;

/// <summary>
/// Scenario 7: Blocking — block player, block list, interaction prevention.
/// </summary>
public class T7_SocialBlockTests : IClassFixture<WarTestServer>
{
    private readonly WarTestServer _server;

    public T7_SocialBlockTests(WarTestServer server) => _server = server;

    [Fact]
    public async Task BlockPlayer_Adds_To_BlockList()
    {
        var (blocker, blockerId) = await _server.CreateAndJoinPlayerAsync("Blocker1");
        var (target, targetId) = await _server.CreateAndJoinPlayerAsync("Blocked1");

        await blocker.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);
        await target.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);

        await blocker.InvokeCoreAsync("BlockPlayer", new object[] { targetId });
        await Task.Delay(200);

        var blockList = await blocker.InvokeAsync<object>("GetBlockList");
        var json = JsonSerializer.Serialize(blockList);
        json.Should().Contain("Blocked1");

        await blocker.StopAsync();
        await target.StopAsync();
    }

    [Fact]
    public async Task BlockedPlayer_Cannot_Send_FriendRequest()
    {
        var (playerA, playerAId) = await _server.CreateAndJoinPlayerAsync("BlockerA");
        var (playerB, playerBId) = await _server.CreateAndJoinPlayerAsync("BlockedB");

        await playerA.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);
        await playerB.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);

        await playerA.InvokeCoreAsync("BlockPlayer", new object[] { playerBId });
        await Task.Delay(200);

        var errorTcs = new TaskCompletionSource<string>();
        playerB.On<string>("SocialError", msg => errorTcs.TrySetResult(msg));

        await playerB.InvokeCoreAsync("SendFriendRequest", new object[] { playerAId });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        cts.Token.Register(() => errorTcs.TrySetCanceled());
        var error = await errorTcs.Task;
        error.Should().NotBeNullOrEmpty();

        await playerA.StopAsync();
        await playerB.StopAsync();
    }

    [Fact]
    public async Task UnblockPlayer_Removes_From_BlockList()
    {
        var (blocker, _) = await _server.CreateAndJoinPlayerAsync("UnblockTest");
        var (target, targetId) = await _server.CreateAndJoinPlayerAsync("UnblockTarget");

        await blocker.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);
        await target.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);

        await blocker.InvokeCoreAsync("BlockPlayer", new object[] { targetId });
        await Task.Delay(200);

        await blocker.InvokeCoreAsync("UnblockPlayer", new object[] { targetId });
        await Task.Delay(200);

        var blockList = await blocker.InvokeAsync<object>("GetBlockList");
        var json = JsonSerializer.Serialize(blockList);
        json.Should().NotContain("UnblockTarget");

        await blocker.StopAsync();
        await target.StopAsync();
    }
}
