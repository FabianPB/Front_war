using War.Core.Combat;
using War.Core.Resources;

namespace War.Core.Skills.Catalogs;

/// <summary>
/// Complete skill catalog for the Juramentada class.
///
/// Identity: Melee holy hybrid — the only class with dedicated healing skills.
/// Primary stat: MagicAttack (all skills scale with MagicAttack).
/// Damage type: Magical.
/// Range: Melee (3m) with select ranged abilities (8–10m).
/// Conditions: Poison (state), Weaken (CC), Blind (CC).
///
/// Balance rationale:
///   Juramentada gains +10 MagicAttack per level vs Sorcerer's +14.
///   At level 50 she reaches ~490 MagAtk vs Sorcerer's ~686 (ratio 0.714).
///   Damage coefficients are ~12–15% higher than equivalent Sorcerer slots to
///   partially compensate, but total kit DPS is intentionally ~80% of Sorcerer
///   because two skill slots are dedicated to heals.
/// </summary>
public static class JuramentadaSkillCatalog
{
    public const string GolpeSagradoSkillId = "juramentada.skill.golpe-sagrado";
    public const string MarcaDeCorrupcionSkillId = "juramentada.skill.marca-de-corrupcion";
    public const string ResplandorSanadorSkillId = "juramentada.skill.resplandor-sanador";
    public const string OndaDeJuicioSkillId = "juramentada.skill.onda-de-juicio";
    public const string LaceracionImpiaSkillId = "juramentada.skill.laceracion-impia";
    public const string CadenasDeLuzSkillId = "juramentada.skill.cadenas-de-luz";
    public const string BendicionDeBatallaSkillId = "juramentada.skill.bendicion-de-batalla";
    public const string FlageloPurificadorSkillId = "juramentada.skill.flagelo-purificador";
    public const string JuicioRadianteSkillId = "juramentada.skill.juicio-radiante";
    public const string ColapsoEspiritualSkillId = "juramentada.skill.colapso-espiritual";
    public const string PlagaPurificadoraSkillId = "juramentada.skill.plaga-purificadora";
    public const string SentenciaDivinaSkillId = "juramentada.skill.sentencia-divina";
    public const string AvatarDelJuramentoSkillId = "juramentada.ultimate.avatar-del-juramento";

    private static readonly IReadOnlyList<decimal> StandardProgression = CreateProgression(1.08m, 1.16m, 1.25m, 1.35m, 1.46m, 1.58m, 1.71m, 1.85m, 2.00m);
    private static readonly IReadOnlyList<decimal> BurstProgression = CreateProgression(1.10m, 1.20m, 1.31m, 1.43m, 1.56m, 1.70m, 1.85m, 2.01m, 2.18m);
    private static readonly IReadOnlyList<decimal> MultiHitProgression = CreateProgression(1.07m, 1.15m, 1.24m, 1.34m, 1.45m, 1.57m, 1.70m, 1.84m, 2.00m);
    private static readonly IReadOnlyList<decimal> ControlProgression = CreateProgression(1.07m, 1.15m, 1.23m, 1.32m, 1.42m, 1.53m, 1.65m, 1.78m, 1.92m);
    private static readonly IReadOnlyList<decimal> HealProgression = CreateProgression(1.06m, 1.13m, 1.21m, 1.30m, 1.40m, 1.51m, 1.63m, 1.76m, 1.90m);

    private static readonly IReadOnlyList<int> AvatarDamagePercentagesByAscension = Array.AsReadOnly(
    [
        550,
        605,
        666,
        733,
        880,
        1056,
        1267,
        1647,
        2141,
        3212
    ]);

    private static readonly IReadOnlyList<SkillPendingDatum> AvatarPendingData = Array.AsReadOnly(
    [
        new SkillPendingDatum("juramentada.avatar-del-juramento.targeting-shape", "The final area footprint, acquisition logic, and cast range are pending. The current translation uses a selected target per hit and leaves range at 0 as a non-authoritative placeholder."),
        new SkillPendingDatum("juramentada.avatar-del-juramento.cooldown", "The definitive cooldown is pending. The pilot skill currently exposes a 0-second cooldown placeholder so the combat translator stays deterministic."),
        new SkillPendingDatum("juramentada.avatar-del-juramento.resource-cost", "The definitive mana or ultimate-charge cost is pending. The pilot skill currently declares no cast cost to avoid fabricating progression balance."),
        new SkillPendingDatum("juramentada.avatar-del-juramento.cc-base-chance", "The base chance for the Blind proxy is pending. The current combat translation relies on the actor's existing status-chance pipeline.", true),
        new SkillPendingDatum("juramentada.avatar-del-juramento.cc-duration", "The base duration for the Blind proxy is pending.", true),
        new SkillPendingDatum("juramentada.avatar-del-juramento.weaken-duration", "Weaken is added from ascension 8, but its explicit base duration for this skill is pending.", true),
        new SkillPendingDatum("juramentada.avatar-del-juramento.self-heal-timing", "The exact trigger timing for the ascension-10 self-heal is pending. The pilot currently models it as an OnCompletion follow-up action.", true),
        new SkillPendingDatum("juramentada.avatar-del-juramento.ascension-2-3-cost", "The 2 -> 3 ascension material requirement is not defined yet.")
    ]);

    private static readonly IReadOnlyList<string> AvatarSecurityNotes = Array.AsReadOnly(
    [
        "Invulnerability must be granted before the first scheduled damage hit is released; otherwise same-frame retaliation can land before the protection window exists.",
        "The multi-hit schedule should carry a single cast instance identifier so cancellation, rollback, or actor-queue reordering cannot duplicate or orphan remaining hits.",
        "Each scheduled hit should be rebound against live target conditions and protections when it fires; a cast-time snapshot is not authoritative for delayed executions.",
        "Cast resource validation must complete before protection grants are committed; otherwise the skill could yield free invulnerability on failed casts.",
        "The self-heal triggered at completion must not execute if the cast was cancelled or interrupted mid-channel."
    ]);

    public static SkillDefinition GolpeSagrado { get; } = CreateGolpeSagrado();
    public static SkillDefinition MarcaDeCorrupcion { get; } = CreateMarcaDeCorrupcion();
    public static SkillDefinition ResplandorSanador { get; } = CreateResplandorSanador();
    public static SkillDefinition OndaDeJuicio { get; } = CreateOndaDeJuicio();
    public static SkillDefinition LaceracionImpia { get; } = CreateLaceracionImpia();
    public static SkillDefinition CadenasDeLuz { get; } = CreateCadenasDeLuz();
    public static SkillDefinition BendicionDeBatalla { get; } = CreateBendicionDeBatalla();
    public static SkillDefinition FlageloPurificador { get; } = CreateFlageloPurificador();
    public static SkillDefinition JuicioRadiante { get; } = CreateJuicioRadiante();
    public static SkillDefinition ColapsoEspiritual { get; } = CreateColapsoEspiritual();
    public static SkillDefinition PlagaPurificadora { get; } = CreatePlagaPurificadora();
    public static SkillDefinition SentenciaDivina { get; } = CreateSentenciaDivina();
    public static SkillDefinition AvatarDelJuramento { get; } = CreateAvatarDelJuramento();

    public static IReadOnlyList<SkillDefinition> All { get; } = Array.AsReadOnly(
    [
        GolpeSagrado,
        MarcaDeCorrupcion,
        ResplandorSanador,
        OndaDeJuicio,
        LaceracionImpia,
        CadenasDeLuz,
        BendicionDeBatalla,
        FlageloPurificador,
        JuicioRadiante,
        ColapsoEspiritual,
        PlagaPurificadora,
        SentenciaDivina,
        AvatarDelJuramento
    ]);

    public static ClassSkillCatalog CreateCatalog()
    {
        return new ClassSkillCatalog(ClassType.Juramentada, All);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 01 — Golpe Sagrado (Holy Strike)
    //  Role: Poke / Pressure — melee holy poke, Weaken at high ascension
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateGolpeSagrado()
    {
        var coefficients = CreateCoefficientSeries(1.25m, StandardProgression);
        var ascensions = CreateDamageAscensionOverrides("Golpe Sagrado", "GolpeSagrado", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("golpe-sagrado-weaken", CombatConditionType.Weaken, 0.30m, 3.5m, "Ascension 5 unlocks Weaken on impact."), "Weaken can now be applied on impact.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("golpe-sagrado-weaken", BaseDurationSeconds: 4.5m), "Weaken duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("golpe-sagrado-weaken", BaseApplyChance: 0.40m, ApplyChanceMultiplier: 1.15m), "Weaken application became more reliable.");

        return CreateJuramentadaSkill(
            GolpeSagradoSkillId,
            "Golpe Sagrado",
            "La juramentada canaliza energía sagrada en su arma y descarga un golpe consagrado sobre el enemigo. Inflige daño mágico inmediato y sacude la resolución espiritual del objetivo. Ascensiones superiores permiten que el impacto debilite al enemigo, reduciendo su capacidad combativa.",
            SkillSlot.Slot01,
            1,
            Elements(SkillElementType.Arcane, SkillElementType.Neutral),
            Roles(SkillCombatRole.Poke, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                // Golpe Sagrado: golpe melee con arma → daño FÍSICO (la fuerza del impacto)
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Weaken),
                Targeting: SingleTargeting(3m),
                // Golpe Sagrado: poke melee directo, gesto rápido de ejecución
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 4m, CastTimeSeconds: 0.30m),
                ResourceCosts: ManaCosts(16m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Close-range holy poke that evolves into a Weaken setup tool.",
            metadata: CreateMetadata("arcane", "melee", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 02 — Marca de Corrupción (Corruption Mark)
    //  Role: Poke / Pressure — ranged Poison setup
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateMarcaDeCorrupcion()
    {
        var coefficients = CreateCoefficientSeries(1.30m, StandardProgression);
        var ascensions = CreateDamageAscensionOverrides("Marca de Corrupcion", "MarcaDeCorrupcion", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("marca-de-corrupcion-poison", CombatConditionType.Poison, 0.35m, 4m, "Ascension 5 unlocks Poison on the corruption mark."), "Poison can now be applied on impact.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("marca-de-corrupcion-poison", BaseDurationSeconds: 5m), "Poison duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("marca-de-corrupcion-poison", BaseApplyChance: 0.48m), "Poison application became more consistent.");

        return CreateJuramentadaSkill(
            MarcaDeCorrupcionSkillId,
            "Marca de Corrupcion",
            "La juramentada proyecta un sello de energía corrupta que marca al enemigo a distancia. Inflige daño mágico y debilita la resistencia vital del objetivo. En niveles avanzados, la marca impregna al enemigo con veneno espiritual.",
            SkillSlot.Slot02,
            4,
            Elements(SkillElementType.Poison),
            Roles(SkillCombatRole.Poke, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Poison),
                Targeting: SingleTargeting(8m),
                // Marca de Corrupción: proyectar sello a distancia, gesto ceremonial corto
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 6m, CastTimeSeconds: 0.40m),
                ResourceCosts: ManaCosts(20m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Mid-range Poison setup projectile.",
            metadata: CreateMetadata("poison", "ranged", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 03 — Resplandor Sanador (Healing Radiance)
    //  Role: Heal — self-heal with CC immunity at high ascension
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateResplandorSanador()
    {
        var coefficients = CreateCoefficientSeries(0.85m, HealProgression);
        var ascensions = CreateHealAscensionOverrides("Resplandor Sanador", "ResplandorSanador", coefficients);
        ApplyCastProtectionOverride(ascensions, 5,
            Array.AsReadOnly([CreateConditionShieldGrant("resplandor-sanador-condition-shield", 2m)]),
            "A brief condition immunity shield is granted on cast.");
        ApplyCastProtectionOverride(ascensions, 8,
            Array.AsReadOnly([CreateConditionShieldGrant("resplandor-sanador-condition-shield", 3m)]),
            "Condition immunity shield duration increased.");

        return CreateJuramentadaSkill(
            ResplandorSanadorSkillId,
            "Resplandor Sanador",
            "La juramentada invoca una oleada de luz restauradora que envuelve su cuerpo. Restaura puntos de vida propios basados en su poder mágico. En ascensiones superiores, la luz sagrada también otorga inmunidad breve contra efectos negativos.",
            SkillSlot.Slot03,
            7,
            Elements(SkillElementType.Arcane, SkillElementType.Neutral),
            Roles(SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreateHealAction(coefficients[0]),
                Targeting: SelfTargeting(),
                // Resplandor Sanador: auto-cura reactiva, debe ser rápida para supervivencia
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 10m, CastTimeSeconds: 0.25m),
                ResourceCosts: ManaCosts(28m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Primary maintenance self-heal. Condition shield at ascension 5 provides survivability window.",
            metadata: CreateMetadata("heal", "self", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 04 — Onda de Juicio (Judgment Wave)
    //  Role: Area / Pressure — melee AoE with Weaken
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateOndaDeJuicio()
    {
        var coefficients = CreateCoefficientSeries(1.38m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Onda de Juicio", "OndaDeJuicio", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("onda-de-juicio-weaken", CombatConditionType.Weaken, 0.45m, 4m, "Ascension 5 unlocks Weaken in the judgment wave."), "Weaken can now be applied to nearby enemies.");
        ApplyActionOverride(ascensions, 8, CreateDamageAction(coefficients[7], CombatConditionType.Weaken, [CreateConditionSynergy("onda-de-juicio-vs-weaken", CombatConditionType.Weaken, 1.15m, "Ascension 8 improves damage against targets already affected by Weaken.")]), "Damage now scales up against Weaken targets.");
        ApplyEffectOverride(ascensions, 9, new SkillConditionEffectOverride("onda-de-juicio-weaken", BaseDurationSeconds: 5m), "Weaken duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("onda-de-juicio-weaken", BaseApplyChance: 0.55m), "Weaken application became more reliable.");

        return CreateJuramentadaSkill(
            OndaDeJuicioSkillId,
            "Onda de Juicio",
            "La juramentada libera una onda expansiva de energía divina desde su posición. Los enemigos cercanos reciben daño mágico y su espíritu es sacudido por el juicio sagrado. Ascensiones superiores permiten que la onda debilite persistentemente a los afectados.",
            SkillSlot.Slot04,
            10,
            Elements(SkillElementType.Arcane),
            Roles(SkillCombatRole.Area, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Weaken),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.Area, SkillTargetAffinity.Enemy, 0m, 4m, 4, false, "Self-centered close-area judgment wave. The current combat translator resolves the selected target only until spatial AoE selection is authoritative."),
                // Onda de Juicio: AoE self-centered, wind-up para liberar la onda divina
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 9m, CastTimeSeconds: 0.55m),
                ResourceCosts: ManaCosts(28m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Close-range holy AoE pressure wave with Weaken application.",
            metadata: CreateMetadata("arcane", "aoe", "provisional-balance"),
            pendingData: SpatialPendingData("onda-de-juicio", "The area footprint is modeled in metadata, but the current translator still resolves a selected target only.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 05 — Laceración Impía (Unholy Laceration)
    //  Role: MultiHit / Pressure — rapid Poison-applying strikes
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateLaceracionImpia()
    {
        var coefficients = CreateCoefficientSeries(0.62m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Laceracion Impia", "LaceracionImpia", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("laceracion-impia-poison", CombatConditionType.Poison, 0.28m, 3.5m, "Ascension 5 unlocks Poison on the lacerating strikes."), "Poison can now be applied by the rapid strikes.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("laceracion-impia-poison", BaseApplyChance: 0.40m), "Poison application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(4, 0.90m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one extra lacerating strike."), "One additional strike was added.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("laceracion-impia-poison", BaseDurationSeconds: 4.5m), "Poison duration increased.");

        return CreateJuramentadaSkill(
            LaceracionImpiaSkillId,
            "Laceracion Impia",
            "La juramentada ejecuta una serie de cortes rápidos imbuidos de energía corrupta. Cada impacto inflige daño mágico y puede envenenar al objetivo. En niveles avanzados, los cortes multiplican la acumulación de veneno espiritual.",
            SkillSlot.Slot05,
            13,
            Elements(SkillElementType.Poison),
            Roles(SkillCombatRole.MultiHit, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                // Laceración Impía: cortes de arma → daño FÍSICO (laceración)
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Poison),
                Targeting: SingleTargeting(3m),
                // Laceración Impía: "serie de cortes rápidos", multi-hit ejecución ágil
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 10m, CastTimeSeconds: 0.45m),
                ResourceCosts: ManaCosts(30m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(3, 0.75m, SkillHitDistributionMode.EvenlyDistributed, true, "Three rapid lacerating strikes are scheduled against the current target.")),
            FreezeAscensions(ascensions),
            notes: "Melee multi-hit Poison applicator.",
            metadata: CreateMetadata("poison", "melee", "multihit", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 06 — Cadenas de Luz (Chains of Light)
    //  Role: Control / Pressure — primary Blind applicator
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateCadenasDeLuz()
    {
        var coefficients = CreateCoefficientSeries(1.25m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Cadenas de Luz", "CadenasDeLuz", coefficients);
        ApplyAddedEffect(ascensions, 4, CreateCrowdControlEffect("cadenas-de-luz-blind", CombatConditionType.Blind, 0.35m, 4m, "Ascension 4 unlocks Blind on the chains."), "Blind can now be applied by the chains.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("cadenas-de-luz-blind", BaseApplyChance: 0.45m, BaseDurationSeconds: 5m), "Blind application and duration increased.");
        ApplyAddedEffect(ascensions, 9, CreateCrowdControlEffect("cadenas-de-luz-weaken", CombatConditionType.Weaken, 0.20m, 1.5m, "Ascension 9 unlocks an additional Weaken chance."), "Weaken chance unlocked.");

        return CreateJuramentadaSkill(
            CadenasDeLuzSkillId,
            "Cadenas de Luz",
            "La juramentada invoca cadenas de energía luminosa que envuelven al enemigo. Inflige daño mágico y ciega temporalmente al objetivo con resplandor divino. Ascensiones avanzadas permiten que las cadenas también debiliten al enemigo.",
            SkillSlot.Slot06,
            16,
            Elements(SkillElementType.Arcane),
            Roles(SkillCombatRole.Control, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                // Cadenas de Luz: cadenas que envuelven y oprimen → daño FÍSICO (restricción)
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Blind),
                Targeting: SingleTargeting(10m),
                // Cadenas de Luz: "invoca cadenas", ritual divino con proyección
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 11m, CastTimeSeconds: 0.50m),
                ResourceCosts: ManaCosts(32m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Mid-range control tool with primary Blind access and late Weaken layering.",
            metadata: CreateMetadata("arcane", "control", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 07 — Bendición de Batalla (Battle Blessing)
    //  Role: Heal — emergency heal with invulnerability grant
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateBendicionDeBatalla()
    {
        var coefficients = CreateCoefficientSeries(1.30m, HealProgression);
        var ascensions = CreateHealAscensionOverrides("Bendicion de Batalla", "BendicionDeBatalla", coefficients);
        ApplyCastProtectionOverride(ascensions, 5,
            Array.AsReadOnly([CreateInvulnerabilityGrant("bendicion-de-batalla-invulnerability", 2m)]),
            "A brief invulnerability window is granted on cast.");
        ApplyCastProtectionOverride(ascensions, 8,
            Array.AsReadOnly([CreateInvulnerabilityGrant("bendicion-de-batalla-invulnerability", 3m)]),
            "Invulnerability window increased.");

        return CreateJuramentadaSkill(
            BendicionDeBatallaSkillId,
            "Bendicion de Batalla",
            "La juramentada invoca una bendición ancestral que restaura una cantidad significativa de vida y otorga protección divina momentánea. En ascensiones superiores, la bendición envuelve a la juramentada en un campo de invulnerabilidad breve.",
            SkillSlot.Slot07,
            20,
            Elements(SkillElementType.Neutral),
            Roles(SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreateHealAction(coefficients[0]),
                Targeting: SelfTargeting(),
                // Bendición de Batalla: heal mayor + invulnerabilidad, canalización más larga
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 16m, CastTimeSeconds: 0.60m),
                ResourceCosts: ManaCosts(38m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Emergency self-heal with late-game invulnerability. Highest heal coefficient in the kit.",
            metadata: CreateMetadata("heal", "self", "defensive", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 08 — Flagelo Purificador (Purifying Scourge)
    //  Role: Burst / Detonation — detonates Poison
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateFlageloPurificador()
    {
        var coefficients = CreateCoefficientSeries(2.10m, BurstProgression);
        var ascensions = CreateDamageAscensionOverrides("Flagelo Purificador", "FlageloPurificador", coefficients);
        // Flagelo Purificador: descarga violenta → daño FÍSICO (impacto)
        ApplyActionOverride(ascensions, 6, CreatePhysDamageAction(coefficients[5], CombatConditionType.Poison, [CreateConditionSynergy("flagelo-purificador-vs-poison", CombatConditionType.Poison, 1.50m, "Ascension 6 increases the Poison detonation multiplier.")]), "Poison detonation multiplier increased.");
        ApplyActionOverride(ascensions, 8, CreatePhysDamageAction(coefficients[7], CombatConditionType.Poison, [CreateConditionSynergy("flagelo-purificador-vs-poison", CombatConditionType.Poison, 1.65m, "Ascension 8 significantly improves detonation damage against Poison targets.")]), "Poison detonation multiplier increased again.");
        ApplyActionOverride(ascensions, 10, CreatePhysDamageAction(coefficients[9], CombatConditionType.Poison, [CreateConditionSynergy("flagelo-purificador-vs-poison", CombatConditionType.Poison, 1.80m, "Ascension 10 maximizes the detonation multiplier against Poison targets.")]), "Poison detonation multiplier maximized.");

        return CreateJuramentadaSkill(
            FlageloPurificadorSkillId,
            "Flagelo Purificador",
            "La juramentada concentra energía purificadora y la descarga violentamente sobre el enemigo. Inflige daño físico elevado. Si el objetivo se encuentra envenenado, la energía santa reacciona con la corrupción causando daño adicional devastador.",
            SkillSlot.Slot08,
            24,
            Elements(SkillElementType.Poison),
            Roles(SkillCombatRole.Burst, SkillCombatRole.Detonation),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Poison, [CreateConditionSynergy("flagelo-purificador-vs-poison", CombatConditionType.Poison, 1.35m, "Base version deals extra damage when the target is already affected by Poison.")]),
                Targeting: SingleTargeting(3m),
                // Flagelo Purificador: "concentra energía purificadora", burst detonator, wind-up
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 14m, CastTimeSeconds: 0.80m),
                ResourceCosts: ManaCosts(36m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Poison detonation skill that leverages Poison already present on the target.",
            metadata: CreateMetadata("poison", "burst", "detonation", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 09 — Juicio Radiante (Radiant Judgment)
    //  Role: Chain / MultiHit / Pressure — multi-hit with Blind
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateJuicioRadiante()
    {
        var coefficients = CreateCoefficientSeries(0.78m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Juicio Radiante", "JuicioRadiante", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateCrowdControlEffect("juicio-radiante-blind", CombatConditionType.Blind, 0.30m, 3.5m, "Ascension 5 unlocks Blind on the radiant strikes."), "Blind can now be applied by the chained strikes.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("juicio-radiante-blind", BaseApplyChance: 0.40m), "Blind application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(5, 1.10m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one more radiant strike."), "One additional radiant strike was added.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("juicio-radiante-blind", BaseDurationSeconds: 4.5m), "Blind duration increased.");

        return CreateJuramentadaSkill(
            JuicioRadianteSkillId,
            "Juicio Radiante",
            "La juramentada invoca un juicio divino que se manifiesta como múltiples rayos de luz sagrada. Cada impacto inflige daño mágico y puede cegar al enemigo. Ascensiones superiores intensifican la luz hasta que cada golpe amenaza con ceguera total.",
            SkillSlot.Slot09,
            28,
            Elements(SkillElementType.Arcane),
            Roles(SkillCombatRole.Chain, SkillCombatRole.MultiHit, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Blind),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.SingleTarget, SkillTargetAffinity.Enemy, 8m, null, 4, true, "Chain spread is documented in metadata. The current translator repeats hits against the selected target until nearby-target chain runtime exists."),
                // Juicio Radiante: "múltiples rayos de luz", ejecución ágil multi-hit
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 13m, CastTimeSeconds: 0.50m),
                ResourceCosts: ManaCosts(36m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(4, 0.90m, SkillHitDistributionMode.EvenlyDistributed, true, "Current fallback executes four rapid radiant strikes on the selected target.")),
            FreezeAscensions(ascensions),
            notes: "Chain holy multi-hit with Blind application.",
            metadata: CreateMetadata("arcane", "chain", "multihit", "provisional-balance"),
            pendingData: SpatialPendingData("juicio-radiante", "Multi-target chain propagation remains metadata-only; the current combat translator repeats hits against the selected target.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 10 — Colapso Espiritual (Spiritual Collapse)
    //  Role: Burst / Detonation — detonates Weaken
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateColapsoEspiritual()
    {
        var coefficients = CreateCoefficientSeries(1.85m, BurstProgression);
        var ascensions = CreateDamageAscensionOverrides("Colapso Espiritual", "ColapsoEspiritual", coefficients);
        ApplyActionOverride(ascensions, 6, CreateDamageAction(coefficients[5], CombatConditionType.Weaken, [CreateConditionSynergy("colapso-espiritual-vs-weaken", CombatConditionType.Weaken, 1.50m, "Ascension 6 increases the Weaken detonation multiplier.")]), "Weaken detonation multiplier increased.");
        ApplyActionOverride(ascensions, 8, CreateDamageAction(coefficients[7], CombatConditionType.Weaken, [CreateConditionSynergy("colapso-espiritual-vs-weaken", CombatConditionType.Weaken, 1.65m, "Ascension 8 significantly improves detonation damage against Weaken targets.")]), "Weaken detonation multiplier increased again.");
        ApplyActionOverride(ascensions, 10, CreateDamageAction(coefficients[9], CombatConditionType.Weaken, [CreateConditionSynergy("colapso-espiritual-vs-weaken", CombatConditionType.Weaken, 1.80m, "Ascension 10 maximizes the detonation multiplier against Weaken targets.")]), "Weaken detonation multiplier maximized.");

        return CreateJuramentadaSkill(
            ColapsoEspiritualSkillId,
            "Colapso Espiritual",
            "La juramentada concentra su voluntad divina y la descarga sobre un enemigo debilitado. Inflige daño mágico alto. Si el objetivo ya se encuentra afectado por Weaken, la energía espiritual colapsa causando daño devastador adicional.",
            SkillSlot.Slot10,
            32,
            Elements(SkillElementType.Arcane),
            Roles(SkillCombatRole.Burst, SkillCombatRole.Detonation),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Weaken, [CreateConditionSynergy("colapso-espiritual-vs-weaken", CombatConditionType.Weaken, 1.35m, "Base version deals extra damage when the target is already affected by Weaken.")]),
                Targeting: SingleTargeting(8m),
                // Colapso Espiritual: "concentra voluntad divina", detonator con gesto ceremonial
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 15m, CastTimeSeconds: 0.75m),
                ResourceCosts: ManaCosts(42m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Weaken detonation skill that leverages Weaken already present on the target.",
            metadata: CreateMetadata("arcane", "burst", "detonation", "provisional-balance"));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 11 — Plaga Purificadora (Purifying Plague)
    //  Role: Area / MultiHit / Control — AoE Poison + Weaken
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreatePlagaPurificadora()
    {
        var coefficients = CreateCoefficientSeries(0.55m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Plaga Purificadora", "PlagaPurificadora", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("plaga-purificadora-poison", CombatConditionType.Poison, 0.26m, 3.5m, "Ascension 5 unlocks Poison inside the plague zone."), "Poison can now be applied by the plague.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("plaga-purificadora-poison", BaseApplyChance: 0.34m), "Poison application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(7, 3.5m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one more plague pulse."), "One additional plague pulse was added.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("plaga-purificadora-poison", BaseApplyChance: 0.42m, BaseDurationSeconds: 4.5m), "Poison became more reliable and lasted longer.");

        return CreateJuramentadaSkill(
            PlagaPurificadoraSkillId,
            "Plaga Purificadora",
            "La juramentada invoca una plaga sagrada que se extiende sobre el campo de batalla. Múltiples oleadas de energía corrupta golpean repetidamente el área. Ascensiones superiores permiten que cada oleada envenene a los enemigos atrapados.",
            SkillSlot.Slot11,
            36,
            Elements(SkillElementType.Poison),
            Roles(SkillCombatRole.Area, SkillCombatRole.MultiHit, SkillCombatRole.Control),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Poison),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.GroundPoint, SkillTargetAffinity.Enemy, 8m, 4m, 5, true, "Persistent area coverage is modeled in metadata. The current translator resolves the selected target only while still scheduling repeated hits."),
                // Plaga Purificadora: GroundPoint "invoca una plaga", canalización ritual larga
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 16m, CastTimeSeconds: 0.85m),
                ResourceCosts: ManaCosts(44m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(6, 3m, SkillHitDistributionMode.EvenlyDistributed, true, "Six repeated plague pulses are scheduled during the active window.")),
            FreezeAscensions(ascensions),
            notes: "Persistent plague zone modeled as repeated hits with metadata-only area acquisition.",
            metadata: CreateMetadata("poison", "area", "multihit", "control", "provisional-balance"),
            pendingData: SpatialPendingData("plaga-purificadora", "Persistent area acquisition and multi-target resolution are still metadata-only.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 12 — Sentencia Divina (Divine Sentence)
    //  Role: Control / Burst — heavy Blind + conditional Weaken
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateSentenciaDivina()
    {
        var coefficients = CreateCoefficientSeries(1.68m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Sentencia Divina", "SentenciaDivina", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateCrowdControlEffect("sentencia-divina-blind", CombatConditionType.Blind, 0.45m, 4.5m, "Ascension 5 unlocks deep Blind on divine sentence."), "Blind can now be applied on impact.");
        ApplyAddedEffect(ascensions, 8, CreateCrowdControlEffect("sentencia-divina-weaken", CombatConditionType.Weaken, 0.28m, 1.30m, "Ascension 8 allows Weaken only when the target is already Poisoned.", [CombatConditionType.Poison]), "Weaken can now trigger against Poisoned targets.");
        ApplyEffectOverride(ascensions, 9, new SkillConditionEffectOverride("sentencia-divina-weaken", BaseApplyChance: 0.38m), "Weaken application improved.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("sentencia-divina-weaken", BaseDurationSeconds: 1.60m), "Weaken duration increased.");

        return CreateJuramentadaSkill(
            SentenciaDivinaSkillId,
            "Sentencia Divina",
            "La juramentada concentra energía sagrada en un punto que explota en un pulso de luz cegadora. Inflige daño mágico y ciega profundamente al objetivo. En niveles avanzados, la sentencia puede debilitar al enemigo si su cuerpo ya está corrompido por veneno.",
            SkillSlot.Slot12,
            40,
            Elements(SkillElementType.Arcane),
            Roles(SkillCombatRole.Control, SkillCombatRole.Burst),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Blind),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.GroundPoint, SkillTargetAffinity.Enemy, 10m, 3m, 3, true, "The divine sentence footprint is modeled in metadata. The current translator resolves the selected target only."),
                // Sentencia Divina: GroundPoint "concentra energía sagrada", burst ceremonial
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 17m, CastTimeSeconds: 0.90m),
                ResourceCosts: ManaCosts(46m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Heavy holy control burst with gated Weaken on Poison-saturated targets.",
            metadata: CreateMetadata("arcane", "control", "burst", "provisional-balance"),
            pendingData: SpatialPendingData("sentencia-divina", "The divine sentence is conceptually an area burst, but the current translator still resolves the selected target only.", true));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Slot 13 — Avatar del Juramento (Avatar of the Oath) — ULTIMATE
    //  Role: Ultimate — multi-hit holy strikes + invulnerability + self-heal
    // ─────────────────────────────────────────────────────────────────────
    private static SkillDefinition CreateAvatarDelJuramento()
    {
        return new SkillDefinition(
            AvatarDelJuramentoSkillId,
            "Avatar del Juramento",
            "La juramentada canaliza la esencia de su juramento sagrado y se transforma en un avatar de luz divina. Durante varios segundos, descarga una serie de golpes sagrados devastadores sobre el objetivo. Cada impacto inflige daño mágico masivo y puede desencadenar efectos de ceguera y debilitamiento. Mientras canaliza, la juramentada entra en un estado de invulnerabilidad absoluta. En sus formas más avanzadas, la transformación culmina con una restauración espiritual que sana parte de la vida perdida.",
            ClassType.Juramentada,
            SkillSlot.Slot13,
            true,
            24,
            new SkillTuningSnapshot(
                Action: CreateDamageAction(AvatarDamagePercentagesByAscension[0] / 100m, null),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.SingleTarget, SkillTargetAffinity.Enemy, 0m, null, 1, true, "The pilot resolves each hit against a selected target. Final AoE footprint and cast range are still pending combat design data."),
                // Avatar del Juramento (ULTIMATE): transformación divina, canalización épica
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 0m, CastTimeSeconds: 1.30m),
                ResourceCosts: Array.Empty<SkillResourceCostDefinition>(),
                Effects: Array.AsReadOnly([CreateCrowdControlEffect("avatar-del-juramento-blind-proxy", CombatConditionType.Blind, null, null, "The skill's blinding effect uses Blind as the combat proxy. The exact base chance and duration are still pending.")]),
                MultiHit: new SkillMultiHitProfile(8, 3m, SkillHitDistributionMode.EvenlyDistributed, true, "Eight independent magical hits are evenly distributed across the active 3-second window."),
                CastProtections: Array.AsReadOnly([CreateInvulnerabilityGrant("avatar-del-juramento-cast-invulnerability", 3m)]),
                TriggeredActions: Array.Empty<SkillTriggeredActionDefinition>()),
            CreateAvatarAscensionOverrides(),
            Elements(SkillElementType.Arcane),
            Roles(SkillCombatRole.MultiHit, SkillCombatRole.Control, SkillCombatRole.Ultimate),
            Notes: "Juramentada pilot ultimate. Multi-hit is modeled as a scheduled repeated combat event.",
            Metadata: new Dictionary<string, string>
            {
                ["skill.pilot"] = "true",
                ["skill.class"] = "Juramentada",
                ["skill.kind"] = "Ultimate",
                ["skill.multi_hit.count"] = "8",
                ["skill.multi_hit.duration_seconds"] = "3"
            },
            PendingData: AvatarPendingData,
            SecurityNotes: AvatarSecurityNotes);
    }

    private static IReadOnlyDictionary<int, SkillAscensionOverrides> CreateAvatarAscensionOverrides()
    {
        return new Dictionary<int, SkillAscensionOverrides>
        {
            [2] = new SkillAscensionOverrides(2, MagnitudeProfile: CreateMagicMagnitudeProfile(AvatarDamagePercentagesByAscension[1] / 100m, "AvatarDelJuramentoDamagePerHit_605pct"), Note: $"Per-hit damage increased to {AvatarDamagePercentagesByAscension[1]}% of MagicAttack."),
            [3] = new SkillAscensionOverrides(3, MagnitudeProfile: CreateMagicMagnitudeProfile(AvatarDamagePercentagesByAscension[2] / 100m, "AvatarDelJuramentoDamagePerHit_666pct"), Note: $"Per-hit damage increased to {AvatarDamagePercentagesByAscension[2]}% of MagicAttack."),
            [4] = new SkillAscensionOverrides(4, MagnitudeProfile: CreateMagicMagnitudeProfile(AvatarDamagePercentagesByAscension[3] / 100m, "AvatarDelJuramentoDamagePerHit_733pct"), Note: $"Per-hit damage increased to {AvatarDamagePercentagesByAscension[3]}% of MagicAttack."),
            [5] = new SkillAscensionOverrides(5, MagnitudeProfile: CreateMagicMagnitudeProfile(AvatarDamagePercentagesByAscension[4] / 100m, "AvatarDelJuramentoDamagePerHit_880pct"), CastProtections: Array.AsReadOnly([CreateInvulnerabilityGrant("avatar-del-juramento-cast-invulnerability", 4m)]), Note: $"Per-hit damage increased to {AvatarDamagePercentagesByAscension[4]}% of MagicAttack and cast invulnerability duration increased to 4 seconds."),
            [6] = new SkillAscensionOverrides(6, MagnitudeProfile: CreateMagicMagnitudeProfile(AvatarDamagePercentagesByAscension[5] / 100m, "AvatarDelJuramentoDamagePerHit_1056pct"), Note: $"Per-hit damage increased to {AvatarDamagePercentagesByAscension[5]}% of MagicAttack."),
            [7] = new SkillAscensionOverrides(7, MagnitudeProfile: CreateMagicMagnitudeProfile(AvatarDamagePercentagesByAscension[6] / 100m, "AvatarDelJuramentoDamagePerHit_1267pct"), Note: $"Per-hit damage increased to {AvatarDamagePercentagesByAscension[6]}% of MagicAttack."),
            [8] = new SkillAscensionOverrides(8, MagnitudeProfile: CreateMagicMagnitudeProfile(AvatarDamagePercentagesByAscension[7] / 100m, "AvatarDelJuramentoDamagePerHit_1647pct"), EffectOverrides: Array.AsReadOnly([new SkillConditionEffectOverride("avatar-del-juramento-blind-proxy", ApplyChanceMultiplier: 1.5m, Note: "Ascension 8 increases the base Blind chance by 50%.")]), AddedEffects: Array.AsReadOnly([CreateCrowdControlEffect("avatar-del-juramento-weaken", CombatConditionType.Weaken, 1m, null, "Weaken is added from ascension 8. The skill-specific duration is still pending.")]), Note: $"Per-hit damage increased to {AvatarDamagePercentagesByAscension[7]}% of MagicAttack, the Blind chance modifier increased, and Weaken was added to each hit."),
            [9] = new SkillAscensionOverrides(9, MagnitudeProfile: CreateMagicMagnitudeProfile(AvatarDamagePercentagesByAscension[8] / 100m, "AvatarDelJuramentoDamagePerHit_2141pct"), Note: $"Per-hit damage increased to {AvatarDamagePercentagesByAscension[8]}% of MagicAttack."),
            [10] = new SkillAscensionOverrides(10, MagnitudeProfile: CreateMagicMagnitudeProfile(AvatarDamagePercentagesByAscension[9] / 100m, "AvatarDelJuramentoDamagePerHit_3212pct"), TriggeredActions: Array.AsReadOnly([new SkillTriggeredActionDefinition("ascension-10-self-heal", SkillExecutionTriggerPhase.OnCompletion, new SkillActionDefinition(SkillActionType.Heal, new SkillMagnitudeProfile(0m, SkillScalingType.TargetMissingHp, 0.5m, ConfigurationName: "AvatarDelJuramentoAscension10SelfHeal"), null, CharacterResourceType.Hp, false, false), SkillTriggeredActionTargetSelector.Self, Array.Empty<SkillConditionEffectDefinition>(), "The ascension-10 self-heal is modeled as an OnCompletion self-targeted heal for 50% of missing HP.")]), Note: $"Per-hit damage increased to {AvatarDamagePercentagesByAscension[9]}% of MagicAttack and a completion-phase self-heal was added.")
        };
    }

    // ═════════════════════════════════════════════════════════════════════
    //  FACTORY HELPERS
    // ═════════════════════════════════════════════════════════════════════

    private static SkillDefinition CreateJuramentadaSkill(
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
        return new SkillDefinition(id, name, description, ClassType.Juramentada, slot, false, unlockLevel, baseTuning, ascensionOverrides, elements, roles, notes, metadata, MergePendingData(id, metadata, pendingData), securityNotes);
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
                MagnitudeProfile: CreateMagicMagnitudeProfile(coefficients[ascension - 1], $"{configPrefix}_{ascension}"),
                Note: $"{skillName} damage coefficient increased to x{FormatCoefficient(coefficients[ascension - 1])} MagicAttack.");
        }

        return overrides;
    }

    private static Dictionary<int, SkillAscensionOverrides> CreateHealAscensionOverrides(string skillName, string configPrefix, IReadOnlyList<decimal> coefficients)
    {
        var overrides = new Dictionary<int, SkillAscensionOverrides>();

        for (var ascension = 2; ascension <= SkillCatalogRules.MaximumAscensionLevel; ascension++)
        {
            overrides[ascension] = new SkillAscensionOverrides(
                AscensionLevel: ascension,
                MagnitudeProfile: CreateMagicMagnitudeProfile(coefficients[ascension - 1], $"{configPrefix}_{ascension}"),
                Note: $"{skillName} heal coefficient increased to x{FormatCoefficient(coefficients[ascension - 1])} MagicAttack.");
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

    private static SkillActionDefinition CreateDamageAction(decimal coefficient, CombatConditionType? damageConditionType, IReadOnlyList<SkillConditionSynergyDefinition>? synergies = null)
    {
        return new SkillActionDefinition(SkillActionType.Damage, CreateMagicMagnitudeProfile(coefficient, $"MagicDamage_{FormatCoefficient(coefficient)}"), SkillDamageType.Magical, CharacterResourceType.Hp, true, true, damageConditionType, synergies);
    }

    /// <summary>
    /// Salida Physical — para las ~40% de skills que hacen daño de impacto divino
    /// con el arma (golpes consagrados, laceraciones, cadenas).
    /// Mismo escalado dual de entrada (60/40) pero la defensa del target usa Defense.
    /// </summary>
    private static SkillActionDefinition CreatePhysDamageAction(decimal coefficient, CombatConditionType? damageConditionType, IReadOnlyList<SkillConditionSynergyDefinition>? synergies = null)
    {
        return new SkillActionDefinition(SkillActionType.Damage, CreateMagicMagnitudeProfile(coefficient, $"JuraPhysDamage_{FormatCoefficient(coefficient)}"), SkillDamageType.Physical, CharacterResourceType.Hp, true, true, damageConditionType, synergies);
    }

    private static SkillActionDefinition CreateHealAction(decimal coefficient)
    {
        return new SkillActionDefinition(SkillActionType.Heal, CreateMagicMagnitudeProfile(coefficient, $"MagicHeal_{FormatCoefficient(coefficient)}"), null, CharacterResourceType.Hp, false, false, null, null);
    }

    /// <summary>
    /// Juramentada: 60% MagicAttack + 40% PhysicalAttack (dual scaling).
    /// Clase híbrida divina — su arma santa contribuye daño físico significativo.
    /// </summary>
    private static SkillMagnitudeProfile CreateMagicMagnitudeProfile(decimal coefficient, string configurationName)
    {
        return new SkillMagnitudeProfile(
            0m,
            SkillScalingType.MagicAttack, coefficient * 0.60m,
            SkillScalingType.PhysicalAttack, coefficient * 0.40m,
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

    private static SkillTargetingProfile SelfTargeting()
    {
        return new SkillTargetingProfile(SkillTargetingPattern.Self, SkillTargetAffinity.Self, 0m, null, 1, false, "Self-targeted skill. No enemy selection required.");
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
            ["skill.class"] = "Juramentada"
        };

        for (var index = 0; index < tags.Length; index++)
        {
            metadata[$"skill.tag.{index + 1}"] = tags[index];
        }

        return metadata;
    }

    private static IReadOnlyList<SkillPendingDatum>? SpatialPendingData(string skillKey, string description, bool blocksExactCombatSimulation)
    {
        return Array.AsReadOnly([new SkillPendingDatum($"juramentada.{skillKey}.spatial-runtime", description, blocksExactCombatSimulation)]);
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
