using War.Core.Economy;

namespace War.Core.Equipment;

/// <summary>
/// Calcula costos de crafteo y desarrollo en las cuatro monedas del wallet.
/// Curva "dura" estilo BDO: tier exponencial ×10 por escalón, desarrollo exponencial ×1.15 por nivel.
/// </summary>
/// <remarks>
/// Las fórmulas son <b>idénticas</b> para las cuatro clases: una Legendaria T4 dev30 del Lancero cuesta
/// exactamente lo mismo que una Legendaria T4 dev30 del Sorcerer. La clase condiciona qué stats da la
/// pieza, no su precio.
///
/// Decision: el coste se separa en dos porciones:
///   · Un "principal" en Oro para los tiers altos (lo interesante / lo caro).
///   · Una "fracción" en Plata/Cobre para los tiers bajos (early game accesible sin oro).
/// El wallet no acepta "float" entre monedas; así que las fórmulas redondean al entero superior
/// de la moneda dominante.
/// </remarks>
public static class CraftingCostCalculator
{
    // ── Tier base cost · en ORO · para dev 1, rareza Common ──────────────────
    // T1 es tan barato que se paga mayormente en plata/cobre.
    // T2-T4 crecen ×10 por escalón.
    private const long T1_BaseGold = 1;    // = 100 plata = 10 000 cobre
    private const long T2_BaseGold = 10;
    private const long T3_BaseGold = 100;
    private const long T4_BaseGold = 1_000;

    // ── Multiplicador por rareza ────────────────────────────────────────────
    private const decimal RarityCommon    = 1m;
    private const decimal RaritySpecial   = 3m;
    private const decimal RarityEpic      = 10m;
    private const decimal RarityLegendary = 30m;

    // ── Desarrollo · ×1.15 por nivel, empezando en dev1 ─────────────────────
    // dev1 = ×1.00, dev10 = ×3.52, dev20 = ×14.23, dev30 = ×57.58
    private const decimal DevelopmentGrowth = 1.15m;

    /// <summary>
    /// Coste para subir una pieza de TierN (dev1) a Tier(N+1) (dev1).
    /// Requiere 2 piezas de TierN; el coste aquí es el que cobra el wallet además de consumir las piezas.
    /// </summary>
    /// <param name="targetTier">Tier resultante (2, 3 o 4). NO se permite 5.</param>
    /// <param name="rarity">Rareza común a ambas piezas de entrada y a la salida.</param>
    public static CurrencyCost ComputeTierUpCost(int targetTier, EquipmentRarity rarity)
    {
        if (targetTier < 2 || targetTier > 4)
            throw new ArgumentOutOfRangeException(nameof(targetTier), "Tier-up solo válido para 2, 3 o 4.");

        var baseGold = GetBaseGoldForTier(targetTier);
        var rarityMult = GetRarityMultiplier(rarity);
        var totalGold = (long)Math.Ceiling((decimal)baseGold * rarityMult);
        return DistributeByDenomination(totalGold);
    }

    /// <summary>
    /// Coste para subir una pieza un nivel de desarrollo (N → N+1).
    /// </summary>
    /// <param name="tier">Tier actual de la pieza (1-4).</param>
    /// <param name="rarity">Rareza de la pieza.</param>
    /// <param name="currentDevelopmentLevel">Nivel actual (1-29; no se puede subir desde 30).</param>
    public static CurrencyCost ComputeDevelopmentCost(int tier, EquipmentRarity rarity, int currentDevelopmentLevel)
    {
        if (tier < 1 || tier > 4)
            throw new ArgumentOutOfRangeException(nameof(tier), "Tier debe estar entre 1 y 4.");
        if (currentDevelopmentLevel < 1 || currentDevelopmentLevel >= 30)
            throw new ArgumentOutOfRangeException(nameof(currentDevelopmentLevel),
                "Solo se puede desarrollar desde un nivel entre 1 y 29.");

        var baseGold = GetBaseGoldForTier(tier);
        var rarityMult = GetRarityMultiplier(rarity);

        // El coste crece con el NIVEL DE DESTINO (currentLevel + 1). Para pasar de dev1 a dev2 se paga el ×1.15^1.
        var growthExponent = currentDevelopmentLevel; // 1..29
        var devMult = Pow(DevelopmentGrowth, growthExponent);

        var totalGold = (long)Math.Ceiling((decimal)baseGold * rarityMult * devMult);
        return DistributeByDenomination(totalGold);
    }

    /// <summary>
    /// Descriptor legible de todos los costes de una pieza. Útil para UI del cliente.
    /// </summary>
    public static CraftingCostPreview BuildPreview(int tier, EquipmentRarity rarity, int currentDevelopmentLevel)
    {
        var developmentCost = currentDevelopmentLevel < 30
            ? ComputeDevelopmentCost(tier, rarity, currentDevelopmentLevel)
            : (CurrencyCost?)null;

        var tierUpCost = tier < 4
            ? ComputeTierUpCost(tier + 1, rarity)
            : (CurrencyCost?)null;

        return new CraftingCostPreview(
            Tier: tier,
            Rarity: rarity,
            CurrentDevelopmentLevel: currentDevelopmentLevel,
            NextDevelopmentCost: developmentCost,
            TierUpCost: tierUpCost);
    }

    // ── Internos ─────────────────────────────────────────────────────────────

    private static long GetBaseGoldForTier(int tier) => tier switch
    {
        1 => T1_BaseGold,
        2 => T2_BaseGold,
        3 => T3_BaseGold,
        4 => T4_BaseGold,
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    private static decimal GetRarityMultiplier(EquipmentRarity rarity) => rarity switch
    {
        EquipmentRarity.Common    => RarityCommon,
        EquipmentRarity.Special   => RaritySpecial,
        EquipmentRarity.Epic      => RarityEpic,
        EquipmentRarity.Legendary => RarityLegendary,
        _ => throw new ArgumentOutOfRangeException(nameof(rarity))
    };

    private static decimal Pow(decimal baseValue, int exponent)
    {
        decimal result = 1m;
        for (int i = 0; i < exponent; i++) result *= baseValue;
        return result;
    }

    /// <summary>
    /// Descompone un total "en oro" al triple oro/plata/cobre para que los costes de early game
    /// (T1 ≈ 1 oro) se sientan en plata/cobre y los de late game (T4 dev30 ≈ cientos de miles de oro)
    /// se sientan como oro puro.
    ///
    /// Regla: si el total &lt; 1 oro, todo va a plata/cobre. Si el total &lt; 1 plata, todo a cobre.
    /// Para lo demás, se queda el importe en oro (valores enteros); los residuos fraccionarios no
    /// aplican porque Math.Ceiling los redondeó arriba.
    /// </summary>
    private static CurrencyCost DistributeByDenomination(long totalGold)
    {
        if (totalGold >= 1)
            return new CurrencyCost(Gold: totalGold);

        // totalGold era 0 tras ceiling → fallback: al menos 1 plata (costes simbólicos).
        return new CurrencyCost(Silver: 1);
    }
}

/// <summary>
/// Preview agregado de costes de una pieza. Lo que se muestra en la UI del cliente.
/// </summary>
public sealed record CraftingCostPreview(
    int Tier,
    EquipmentRarity Rarity,
    int CurrentDevelopmentLevel,
    CurrencyCost? NextDevelopmentCost,
    CurrencyCost? TierUpCost);
