using System.Collections.Concurrent;
using War.Core.Chapel;
using War.Core.Economy;

namespace War.Api.Application.Economy;

/// <summary>
/// Gestiona las Capillas de Economía de todos los jugadores. Singleton in-memory por ahora;
/// cuando haya BD, se migra a una implementación persistida con la misma API.
/// </summary>
/// <remarks>
/// También implementa <see cref="IWalletCapProvider"/> para que el wallet pueda consultarlo
/// directamente y obtener los caps dinámicos sin conocer la Capilla.
/// </remarks>
public sealed class PlayerChapelService : IWalletCapProvider
{
    private readonly ConcurrentDictionary<Guid, EconomyChapel> _chapels = new();

    public EconomyChapel GetOrCreate(Guid playerId) =>
        _chapels.GetOrAdd(playerId, id => new EconomyChapel(id));

    public int GetLevel(Guid playerId) => GetOrCreate(playerId).Level;

    public ChapelPossessionCaps GetPossessionCaps(Guid playerId) =>
        EconomyChapelRules.GetPossessionCaps(GetOrCreate(playerId).Level);

    public ChapelConversionLimits GetConversionLimits(Guid playerId) =>
        GetOrCreate(playerId).GetConversionLimits();

    // ── IWalletCapProvider ─────────────────────────────────────────────────

    public long GetPossessionCap(Guid playerId, CurrencyType currency) =>
        GetOrCreate(playerId).GetPossessionCap(currency);

    // ── Upgrade ───────────────────────────────────────────────────────────

    /// <summary>
    /// Intenta subir la Capilla del jugador un nivel. La capa que llama es responsable de
    /// haber validado/cobrado los recursos del upgrade (sistema aún por definir). Este método
    /// solo valida el prerequisito de nivel de personaje.
    /// </summary>
    public ChapelUpgradeResult TryUpgrade(Guid playerId, int characterLevel)
    {
        var chapel = GetOrCreate(playerId);
        var previousLevel = chapel.Level;

        if (previousLevel >= EconomyChapelRules.MaxLevel)
            return ChapelUpgradeResult.Fail("MAX_LEVEL", "La Capilla ya está al máximo nivel.");

        var required = EconomyChapelRules.CharacterLevelRequiredFor(previousLevel + 1);
        if (characterLevel < required)
            return ChapelUpgradeResult.Fail("CHAR_LEVEL_TOO_LOW",
                $"Requiere nivel de personaje {required} (actual: {characterLevel}).");

        var ok = chapel.TryUpgrade(characterLevel);
        return ok
            ? ChapelUpgradeResult.Ok(previousLevel, chapel.Level)
            : ChapelUpgradeResult.Fail("UNKNOWN", "No se pudo subir la Capilla.");
    }
}

public sealed record ChapelUpgradeResult(
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    int PreviousLevel,
    int NewLevel)
{
    public static ChapelUpgradeResult Ok(int prev, int @new) =>
        new(true, null, null, prev, @new);

    public static ChapelUpgradeResult Fail(string code, string msg) =>
        new(false, code, msg, 0, 0);
}
