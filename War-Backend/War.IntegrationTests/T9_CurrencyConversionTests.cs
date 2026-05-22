using System.Text.Json;

namespace War.IntegrationTests;

/// <summary>
/// Escenario 9 · Conversión de moneda (cobre→plata, plata→oro) y Capilla de Economía.
///
/// Un jugador recién creado tiene Capilla en nivel 1 (el mínimo), que permite convertir hasta
/// 500 plata/día (= 250 000 cobre consumidos) y 40 oro/día (= 40 000 plata consumidas).
///
/// Estos tests validan que:
///   · GetChapelState devuelve la estructura esperada con los caps del nivel 1.
///   · GetWallet y GetConversionQuotas funcionan.
///   · Convertir sin saldo falla con error claro.
///   · Las cuotas se respetan.
/// </summary>
public class T9_CurrencyConversionTests : IClassFixture<WarTestServer>
{
    private readonly WarTestServer _server;

    public T9_CurrencyConversionTests(WarTestServer server) => _server = server;

    [Fact]
    public async Task GetChapelState_Returns_Level1_Defaults()
    {
        var (conn, _) = await _server.CreateAndJoinPlayerAsync("ChapelTester");

        using var doc = await conn.InvokeJsonAsync("GetChapelState");
        var root = doc.RootElement;

        root.Prop("Level").GetInt32().Should().Be(1);
        root.Prop("MaxLevel").GetInt32().Should().Be(10);
        root.Prop("SilverConvDaily").GetInt64().Should().Be(500);
        root.Prop("GoldConvDaily").GetInt64().Should().Be(40);

        var caps = root.Prop("PossessionCaps");
        caps.Prop("Copper").GetInt64().Should().Be(1_000_000);
        caps.Prop("Silver").GetInt64().Should().Be(10_000);
        caps.Prop("Gold").GetInt64().Should().Be(1_000);

        await conn.StopAsync();
    }

    [Fact]
    public async Task GetConversionQuotas_Initially_Zero_Usage()
    {
        var (conn, _) = await _server.CreateAndJoinPlayerAsync("QuotaTester");

        using var doc = await conn.InvokeJsonAsync("GetConversionQuotas");
        var root = doc.RootElement;

        root.Prop("SilverUsedToday").GetInt64().Should().Be(0);
        root.Prop("GoldUsedToday").GetInt64().Should().Be(0);
        root.Prop("SilverLimitDaily").GetInt64().Should().Be(500);
        root.Prop("GoldLimitDaily").GetInt64().Should().Be(40);

        await conn.StopAsync();
    }

    [Fact]
    public async Task ConvertCurrency_Without_Copper_Fails_With_Error()
    {
        var (conn, _) = await _server.CreateAndJoinPlayerAsync("BrokeTester");

        var errorTcs = new TaskCompletionSource<string>();
        conn.On<string>("Error", msg => errorTcs.TrySetResult(msg));

        // Intenta crear 10 plata → necesitaría 5 000 cobre y el jugador no tiene nada.
        await conn.InvokeCoreAsync("ConvertCurrency", new object[] { "silver", 10L });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        cts.Token.Register(() => errorTcs.TrySetCanceled());
        var err = await errorTcs.Task;
        err.Should().Contain("rechazada");

        await conn.StopAsync();
    }

    [Fact]
    public async Task GetWallet_Initially_Zero_Balances()
    {
        var (conn, _) = await _server.CreateAndJoinPlayerAsync("WalletTester");

        using var doc = await conn.InvokeJsonAsync("GetWallet");
        var root = doc.RootElement;

        root.Prop("Copper").GetInt64().Should().Be(0);
        root.Prop("Silver").GetInt64().Should().Be(0);
        root.Prop("Gold").GetInt64().Should().Be(0);

        await conn.StopAsync();
    }
}
