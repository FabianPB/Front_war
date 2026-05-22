using War.Core.Combat;
using War.Core.Resources;

namespace War.Core.Skills.Catalogs;

/// <summary>
/// Complete skill catalog for the Bruiser class.
///
/// Identity: Heavy Tank/Brawler — the highest-HP, highest-defense class with
/// devastating melee crowd control and area denial.
/// Primary stat: PhysicalAttack (all skills scale with PhysicalAttack).
/// Damage type: Physical.
/// Range: Melee (2.5m) with select charge and ground-targeted abilities.
/// Conditions: Heat (state — rage/fury), Stun (CC — heavy impacts), Weaken (CC — armor crush).
///
/// Balance rationale:
///   Bruiser gains +11 PhysicalAttack per level vs Sorcerer's +14 MagicAttack (ratio 0.786).
///   To match raw damage the coefficients would need ~1.27x multiplier, but Bruiser is a
///   dedicated tank so damage is intentionally reduced by ~20-25%.
///   Net coefficients are similar to or slightly lower than Sorcerer's, compensated by
///   more crowd control, self-protection, and the highest base survivability in the game
///   (68 HP/lvl, 5 Def/lvl, 4 MagRes/lvl).
/// </summary>
public static class BruiserSkillCatalog
{
    public const string PunoDeHierroSkillId = "bruiser.skill.puno-de-hierro";
    public const string RugidoDeFuriaSkillId = "bruiser.skill.rugido-de-furia";
    public const string EmbestidaAcorazadaSkillId = "bruiser.skill.embestida-acorazada";
    public const string OndaSismicaSkillId = "bruiser.skill.onda-sismica";
    public const string RafagaDeGolpesSkillId = "bruiser.skill.rafaga-de-golpes";
    public const string FracturaDeArmaduraSkillId = "bruiser.skill.fractura-de-armadura";
    public const string GolpeDemoledorSkillId = "bruiser.skill.golpe-demoledor";
    public const string ErupcionDeIraSkillId = "bruiser.skill.erupcion-de-ira";
    public const string MartilloDeGuerraSkillId = "bruiser.skill.martillo-de-guerra";
    public const string ImpactoDevastadorSkillId = "bruiser.skill.impacto-devastador";
    public const string TempestadDeFuriaSkillId = "bruiser.skill.tempestad-de-furia";
    public const string CataclismoSkillId = "bruiser.skill.cataclismo";
    public const string TitanDeGuerraSkillId = "bruiser.ultimate.titan-de-guerra";

    private static readonly IReadOnlyList<decimal> StandardProgression = CreateProgression(1.08m, 1.16m, 1.25m, 1.35m, 1.46m, 1.58m, 1.71m, 1.85m, 2.00m);
    private static readonly IReadOnlyList<decimal> BurstProgression = CreateProgression(1.10m, 1.20m, 1.31m, 1.43m, 1.56m, 1.70m, 1.85m, 2.01m, 2.18m);
    private static readonly IReadOnlyList<decimal> MultiHitProgression = CreateProgression(1.07m, 1.15m, 1.24m, 1.34m, 1.45m, 1.57m, 1.70m, 1.84m, 2.00m);
    private static readonly IReadOnlyList<decimal> ControlProgression = CreateProgression(1.07m, 1.15m, 1.23m, 1.32m, 1.42m, 1.53m, 1.65m, 1.78m, 1.92m);

    private static readonly IReadOnlyList<int> TitanDamagePercentagesByAscension = Array.AsReadOnly(
    [
        960,
        1056,
        1162,
        1278,
        1534,
        1841,
        2209,
        2872,
        3734,
        5601
    ]);

    private static readonly IReadOnlyList<SkillPendingDatum> TitanPendingData = Array.AsReadOnly(
    [
        new SkillPendingDatum("bruiser.titan-de-guerra.targeting-shape", "The final area footprint, acquisition logic, and cast range are pending. The current translation uses a selected target per hit and leaves range at 0 as a non-authoritative placeholder."),
        new SkillPendingDatum("bruiser.titan-de-guerra.cooldown", "The definitive cooldown is pending. The pilot skill currently exposes a 0-second cooldown placeholder so the combat translator stays deterministic."),
        new SkillPendingDatum("bruiser.titan-de-guerra.resource-cost", "The definitive mana or ultimate-charge cost is pending. The pilot skill currently declares no cast cost to avoid fabricating progression balance."),
        new SkillPendingDatum("bruiser.titan-de-guerra.stun-base-chance", "The base chance for the Stun applied at ascension 8 is pending. The current combat translation relies on the actor's existing status-chance pipeline.", true),
        new SkillPendingDatum("bruiser.titan-de-guerra.stun-duration", "The base duration for the Stun applied at ascension 8 is pending.", true),
        new SkillPendingDatum("bruiser.titan-de-guerra.heat-duration", "Heat is added from ascension 8, but its explicit base duration for this skill is pending.", true),
        new SkillPendingDatum("bruiser.titan-de-guerra.self-heal-timing", "The exact trigger timing for the ascension-10 self-heal is pending. The pilot currently models it as an OnCompletion follow-up action.", true),
        new SkillPendingDatum("bruiser.titan-de-guerra.ascension-2-3-cost", "The 2 -> 3 ascension material requirement is not defined yet.")
    ]);

    private static readonly IReadOnlyList<string> TitanSecurityNotes = Array.AsReadOnly(
    [
        "Invulnerability must be granted before the first scheduled damage hit is released; otherwise same-frame retaliation can land before the protection window exists.",
        "The multi-hit schedule should carry a single cast instance identifier so cancellation, rollback, or actor-queue reordering cannot duplicate or orphan remaining hits.",
        "Each scheduled hit should be rebound against live target conditions and protections when it fires; a cast-time snapshot is not authoritative for delayed executions.",
        "Cast resource validation must complete before protection grants are committed; otherwise the skill could yield free invulnerability on failed casts.",
        "The self-heal triggered at completion must not execute if the cast was cancelled or interrupted mid-channel."
    ]);

    public static SkillDefinition PunoDeHierro { get; } = CreatePunoDeHierro();
    public static SkillDefinition RugidoDeFuria { get; } = CreateRugidoDeFuria();
    public static SkillDefinition EmbestidaAcorazada { get; } = CreateEmbestidaAcorazada();
    public static SkillDefinition OndaSismica { get; } = CreateOndaSismica();
    public static SkillDefinition RafagaDeGolpes { get; } = CreateRafagaDeGolpes();
    public static SkillDefinition FracturaDeArmadura { get; } = CreateFracturaDeArmadura();
    public static SkillDefinition GolpeDemoledor { get; } = CreateGolpeDemoledor();
    public static SkillDefinition ErupcionDeIra { get; } = CreateErupcionDeIra();
    public static SkillDefinition MartilloDeGuerra { get; } = CreateMartilloDeGuerra();
    public static SkillDefinition ImpactoDevastador { get; } = CreateImpactoDevastador();
    public static SkillDefinition TempestadDeFuria { get; } = CreateTempestadDeFuria();
    public static SkillDefinition Cataclismo { get; } = CreateCataclismo();
    public static SkillDefinition TitanDeGuerra { get; } = CreateTitanDeGuerra();

    public static IReadOnlyList<SkillDefinition> All { get; } = Array.AsReadOnly(
    [
        PunoDeHierro,
        RugidoDeFuria,
        EmbestidaAcorazada,
        OndaSismica,
        RafagaDeGolpes,
        FracturaDeArmadura,
        GolpeDemoledor,
        ErupcionDeIra,
        MartilloDeGuerra,
        ImpactoDevastador,
        TempestadDeFuria,
        Cataclismo,
        TitanDeGuerra
    ]);

    public static ClassSkillCatalog CreateCatalog()
    {
        return new ClassSkillCatalog(ClassType.Bruiser, All);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 01 — Puño de Hierro (Iron Fist)
    //  Role: Poke / Pressure — melee stun poke, Weaken at high ascension
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreatePunoDeHierro()
    {
        var coefficients = CreateCoefficientSeries(1.15m, StandardProgression);
        var ascensions = CreateDamageAscensionOverrides("Puno de Hierro", "PunoDeHierro", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateCrowdControlEffect("puno-de-hierro-weaken", CombatConditionType.Weaken, 0.30m, 3.5m, "Ascension 5 unlocks Weaken on impact."), "Weaken can now be applied on impact.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("puno-de-hierro-weaken", BaseDurationSeconds: 4.5m), "Weaken duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("puno-de-hierro-weaken", BaseApplyChance: 0.40m, ApplyChanceMultiplier: 1.15m), "Weaken application became more reliable.");

        return CreateBruiserSkill(
            PunoDeHierroSkillId,
            "Puno de Hierro",
            "El bruiser concentra toda su fuerza bruta en un punetazo devastador que sacude al enemigo hasta los huesos. Inflige dano fisico contundente y puede aturdir brevemente al objetivo con la fuerza del impacto. Ascensiones superiores permiten que el golpe debilite la armadura enemiga.",
            SkillSlot.Slot01,
            1,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Poke, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Stun),
                Targeting: SingleTargeting(2.5m),
                // Puño de Hierro: swing directo. Bruiser poke base, razonablemente rápido para combo
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 4m, CastTimeSeconds: 0.30m),
                ResourceCosts: ManaCosts(12m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Close-range iron fist poke that evolves into a Weaken setup tool.",
            metadata: CreateMetadata("neutral", "melee", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 02 — Rugido de Furia (Fury Roar)
    //  Role: Poke / Pressure — rage-fueled strike with Heat
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateRugidoDeFuria()
    {
        var coefficients = CreateCoefficientSeries(1.28m, StandardProgression);
        var ascensions = CreateDamageAscensionOverrides("Rugido de Furia", "RugidoDeFuria", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("rugido-de-furia-heat", CombatConditionType.Heat, 0.35m, 4m, "Ascension 5 unlocks Heat on the fury roar."), "Heat can now be applied on impact.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("rugido-de-furia-heat", BaseDurationSeconds: 5m), "Heat duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("rugido-de-furia-heat", BaseApplyChance: 0.48m), "Heat application became more consistent.");

        return CreateBruiserSkill(
            RugidoDeFuriaSkillId,
            "Rugido de Furia",
            "El bruiser desata un rugido furioso acompañado de un golpe ardiente cargado de ira. Inflige daño mágico sobrenatural imbuido de fuego interno y sacude la voluntad del enemigo. En niveles avanzados, el rugido impregna al objetivo con el calor abrasador de la furia.",
            SkillSlot.Slot02,
            4,
            Elements(SkillElementType.Fire),
            Roles(SkillCombatRole.Poke, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                // Rugido de Furia: la ira trasciende lo físico → daño MÁGICO (la única del Bruiser)
                Action: CreateMagDamageAction(coefficients[0], CombatConditionType.Heat),
                Targeting: SingleTargeting(2.5m),
                // Rugido de Furia: rugido + golpe ardiente, gesto emocional + swing
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 6m, CastTimeSeconds: 0.40m),
                ResourceCosts: ManaCosts(16m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Melee rage poke with Heat application at higher ascensions.",
            metadata: CreateMetadata("fire", "melee", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 03 — Embestida Acorazada (Armored Charge)
    //  Role: Poke / Burst — gap-closer with Weaken and self-protection
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateEmbestidaAcorazada()
    {
        var coefficients = CreateCoefficientSeries(1.60m, StandardProgression);
        var ascensions = CreateDamageAscensionOverrides("Embestida Acorazada", "EmbestidaAcorazada", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateCrowdControlEffect("embestida-acorazada-weaken", CombatConditionType.Weaken, 0.40m, 4m, "Ascension 5 unlocks Weaken on the armored charge."), "Weaken can now be applied on impact.");
        ApplyCastProtectionOverride(ascensions, 5,
            Array.AsReadOnly([CreateConditionShieldGrant("embestida-acorazada-condition-shield", 2m)]),
            "A brief condition immunity shield is granted during the charge.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("embestida-acorazada-weaken", BaseDurationSeconds: 5m), "Weaken duration increased.");
        ApplyCastProtectionOverride(ascensions, 8,
            Array.AsReadOnly([CreateConditionShieldGrant("embestida-acorazada-condition-shield", 3m)]),
            "Condition immunity shield duration increased.");
        ApplyAddedEffect(ascensions, 10, CreateCrowdControlEffect("embestida-acorazada-stun", CombatConditionType.Stun, 0.18m, 1.10m, "Ascension 10 unlocks an additional Stun chance on the charge."), "Stun chance unlocked.");

        return CreateBruiserSkill(
            EmbestidaAcorazadaSkillId,
            "Embestida Acorazada",
            "El bruiser se lanza en una embestida blindada contra el enemigo, cubriendo una distancia considerable con su armadura como escudo. Inflige dano fisico elevado y puede quebrar la defensa del objetivo. Ascensiones superiores otorgan proteccion durante la carga y permiten aturdir al impactar.",
            SkillSlot.Slot03,
            7,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Poke, SkillCombatRole.Burst),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Weaken),
                Targeting: SingleTargeting(6m),
                // Embestida Acorazada: "embestida blindada", gap-closer con inicio de carga
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 8m, CastTimeSeconds: 0.50m),
                ResourceCosts: ManaCosts(22m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Gap-closer charge with self-protection and Weaken. Condition shield at ascension 5 provides survivability during the dash.",
            metadata: CreateMetadata("neutral", "melee", "gap-closer", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 04 — Onda Sismica (Seismic Wave)
    //  Role: Area / Pressure — self-centered AoE with Stun and Weaken
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateOndaSismica()
    {
        var coefficients = CreateCoefficientSeries(1.35m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Onda Sismica", "OndaSismica", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateCrowdControlEffect("onda-sismica-weaken", CombatConditionType.Weaken, 0.45m, 4m, "Ascension 5 unlocks Weaken in the seismic wave."), "Weaken can now be applied to nearby enemies.");
        ApplyActionOverride(ascensions, 8, CreatePhysDamageAction(coefficients[7], CombatConditionType.Stun, [CreateConditionSynergy("onda-sismica-vs-weaken", CombatConditionType.Weaken, 1.15m, "Ascension 8 improves damage against targets already affected by Weaken.")]), "Damage now scales up against Weaken targets.");
        ApplyEffectOverride(ascensions, 9, new SkillConditionEffectOverride("onda-sismica-weaken", BaseDurationSeconds: 5m), "Weaken duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("onda-sismica-weaken", BaseApplyChance: 0.55m), "Weaken application became more reliable.");

        return CreateBruiserSkill(
            OndaSismicaSkillId,
            "Onda Sismica",
            "El bruiser golpea el suelo con fuerza descomunal, generando una onda sismica que sacude a todos los enemigos cercanos. Inflige dano fisico en area y puede aturdir a los afectados. Ascensiones superiores permiten que la onda debilite persistentemente a los enemigos atrapados.",
            SkillSlot.Slot04,
            10,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Area, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Stun),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.Area, SkillTargetAffinity.Enemy, 0m, 4m, 4, false, "Self-centered seismic wave. The current combat translator resolves the selected target only until spatial AoE selection is authoritative."),
                // Onda Sísmica: "golpea el suelo con fuerza descomunal", slam grande
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 9m, CastTimeSeconds: 0.75m),
                ResourceCosts: ManaCosts(24m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Close-range seismic AoE with Stun and Weaken application.",
            metadata: CreateMetadata("neutral", "aoe", "provisional-balance"),
            pendingData: SpatialPendingData("onda-sismica", "The area footprint is modeled in metadata, but the current translator still resolves a selected target only.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 05 — Rafaga de Golpes (Flurry of Blows)
    //  Role: MultiHit / Pressure — rapid fury strikes with Heat
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateRafagaDeGolpes()
    {
        var coefficients = CreateCoefficientSeries(0.60m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Rafaga de Golpes", "RafagaDeGolpes", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("rafaga-de-golpes-heat", CombatConditionType.Heat, 0.28m, 3.5m, "Ascension 5 unlocks Heat on the rapid strikes."), "Heat can now be applied by the rapid strikes.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("rafaga-de-golpes-heat", BaseApplyChance: 0.40m), "Heat application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(4, 0.75m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one extra strike to the flurry."), "One additional strike was added.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("rafaga-de-golpes-heat", BaseDurationSeconds: 4.5m), "Heat duration increased.");

        return CreateBruiserSkill(
            RafagaDeGolpesSkillId,
            "Rafaga de Golpes",
            "El bruiser desata una rafaga de punos furiosos contra el enemigo. Cada impacto inflige dano fisico imbuido de furia y puede acumular calor abrasador en el objetivo. En niveles avanzados, los golpes multiplican la acumulacion de calor interno.",
            SkillSlot.Slot05,
            13,
            Elements(SkillElementType.Fire),
            Roles(SkillCombatRole.MultiHit, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Heat),
                Targeting: SingleTargeting(2.5m),
                // Ráfaga de Golpes: "ráfaga de puños furiosos", multi-hit rápido
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 10m, CastTimeSeconds: 0.35m),
                ResourceCosts: ManaCosts(26m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(3, 0.75m, SkillHitDistributionMode.EvenlyDistributed, true, "Three rapid fury strikes are scheduled against the current target.")),
            FreezeAscensions(ascensions),
            notes: "Melee multi-hit Heat applicator.",
            metadata: CreateMetadata("fire", "melee", "multihit", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 06 — Fractura de Armadura (Armor Fracture)
    //  Role: Control / Pressure — primary Weaken applicator with late Stun
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateFracturaDeArmadura()
    {
        var coefficients = CreateCoefficientSeries(1.22m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Fractura de Armadura", "FracturaDeArmadura", coefficients);
        ApplyAddedEffect(ascensions, 4, CreateCrowdControlEffect("fractura-de-armadura-weaken", CombatConditionType.Weaken, 0.35m, 4m, "Ascension 4 unlocks Weaken on the armor fracture."), "Weaken can now be applied by the fracture.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("fractura-de-armadura-weaken", BaseApplyChance: 0.45m, BaseDurationSeconds: 5m), "Weaken application and duration increased.");
        ApplyAddedEffect(ascensions, 9, CreateCrowdControlEffect("fractura-de-armadura-stun", CombatConditionType.Stun, 0.20m, 1.0m, "Ascension 9 unlocks an additional Stun chance."), "Stun chance unlocked.");

        return CreateBruiserSkill(
            FracturaDeArmaduraSkillId,
            "Fractura de Armadura",
            "El bruiser concentra su fuerza en un golpe preciso disenado para fracturar la armadura del enemigo. Inflige dano fisico y expone las debilidades estructurales del objetivo. Ascensiones avanzadas permiten que el impacto tambien aturda al enemigo.",
            SkillSlot.Slot06,
            16,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Control, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Weaken),
                Targeting: SingleTargeting(2.5m),
                // Fractura de Armadura: "golpe preciso", ejecución pesada con apuntado
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 11m, CastTimeSeconds: 0.55m),
                ResourceCosts: ManaCosts(28m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Primary armor-crushing control tool with Weaken access and late Stun layering.",
            metadata: CreateMetadata("neutral", "control", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 07 — Golpe Demoledor (Demolishing Blow)
    //  Role: Burst / Pressure — heavy single-target burst with Heat synergy
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateGolpeDemoledor()
    {
        var coefficients = CreateCoefficientSeries(1.95m, BurstProgression);
        var ascensions = CreateDamageAscensionOverrides("Golpe Demoledor", "GolpeDemoledor", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("golpe-demoledor-heat", CombatConditionType.Heat, 0.45m, 4.5m, "Ascension 5 unlocks Heat on the demolishing blow."), "Heat can now be applied on impact.");
        ApplyActionOverride(ascensions, 8, CreatePhysDamageAction(coefficients[7], CombatConditionType.Stun, [CreateConditionSynergy("golpe-demoledor-vs-heat", CombatConditionType.Heat, 1.20m, "Ascension 8 improves damage against targets already affected by Heat.")]), "Damage now scales up against Heat targets.");
        ApplyEffectOverride(ascensions, 9, new SkillConditionEffectOverride("golpe-demoledor-heat", BaseApplyChance: 0.55m), "Heat application improved.");
        ApplyActionOverride(ascensions, 10, CreatePhysDamageAction(coefficients[9], CombatConditionType.Stun, [CreateConditionSynergy("golpe-demoledor-vs-heat", CombatConditionType.Heat, 1.30m, "Ascension 10 significantly improves detonation damage against Heat targets.")]), "Heat synergy multiplier increased.");

        return CreateBruiserSkill(
            GolpeDemoledorSkillId,
            "Golpe Demoledor",
            "El bruiser carga toda su masa y furia en un unico golpe demoledor que puede derrumbar muros. Inflige dano fisico masivo y aturde al enemigo con la fuerza del impacto. En niveles avanzados, el golpe arde con el calor de la ira y causa dano adicional a objetivos ya inflamados.",
            SkillSlot.Slot07,
            20,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Burst, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Stun),
                Targeting: SingleTargeting(2.5m),
                // Golpe Demoledor: "carga toda su masa y furia en un único golpe", el wind-up máximo para single-target
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 14m, CastTimeSeconds: 0.95m),
                ResourceCosts: ManaCosts(34m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Heavy burst with Heat synergy at high ascensions. Highest single-hit coefficient in the regular kit.",
            metadata: CreateMetadata("neutral", "burst", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 08 — Erupcion de Ira (Wrath Eruption)
    //  Role: Control — Heat applicator with late Stun
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateErupcionDeIra()
    {
        var coefficients = CreateCoefficientSeries(1.12m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Erupcion de Ira", "ErupcionDeIra", coefficients);
        ApplyAddedEffect(ascensions, 4, CreateStateEffect("erupcion-de-ira-heat", CombatConditionType.Heat, 0.45m, 4.5m, "Ascension 4 unlocks Heat on the wrath eruption."), "Heat can now be applied on impact.");
        ApplyAddedEffect(ascensions, 8, CreateCrowdControlEffect("erupcion-de-ira-stun", CombatConditionType.Stun, 0.30m, 1.25m, "Ascension 8 unlocks Stun on the wrath eruption."), "Stun can now be applied on impact.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("erupcion-de-ira-stun", BaseApplyChance: 0.42m), "Stun application became more reliable.");

        return CreateBruiserSkill(
            ErupcionDeIraSkillId,
            "Erupcion de Ira",
            "El bruiser canaliza su ira mas profunda y la libera en una erupcion de furia ardiente sobre el enemigo. Inflige dano fisico imbuido de calor interno. En ascensiones superiores, la erupcion puede aturdir al objetivo con la fuerza de la descarga emocional.",
            SkillSlot.Slot08,
            24,
            Elements(SkillElementType.Fire),
            Roles(SkillCombatRole.Control),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Heat),
                Targeting: SingleTargeting(2.5m),
                // Erupción de Ira: "canaliza su ira más profunda", channel emocional y release
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 12m, CastTimeSeconds: 0.70m),
                ResourceCosts: ManaCosts(30m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Heat and Stun control tool. Early Heat access at ascension 4 with Stun layering at ascension 8.",
            metadata: CreateMetadata("fire", "control", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 09 — Martillo de Guerra (War Hammer)
    //  Role: Chain / MultiHit / Pressure — multi-hit with Stun and Weaken
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateMartilloDeGuerra()
    {
        var coefficients = CreateCoefficientSeries(0.75m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Martillo de Guerra", "MartilloDeGuerra", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateCrowdControlEffect("martillo-de-guerra-weaken", CombatConditionType.Weaken, 0.30m, 3.5m, "Ascension 5 unlocks Weaken on the war hammer strikes."), "Weaken can now be applied by the chained strikes.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("martillo-de-guerra-weaken", BaseApplyChance: 0.40m), "Weaken application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(5, 0.90m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one more war hammer strike."), "One additional war hammer strike was added.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("martillo-de-guerra-weaken", BaseDurationSeconds: 4.5m), "Weaken duration increased.");

        return CreateBruiserSkill(
            MartilloDeGuerraSkillId,
            "Martillo de Guerra",
            "El bruiser descarga una serie de golpes de martillo de guerra sobre los enemigos cercanos. Cada impacto inflige dano fisico contundente y puede debilitar la armadura del objetivo. Ascensiones superiores incrementan el numero de impactos y la consistencia del debilitamiento.",
            SkillSlot.Slot09,
            28,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Chain, SkillCombatRole.MultiHit, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Stun),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.SingleTarget, SkillTargetAffinity.Enemy, 3m, null, 4, true, "Chain spread is documented in metadata. The current translator repeats hits against the selected target until nearby-target chain runtime exists."),
                // Martillo de Guerra: "serie de golpes de martillo", multi-hit más lento por arma pesada
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 13m, CastTimeSeconds: 0.55m),
                ResourceCosts: ManaCosts(32m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(4, 0.90m, SkillHitDistributionMode.EvenlyDistributed, true, "Current fallback executes four rapid war hammer strikes on the selected target.")),
            FreezeAscensions(ascensions),
            notes: "Chain multi-hit with Stun damage condition and Weaken application.",
            metadata: CreateMetadata("neutral", "chain", "multihit", "provisional-balance"),
            pendingData: SpatialPendingData("martillo-de-guerra", "Multi-target chain propagation remains metadata-only; the current combat translator repeats hits against the selected target.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 10 — Impacto Devastador (Devastating Impact)
    //  Role: Burst / Detonation — detonates Heat
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateImpactoDevastador()
    {
        var coefficients = CreateCoefficientSeries(1.80m, BurstProgression);
        var ascensions = CreateDamageAscensionOverrides("Impacto Devastador", "ImpactoDevastador", coefficients);
        ApplyActionOverride(ascensions, 6, CreatePhysDamageAction(coefficients[5], CombatConditionType.Heat, [CreateConditionSynergy("impacto-devastador-vs-heat", CombatConditionType.Heat, 1.50m, "Ascension 6 increases the Heat detonation multiplier.")]), "Heat detonation multiplier increased.");
        ApplyActionOverride(ascensions, 8, CreatePhysDamageAction(coefficients[7], CombatConditionType.Heat, [CreateConditionSynergy("impacto-devastador-vs-heat", CombatConditionType.Heat, 1.65m, "Ascension 8 significantly improves detonation damage against Heat targets.")]), "Heat detonation multiplier increased again.");
        ApplyActionOverride(ascensions, 10, CreatePhysDamageAction(coefficients[9], CombatConditionType.Heat, [CreateConditionSynergy("impacto-devastador-vs-heat", CombatConditionType.Heat, 1.80m, "Ascension 10 maximizes the detonation multiplier against Heat targets.")]), "Heat detonation multiplier maximized.");

        return CreateBruiserSkill(
            ImpactoDevastadorSkillId,
            "Impacto Devastador",
            "El bruiser concentra toda su potencia en un impacto demoledor que hace temblar la tierra. Inflige dano fisico elevado. Si el objetivo se encuentra ardiendo con calor interno, la energia del impacto reacciona con el calor causando dano adicional devastador.",
            SkillSlot.Slot10,
            32,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Burst, SkillCombatRole.Detonation),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Heat, [CreateConditionSynergy("impacto-devastador-vs-heat", CombatConditionType.Heat, 1.35m, "Base version deals extra damage when the target is already affected by Heat.")]),
                Targeting: SingleTargeting(2.5m),
                // Impacto Devastador: "concentra toda su potencia", detonator con wind-up
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 15m, CastTimeSeconds: 0.90m),
                ResourceCosts: ManaCosts(38m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Heat detonation skill that leverages Heat already present on the target.",
            metadata: CreateMetadata("neutral", "burst", "detonation", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 11 — Tempestad de Furia (Fury Tempest)
    //  Role: Area / MultiHit / Control — AoE persistent fire zone with Heat
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateTempestadDeFuria()
    {
        var coefficients = CreateCoefficientSeries(0.50m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Tempestad de Furia", "TempestadDeFuria", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("tempestad-de-furia-heat", CombatConditionType.Heat, 0.26m, 3.5m, "Ascension 5 unlocks Heat inside the fury tempest zone."), "Heat can now be applied by the tempest.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("tempestad-de-furia-heat", BaseApplyChance: 0.34m), "Heat application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(7, 3.0m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one more fury tempest pulse."), "One additional tempest pulse was added.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("tempestad-de-furia-heat", BaseApplyChance: 0.42m, BaseDurationSeconds: 4.5m), "Heat became more reliable and lasted longer.");

        return CreateBruiserSkill(
            TempestadDeFuriaSkillId,
            "Tempestad de Furia",
            "El bruiser desata una tempestad de furia incandescente que cubre el campo de batalla. Multiples oleadas de energia furiosa golpean repetidamente el area. Ascensiones superiores permiten que cada oleada impregne a los enemigos con calor abrasador.",
            SkillSlot.Slot11,
            36,
            Elements(SkillElementType.Fire),
            Roles(SkillCombatRole.Area, SkillCombatRole.MultiHit, SkillCombatRole.Control),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Heat),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.GroundPoint, SkillTargetAffinity.Enemy, 4m, 4m, 5, true, "Persistent area coverage is modeled in metadata. The current translator resolves the selected target only while still scheduling repeated hits."),
                // Tempestad de Furia: "tempestad de furia incandescente", canalización amplia
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 16m, CastTimeSeconds: 0.95m),
                ResourceCosts: ManaCosts(40m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(6, 3m, SkillHitDistributionMode.EvenlyDistributed, true, "Six repeated fury tempest pulses are scheduled during the active window.")),
            FreezeAscensions(ascensions),
            notes: "Persistent fury zone modeled as repeated hits with metadata-only area acquisition.",
            metadata: CreateMetadata("fire", "area", "multihit", "control", "provisional-balance"),
            pendingData: SpatialPendingData("tempestad-de-furia", "Persistent area acquisition and multi-target resolution are still metadata-only.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 12 — Cataclismo (Cataclysm)
    //  Role: Control / Burst — heavy Stun + conditional Stun on Heat
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateCataclismo()
    {
        var coefficients = CreateCoefficientSeries(1.58m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Cataclismo", "Cataclismo", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateCrowdControlEffect("cataclismo-weaken", CombatConditionType.Weaken, 0.45m, 4.5m, "Ascension 5 unlocks deep Weaken on the cataclysm."), "Weaken can now be applied on impact.");
        ApplyAddedEffect(ascensions, 8, CreateCrowdControlEffect("cataclismo-stun", CombatConditionType.Stun, 0.28m, 1.30m, "Ascension 8 allows Stun only when the target is already affected by Heat.", [CombatConditionType.Heat]), "Stun can now trigger against Heat-affected targets.");
        ApplyEffectOverride(ascensions, 9, new SkillConditionEffectOverride("cataclismo-stun", BaseApplyChance: 0.38m), "Stun application improved.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("cataclismo-stun", BaseDurationSeconds: 1.60m), "Stun duration increased.");

        return CreateBruiserSkill(
            CataclismoSkillId,
            "Cataclismo",
            "El bruiser concentra una fuerza sismica descomunal y la descarga sobre un punto del campo de batalla, causando un cataclismo localizado. Inflige dano fisico y debilita profundamente a los objetivos. En niveles avanzados, el cataclismo puede aturdir a los enemigos cuyo cuerpo ya arde con calor interno.",
            SkillSlot.Slot12,
            40,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Control, SkillCombatRole.Burst),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Stun),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.GroundPoint, SkillTargetAffinity.Enemy, 4m, 3m, 3, true, "The cataclysm footprint is modeled in metadata. The current translator resolves the selected target only."),
                // Cataclismo: "concentra una fuerza sísmica descomunal", el skill regular más lento
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 17m, CastTimeSeconds: 1.10m),
                ResourceCosts: ManaCosts(42m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Heavy seismic control burst with gated Stun on Heat-saturated targets.",
            metadata: CreateMetadata("neutral", "control", "burst", "provisional-balance"),
            pendingData: SpatialPendingData("cataclismo", "The cataclysm is conceptually an area burst, but the current translator still resolves the selected target only.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 13 — Titan de Guerra (War Titan) — ULTIMATE
    //  Role: Ultimate — multi-hit devastating strikes + invulnerability + self-heal
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateTitanDeGuerra()
    {
        return new SkillDefinition(
            TitanDeGuerraSkillId,
            "Titan de Guerra",
            "El bruiser canaliza la esencia de un titan ancestral de guerra y se transforma en una fuerza imparable de destruccion. Durante varios segundos, descarga una serie de golpes titanicos devastadores sobre el objetivo. Cada impacto inflige dano fisico masivo y puede desencadenar efectos de aturdimiento y calor abrasador. Mientras canaliza, el bruiser entra en un estado de invulnerabilidad absoluta. En sus formas mas avanzadas, la transformacion culmina con una regeneracion que sana una porcion significativa de la vida perdida.",
            ClassType.Bruiser,
            SkillSlot.Slot13,
            true,
            24,
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(TitanDamagePercentagesByAscension[0] / 100m, null),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.SingleTarget, SkillTargetAffinity.Enemy, 0m, null, 1, true, "The pilot resolves each hit against a selected target. Final AoE footprint and cast range are still pending combat design data."),
                // Titán de Guerra (ULTIMATE): "transformación en titán ancestral", el ult más épico y lento
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 0m, CastTimeSeconds: 1.80m),
                ResourceCosts: Array.Empty<SkillResourceCostDefinition>(),
                Effects: Array.AsReadOnly([CreateCrowdControlEffect("titan-de-guerra-stun-proxy", CombatConditionType.Stun, null, null, "The skill's stunning effect uses Stun as the combat proxy. The exact base chance and duration are still pending.")]),
                MultiHit: new SkillMultiHitProfile(6, 3m, SkillHitDistributionMode.EvenlyDistributed, true, "Six independent physical hits are evenly distributed across the active 3-second window."),
                CastProtections: Array.AsReadOnly([CreateInvulnerabilityGrant("titan-de-guerra-cast-invulnerability", 3m)]),
                TriggeredActions: Array.Empty<SkillTriggeredActionDefinition>()),
            CreateTitanAscensionOverrides(),
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.MultiHit, SkillCombatRole.Control, SkillCombatRole.Ultimate),
            Notes: "Bruiser pilot ultimate. Multi-hit is modeled as a scheduled repeated combat event.",
            Metadata: new Dictionary<string, string>
            {
                ["skill.pilot"] = "true",
                ["skill.class"] = "Bruiser",
                ["skill.kind"] = "Ultimate",
                ["skill.multi_hit.count"] = "6",
                ["skill.multi_hit.duration_seconds"] = "3"
            },
            PendingData: TitanPendingData,
            SecurityNotes: TitanSecurityNotes);
    }

    private static IReadOnlyDictionary<int, SkillAscensionOverrides> CreateTitanAscensionOverrides()
    {
        return new Dictionary<int, SkillAscensionOverrides>
        {
            [2] = new SkillAscensionOverrides(2, MagnitudeProfile: CreatePhysicalMagnitudeProfile(TitanDamagePercentagesByAscension[1] / 100m, "TitanDeGuerraDamagePerHit_1056pct"), Note: $"Per-hit damage increased to {TitanDamagePercentagesByAscension[1]}% of PhysicalAttack."),
            [3] = new SkillAscensionOverrides(3, MagnitudeProfile: CreatePhysicalMagnitudeProfile(TitanDamagePercentagesByAscension[2] / 100m, "TitanDeGuerraDamagePerHit_1162pct"), Note: $"Per-hit damage increased to {TitanDamagePercentagesByAscension[2]}% of PhysicalAttack."),
            [4] = new SkillAscensionOverrides(4, MagnitudeProfile: CreatePhysicalMagnitudeProfile(TitanDamagePercentagesByAscension[3] / 100m, "TitanDeGuerraDamagePerHit_1278pct"), Note: $"Per-hit damage increased to {TitanDamagePercentagesByAscension[3]}% of PhysicalAttack."),
            [5] = new SkillAscensionOverrides(5, MagnitudeProfile: CreatePhysicalMagnitudeProfile(TitanDamagePercentagesByAscension[4] / 100m, "TitanDeGuerraDamagePerHit_1534pct"), CastProtections: Array.AsReadOnly([CreateInvulnerabilityGrant("titan-de-guerra-cast-invulnerability", 4m)]), Note: $"Per-hit damage increased to {TitanDamagePercentagesByAscension[4]}% of PhysicalAttack and cast invulnerability duration increased to 4 seconds."),
            [6] = new SkillAscensionOverrides(6, MagnitudeProfile: CreatePhysicalMagnitudeProfile(TitanDamagePercentagesByAscension[5] / 100m, "TitanDeGuerraDamagePerHit_1841pct"), Note: $"Per-hit damage increased to {TitanDamagePercentagesByAscension[5]}% of PhysicalAttack."),
            [7] = new SkillAscensionOverrides(7, MagnitudeProfile: CreatePhysicalMagnitudeProfile(TitanDamagePercentagesByAscension[6] / 100m, "TitanDeGuerraDamagePerHit_2209pct"), Note: $"Per-hit damage increased to {TitanDamagePercentagesByAscension[6]}% of PhysicalAttack."),
            [8] = new SkillAscensionOverrides(8, MagnitudeProfile: CreatePhysicalMagnitudeProfile(TitanDamagePercentagesByAscension[7] / 100m, "TitanDeGuerraDamagePerHit_2872pct"), EffectOverrides: Array.AsReadOnly([new SkillConditionEffectOverride("titan-de-guerra-stun-proxy", ApplyChanceMultiplier: 1.5m, Note: "Ascension 8 increases the base Stun chance by 50%.")]), AddedEffects: Array.AsReadOnly([CreateStateEffect("titan-de-guerra-heat", CombatConditionType.Heat, 1m, null, "Heat is added from ascension 8. The skill-specific duration is still pending.")]), Note: $"Per-hit damage increased to {TitanDamagePercentagesByAscension[7]}% of PhysicalAttack, the Stun chance modifier increased, and Heat was added to each hit."),
            [9] = new SkillAscensionOverrides(9, MagnitudeProfile: CreatePhysicalMagnitudeProfile(TitanDamagePercentagesByAscension[8] / 100m, "TitanDeGuerraDamagePerHit_3734pct"), Note: $"Per-hit damage increased to {TitanDamagePercentagesByAscension[8]}% of PhysicalAttack."),
            [10] = new SkillAscensionOverrides(10, MagnitudeProfile: CreatePhysicalMagnitudeProfile(TitanDamagePercentagesByAscension[9] / 100m, "TitanDeGuerraDamagePerHit_5601pct"), TriggeredActions: Array.AsReadOnly([new SkillTriggeredActionDefinition("ascension-10-self-heal", SkillExecutionTriggerPhase.OnCompletion, new SkillActionDefinition(SkillActionType.Heal, new SkillMagnitudeProfile(0m, SkillScalingType.TargetMissingHp, 0.4m, ConfigurationName: "TitanDeGuerraAscension10SelfHeal"), null, CharacterResourceType.Hp, false, false), SkillTriggeredActionTargetSelector.Self, Array.Empty<SkillConditionEffectDefinition>(), "The ascension-10 self-heal is modeled as an OnCompletion self-targeted heal for 40% of missing HP.")]), Note: $"Per-hit damage increased to {TitanDamagePercentagesByAscension[9]}% of PhysicalAttack and a completion-phase self-heal was added.")
        };
    }

    // ═════════════════════════════════════════════════════════════════════
    //  FACTORY HELPERS
    // ═════════════════════════════════════════════════════════════════════

    private static SkillDefinition CreateBruiserSkill(
        string id,
        string name,
        string description,
        SkillSlot slot,
        int unlockLevel,
        IReadOnlyList<SkillElementType> elements,
        IReadOnlyList<SkillCombatRole> roles,
        SkillTuningSnapshot baseTuning,
        IReadOnlyDictionary<int, SkillAscensionOverrides> ascensionOverrides,
        string? notes = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyList<SkillPendingDatum>? pendingData = null,
        IReadOnlyList<string>? securityNotes = null)
    {
        return new SkillDefinition(id, name, description, ClassType.Bruiser, slot, false, unlockLevel, baseTuning, ascensionOverrides, elements, roles, notes, metadata, MergePendingData(id, metadata, pendingData), securityNotes);
    }

    private static IReadOnlyList<SkillPendingDatum>? MergePendingData(string skillId, IReadOnlyDictionary<string, string>? metadata, IReadOnlyList<SkillPendingDatum>? pendingData)
    {
        var items = new List<SkillPendingDatum>();

        if ((metadata ?? new Dictionary<string, string>()).Values.Any(value => string.Equals(value, "provisional-balance", StringComparison.OrdinalIgnoreCase)))
        {
            items.Add(new SkillPendingDatum($"{skillId}.authoritative-balance", "The exact damage, cooldown, and mana-cost tuning for this skill is still provisional and should be replaced with authoritative balance data in a later pass."));
        }

        if (pendingData is not null)
        {
            items.AddRange(pendingData);
        }

        return items.Count == 0
            ? null
            : Array.AsReadOnly(items.ToArray());
    }

    private static Dictionary<int, SkillAscensionOverrides> CreateDamageAscensionOverrides(string skillName, string configPrefix, IReadOnlyList<decimal> coefficients)
    {
        var overrides = new Dictionary<int, SkillAscensionOverrides>();

        for (var ascension = 2; ascension <= SkillCatalogRules.MaximumAscensionLevel; ascension++)
        {
            overrides[ascension] = new SkillAscensionOverrides(
                AscensionLevel: ascension,
                MagnitudeProfile: CreatePhysicalMagnitudeProfile(coefficients[ascension - 1], $"{configPrefix}_{ascension}"),
                Note: $"{skillName} damage coefficient increased to x{FormatCoefficient(coefficients[ascension - 1])} PhysicalAttack.");
        }

        return overrides;
    }

    private static void ApplyAddedEffect(IDictionary<int, SkillAscensionOverrides> overrides, int ascensionLevel, SkillConditionEffectDefinition effect, string extraNote)
    {
        var current = overrides[ascensionLevel];
        overrides[ascensionLevel] = current with { AddedEffects = Array.AsReadOnly([effect]), Note = CombineNotes(current.Note, extraNote) };
    }

    private static void ApplyEffectOverride(IDictionary<int, SkillAscensionOverrides> overrides, int ascensionLevel, SkillConditionEffectOverride effectOverride, string extraNote)
    {
        var current = overrides[ascensionLevel];
        overrides[ascensionLevel] = current with { EffectOverrides = Array.AsReadOnly([effectOverride]), Note = CombineNotes(current.Note, extraNote) };
    }

    private static void ApplyActionOverride(IDictionary<int, SkillAscensionOverrides> overrides, int ascensionLevel, SkillActionDefinition action, string extraNote)
    {
        var current = overrides[ascensionLevel];
        overrides[ascensionLevel] = current with { Action = action, Note = CombineNotes(current.Note, extraNote) };
    }

    private static void ApplyMultiHitOverride(IDictionary<int, SkillAscensionOverrides> overrides, int ascensionLevel, SkillMultiHitProfile multiHit, string extraNote)
    {
        var current = overrides[ascensionLevel];
        overrides[ascensionLevel] = current with { MultiHit = multiHit, Note = CombineNotes(current.Note, extraNote) };
    }

    private static void ApplyCastProtectionOverride(IDictionary<int, SkillAscensionOverrides> overrides, int ascensionLevel, IReadOnlyList<SkillProtectionGrantDefinition> protections, string extraNote)
    {
        var current = overrides[ascensionLevel];
        overrides[ascensionLevel] = current with { CastProtections = protections, Note = CombineNotes(current.Note, extraNote) };
    }

    private static SkillActionDefinition CreatePhysDamageAction(decimal coefficient, CombatConditionType? damageConditionType, IReadOnlyList<SkillConditionSynergyDefinition>? synergies = null)
    {
        return new SkillActionDefinition(SkillActionType.Damage, CreatePhysicalMagnitudeProfile(coefficient, $"PhysDamage_{FormatCoefficient(coefficient)}"), SkillDamageType.Physical, CharacterResourceType.Hp, true, true, damageConditionType, synergies);
    }

    /// <summary>
    /// Salida Magical — para la ~10% de skills cuya furia canaliza daño sobrenatural.
    /// Rugido de Furia: la ira interna trasciende lo físico y se manifiesta como daño mágico.
    /// </summary>
    private static SkillActionDefinition CreateMagDamageAction(decimal coefficient, CombatConditionType? damageConditionType, IReadOnlyList<SkillConditionSynergyDefinition>? synergies = null)
    {
        return new SkillActionDefinition(SkillActionType.Damage, CreatePhysicalMagnitudeProfile(coefficient, $"BruiMagDamage_{FormatCoefficient(coefficient)}"), SkillDamageType.Magical, CharacterResourceType.Hp, true, true, damageConditionType, synergies);
    }

    /// <summary>
    /// Bruiser: 90% PhysicalAttack + 10% MagicAttack (dual scaling).
    /// La furia interna del bruiser canaliza una fracción mágica involuntaria.
    /// </summary>
    private static SkillMagnitudeProfile CreatePhysicalMagnitudeProfile(decimal coefficient, string configurationName)
    {
        return new SkillMagnitudeProfile(
            0m,
            SkillScalingType.PhysicalAttack, coefficient * 0.90m,
            SkillScalingType.MagicAttack, coefficient * 0.10m,
            configurationName);
    }

    private static SkillConditionEffectDefinition CreateStateEffect(string effectKey, CombatConditionType condition, decimal? baseApplyChance, decimal? baseDurationSeconds, string note, IReadOnlyList<CombatConditionType>? requiredTargetConditions = null)
    {
        return new SkillConditionEffectDefinition(effectKey, condition, baseDurationSeconds, baseApplyChance, 0m, 1m, requiredTargetConditions, note);
    }

    private static SkillConditionEffectDefinition CreateCrowdControlEffect(string effectKey, CombatConditionType condition, decimal? baseApplyChance, decimal? baseDurationSeconds, string note, IReadOnlyList<CombatConditionType>? requiredTargetConditions = null)
    {
        return new SkillConditionEffectDefinition(effectKey, condition, baseDurationSeconds, baseApplyChance, 0m, 1m, requiredTargetConditions, note);
    }

    private static SkillConditionSynergyDefinition CreateConditionSynergy(string synergyKey, CombatConditionType requiredTargetCondition, decimal magnitudeMultiplier, string note)
    {
        return new SkillConditionSynergyDefinition(synergyKey, requiredTargetCondition, magnitudeMultiplier, 0m, note);
    }

    private static SkillProtectionGrantDefinition CreateInvulnerabilityGrant(string grantKey, decimal durationSeconds)
    {
        return new SkillProtectionGrantDefinition(grantKey, CombatProtectionType.Invulnerability, CombatProtectionBlockType.Damage | CombatProtectionBlockType.NegativeConditions | CombatProtectionBlockType.CrowdControl, durationSeconds, CombatProtectionRefreshPolicy.IgnoreIfAlreadyActive, false, "Full invulnerability grant — blocks damage, negative conditions, and crowd control.");
    }

    private static SkillProtectionGrantDefinition CreateConditionShieldGrant(string grantKey, decimal durationSeconds)
    {
        return new SkillProtectionGrantDefinition(grantKey, CombatProtectionType.Invulnerability, CombatProtectionBlockType.NegativeConditions | CombatProtectionBlockType.CrowdControl, durationSeconds, CombatProtectionRefreshPolicy.IgnoreIfAlreadyActive, false, "Condition shield — blocks negative conditions and crowd control but not direct damage.");
    }

    private static SkillTargetingProfile SingleTargeting(decimal range)
    {
        return new SkillTargetingProfile(SkillTargetingPattern.SingleTarget, SkillTargetAffinity.Enemy, range, null, 1, true);
    }

    private static IReadOnlyList<SkillResourceCostDefinition> ManaCosts(decimal amount)
    {
        return Array.AsReadOnly([new SkillResourceCostDefinition(CharacterResourceType.Mana, amount)]);
    }

    private static IReadOnlyList<SkillElementType> Elements(params SkillElementType[] elements)
    {
        return Array.AsReadOnly(elements.ToArray());
    }

    private static IReadOnlyList<SkillCombatRole> Roles(params SkillCombatRole[] roles)
    {
        return Array.AsReadOnly(roles.ToArray());
    }

    private static IReadOnlyDictionary<string, string> CreateMetadata(params string[] tags)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["skill.class"] = "Bruiser"
        };

        for (var index = 0; index < tags.Length; index++)
        {
            metadata[$"skill.tag.{index + 1}"] = tags[index];
        }

        return metadata;
    }

    private static IReadOnlyList<SkillPendingDatum>? SpatialPendingData(string skillKey, string description, bool blocksExactCombatSimulation)
    {
        return Array.AsReadOnly([new SkillPendingDatum($"bruiser.{skillKey}.spatial-runtime", description, blocksExactCombatSimulation)]);
    }

    private static IReadOnlyDictionary<int, SkillAscensionOverrides> FreezeAscensions(Dictionary<int, SkillAscensionOverrides> overrides)
    {
        return new Dictionary<int, SkillAscensionOverrides>(overrides);
    }

    private static IReadOnlyList<decimal> CreateProgression(params decimal[] laterMultipliers)
    {
        return Array.AsReadOnly((new[] { 1m }.Concat(laterMultipliers)).ToArray());
    }

    private static IReadOnlyList<decimal> CreateCoefficientSeries(decimal baseCoefficient, IReadOnlyList<decimal> progression)
    {
        return Array.AsReadOnly(progression.Select(multiplier => decimal.Round(baseCoefficient * multiplier, 2, MidpointRounding.AwayFromZero)).ToArray());
    }

    private static string CombineNotes(string? baseNote, string extraNote)
    {
        return string.IsNullOrWhiteSpace(baseNote) ? extraNote : $"{baseNote} {extraNote}";
    }

    private static string FormatCoefficient(decimal coefficient)
    {
        var rounded = decimal.Round(coefficient, 2, MidpointRounding.AwayFromZero);
        return rounded == decimal.Truncate(rounded) ? rounded.ToString("N0") : rounded.ToString("N2");
    }
}
