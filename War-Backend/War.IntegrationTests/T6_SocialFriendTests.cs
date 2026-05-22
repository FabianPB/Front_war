using System.Text.Json;

namespace War.IntegrationTests;

/// <summary>
/// Scenario 6: Social system — friend requests, accept/reject, friend list.
/// </summary>
public class T6_SocialFriendTests : IClassFixture<WarTestServer>
{
    private readonly WarTestServer _server;

    public T6_SocialFriendTests(WarTestServer server) => _server = server;

    [Fact]
    public async Task SendFriendRequest_Notifies_Sender()
    {
        var (sender, senderId) = await _server.CreateAndJoinPlayerAsync("FriendSender");
        var (receiver, receiverId) = await _server.CreateAndJoinPlayerAsync("FriendReceiver");

        // Move close together
        await sender.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);
        await receiver.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);

        // Sender gets "FriendRequestSent" notification
        var notifTcs = new TaskCompletionSource<JsonDocument>();
        sender.On<object>("SocialNotification", n =>
        {
            var json = JsonSerializer.Serialize(n);
            notifTcs.TrySetResult(JsonDocument.Parse(json));
        });

        await sender.InvokeCoreAsync("SendFriendRequest", new object[] { receiverId });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => notifTcs.TrySetCanceled());
        using var notif = await notifTcs.Task;

        notif.RootElement.Prop("Type").GetString().Should().Be("FriendRequestSent");

        await sender.StopAsync();
        await receiver.StopAsync();
    }

    [Fact]
    public async Task AcceptFriendRequest_Adds_To_FriendList()
    {
        var (sender, senderId) = await _server.CreateAndJoinPlayerAsync("FriendAcceptSender");
        var (receiver, receiverId) = await _server.CreateAndJoinPlayerAsync("FriendAcceptReceiver");

        await sender.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);
        await receiver.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);

        // Send friend request
        await sender.InvokeCoreAsync("SendFriendRequest", new object[] { receiverId });
        await Task.Delay(500);

        // Get pending requests on receiver to find the requestId
        var pending = await receiver.InvokeAsync<object>("GetPendingFriendRequests");
        var pendingJson = JsonSerializer.Serialize(pending);
        using var pendingDoc = JsonDocument.Parse(pendingJson);

        var requests = pendingDoc.RootElement.EnumerateArray().ToList();
        requests.Should().NotBeEmpty("receiver should have pending friend requests");

        var requestId = requests[0].Prop("RequestId").GetString()!;

        // Accept the request
        await receiver.InvokeCoreAsync("RespondFriendRequest", new object[] { requestId, true });
        await Task.Delay(300);

        // Check friend list
        var friends = await receiver.InvokeAsync<object>("GetFriendList");
        var friendsJson = JsonSerializer.Serialize(friends);
        friendsJson.Should().Contain("FriendAcceptSender");

        await sender.StopAsync();
        await receiver.StopAsync();
    }

    [Fact]
    public async Task SelfFriendRequest_Is_Rejected()
    {
        var (conn, playerId) = await _server.CreateAndJoinPlayerAsync("SelfFriendTest");

        var errorTcs = new TaskCompletionSource<string>();
        conn.On<string>("SocialError", msg => errorTcs.TrySetResult(msg));

        await conn.InvokeCoreAsync("SendFriendRequest", new object[] { playerId });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        cts.Token.Register(() => errorTcs.TrySetCanceled());
        var error = await errorTcs.Task;
        error.Should().NotBeNullOrEmpty();

        await conn.StopAsync();
    }
}
