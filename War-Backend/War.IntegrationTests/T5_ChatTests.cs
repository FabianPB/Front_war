using System.Text.Json;

namespace War.IntegrationTests;

/// <summary>
/// Scenario 5: Chat messaging between nearby players.
/// ChatMessage event sends a single object with properties: PlayerId, DisplayName, ClassName, Message, Timestamp
/// </summary>
public class T5_ChatTests : IClassFixture<WarTestServer>
{
    private readonly WarTestServer _server;

    public T5_ChatTests(WarTestServer server) => _server = server;

    [Fact]
    public async Task SendChatMessage_Is_Received_By_Nearby_Player()
    {
        var (sender, _) = await _server.CreateAndJoinPlayerAsync("ChatSender");
        var (receiver, _) = await _server.CreateAndJoinPlayerAsync("ChatReceiver");

        // Move both together
        await sender.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);
        await receiver.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);

        // ChatMessage sends a SINGLE object, not separate params
        var receivedTcs = new TaskCompletionSource<JsonDocument>();
        receiver.On<object>("ChatMessage", payload =>
        {
            var json = JsonSerializer.Serialize(payload);
            receivedTcs.TrySetResult(JsonDocument.Parse(json));
        });

        await sender.InvokeCoreAsync("SendChatMessage", new object[] { "Hello World!" });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => receivedTcs.TrySetCanceled());

        using var received = await receivedTcs.Task;
        received.RootElement.Prop("DisplayName").GetString().Should().Be("ChatSender");
        received.RootElement.Prop("Message").GetString().Should().Be("Hello World!");

        await sender.StopAsync();
        await receiver.StopAsync();
    }

    [Fact]
    public async Task SendChatMessage_Is_Not_Received_By_Distant_Player()
    {
        var (sender, _) = await _server.CreateAndJoinPlayerAsync("FarSender");
        var (receiver, _) = await _server.CreateAndJoinPlayerAsync("FarReceiver");

        // Move far apart
        await sender.InvokeCoreAsync("MoveTo", new object[] { 0.0f, 0.0f });
        await Task.Delay(250);
        await receiver.InvokeCoreAsync("MoveTo", new object[] { 90.0f, 90.0f });
        await Task.Delay(250);

        var received = false;
        receiver.On<object>("ChatMessage", _ => { received = true; });

        await sender.InvokeCoreAsync("SendChatMessage", new object[] { "You shouldn't hear this" });

        await Task.Delay(500);
        received.Should().BeFalse("distant players should not receive chat messages");

        await sender.StopAsync();
        await receiver.StopAsync();
    }

    [Fact]
    public async Task SendChatMessage_Empty_Is_Silently_Ignored()
    {
        // Server returns silently for empty messages (no throw, no event)
        var (conn, _) = await _server.CreateAndJoinPlayerAsync("EmptyMsgTest");

        // Should complete without error
        await conn.InvokeCoreAsync("SendChatMessage", new object[] { "" });

        // If we get here, test passes — no crash
        await conn.StopAsync();
    }
}
