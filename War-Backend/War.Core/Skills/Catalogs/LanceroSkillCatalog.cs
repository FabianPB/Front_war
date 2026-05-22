using War.Core.Combat;
using War.Core.Resources;

namespace War.Core.Skills.Catalogs;

/// <summary>
/// Complete skill catalog for the Lancero class.
///
/// Identity: Physical DPS spear warrior — highest physical attack in the game,
/// fastest basic attack, high mobility and sustained physical damage output.
/// Primary stat: PhysicalAttack (all skills scale with PhysicalAttack).
/// Damage type: Physical.
/// Range: Melee-to-mid (4–8m) with spear reach.
/// Conditions: Poison (state), Electrified (state), Paralyze (CC).
///
/// Balance rationale:
///   Lancero gains +13 PhysicalAttack per level vs Sorcerer's +14 MagicAttack.
///   At level 50 he reaches ~638 PhysAtk vs Sorcerer's ~686 MagAtk (ratio 0.929).
///   Damage coefficients are ~8–12% higher than equivalent Sorcerer slots to
///   compensate, and the kit is pure DPS with no heal slots.
/// </summary>
public static class LanceroSkillCatalog
{
    public const string EstocadaVelozSkillId = "lancero.skill.estocada-veloz";
    public const string PuntaEnvenenadaSkillId = "lancero.skill.punta-envenenada";
    public const string RelampagoDeLanzaSkillId = "lancero.skill.relampago-de-lanza";
    public const string RemolinoDeAstaSkillId = "lancero.skill.remolino-de-asta";
    public const string LluviaDeEspinasSkillId = "lancero.skill.lluvia-de-espinas";
    public const string DescargaDeAstaSkillId = "lancero.skill.descarga-de-asta";
    public const string EmpalamientoSkillId = "lancero.skill.empalamiento";
    public const string ErupcionToxicaSkillId = "lancero.skill.erupcion-toxica";
    public const string CadenaDeRayosSkillId = "lancero.skill.cadena-de-rayos";
    public const string PerforacionVitalSkillId = "lancero.skill.perforacion-vital";
    public const string TormentaDeLanzasSkillId = "lancero.skill.tormenta-de-lanzas";
    public const string LanzaCelestialSkillId = "lancero.skill.lanza-celestial";
    public const string DragonDeMilLanzasSkillId = "lancero.ultimate.dragon-de-mil-lanzas";

    private static readonly IReadOnlyList<decimal> StandardProgression = CreateProgression(1.08m, 1.16m, 1.25m, 1.35m, 1.46m, 1.58m, 1.71m, 1.85m, 2.00m);
    private static readonly IReadOnlyList<decimal> BurstProgression = CreateProgression(1.10m, 1.20m, 1.31m, 1.43m, 1.56m, 1.70m, 1.85m, 2.01m, 2.18m);
    private static readonly IReadOnlyList<decimal> MultiHitProgression = CreateProgression(1.07m, 1.15m, 1.24m, 1.34m, 1.45m, 1.57m, 1.70m, 1.84m, 2.00m);
    private static readonly IReadOnlyList<decimal> ControlProgression = CreateProgression(1.07m, 1.15m, 1.23m, 1.32m, 1.42m, 1.53m, 1.65m, 1.78m, 1.92m);

    private static readonly IReadOnlyList<int> DragonDamagePercentagesByAscension = Array.AsReadOnly(
    [
        600,
        660,
        726,
        799,
        959,
        1151,
        1381,
        1795,
        2334,
        3501
    ]);

    private static readonly IReadOnlyList<SkillPendingDatum> DragonPendingData = Array.AsReadOnly(
    [
        new SkillPendingDatum("lancero.dragon-de-mil-lanzas.targeting-shape", "The final area footprint, acquisition logic, and cast range are pending. The current translation uses a selected target per hit and leaves range at 0 as a non-authoritative placeholder."),
        new SkillPendingDatum("lancero.dragon-de-mil-lanzas.cooldown", "The definitive cooldown is pending. The pilot skill currently exposes a 0-second cooldown placeholder so the combat translator stays deterministic."),
        new SkillPendingDatum("lancero.dragon-de-mil-lanzas.resource-cost", "The definitive mana or ultimate-charge cost is pending. The pilot skill currently declares no cast cost to avoid fabricating progression balance."),
        new SkillPendingDatum("lancero.dragon-de-mil-lanzas.paralyze-base-chance", "The base chance for the Paralyze effect is pending. The current combat translation relies on the actor's existing status-chance pipeline.", true),
        new SkillPendingDatum("lancero.dragon-de-mil-lanzas.paralyze-duration", "The base duration for the Paralyze effect is pending.", true),
        new SkillPendingDatum("lancero.dragon-de-mil-lanzas.electrified-duration", "Electrified is added from ascension 8, but its explicit base duration for this skill is pending.", true),
        new SkillPendingDatum("lancero.dragon-de-mil-lanzas.self-heal-timing", "The exact trigger timing for the ascension-10 self-heal is pending. The pilot currently models it as an OnCompletion follow-up action.", true),
        new SkillPendingDatum("lancero.dragon-de-mil-lanzas.ascension-2-3-cost", "The 2 -> 3 ascension material requirement is not defined yet.")
    ]);

    private static readonly IReadOnlyList<string> DragonSecurityNotes = Array.AsReadOnly(
    [
        "Invulnerability must be granted before the first scheduled damage hit is released; otherwise same-frame retaliation can land before the protection window exists.",
        "The multi-hit schedule should carry a single cast instance identifier so cancellation, rollback, or actor-queue reordering cannot duplicate or orphan remaining hits.",
        "Each scheduled hit should be rebound against live target conditions and protections when it fires; a cast-time snapshot is not authoritative for delayed executions.",
        "Cast resource validation must complete before protection grants are committed; otherwise the skill could yield free invulnerability on failed casts.",
        "The self-heal triggered at completion must not execute if the cast was cancelled or interrupted mid-channel."
    ]);

    public static SkillDefinition EstocadaVeloz { get; } = CreateEstocadaVeloz();
    public static SkillDefinition PuntaEnvenenada { get; } = CreatePuntaEnvenenada();
    public static SkillDefinition RelampagoDeLanza { get; } = CreateRelampagoDeLanza();
    public static SkillDefinition RemolinoDeAsta { get; } = CreateRemolinoDeAsta();
    public static SkillDefinition LluviaDeEspinas { get; } = CreateLluviaDeEspinas();
    public static SkillDefinition DescargaDeAsta { get; } = CreateDescargaDeAsta();
    public static SkillDefinition Empalamiento { get; } = CreateEmpalamiento();
    public static SkillDefinition ErupcionToxica { get; } = CreateErupcionToxica();
    public static SkillDefinition CadenaDeRayos { get; } = CreateCadenaDeRayos();
    public static SkillDefinition PerforacionVital { get; } = CreatePerforacionVital();
    public static SkillDefinition TormentaDeLanzas { get; } = CreateTormentaDeLanzas();
    public static SkillDefinition LanzaCelestial { get; } = CreateLanzaCelestial();
    public static SkillDefinition DragonDeMilLanzas { get; } = CreateDragonDeMilLanzas();

    public static IReadOnlyList<SkillDefinition> All { get; } = Array.AsReadOnly(
    [
        EstocadaVeloz,
        PuntaEnvenenada,
        RelampagoDeLanza,
        RemolinoDeAsta,
        LluviaDeEspinas,
        DescargaDeAsta,
        Empalamiento,
        ErupcionToxica,
        CadenaDeRayos,
        PerforacionVital,
        TormentaDeLanzas,
        LanzaCelestial,
        DragonDeMilLanzas
    ]);

    public static ClassSkillCatalog CreateCatalog()
    {
        return new ClassSkillCatalog(ClassType.Lancero, All);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 01 — Estocada Veloz (Swift Thrust)
    //  Role: Poke / Pressure — quick spear poke, Poison at high ascension
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateEstocadaVeloz()
    {
        var coefficients = CreateCoefficientSeries(1.18m, StandardProgression);
        var ascensions = CreateDamageAscensionOverrides("Estocada Veloz", "EstocadaVeloz", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("estocada-veloz-poison", CombatConditionType.Poison, 0.35m, 3.5m, "Ascension 5 unlocks Poison on the spear thrust."), "Poison can now be applied on impact.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("estocada-veloz-poison", BaseDurationSeconds: 4.5m), "Poison duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("estocada-veloz-poison", BaseApplyChance: 0.45m, ApplyChanceMultiplier: 1.15m), "Poison application became more reliable.");

        return CreateLanceroSkill(
            EstocadaVelozSkillId,
            "Estocada Veloz",
            "El lancero ejecuta una estocada fulminante con la punta de su lanza, perforando la defensa del enemigo con velocidad sobrehumana. Inflige daño fisico inmediato y desestabiliza al objetivo. Ascensiones superiores permiten que la punta impregne al enemigo con veneno latente.",
            SkillSlot.Slot01,
            1,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Poke, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Poison),
                Targeting: SingleTargeting(4m),
                // Estocada Veloz: "velocidad sobrehumana". La skill más rápida del juego
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 4m, CastTimeSeconds: 0.20m),
                ResourceCosts: ManaCosts(14m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Fast melee spear poke that evolves into a Poison setup tool.",
            metadata: CreateMetadata("neutral", "melee", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 02 — Punta Envenenada (Poisoned Tip)
    //  Role: Poke / Pressure — dedicated Poison applicator
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreatePuntaEnvenenada()
    {
        var coefficients = CreateCoefficientSeries(1.32m, StandardProgression);
        var ascensions = CreateDamageAscensionOverrides("Punta Envenenada", "PuntaEnvenenada", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("punta-envenenada-poison", CombatConditionType.Poison, 0.38m, 4m, "Ascension 5 unlocks Poison on the envenomed spear strike."), "Poison can now be applied on impact.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("punta-envenenada-poison", BaseDurationSeconds: 5m), "Poison duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("punta-envenenada-poison", BaseApplyChance: 0.50m), "Poison application became more consistent.");

        return CreateLanceroSkill(
            PuntaEnvenenadaSkillId,
            "Punta Envenenada",
            "El lancero unta la punta de su lanza con un veneno letal y ejecuta un golpe preciso que inyecta la toxina directamente en la herida. Inflige daño fisico y corrompe la vitalidad del objetivo. Ascensiones avanzadas intensifican la potencia del veneno.",
            SkillSlot.Slot02,
            4,
            Elements(SkillElementType.Poison),
            Roles(SkillCombatRole.Poke, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Poison),
                Targeting: SingleTargeting(4m),
                // Punta Envenenada: untar la lanza + golpe preciso, ligeramente más lento
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 6m, CastTimeSeconds: 0.30m),
                ResourceCosts: ManaCosts(18m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Dedicated Poison setup strike with higher coefficient.",
            metadata: CreateMetadata("poison", "melee", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 03 — Relampago de Lanza (Spear Lightning)
    //  Role: Poke / Burst — lightning-enhanced spear strike
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateRelampagoDeLanza()
    {
        var coefficients = CreateCoefficientSeries(1.65m, StandardProgression);
        var ascensions = CreateDamageAscensionOverrides("Relampago de Lanza", "RelampagoDeLanza", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("relampago-de-lanza-electrified", CombatConditionType.Electrified, 0.40m, 4m, "Ascension 5 unlocks Electrified on the lightning thrust."), "Electrified can now be applied on impact.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("relampago-de-lanza-electrified", BaseDurationSeconds: 5m), "Electrified duration increased.");
        ApplyAddedEffect(ascensions, 10, CreateCrowdControlEffect("relampago-de-lanza-paralyze", CombatConditionType.Paralyze, 0.18m, 1.10m, "Ascension 10 unlocks a Paralyze chance on the lightning strike."), "Paralyze chance unlocked.");

        return CreateLanceroSkill(
            RelampagoDeLanzaSkillId,
            "Relampago de Lanza",
            "El lancero canaliza energia electrica a traves del asta de su lanza y la descarga en una estocada relampagueante que electrifica al enemigo. Inflige daño fisico potenciado por el rayo. Ascensiones avanzadas pueden paralizar al objetivo con la descarga.",
            SkillSlot.Slot03,
            7,
            Elements(SkillElementType.Lightning, SkillElementType.Neutral),
            Roles(SkillCombatRole.Poke, SkillCombatRole.Burst),
            new SkillTuningSnapshot(
                // Relámpago de Lanza: "canaliza energía eléctrica" → daño MÁGICO
                Action: CreateMagDamageAction(coefficients[0], CombatConditionType.Electrified),
                Targeting: SingleTargeting(6m),
                // Relámpago de Lanza: canalizar electricidad en la punta, wind-up eléctrico
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 8m, CastTimeSeconds: 0.40m),
                ResourceCosts: ManaCosts(24m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Extended-range lightning thrust with Electrified application and late Paralyze.",
            metadata: CreateMetadata("lightning", "melee", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 04 — Remolino de Asta (Shaft Whirlwind)
    //  Role: Area / Pressure — self-centered AoE with Poison
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateRemolinoDeAsta()
    {
        var coefficients = CreateCoefficientSeries(1.38m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Remolino de Asta", "RemolinoDeAsta", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("remolino-de-asta-poison", CombatConditionType.Poison, 0.45m, 4m, "Ascension 5 unlocks Poison in the whirlwind."), "Poison can now be applied to nearby enemies.");
        ApplyActionOverride(ascensions, 8, CreatePhysDamageAction(coefficients[7], CombatConditionType.Poison, [CreateConditionSynergy("remolino-de-asta-vs-poison", CombatConditionType.Poison, 1.15m, "Ascension 8 improves damage against targets already affected by Poison.")]), "Damage now scales up against Poison targets.");
        ApplyEffectOverride(ascensions, 9, new SkillConditionEffectOverride("remolino-de-asta-poison", BaseDurationSeconds: 5m), "Poison duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("remolino-de-asta-poison", BaseApplyChance: 0.55m), "Poison application became more reliable.");

        return CreateLanceroSkill(
            RemolinoDeAstaSkillId,
            "Remolino de Asta",
            "El lancero gira su lanza a velocidad vertiginosa creando un remolino de acero que golpea a todos los enemigos cercanos. Inflige daño fisico en area y puede envenenar a los afectados con fragmentos toxicos desprendidos del asta. Ascensiones avanzadas intensifican el daño contra objetivos ya envenenados.",
            SkillSlot.Slot04,
            10,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Area, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Poison),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.Area, SkillTargetAffinity.Enemy, 0m, 4m, 4, false, "Self-centered close-area whirlwind. The current combat translator resolves the selected target only until spatial AoE selection is authoritative."),
                // Remolino de Asta: "gira su lanza a velocidad vertiginosa", requiere wind-up del spin
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 9m, CastTimeSeconds: 0.55m),
                ResourceCosts: ManaCosts(26m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Close-range physical AoE pressure whirlwind with Poison application.",
            metadata: CreateMetadata("neutral", "aoe", "provisional-balance"),
            pendingData: SpatialPendingData("remolino-de-asta", "The area footprint is modeled in metadata, but the current translator still resolves a selected target only.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 05 — Lluvia de Espinas (Rain of Thorns)
    //  Role: MultiHit / Pressure — rapid Poison-applying strikes
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateLluviaDeEspinas()
    {
        var coefficients = CreateCoefficientSeries(0.62m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Lluvia de Espinas", "LluviaDeEspinas", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("lluvia-de-espinas-poison", CombatConditionType.Poison, 0.28m, 3.5m, "Ascension 5 unlocks Poison on the thorn strikes."), "Poison can now be applied by the rapid strikes.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("lluvia-de-espinas-poison", BaseApplyChance: 0.40m), "Poison application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(4, 0.75m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one extra thorn strike."), "One additional strike was added.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("lluvia-de-espinas-poison", BaseDurationSeconds: 4.5m), "Poison duration increased.");

        return CreateLanceroSkill(
            LluviaDeEspinasSkillId,
            "Lluvia de Espinas",
            "El lancero ejecuta una serie de estocadas rapidas con la punta de su lanza, desgarrando al enemigo con multiples impactos consecutivos. Cada golpe puede inyectar fragmentos de veneno en las heridas abiertas. Ascensiones superiores multiplican la saturacion toxica.",
            SkillSlot.Slot05,
            13,
            Elements(SkillElementType.Poison),
            Roles(SkillCombatRole.MultiHit, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Poison),
                Targeting: SingleTargeting(4m),
                // Lluvia de Espinas: "estocadas rápidas", multi-hit veloz
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 10m, CastTimeSeconds: 0.25m),
                ResourceCosts: ManaCosts(28m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(3, 0.75m, SkillHitDistributionMode.EvenlyDistributed, true, "Three rapid thorn strikes are scheduled against the current target.")),
            FreezeAscensions(ascensions),
            notes: "Melee multi-hit Poison applicator.",
            metadata: CreateMetadata("poison", "melee", "multihit", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 06 — Descarga de Asta (Shaft Discharge)
    //  Role: Control / Pressure — line attack with Electrified
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateDescargaDeAsta()
    {
        var coefficients = CreateCoefficientSeries(1.28m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Descarga de Asta", "DescargaDeAsta", coefficients);
        ApplyAddedEffect(ascensions, 4, CreateStateEffect("descarga-de-asta-electrified", CombatConditionType.Electrified, 0.35m, 4m, "Ascension 4 unlocks Electrified on the discharge."), "Electrified can now be applied by the discharge.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("descarga-de-asta-electrified", BaseApplyChance: 0.45m, BaseDurationSeconds: 5m), "Electrified application and duration increased.");
        ApplyAddedEffect(ascensions, 9, CreateCrowdControlEffect("descarga-de-asta-paralyze", CombatConditionType.Paralyze, 0.20m, 1.0m, "Ascension 9 unlocks an additional Paralyze chance."), "Paralyze chance unlocked.");

        return CreateLanceroSkill(
            DescargaDeAstaSkillId,
            "Descarga de Asta",
            "El lancero clava su lanza en el suelo y libera una descarga electrica lineal que recorre el terreno frente a el. Los enemigos en la linea de impacto reciben daño fisico y quedan electrificados. Ascensiones avanzadas permiten que la descarga paralice brevemente a los alcanzados.",
            SkillSlot.Slot06,
            16,
            Elements(SkillElementType.Lightning),
            Roles(SkillCombatRole.Control, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                // Descarga de Asta: "descarga eléctrica lineal" → daño MÁGICO
                Action: CreateMagDamageAction(coefficients[0], CombatConditionType.Electrified),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.Line, SkillTargetAffinity.Enemy, 6m, null, 3, true, "Linear discharge along the spear direction. The current translator resolves the selected target only until line-targeting runtime exists."),
                // Descarga de Asta: "clava su lanza en el suelo", gesto de plantar + liberar
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 11m, CastTimeSeconds: 0.50m),
                ResourceCosts: ManaCosts(30m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Mid-range line control tool with primary Electrified access and late Paralyze layering.",
            metadata: CreateMetadata("lightning", "control", "provisional-balance"),
            pendingData: SpatialPendingData("descarga-de-asta", "The line targeting footprint is modeled in metadata, but the current translator still resolves a selected target only.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 07 — Empalamiento (Impalement)
    //  Role: Burst / Pressure — heavy single-target spear strike
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateEmpalamiento()
    {
        var coefficients = CreateCoefficientSeries(2.05m, BurstProgression);
        var ascensions = CreateDamageAscensionOverrides("Empalamiento", "Empalamiento", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("empalamiento-poison", CombatConditionType.Poison, 0.45m, 4.5m, "Ascension 5 unlocks Poison on the impalement."), "Poison can now be applied on impact.");
        ApplyActionOverride(ascensions, 8, CreatePhysDamageAction(coefficients[7], CombatConditionType.Poison, [CreateConditionSynergy("empalamiento-vs-poison", CombatConditionType.Poison, 1.20m, "Ascension 8 improves damage against targets already affected by Poison.")]), "Damage now scales up against Poison targets.");
        ApplyEffectOverride(ascensions, 9, new SkillConditionEffectOverride("empalamiento-poison", BaseApplyChance: 0.55m), "Poison application improved.");
        ApplyActionOverride(ascensions, 10, CreatePhysDamageAction(coefficients[9], CombatConditionType.Poison, [CreateConditionSynergy("empalamiento-vs-poison", CombatConditionType.Poison, 1.30m, "Ascension 10 maximizes detonation damage against Poison targets.")]), "Poison synergy multiplier maximized.");

        return CreateLanceroSkill(
            EmpalamientoSkillId,
            "Empalamiento",
            "El lancero concentra toda su fuerza en un unico golpe devastador, clavando la lanza profundamente en el cuerpo del enemigo. Inflige daño fisico masivo. Si el objetivo ya esta envenenado, la herida profunda amplifica el efecto toxico causando daño adicional.",
            SkillSlot.Slot07,
            20,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Burst, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Poison),
                Targeting: SingleTargeting(4m),
                // Empalamiento: "concentra toda su fuerza en un único golpe devastador", wind-up grande
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 14m, CastTimeSeconds: 0.70m),
                ResourceCosts: ManaCosts(36m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Heavy single-target burst with escalating Poison synergy multiplier.",
            metadata: CreateMetadata("neutral", "burst", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 08 — Erupcion Toxica (Toxic Eruption)
    //  Role: Control — Poison applicator with Paralyze at high ascension
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateErupcionToxica()
    {
        var coefficients = CreateCoefficientSeries(1.18m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Erupcion Toxica", "ErupcionToxica", coefficients);
        ApplyAddedEffect(ascensions, 4, CreateStateEffect("erupcion-toxica-poison", CombatConditionType.Poison, 0.45m, 4.5m, "Ascension 4 unlocks Poison on the toxic eruption."), "Poison can now be applied on impact.");
        ApplyAddedEffect(ascensions, 8, CreateCrowdControlEffect("erupcion-toxica-paralyze", CombatConditionType.Paralyze, 0.30m, 1.25m, "Ascension 8 unlocks Paralyze on the toxic eruption."), "Paralyze can now trigger on impact.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("erupcion-toxica-paralyze", BaseApplyChance: 0.42m), "Paralyze application became more reliable.");

        return CreateLanceroSkill(
            ErupcionToxicaSkillId,
            "Erupcion Toxica",
            "El lancero golpea el suelo con su lanza liberando una erupcion de energia toxica concentrada que envuelve al enemigo. Inflige daño fisico e impregna al objetivo con veneno virulento. En ascensiones avanzadas, la concentracion toxica es tan intensa que puede paralizar momentaneamente al enemigo.",
            SkillSlot.Slot08,
            24,
            Elements(SkillElementType.Poison),
            Roles(SkillCombatRole.Control),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Poison),
                Targeting: SingleTargeting(4m),
                // Erupción Tóxica: "golpea el suelo", gesto de impacto al suelo
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 12m, CastTimeSeconds: 0.55m),
                ResourceCosts: ManaCosts(32m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Poison control tool with late Paralyze access.",
            metadata: CreateMetadata("poison", "control", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 09 — Cadena de Rayos (Lightning Chain)
    //  Role: Chain / MultiHit / Pressure — multi-hit with Electrified
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateCadenaDeRayos()
    {
        var coefficients = CreateCoefficientSeries(0.78m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Cadena de Rayos", "CadenaDeRayos", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("cadena-de-rayos-electrified", CombatConditionType.Electrified, 0.30m, 3.5m, "Ascension 5 unlocks Electrified on the chain strikes."), "Electrified can now be applied by the chained strikes.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("cadena-de-rayos-electrified", BaseApplyChance: 0.40m), "Electrified application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(5, 0.90m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one more lightning strike."), "One additional lightning strike was added.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("cadena-de-rayos-electrified", BaseDurationSeconds: 4.5m), "Electrified duration increased.");

        return CreateLanceroSkill(
            CadenaDeRayosSkillId,
            "Cadena de Rayos",
            "El lancero lanza su arma cargada de electricidad que rebota entre los enemigos cercanos, golpeando a cada uno con una descarga electrica. Cada impacto inflige daño fisico y puede electrificar al objetivo. Ascensiones superiores intensifican la carga hasta que cada impacto amenaza con electrocucion total.",
            SkillSlot.Slot09,
            28,
            Elements(SkillElementType.Lightning),
            Roles(SkillCombatRole.Chain, SkillCombatRole.MultiHit, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                // Cadena de Rayos: "cargada de electricidad que rebota" → daño MÁGICO
                Action: CreateMagDamageAction(coefficients[0], CombatConditionType.Electrified),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.SingleTarget, SkillTargetAffinity.Enemy, 6m, null, 4, true, "Chain spread is documented in metadata. The current translator repeats hits against the selected target until nearby-target chain runtime exists."),
                // Cadena de Rayos: "lanza su arma cargada", throw ágil
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 13m, CastTimeSeconds: 0.40m),
                ResourceCosts: ManaCosts(34m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(4, 0.90m, SkillHitDistributionMode.EvenlyDistributed, true, "Current fallback executes four rapid lightning strikes on the selected target.")),
            FreezeAscensions(ascensions),
            notes: "Chain lightning multi-hit with Electrified application.",
            metadata: CreateMetadata("lightning", "chain", "multihit", "provisional-balance"),
            pendingData: SpatialPendingData("cadena-de-rayos", "Multi-target chain propagation remains metadata-only; the current combat translator repeats hits against the selected target.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 10 — Perforacion Vital (Vital Perforation)
    //  Role: Burst / Detonation — detonates Poison
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreatePerforacionVital()
    {
        var coefficients = CreateCoefficientSeries(1.85m, BurstProgression);
        var ascensions = CreateDamageAscensionOverrides("Perforacion Vital", "PerforacionVital", coefficients);
        ApplyActionOverride(ascensions, 6, CreatePhysDamageAction(coefficients[5], CombatConditionType.Poison, [CreateConditionSynergy("perforacion-vital-vs-poison", CombatConditionType.Poison, 1.50m, "Ascension 6 increases the Poison detonation multiplier.")]), "Poison detonation multiplier increased.");
        ApplyActionOverride(ascensions, 8, CreatePhysDamageAction(coefficients[7], CombatConditionType.Poison, [CreateConditionSynergy("perforacion-vital-vs-poison", CombatConditionType.Poison, 1.65m, "Ascension 8 significantly improves detonation damage against Poison targets.")]), "Poison detonation multiplier increased again.");
        ApplyActionOverride(ascensions, 10, CreatePhysDamageAction(coefficients[9], CombatConditionType.Poison, [CreateConditionSynergy("perforacion-vital-vs-poison", CombatConditionType.Poison, 1.80m, "Ascension 10 maximizes the detonation multiplier against Poison targets.")]), "Poison detonation multiplier maximized.");

        return CreateLanceroSkill(
            PerforacionVitalSkillId,
            "Perforacion Vital",
            "El lancero localiza un punto vital del enemigo y ejecuta una perforacion quirurgica con su lanza. Inflige daño fisico elevado. Si el objetivo se encuentra envenenado, la herida penetrante reacciona con las toxinas causando un estallido de daño devastador.",
            SkillSlot.Slot10,
            32,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Burst, SkillCombatRole.Detonation),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Poison, [CreateConditionSynergy("perforacion-vital-vs-poison", CombatConditionType.Poison, 1.35m, "Base version deals extra damage when the target is already affected by Poison.")]),
                Targeting: SingleTargeting(4m),
                // Perforación Vital: "perforación quirúrgica", burst preciso con timing
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 15m, CastTimeSeconds: 0.60m),
                ResourceCosts: ManaCosts(40m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Poison detonation skill that leverages Poison already present on the target.",
            metadata: CreateMetadata("neutral", "burst", "detonation", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 11 — Tormenta de Lanzas (Storm of Spears)
    //  Role: Area / MultiHit / Control — AoE persistent lightning zone
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateTormentaDeLanzas()
    {
        var coefficients = CreateCoefficientSeries(0.52m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Tormenta de Lanzas", "TormentaDeLanzas", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("tormenta-de-lanzas-electrified", CombatConditionType.Electrified, 0.26m, 3.5m, "Ascension 5 unlocks Electrified inside the spear storm zone."), "Electrified can now be applied by the storm.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("tormenta-de-lanzas-electrified", BaseApplyChance: 0.34m), "Electrified application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(7, 3.0m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one more storm pulse."), "One additional storm pulse was added.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("tormenta-de-lanzas-electrified", BaseApplyChance: 0.42m, BaseDurationSeconds: 4.5m), "Electrified became more reliable and lasted longer.");

        return CreateLanceroSkill(
            TormentaDeLanzasSkillId,
            "Tormenta de Lanzas",
            "El lancero invoca una tormenta de lanzas espectrales que caen del cielo sobre un area del campo de batalla. Cada lanza impacta con fuerza electrica, golpeando repetidamente a los enemigos atrapados en la zona. Ascensiones superiores electrifican el area hasta saturar a los enemigos.",
            SkillSlot.Slot11,
            36,
            Elements(SkillElementType.Neutral, SkillElementType.Lightning),
            Roles(SkillCombatRole.Area, SkillCombatRole.MultiHit, SkillCombatRole.Control),
            new SkillTuningSnapshot(
                // Tormenta de Lanzas: "lanzas espectrales... fuerza eléctrica" → daño MÁGICO
                Action: CreateMagDamageAction(coefficients[0], CombatConditionType.Electrified),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.GroundPoint, SkillTargetAffinity.Enemy, 6m, 4m, 5, true, "Persistent area coverage is modeled in metadata. The current translator resolves the selected target only while still scheduling repeated hits."),
                // Tormenta de Lanzas: GroundPoint "invoca una tormenta", invocación ritual
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 16m, CastTimeSeconds: 0.80m),
                ResourceCosts: ManaCosts(42m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(6, 3m, SkillHitDistributionMode.EvenlyDistributed, true, "Six repeated storm pulses are scheduled during the active window.")),
            FreezeAscensions(ascensions),
            notes: "Persistent storm zone modeled as repeated hits with metadata-only area acquisition.",
            metadata: CreateMetadata("lightning", "area", "multihit", "control", "provisional-balance"),
            pendingData: SpatialPendingData("tormenta-de-lanzas", "Persistent area acquisition and multi-target resolution are still metadata-only.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 12 — Lanza Celestial (Celestial Spear)
    //  Role: Control / Burst — heavy Electrified + conditional Paralyze
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateLanzaCelestial()
    {
        var coefficients = CreateCoefficientSeries(1.62m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Lanza Celestial", "LanzaCelestial", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("lanza-celestial-electrified", CombatConditionType.Electrified, 0.45m, 4.5m, "Ascension 5 unlocks deep Electrified on celestial spear."), "Electrified can now be applied on impact.");
        ApplyAddedEffect(ascensions, 8, CreateCrowdControlEffect("lanza-celestial-paralyze", CombatConditionType.Paralyze, 0.28m, 1.30m, "Ascension 8 allows Paralyze only when the target is already Electrified.", [CombatConditionType.Electrified]), "Paralyze can now trigger against Electrified targets.");
        ApplyEffectOverride(ascensions, 9, new SkillConditionEffectOverride("lanza-celestial-paralyze", BaseApplyChance: 0.38m), "Paralyze application improved.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("lanza-celestial-paralyze", BaseDurationSeconds: 1.60m), "Paralyze duration increased.");

        return CreateLanceroSkill(
            LanzaCelestialSkillId,
            "Lanza Celestial",
            "El lancero concentra energia celestial en la punta de su lanza y la arroja hacia el cielo. La lanza cae como un rayo divino sobre un area del campo de batalla, infligiendo daño fisico y electrificando profundamente a los alcanzados. En niveles avanzados, la descarga celestial puede paralizar a los enemigos cuyo cuerpo ya esta saturado de electricidad.",
            SkillSlot.Slot12,
            40,
            Elements(SkillElementType.Lightning),
            Roles(SkillCombatRole.Control, SkillCombatRole.Burst),
            new SkillTuningSnapshot(
                // Lanza Celestial: "energía celestial... rayo divino" → daño MÁGICO
                Action: CreateMagDamageAction(coefficients[0], CombatConditionType.Electrified),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.GroundPoint, SkillTargetAffinity.Enemy, 8m, 3m, 3, true, "The celestial spear footprint is modeled in metadata. The current translator resolves the selected target only."),
                // Lanza Celestial: "arroja hacia el cielo... cae como un rayo divino", máximo wind-up
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 17m, CastTimeSeconds: 0.95m),
                ResourceCosts: ManaCosts(44m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Heavy lightning control burst with gated Paralyze on Electrified-saturated targets.",
            metadata: CreateMetadata("lightning", "control", "burst", "provisional-balance"),
            pendingData: SpatialPendingData("lanza-celestial", "The celestial spear is conceptually an area burst, but the current translator still resolves the selected target only.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 13 — Dragon de Mil Lanzas (Dragon of a Thousand Spears) — ULTIMATE
    //  Role: Ultimate — multi-hit physical strikes + invulnerability + self-heal
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateDragonDeMilLanzas()
    {
        return new SkillDefinition(
            DragonDeMilLanzasSkillId,
            "Dragon de Mil Lanzas",
            "El lancero canaliza toda su fuerza vital y desata una tempestad de mil lanzas espectrales que perforan al enemigo en una rafaga imparable. Durante varios segundos, el lancero se convierte en un torbellino de acero, descargando doce golpes devastadores a velocidad sobrehumana. Cada impacto inflige daño fisico masivo y puede desencadenar efectos de paralisis y electrificacion. Mientras canaliza, el lancero entra en un estado de invulnerabilidad absoluta. En sus formas mas avanzadas, la canalizacion culmina con una regeneracion vital que restaura parte de la vida perdida.",
            ClassType.Lancero,
            SkillSlot.Slot13,
            true,
            24,
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(DragonDamagePercentagesByAscension[0] / 100m, null),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.SingleTarget, SkillTargetAffinity.Enemy, 0m, null, 1, true, "The pilot resolves each hit against a selected target. Final AoE footprint and cast range are still pending combat design data."),
                // Dragón de Mil Lanzas (ULTIMATE): "canaliza toda su fuerza vital", el ult más rápido del juego (clase ligera)
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 0m, CastTimeSeconds: 1.10m),
                ResourceCosts: Array.Empty<SkillResourceCostDefinition>(),
                Effects: Array.AsReadOnly([CreateCrowdControlEffect("dragon-de-mil-lanzas-paralyze-proxy", CombatConditionType.Paralyze, null, null, "The skill's paralyzing effect uses Paralyze as the combat proxy. The exact base chance and duration are still pending.")]),
                MultiHit: new SkillMultiHitProfile(12, 3m, SkillHitDistributionMode.EvenlyDistributed, true, "Twelve independent physical hits are evenly distributed across the active 3-second window."),
                CastProtections: Array.AsReadOnly([CreateInvulnerabilityGrant("dragon-de-mil-lanzas-cast-invulnerability", 3m)]),
                TriggeredActions: Array.Empty<SkillTriggeredActionDefinition>()),
            CreateDragonAscensionOverrides(),
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.MultiHit, SkillCombatRole.Control, SkillCombatRole.Ultimate),
            Notes: "Lancero pilot ultimate. Multi-hit is modeled as a scheduled repeated combat event.",
            Metadata: new Dictionary<string, string>
            {
                ["skill.pilot"] = "true",
                ["skill.class"] = "Lancero",
                ["skill.kind"] = "Ultimate",
                ["skill.multi_hit.count"] = "12",
                ["skill.multi_hit.duration_seconds"] = "3"
            },
            PendingData: DragonPendingData,
            SecurityNotes: DragonSecurityNotes);
    }

    private static IReadOnlyDictionary<int, SkillAscensionOverrides> CreateDragonAscensionOverrides()
    {
        return new Dictionary<int, SkillAscensionOverrides>
        {
            [2] = new SkillAscensionOverrides(2, MagnitudeProfile: CreatePhysicalMagnitudeProfile(DragonDamagePercentagesByAscension[1] / 100m, "DragonDeMilLanzasDamagePerHit_660pct"), Note: $"Per-hit damage increased to {DragonDamagePercentagesByAscension[1]}% of PhysicalAttack."),
            [3] = new SkillAscensionOverrides(3, MagnitudeProfile: CreatePhysicalMagnitudeProfile(DragonDamagePercentagesByAscension[2] / 100m, "DragonDeMilLanzasDamagePerHit_726pct"), Note: $"Per-hit damage increased to {DragonDamagePercentagesByAscension[2]}% of PhysicalAttack."),
            [4] = new SkillAscensionOverrides(4, MagnitudeProfile: CreatePhysicalMagnitudeProfile(DragonDamagePercentagesByAscension[3] / 100m, "DragonDeMilLanzasDamagePerHit_799pct"), Note: $"Per-hit damage increased to {DragonDamagePercentagesByAscension[3]}% of PhysicalAttack."),
            [5] = new SkillAscensionOverrides(5, MagnitudeProfile: CreatePhysicalMagnitudeProfile(DragonDamagePercentagesByAscension[4] / 100m, "DragonDeMilLanzasDamagePerHit_959pct"), CastProtections: Array.AsReadOnly([CreateInvulnerabilityGrant("dragon-de-mil-lanzas-cast-invulnerability", 4m)]), Note: $"Per-hit damage increased to {DragonDamagePercentagesByAscension[4]}% of PhysicalAttack and cast invulnerability duration increased to 4 seconds."),
            [6] = new SkillAscensionOverrides(6, MagnitudeProfile: CreatePhysicalMagnitudeProfile(DragonDamagePercentagesByAscension[5] / 100m, "DragonDeMilLanzasDamagePerHit_1151pct"), Note: $"Per-hit damage increased to {DragonDamagePercentagesByAscension[5]}% of PhysicalAttack."),
            [7] = new SkillAscensionOverrides(7, MagnitudeProfile: CreatePhysicalMagnitudeProfile(DragonDamagePercentagesByAscension[6] / 100m, "DragonDeMilLanzasDamagePerHit_1381pct"), Note: $"Per-hit damage increased to {DragonDamagePercentagesByAscension[6]}% of PhysicalAttack."),
            [8] = new SkillAscensionOverrides(8, MagnitudeProfile: CreatePhysicalMagnitudeProfile(DragonDamagePercentagesByAscension[7] / 100m, "DragonDeMilLanzasDamagePerHit_1795pct"), EffectOverrides: Array.AsReadOnly([new SkillConditionEffectOverride("dragon-de-mil-lanzas-paralyze-proxy", ApplyChanceMultiplier: 1.5m, Note: "Ascension 8 increases the base Paralyze chance by 50%.")]), AddedEffects: Array.AsReadOnly([CreateStateEffect("dragon-de-mil-lanzas-electrified", CombatConditionType.Electrified, 1m, null, "Electrified is added from ascension 8. The skill-specific duration is still pending.")]), Note: $"Per-hit damage increased to {DragonDamagePercentagesByAscension[7]}% of PhysicalAttack, the Paralyze chance modifier increased, and Electrified was added to each hit."),
            [9] = new SkillAscensionOverrides(9, MagnitudeProfile: CreatePhysicalMagnitudeProfile(DragonDamagePercentagesByAscension[8] / 100m, "DragonDeMilLanzasDamagePerHit_2334pct"), Note: $"Per-hit damage increased to {DragonDamagePercentagesByAscension[8]}% of PhysicalAttack."),
            [10] = new SkillAscensionOverrides(10, MagnitudeProfile: CreatePhysicalMagnitudeProfile(DragonDamagePercentagesByAscension[9] / 100m, "DragonDeMilLanzasDamagePerHit_3501pct"), TriggeredActions: Array.AsReadOnly([new SkillTriggeredActionDefinition("ascension-10-self-heal", SkillExecutionTriggerPhase.OnCompletion, new SkillActionDefinition(SkillActionType.Heal, new SkillMagnitudeProfile(0m, SkillScalingType.TargetMissingHp, 0.3m, ConfigurationName: "DragonDeMilLanzasAscension10SelfHeal"), null, CharacterResourceType.Hp, false, false), SkillTriggeredActionTargetSelector.Self, Array.Empty<SkillConditionEffectDefinition>(), "The ascension-10 self-heal is modeled as an OnCompletion self-targeted heal for 30% of missing HP.")]), Note: $"Per-hit damage increased to {DragonDamagePercentagesByAscension[9]}% of PhysicalAttack and a completion-phase self-heal was added.")
        };
    }

    // ═════════════════════════════════════════════════════════════════════
    //  FACTORY HELPERS
    // ═════════════════════════════════════════════════════════════════════

    private static SkillDefinition CreateLanceroSkill(
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
        return new SkillDefinition(id, name, description, ClassType.Lancero, slot, false, unlockLevel, baseTuning, ascensionOverrides, elements, roles, notes, metadata, MergePendingData(id, metadata, pendingData), securityNotes);
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
    /// Salida Magical — para las ~40% de skills que canalizan energía eléctrica/tóxica.
    /// Mismo escalado dual de entrada (60/40 Phys/Mag) pero la defensa del target usa MagicResistance.
    /// </summary>
    private static SkillActionDefinition CreateMagDamageAction(decimal coefficient, CombatConditionType? damageConditionType, IReadOnlyList<SkillConditionSynergyDefinition>? synergies = null)
    {
        return new SkillActionDefinition(SkillActionType.Damage, CreatePhysicalMagnitudeProfile(coefficient, $"LancMagDamage_{FormatCoefficient(coefficient)}"), SkillDamageType.Magical, CharacterResourceType.Hp, true, true, damageConditionType, synergies);
    }

    /// <summary>
    /// Lancero: 60% PhysicalAttack + 40% MagicAttack (dual scaling).
    /// La lanza canaliza energía eléctrica/tóxica que alimenta una componente mágica.
    /// </summary>
    private static SkillMagnitudeProfile CreatePhysicalMagnitudeProfile(decimal coefficient, string configurationName)
    {
        return new SkillMagnitudeProfile(
            0m,
            SkillScalingType.PhysicalAttack, coefficient * 0.60m,
            SkillScalingType.MagicAttack, coefficient * 0.40m,
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
            ["skill.class"] = "Lancero"
        };

        for (var index = 0; index < tags.Length; index++)
        {
            metadata[$"skill.tag.{index + 1}"] = tags[index];
        }

        return metadata;
    }

    private static IReadOnlyList<SkillPendingDatum>? SpatialPendingData(string skillKey, string description, bool blocksExactCombatSimulation)
    {
        return Array.AsReadOnly([new SkillPendingDatum($"lancero.{skillKey}.spatial-runtime", description, blocksExactCombatSimulation)]);
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
