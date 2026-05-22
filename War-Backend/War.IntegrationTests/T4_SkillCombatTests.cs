using System.Text.Json;

namespace War.IntegrationTests;

/// <summary>
/// Scenario 4: Skill usage — mana cost, cooldowns, effects.
/// UseSkill is void — it sends CombatResult and PlayerStateUpdate via events.
/// </summary>
public class T4_SkillCombatTests : IClassFixture<WarTestServer>
{
    private readonly WarTestServer _server;

    public T4_SkillCombatTests(WarTestServer server) => _server = server;

    [Fact]
    public async Task UseSkill_Sends_CombatResult_And_Costs_Mana()
    {
        var (attacker, attackerId) = await _server.CreateAndJoinPlayerAsync("SkillUser", "Sorcerer");
        var (target, targetId) = await _server.CreateAndJoinPlayerAsync("SkillTarget", "Bruiser");

        await attacker.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);
        await target.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);

        using var beforeState = await attacker.InvokeJsonAsync("GetMyState");
        var initialMana = beforeState.RootElement.Prop("CurrentMana").GetDecimal();

        // Listen for CombatResult
        var combatTcs = new TaskCompletionSource<JsonDocument>();
        attacker.On<object>("CombatResult", result =>
        {
            var json = JsonSerializer.Serialize(result);
            combatTcs.TrySetResult(JsonDocument.Parse(json));
        });

        // UseSkill is void
        await attacker.InvokeCoreAsync("UseSkill", new object[] { 0, targetId });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => combatTcs.TrySetCanceled());
        using var doc = await combatTcs.Task;

        doc.RootElement.Prop("ActionType").GetString().Should().Be("Skill");
        doc.RootElement.Prop("ActionName").GetString().Should().NotBeNullOrEmpty();

        // Wait for PlayerStateUpdate to have been applied
        await Task.Delay(200);

        using var afterState = await attacker.InvokeJsonAsync("GetMyState");
        var afterMana = afterState.RootElement.Prop("CurrentMana").GetDecimal();
        afterMana.Should().BeLessThan(initialMana);

        await attacker.StopAsync();
        await target.StopAsync();
    }

    [Fact]
    public async Task UseSkill_On_Cooldown_Sends_Error()
    {
        var (attacker, _) = await _server.CreateAndJoinPlayerAsync("CooldownTest", "Sorcerer");
        var (target, targetId) = await _server.CreateAndJoinPlayerAsync("CooldownTarget", "Bruiser");

        await attacker.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);
        await target.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);

        // First use
        await attacker.InvokeCoreAsync("UseSkill", new object[] { 0, targetId });
        await Task.Delay(200);

        // Second use immediately — should get CombatResult with "Blocked" outcome (cooldown)
        var combatTcs = new TaskCompletionSource<JsonDocument>();
        attacker.On<object>("CombatResult", result =>
        {
            var json = JsonSerializer.Serialize(result);
            combatTcs.TrySetResult(JsonDocument.Parse(json));
        });

        await attacker.InvokeCoreAsync("UseSkill", new object[] { 0, targetId });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => combatTcs.TrySetCanceled());
        using var doc = await combatTcs.Task;

        // On cooldown should give "Blocked" outcome
        doc.RootElement.Prop("Outcome").GetString().Should().Be("Blocked");

        await attacker.StopAsync();
        await target.StopAsync();
    }

    [Fact]
    public async Task UseSkill_Invalid_Index_Sends_Error()
    {
        var (attacker, _) = await _server.CreateAndJoinPlayerAsync("BadSkillIdx", "Sorcerer");
        var (target, targetId) = await _server.CreateAndJoinPlayerAsync("BadSkillTarget", "Bruiser");

        await attacker.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);
        await target.InvokeCoreAsync("MoveTo", new object[] { 50.0f, 50.0f });
        await Task.Delay(250);

        var errorTcs = new TaskCompletionSource<string>();
        attacker.On<string>("Error", msg => errorTcs.TrySetResult(msg));

        await attacker.InvokeCoreAsync("UseSkill", new object[] { 99, targetId });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        cts.Token.Register(() => errorTcs.TrySetCanceled());
        var error = await errorTcs.Task;
        error.Should().NotBeNullOrEmpty();

        await attacker.StopAsync();
        await target.StopAsync();
    }

}
