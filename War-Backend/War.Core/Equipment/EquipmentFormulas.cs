namespace War.Core.Equipment;

/// <summary>
/// Fórmulas centralizadas del sistema de equipamiento.
///
/// ═══════════════════════════════════════════════════════════════
/// FÓRMULA PRINCIPAL
/// ═══════════════════════════════════════════════════════════════
///
///   StatFinal = StatBase × TierMultiplier(tier) × DevelopmentMultiplier(dev)
///
/// Donde:
///   StatBase     = valor del catálogo (Tier 1, Desarrollo 1, ya incluye el rango)
///   Tier         = 1-4 (crafteo 2×anterior → siguiente)
///   Desarrollo   = 1-30 (mejora progresiva con rendimientos decrecientes)
///
/// ═══════════════════════════════════════════════════════════════
/// MODELO DE EQUIVALENCIA
/// ═══════════════════════════════════════════════════════════════
///
/// El tier superior NO aplasta al inferior. La relación es de EFICIENCIA:
///
///   · A mismo nivel de desarrollo, tier superior SIEMPRE da más.
///     (T4 dev5 > T3 dev5 > T2 dev5 > T1 dev5)
///
///   · Un tier inferior con desarrollo alto puede IGUALAR o SUPERAR
///     a un tier superior con desarrollo bajo.
///     (T1 dev30 ≈ T3 dev5, T1 dev30 > T4 dev1)
///
///   · La ventaja del tier superior es que avanza más rápido:
///     cada punto de desarrollo rinde más en valor absoluto.
///     Un T4 a dev10 ya supera a un T1 a dev30.
///
/// Tabla de referencia (multiplicadores efectivos):
///
///         dev1    dev5    dev10   dev15   dev20   dev30
///   T1    1.00    2.05    2.50    2.76    2.95    3.21
///   T2    1.25    2.56    3.12    3.45    3.69    4.01
///   T3    1.55    3.18    3.87    4.28    4.57    4.97
///   T4    1.90    3.89    4.74    5.25    5.61    6.10
///
/// ═══════════════════════════════════════════════════════════════
/// RESTRICCIONES VERIFICADAS
/// ═══════════════════════════════════════════════════════════════
///
///   1. A mismo dev, tier superior > tier inferior (siempre)
///   2. T1 dev30 (3.21) > T4 dev1 (1.90) — desarrollo largo vale
///   3. T1 dev30 (3.21) ≈ T3 dev5 (3.18) — equivalencia cruzada
///   4. Rango siguiente T1 > Rango actual T4 dev30 (salto de rango)
///   5. Crafteo 2→1 resetea desarrollo a 1 (decisión estratégica)
///
/// </summary>
public static class EquipmentFormulas
{
    // ── Tier multipliers (suaves, no aplastantes) ──
    // Crafteo: 2 × Tier N → 1 × Tier (N+1) en desarrollo 1
    // Progresión suave: 1.0 → 1.25 → 1.55 → 1.90
    // La ventaja de tier es eficiencia por desarrollo, no base aplastante.
    private static readonly decimal[] TierMultipliers = { 0m, 1.0m, 1.25m, 1.55m, 1.90m };

    public const int MinTier = 1;
    public const int MaxTier = 4;

    // ── Desarrollo ──
    // Fórmula: 1 + 0.65 × ln(n)
    // Más agresivo que antes para que dev30 tenga peso real.
    // Rendimientos decrecientes: dev1=1.00, dev5=2.05, dev15=2.76, dev30=3.21
    public const int MinDevelopment = 1;
    public const int MaxDevelopment = 30;
    private const decimal DevelopmentLogCoefficient = 0.65m;

    // ── Rango multipliers (ya incorporados en los StatBase del catálogo) ──
    // Se usan para generar los StatBase de rangos superiores a partir del Común.
    // T4 dev30 de un rango da multiplicador efectivo ~6.10. El siguiente rango
    // T1 dev1 debe superar eso notablemente.
    // Común=1.0, Especial=8.0, Épico=60.0, Legendario=450.0
    // Verificación: Especial T1 (8.0) > Común T4 dev30 (1.0 × 6.10 = 6.10) ✓
    //               Épico T1 (60.0) > Especial T4 dev30 (8.0 × 6.10 = 48.8) ✓
    //               Legendario T1 (450.0) > Épico T4 dev30 (60.0 × 6.10 = 366.0) ✓
    private static readonly decimal[] RarityMultipliers = { 1.0m, 8.0m, 60.0m, 450.0m };

    // ── Legendario: factor de combinación ──
    // Un legendario otorga ambas variantes al 80% de la especialización individual.
    public const decimal LegendaryHybridFactor = 0.80m;

    // ── Crafteo ──
    public const int CraftInputCount = 2; // se necesitan 2 del tier anterior

    // ══════════════════════════════════════════════════════════════
    // CÁLCULOS
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula el valor final de un stat de una pieza de equipo.
    ///
    ///   StatFinal = baseValue × TierMult(tier) × DevMult(dev)
    ///
    /// Donde baseValue ya incluye el multiplicador de rango (del catálogo).
    /// </summary>
    public static decimal CalculateStatValue(decimal baseValue, int tier, int developmentLevel)
    {
        var tierMult = GetTierMultiplier(tier);
        var devMult = GetDevelopmentMultiplier(developmentLevel);
        return decimal.Round(baseValue * tierMult * devMult, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Multiplicador de tier. T1=1.0, T2=1.25, T3=1.55, T4=1.90.
    ///
    /// Los tiers son SUAVES — la ventaja está en la eficiencia del desarrollo,
    /// no en una base aplastante. Un T1 muy desarrollado (dev30 = 3.21) supera
    /// a un T4 sin desarrollar (dev1 = 1.90).
    /// </summary>
    public static decimal GetTierMultiplier(int tier)
    {
        if (tier < MinTier || tier > MaxTier)
            throw new ArgumentOutOfRangeException(nameof(tier), tier, $"Tier must be between {MinTier} and {MaxTier}.");
        return TierMultipliers[tier];
    }

    /// <summary>
    /// Multiplicador de desarrollo. Logarítmico con rendimientos decrecientes.
    ///
    ///   mult(n) = 1 + 0.65 × ln(n)
    ///
    /// Tabla de referencia:
    ///   dev1  = 1.00    dev10 = 2.50    dev20 = 2.95
    ///   dev5  = 2.05    dev15 = 2.76    dev30 = 3.21
    ///
    /// El desarrollo es la palanca PRINCIPAL de poder. Un equipo bien
    /// desarrollado de tier bajo compite con uno de tier alto sin desarrollar.
    /// </summary>
    public static decimal GetDevelopmentMultiplier(int developmentLevel)
    {
        if (developmentLevel < MinDevelopment || developmentLevel > MaxDevelopment)
            throw new ArgumentOutOfRangeException(nameof(developmentLevel), developmentLevel,
                $"Development level must be between {MinDevelopment} and {MaxDevelopment}.");

        if (developmentLevel == 1) return 1.0m;
        return 1.0m + DevelopmentLogCoefficient * (decimal)Math.Log(developmentLevel);
    }

    /// <summary>
    /// Multiplicador de rango. Se usa para generar stats de rangos superiores
    /// a partir de los stats base de Común.
    ///
    /// Verificación con tier suaves + dev agresivo:
    ///   Especial T1 (8.0) > Común T4 dev30 (6.10) ✓  salto notable
    ///   Épico T1 (60.0) > Especial T4 dev30 (48.8) ✓  salto notable
    ///   Legendario T1 (450.0) > Épico T4 dev30 (366.0) ✓  salto notable
    /// </summary>
    public static decimal GetRarityMultiplier(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => RarityMultipliers[0],
            EquipmentRarity.Special => RarityMultipliers[1],
            EquipmentRarity.Epic => RarityMultipliers[2],
            EquipmentRarity.Legendary => RarityMultipliers[3],
            _ => throw new ArgumentOutOfRangeException(nameof(rarity))
        };
    }

    /// <summary>
    /// Calcula el stat base de un rango superior a partir del stat base Común.
    ///
    ///   statBase(rango) = statBase(Común) × rarityMultiplier(rango)
    ///
    /// Esto se usa en la generación del catálogo, no en runtime.
    /// </summary>
    public static decimal ScaleBaseStatForRarity(decimal commonBaseValue, EquipmentRarity rarity)
    {
        return decimal.Round(commonBaseValue * GetRarityMultiplier(rarity), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Verifica si un crafteo de tier es válido.
    /// Requiere 2 piezas del mismo DefinitionId, mismo tier, tier < MaxTier.
    /// </summary>
    public static bool CanCraftTierUp(EquipmentInstance pieceA, EquipmentInstance pieceB)
    {
        if (pieceA.DefinitionId != pieceB.DefinitionId) return false;
        if (pieceA.Tier != pieceB.Tier) return false;
        if (pieceA.Tier >= MaxTier) return false;
        return true;
    }

    /// <summary>
    /// Ejecuta un crafteo de tier. Devuelve una nueva instancia Tier+1, Desarrollo 1.
    /// Las dos piezas de entrada se destruyen (responsabilidad del caller).
    /// </summary>
    public static EquipmentInstance CraftTierUp(EquipmentInstance pieceA, EquipmentInstance pieceB)
    {
        if (!CanCraftTierUp(pieceA, pieceB))
            throw new InvalidOperationException("Cannot craft tier up: pieces are incompatible.");

        return new EquipmentInstance
        {
            DefinitionId = pieceA.DefinitionId,
            Slot = pieceA.Slot,
            Tier = pieceA.Tier + 1,
            DevelopmentLevel = MinDevelopment // reset total
        };
    }

    /// <summary>
    /// Verifica si una pieza puede desarrollarse más.
    /// </summary>
    public static bool CanDevelop(EquipmentInstance piece)
    {
        return piece.DevelopmentLevel < MaxDevelopment;
    }

    /// <summary>
    /// Incrementa el desarrollo de una pieza en 1 nivel.
    /// </summary>
    public static void Develop(EquipmentInstance piece)
    {
        if (!CanDevelop(piece))
            throw new InvalidOperationException($"Cannot develop further: already at max development {MaxDevelopment}.");
        piece.DevelopmentLevel++;
    }

    // ══════════════════════════════════════════════════════════════
    // HELPERS DE VERIFICACIÓN (para tests y auditoría)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tabla de referencia: muestra el multiplicador efectivo para cada
    /// combinación de tier × desarrollo. Útil para verificar balance.
    /// </summary>
    public static decimal GetEffectiveMultiplier(int tier, int developmentLevel)
    {
        return GetTierMultiplier(tier) * GetDevelopmentMultiplier(developmentLevel);
    }
}
