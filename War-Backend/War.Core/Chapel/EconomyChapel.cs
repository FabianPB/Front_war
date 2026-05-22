using War.Core.Economy;

namespace War.Core.Chapel;

/// <summary>
/// Estado de la Capilla de Economía de un jugador. La Capilla arranca en nivel 1 por defecto
/// (un personaje recién creado ya tiene acceso a los límites mínimos).
/// </summary>
/// <remarks>
/// Esta clase es la "tarjeta" de la Capilla. El servicio que la gestiona (PlayerChapelService)
/// mantiene el mapa PlayerId → EconomyChapel y lo persiste cuando haya BD.
///
/// Decision: Capilla parte en nivel 1 desde el nacimiento del personaje. El nivel 1 da los caps
/// y conversiones mínimas — no es un "upgrade", es la base. Los upgrades llevan del 1 al 10.
/// </remarks>
public sealed class EconomyChapel
{
    public Guid PlayerId { get; }

    /// <summary>Nivel actual de la Capilla (1..10).</summary>
    public int Level { get; private set; } = EconomyChapelRules.MinLevel;

    /// <summary>Timestamp del último upgrade exitoso (para auditoría).</summary>
    public DateTime? LastUpgradedAt { get; private set; }

    public EconomyChapel(Guid playerId)
    {
        PlayerId = playerId;
    }

    /// <summary>
    /// Cap de posesión actual para una moneda, según el nivel de Capilla.
    /// </summary>
    public long GetPossessionCap(CurrencyType currency) =>
        EconomyChapelRules.GetPossessionCap(Level, currency);

    /// <summary>Caps de posesión de las 4 monedas en el nivel actual.</summary>
    public ChapelPossessionCaps GetPossessionCaps() =>
        EconomyChapelRules.GetPossessionCaps(Level);

    /// <summary>
    /// Límites actuales de conversión (diario/semanal/mensual por tipo).
    /// </summary>
    public ChapelConversionLimits GetConversionLimits() =>
        EconomyChapelRules.GetConversionLimits(Level);

    /// <summary>
    /// Intenta subir el nivel en 1. Retorna true si se subió; false si ya está al tope
    /// o si el nivel del personaje no alcanza. La lógica de cobrar los recursos de upgrade
    /// la hace la capa de aplicación (PlayerChapelService); este método solo mueve el contador.
    /// </summary>
    public bool TryUpgrade(int characterLevel)
    {
        if (!EconomyChapelRules.CanUpgrade(Level, characterLevel))
            return false;

        Level++;
        LastUpgradedAt = DateTime.UtcNow;
        return true;
    }
}
