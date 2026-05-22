using System.Text.Json;

namespace War.IntegrationTests;

/// <summary>
/// Escenario 10 · Ascensión de habilidades.
///
/// Un jugador recién creado tiene nivel de ascensión 0 en todas sus skills (aunque el legacy
/// `AscensionLevel` global muestre otra cosa — ese es otro concepto).
///
/// Estos tests validan:
///   · GetSkillAscensionPreview devuelve el coste del primer paso con 1 libro común.
///   · Invocar AscendSkill sin libros/monedas devuelve error.
///   · El catálogo de skills del jugador existe y tiene al menos 1 skill.
/// </summary>
public class T10_SkillAscensionTests : IClassFixture<WarTestServer>
{
    private readonly WarTestServer _server;

    public T10_SkillAscensionTests(WarTestServer server) => _server = server;

    [Fact]
    public async Task GetSkillAscensionPreview_First_Step_Requires_1_Common_Book()
    {
        var (conn, _) = await _server.CreateAndJoinPlayerAsync("AscPreviewer", "Sorcerer");

        // Encontramos el ID de la primera skill.
        using var state = await conn.InvokeJsonAsync("GetMyState");
        var skills = state.RootElement.Prop("Skills");
        skills.GetArrayLength().Should().BeGreaterThan(0);
        var firstSkillId = skills[0].Prop("SkillId").GetString()!;

        using var doc = await conn.InvokeJsonAsync("GetSkillAscensionPreview", firstSkillId);
        var root = doc.RootElement;

        root.Prop("SkillId").GetString().Should().Be(firstSkillId);
        root.Prop("CurrentLevel").GetInt32().Should().Be(0);
        root.Prop("IsMaxed").GetBoolean().Should().BeFalse();
        root.Prop("NextStepBookCount").GetInt32().Should().Be(1);
        root.Prop("NextStepBookRarity").GetString().Should().Be("Common");

        await conn.StopAsync();
    }

    [Fact]
    public async Task AscendSkill_Without_Resources_Fails_With_Error()
    {
        var (conn, _) = await _server.CreateAndJoinPlayerAsync("AscendFailer", "Bruiser");

        using var state = await conn.InvokeJsonAsync("GetMyState");
        var skills = state.RootElement.Prop("Skills");
        var firstSkillId = skills[0].Prop("SkillId").GetString()!;

        var errorTcs = new TaskCompletionSource<string>();
        conn.On<string>("Error", msg => errorTcs.TrySetResult(msg));

        await conn.InvokeCoreAsync("AscendSkill", new object[] { firstSkillId });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        cts.Token.Register(() => errorTcs.TrySetCanceled());
        var err = await errorTcs.Task;
        err.Should().Contain("rechazada");

        await conn.StopAsync();
    }

    [Fact]
    public async Task GetSkillAscensionPreview_Unknown_Skill_Returns_Null()
    {
        var (conn, _) = await _server.CreateAndJoinPlayerAsync("UnknownSkillTester");

        var result = await conn.InvokeCoreAsync<object?>("GetSkillAscensionPreview",
            new object[] { "nonexistent.skill.foo" });
        result.Should().BeNull();

        await conn.StopAsync();
    }
}
