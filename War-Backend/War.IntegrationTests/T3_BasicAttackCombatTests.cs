using System.Text.Json;

namespace War.IntegrationTests;

/// <summary>
/// Scenario 3: Basic attack combat — hit/miss, damage, target HP reduction.
/// </summary>
public class T3_BasicAttackCombatTests : IClassFixture<WarTestServer>
{
    private readonly WarTestServer _server;

    public T3_BasicAttackCombatTests(WarTestServer server) => _server = server;

    [Fact]
    public async Task BasicAttack_Reduces_Target_HP_Or_Misses()
    {
        var (attacker, attackerId) = await _server.CreateAndJoinPlayerAsync("Attacker_BA");
        var (target, targetId) = await _server.CreateAndJoinPlayerAsync("Target_BA");

        // Move both to same position (within combat range of 20 units)
        await attacker.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);
        await target.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);

        // Listen for CombatResult event (BasicAttack is void, sends events)
        var combatTcs = new TaskCompletionSource<JsonDocument>();
        attacker.On<object>("CombatResult", result =>
        {
            var json = JsonSerializer.Serialize(result);
            combatTcs.TrySetResult(JsonDocument.Parse(json));
        });

        // BasicAttack is a void method — no return value
        await attacker.InvokeCoreAsync("BasicAttack", new object[] { targetId });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => combatTcs.TrySetCanceled());
        using var doc = await combatTcs.Task;

        doc.RootElement.Prop("AttackerPlayerId").GetString().Should().Be(attackerId);
        doc.RootElement.Prop("TargetPlayerId").GetString().Should().Be(targetId);
        doc.RootElement.Prop("ActionType").GetString().Should().Be("BasicAttack");

        var outcome = doc.RootElement.Prop("Outcome").GetString();
        outcome.Should().BeOneOf("Hit", "Miss", "CriticalHit");

        if (outcome != "Miss")
        {
            doc.RootElement.Prop("Damage").GetDecimal().Should().BeGreaterThan(0);
        }

        await attacker.StopAsync();
        await target.StopAsync();
    }

    [Fact]
    public async Task BasicAttack_Out_Of_Range_Sends_Error()
    {
        var (attacker, _) = await _server.CreateAndJoinPlayerAsync("Attacker_OOR");
        var (target, targetId) = await _server.CreateAndJoinPlayerAsync("Target_OOR");

        // Move to opposite corners (distance > 20 units)
        await attacker.InvokeCoreAsync("MoveTo", new object[] { 0.0f, 0.0f });
        await Task.Delay(250);
        await target.InvokeCoreAsync("MoveTo", new object[] { 90.0f, 90.0f });
        await Task.Delay(250);

        // Server sends "Error" event instead of throwing
        var errorTcs = new TaskCompletionSource<string>();
        attacker.On<string>("Error", msg => errorTcs.TrySetResult(msg));

        await attacker.InvokeCoreAsync("BasicAttack", new object[] { targetId });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        cts.Token.Register(() => errorTcs.TrySetCanceled());
        var error = await errorTcs.Task;
        error.Should().Contain("rango");

        await attacker.StopAsync();
        await target.StopAsync();
    }

}
