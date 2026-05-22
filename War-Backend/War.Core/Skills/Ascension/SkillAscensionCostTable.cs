using War.Core.Economy;
using War.Core.Skills.Books;

namespace War.Core.Skills.Ascension;

/// <summary>
/// Tabla inmutable de costes de ascensión de habilidades.
/// </summary>
/// <remarks>
/// 10 pasos de ascensión (0→1 … 9→10). Cada paso exige:
///   · Una cierta cantidad de libros (común / especial / épico / legendario)
///   · Un coste en moneda (cobre / plata / oro según el nivel)
///   · Un coste en energía
///
/// La ultimate (skill del slot ultimate, según el <c>SkillCatalogRegistry</c>) exige más libros
/// y más moneda/energía (multiplicador ~1.7×). Los niveles se reportan como "paso" (1..10),
/// donde el paso N representa la transición (N-1) → N.
/// </remarks>
public static class SkillAscensionCostTable
{
    public const int MinStep = 1;
    public const int MaxStep = 10;

    // ── Tabla para skills normales ──────────────────────────────────────────
    // Índice: paso - 1 (0 = paso 1 = asciende de nivel 0 a nivel 1).

    private static readonly SkillAscensionStepCost[] NormalSteps =
    {
        //                       libros                       cobre    plata  oro  energía
        /* 0→1 */ new(NormalBook(1, SkillBookRarity.Common),   3_000,      0,  0,    500),
        /* 1→2 */ new(NormalBook(2, SkillBookRarity.Common),   7_500,      0,  0,  1_200),
        /* 2→3 */ new(NormalBook(3, SkillBookRarity.Common),  20_000,      0,  0,  3_000),
        /* 3→4 */ new(NormalBook(5, SkillBookRarity.Common),  50_000,      0,  0,  7_000),
        /* 4→5 */ new(NormalBook(7, SkillBookRarity.Common), 150_000,      0,  0, 18_000),
        /* 5→6 */ new(NormalBook(1, SkillBookRarity.Special),      0,    500,  0, 40_000),
        /* 6→7 */ new(NormalBook(2, SkillBookRarity.Special),      0,  1_500,  0, 85_000),
        /* 7→8 */ new(NormalBook(1, SkillBookRarity.Epic),         0,  3_500,  0, 180_000),
        /* 8→9 */ new(NormalBook(2, SkillBookRarity.Epic),         0,  9_000,  0, 380_000),
        /* 9→10*/ new(NormalBook(2, SkillBookRarity.Legendary),    0,      0, 400, 800_000),
    };

    // ── Tabla para la ultimate ──────────────────────────────────────────────
    // Más libros + monedas/energía ~×1.7 en los pasos equivalentes.

    private static readonly SkillAscensionStepCost[] UltimateSteps =
    {
        /* 0→1 */ new(UltBook( 2, SkillBookRarity.Common),     5_000,      0,  0,    800),
        /* 1→2 */ new(UltBook( 4, SkillBookRarity.Common),    12_500,      0,  0,  2_000),
        /* 2→3 */ new(UltBook( 6, SkillBookRarity.Common),    33_000,      0,  0,  5_000),
        /* 3→4 */ new(UltBook( 8, SkillBookRarity.Common),    85_000,      0,  0, 12_000),
        /* 4→5 */ new(UltBook(10, SkillBookRarity.Common),   250_000,      0,  0, 30_000),
        /* 5→6 */ new(UltBook( 2, SkillBookRarity.Special),        0,    850,  0, 70_000),
        /* 6→7 */ new(UltBook( 4, SkillBookRarity.Special),        0,  2_500,  0, 145_000),
        /* 7→8 */ new(UltBook( 2, SkillBookRarity.Epic),           0,  6_000,  0, 300_000),
        /* 8→9 */ new(UltBook( 4, SkillBookRarity.Epic),           0, 15_000,  0, 640_000),
        /* 9→10*/ new(UltBook( 5, SkillBookRarity.Legendary),      0,      0, 700, 1_400_000),
    };

    /// <summary>
    /// Coste del paso N (1..10) para una skill. <paramref name="isUltimate"/> selecciona la tabla correcta.
    /// </summary>
    public static SkillAscensionStepCost GetStepCost(int step, bool isUltimate)
    {
        if (step < MinStep || step > MaxStep)
            throw new ArgumentOutOfRangeException(nameof(step),
                $"Paso de ascensión debe estar entre {MinStep} y {MaxStep}.");
        return isUltimate ? UltimateSteps[step - 1] : NormalSteps[step - 1];
    }

    /// <summary>
    /// Convierte el requerimiento de libros a un DefinitionId concreto para una skill dada.
    /// · Común → <see cref="SkillBookCatalog.CommonBookDefinitionId"/> (no depende de la skill).
    /// · Specific → busca el libro específico de la skill + rareza.
    /// </summary>
    public static string ResolveBookDefinitionId(SkillAscensionBookRequirement req, string skillId) =>
        SkillBookCatalog.GetSpecificBookId(skillId, req.Rarity);

    /// <summary>
    /// Coste del paso como <see cref="CurrencyCost"/> listo para cobrar en el wallet.
    /// </summary>
    public static CurrencyCost GetCurrencyCost(SkillAscensionStepCost cost) =>
        new(Copper: cost.Copper, Silver: cost.Silver, Gold: cost.Gold, Energy: cost.Energy);

    // ── Helpers de construcción ─────────────────────────────────────────────

    private static SkillAscensionBookRequirement NormalBook(int count, SkillBookRarity rarity) =>
        new(count, rarity);

    private static SkillAscensionBookRequirement UltBook(int count, SkillBookRarity rarity) =>
        new(count, rarity);
}

/// <summary>Cantidad y rareza de libros requeridos para un paso de ascensión.</summary>
public sealed record SkillAscensionBookRequirement(int Count, SkillBookRarity Rarity);

/// <summary>Coste completo de un paso de ascensión (libros + moneda + energía).</summary>
public sealed record SkillAscensionStepCost(
    SkillAscensionBookRequirement Books,
    long Copper,
    long Silver,
    long Gold,
    long Energy);
