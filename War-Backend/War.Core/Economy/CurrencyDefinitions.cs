namespace War.Core.Economy;

/// <summary>
/// Parámetros rectores del sistema de moneda.
/// </summary>
/// <remarks>
/// IMPORTANTE: los CAPS de posesión por moneda (Cobre/Plata/Oro/Energía) ya NO viven aquí —
/// los define dinámicamente la Capilla de Economía del jugador. Ver <c>EconomyChapelRules</c>.
///
/// La energía NO tiene regeneración temporal: solo se obtiene por meditación (mecánica futura)
/// y por drops/recompensas.
///
/// Las tasas de conversión sí viven aquí porque son globales (no dependen de la Capilla, solo
/// los límites de conversión lo hacen).
/// </remarks>
public static class CurrencyDefinitions
{
    // ── Tasas de conversión (crafteo de moneda) ──────────────────────────────
    // Unidireccionales hacia arriba. El oro NO se puede deshacer a plata ni la plata a cobre.

    /// <summary>Cobre necesario para fabricar 1 plata. (El ratio es "costoso" a propósito).</summary>
    public const long CopperPerSilverConversion = 500;

    /// <summary>Plata necesaria para fabricar 1 oro.</summary>
    public const long SilverPerGoldConversion = 1_000;

    // ── Cap absoluto de seguridad (para prevenir overflow) ───────────────────
    // Muy por encima del cap real que aplica la Capilla. Solo sirve como red de protección
    // ante bugs/exploits que intentaran bypassear la Capilla.

    public const long AbsoluteMaxCopper = 10_000_000_000;     // 10 000 millones
    public const long AbsoluteMaxSilver = 100_000_000;         // 100 millones
    public const long AbsoluteMaxGold   = 5_000_000;           // 5 millones
    public const long AbsoluteMaxEnergy = 100_000_000;         // 100 millones
}
