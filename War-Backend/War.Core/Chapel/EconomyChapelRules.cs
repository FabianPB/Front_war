using War.Core.Economy;

namespace War.Core.Chapel;

/// <summary>
/// Reglas rectoras de la Capilla de Economía. Inmutables, vivas en el Core.
/// </summary>
/// <remarks>
/// La Capilla tiene 10 niveles. Cada uno se desbloquea a un nivel de personaje específico
/// (múltiplos de 8, desde pj-8 hasta pj-80). Todos los límites del wallet — tanto los de posesión
/// como los de conversión — se leen de esta tabla según el nivel actual de Capilla del jugador.
///
/// Si un jugador tiene la Capilla en nivel N, sus límites son los de la fila N. No hay interpolación
/// entre niveles — el salto es discreto cuando el jugador sube la Capilla.
///
/// Todos los valores de esta clase están capados al <c>CurrencyDefinitions.AbsoluteMax*</c> como
/// red de seguridad (no deberían alcanzarlo nunca, pero el wallet valida igualmente).
/// </remarks>
public static class EconomyChapelRules
{
    public const int MinLevel = 1;
    public const int MaxLevel = 10;

    /// <summary>Niveles de personaje necesarios para desbloquear cada nivel de Capilla.</summary>
    public const int CharacterLevelsPerChapelLevel = 8;

    public static int CharacterLevelRequiredFor(int chapelLevel)
    {
        if (chapelLevel < MinLevel || chapelLevel > MaxLevel)
            throw new ArgumentOutOfRangeException(nameof(chapelLevel));
        return chapelLevel * CharacterLevelsPerChapelLevel;
    }

    // ── Tabla A · Límites de POSESIÓN (wallet cap) ───────────────────────────
    // Índice 0 = nivel 1, índice 9 = nivel 10.

    private static readonly long[] CopperPossessionCap =
    {
        1_000_000,      // L1
        2_200_000,      // L2
        4_700_000,      // L3
        10_000_000,     // L4
        22_000_000,     // L5
        47_000_000,     // L6
        100_000_000,    // L7
        220_000_000,    // L8
        470_000_000,    // L9
        1_000_000_000,  // L10
    };

    private static readonly long[] SilverPossessionCap =
    {
        10_000,
        22_000,
        47_000,
        100_000,
        215_000,
        460_000,
        1_000_000,
        2_200_000,
        4_700_000,
        10_000_000,
    };

    private static readonly long[] GoldPossessionCap =
    {
        1_000,
        2_000,
        4_000,
        8_000,
        17_000,
        35_000,
        70_000,
        140_000,
        280_000,
        500_000,
    };

    // La energía también tiene cap escalable — la meditación es la fuente principal, así que
    // para ascender skills altos hace falta tener una Capilla decente.
    // Empieza en 10 000 y crece proporcional a las demás monedas.
    private static readonly long[] EnergyPossessionCap =
    {
        10_000,
        25_000,
        60_000,
        150_000,
        350_000,
        800_000,
        1_800_000,
        4_000_000,
        8_500_000,
        20_000_000,
    };

    // ── Tabla B · Límites de CONVERSIÓN por ventana (no acumulables) ────────
    // Plata creada (consumiendo cobre) al ratio 500:1.

    private static readonly long[] SilverConvPerDay =
    { 500, 1_200, 2_800, 5_500, 10_000, 16_000, 24_000, 35_000, 46_000, 60_000 };

    private static readonly long[] SilverConvPerWeek =
    { 1_800, 4_200, 9_800, 19_000, 35_000, 55_000, 85_000, 125_000, 160_000, 210_000 };

    private static readonly long[] SilverConvPerMonth =
    { 4_500, 10_500, 24_500, 49_000, 90_000, 145_000, 215_000, 315_000, 415_000, 540_000 };

    // Oro creado (consumiendo plata) al ratio 1 000:1.
    private static readonly long[] GoldConvPerDay =
    { 40, 100, 220, 450, 850, 1_400, 2_100, 3_000, 4_000, 5_000 };

    private static readonly long[] GoldConvPerWeek =
    { 140, 350, 770, 1_500, 3_000, 5_000, 7_500, 10_500, 14_000, 17_500 };

    private static readonly long[] GoldConvPerMonth =
    { 360, 900, 2_000, 4_000, 7_500, 12_500, 19_000, 27_000, 36_000, 45_000 };

    // ── Queries ─────────────────────────────────────────────────────────────

    public static long GetPossessionCap(int chapelLevel, CurrencyType currency)
    {
        var idx = ValidateAndIndex(chapelLevel);
        return currency switch
        {
            CurrencyType.Copper => CopperPossessionCap[idx],
            CurrencyType.Silver => SilverPossessionCap[idx],
            CurrencyType.Gold   => GoldPossessionCap[idx],
            CurrencyType.Energy => EnergyPossessionCap[idx],
            _ => throw new ArgumentOutOfRangeException(nameof(currency))
        };
    }

    public static ChapelConversionLimits GetConversionLimits(int chapelLevel)
    {
        var idx = ValidateAndIndex(chapelLevel);
        return new ChapelConversionLimits(
            ChapelLevel: chapelLevel,
            SilverDaily: SilverConvPerDay[idx],
            SilverWeekly: SilverConvPerWeek[idx],
            SilverMonthly: SilverConvPerMonth[idx],
            GoldDaily: GoldConvPerDay[idx],
            GoldWeekly: GoldConvPerWeek[idx],
            GoldMonthly: GoldConvPerMonth[idx]);
    }

    public static ChapelPossessionCaps GetPossessionCaps(int chapelLevel)
    {
        var idx = ValidateAndIndex(chapelLevel);
        return new ChapelPossessionCaps(
            ChapelLevel: chapelLevel,
            Copper: CopperPossessionCap[idx],
            Silver: SilverPossessionCap[idx],
            Gold:   GoldPossessionCap[idx],
            Energy: EnergyPossessionCap[idx]);
    }

    /// <summary>
    /// ¿Puede el jugador con este nivel de personaje subir la Capilla al siguiente nivel?
    /// </summary>
    public static bool CanUpgrade(int currentChapelLevel, int characterLevel) =>
        currentChapelLevel < MaxLevel &&
        characterLevel >= CharacterLevelRequiredFor(currentChapelLevel + 1);

    private static int ValidateAndIndex(int chapelLevel)
    {
        if (chapelLevel < MinLevel || chapelLevel > MaxLevel)
            throw new ArgumentOutOfRangeException(nameof(chapelLevel),
                $"Nivel de Capilla debe estar entre {MinLevel} y {MaxLevel}.");
        return chapelLevel - 1;
    }
}

/// <summary>Límites de conversión por ventana temporal (no acumulables).</summary>
public sealed record ChapelConversionLimits(
    int ChapelLevel,
    long SilverDaily,
    long SilverWeekly,
    long SilverMonthly,
    long GoldDaily,
    long GoldWeekly,
    long GoldMonthly);

/// <summary>Caps de posesión por moneda.</summary>
public sealed record ChapelPossessionCaps(
    int ChapelLevel,
    long Copper,
    long Silver,
    long Gold,
    long Energy);
