using War.Core.Combat;
using War.Core.Resources;

namespace War.Core.Skills.Catalogs;

public static class SorcererSkillCatalog
{
    public const string ChispaIgneaSkillId = "sorcerer.skill.chispa-ignea";
    public const string OrbeVoltaicoSkillId = "sorcerer.skill.orbe-voltaico";
    public const string LanzaGlacialSkillId = "sorcerer.skill.lanza-glacial";
    public const string AnilloIncandescenteSkillId = "sorcerer.skill.anillo-incandescente";
    public const string DescargaDeArcoSkillId = "sorcerer.skill.descarga-de-arco";
    public const string PulsoGlacialSkillId = "sorcerer.skill.pulso-glacial";
    public const string MeteoritoEscarlataSkillId = "sorcerer.skill.meteorito-escarlata";
    public const string PrisionDeEscarchaSkillId = "sorcerer.skill.prision-de-escarcha";
    public const string CadenaDeTruenoSkillId = "sorcerer.skill.cadena-de-trueno";
    public const string ColapsoTermalSkillId = "sorcerer.skill.colapso-termal";
    public const string TormentaFractalSkillId = "sorcerer.skill.tormenta-fractal";
    public const string NucleoCriogenicoSkillId = "sorcerer.skill.nucleo-criogenico";
    public const string TempestadDraconicaSkillId = "sorcerer.ultimate.tornado-dragon";
    public const string TornadoDragonSkillId = TempestadDraconicaSkillId;

    private static readonly IReadOnlyList<decimal> StandardProgression = CreateProgression(1.08m, 1.16m, 1.25m, 1.35m, 1.46m, 1.58m, 1.71m, 1.85m, 2.00m);
    private static readonly IReadOnlyList<decimal> BurstProgression = CreateProgression(1.10m, 1.20m, 1.31m, 1.43m, 1.56m, 1.70m, 1.85m, 2.01m, 2.18m);
    private static readonly IReadOnlyList<decimal> MultiHitProgression = CreateProgression(1.07m, 1.15m, 1.24m, 1.34m, 1.45m, 1.57m, 1.70m, 1.84m, 2.00m);
    private static readonly IReadOnlyList<decimal> ControlProgression = CreateProgression(1.07m, 1.15m, 1.23m, 1.32m, 1.42m, 1.53m, 1.65m, 1.78m, 1.92m);

    private static readonly IReadOnlyList<int> TempestadDamagePercentagesByAscension = Array.AsReadOnly(
    [
        680,
        748,
        823,
        905,
        1086,
        1303,
        1564,
        2033,
        2643,
        3965
    ]);

    private static readonly IReadOnlyList<SkillPendingDatum> TempestadPendingData = Array.AsReadOnly(
    [
        new SkillPendingDatum("sorcerer.tempestad-draconica.targeting-shape", "The final area footprint, acquisition logic, and cast range are pending. The current translation uses a selected target per hit and leaves range at 0 as a non-authoritative placeholder."),
        new SkillPendingDatum("sorcerer.tempestad-draconica.cooldown", "The definitive cooldown is pending. The pilot skill currently exposes a 0-second cooldown placeholder so the combat translator stays deterministic."),
        new SkillPendingDatum("sorcerer.tempestad-draconica.resource-cost", "The definitive mana or ultimate-charge cost is pending. The pilot skill currently declares no cast cost to avoid fabricating progression balance."),
        new SkillPendingDatum("sorcerer.tempestad-draconica.cc-base-chance", "The base chance for the Stun proxy is pending. The current combat translation relies on the actor's existing status-chance pipeline and can only approximate the ascension-8 increase.", true),
        new SkillPendingDatum("sorcerer.tempestad-draconica.cc-duration", "The base duration for the Stun proxy is pending. The skill already models the CC application contract, but runtime duration remains undefined.", true),
        new SkillPendingDatum("sorcerer.tempestad-draconica.heat-duration", "Heat is added from ascension 8, but its explicit base duration for this skill is pending.", true),
        new SkillPendingDatum("sorcerer.tempestad-draconica.self-heal-timing", "The exact trigger timing for the ascension-10 self-heal is pending. The pilot currently models it as an OnCompletion follow-up action after the active window ends.", true),
        new SkillPendingDatum("sorcerer.tempestad-draconica.ascension-2-3-cost", "The 2 -> 3 ascension material requirement is not defined yet.")
    ]);

    private static readonly IReadOnlyList<string> TempestadSecurityNotes = Array.AsReadOnly(
    [
        "Invulnerability must be granted before the first scheduled damage hit is released; otherwise same-frame retaliation can land before the protection window exists.",
        "Projectiles or delayed damage emitted before cast start still need authoritative timestamp checks when they resolve, or they can bypass the intended protection window.",
        "The multi-hit schedule should carry a single cast instance identifier so cancellation, rollback, or actor-queue reordering cannot duplicate or orphan remaining hits.",
        "Each scheduled hit should be rebound against live target conditions and protections when it fires; a cast-time snapshot is not authoritative for delayed executions.",
        "Cast resource validation must complete before protection grants are committed; otherwise the skill could yield free invulnerability on failed casts.",
        "The configured refresh policy is IgnoreIfAlreadyActive to avoid stacking or extending the invulnerability window by accident during future queue or replay systems.",
        "Runtime state storage must distinguish pre-existing debuffs from newly blocked debuffs, because the skill must not cleanse statuses that were already active when the cast began."
    ]);

    public static SkillDefinition ChispaIgnea { get; } = CreateChispaIgnea();
    public static SkillDefinition OrbeVoltaico { get; } = CreateOrbeVoltaico();
    public static SkillDefinition LanzaGlacial { get; } = CreateLanzaGlacial();
    public static SkillDefinition AnilloIncandescente { get; } = CreateAnilloIncandescente();
    public static SkillDefinition DescargaDeArco { get; } = CreateDescargaDeArco();
    public static SkillDefinition PulsoGlacial { get; } = CreatePulsoGlacial();
    public static SkillDefinition MeteoritoEscarlata { get; } = CreateMeteoritoEscarlata();
    public static SkillDefinition PrisionDeEscarcha { get; } = CreatePrisionDeEscarcha();
    public static SkillDefinition CadenaDeTrueno { get; } = CreateCadenaDeTrueno();
    public static SkillDefinition ColapsoTermal { get; } = CreateColapsoTermal();
    public static SkillDefinition TormentaFractal { get; } = CreateTormentaFractal();
    public static SkillDefinition NucleoCriogenico { get; } = CreateNucleoCriogenico();
    public static SkillDefinition TempestadDraconica { get; } = CreateTempestadDraconica();
    public static SkillDefinition TornadoDeDragon => TempestadDraconica;

    public static IReadOnlyList<SkillDefinition> All { get; } = Array.AsReadOnly(
    [
        ChispaIgnea,
        OrbeVoltaico,
        LanzaGlacial,
        AnilloIncandescente,
        DescargaDeArco,
        PulsoGlacial,
        MeteoritoEscarlata,
        PrisionDeEscarcha,
        CadenaDeTrueno,
        ColapsoTermal,
        TormentaFractal,
        NucleoCriogenico,
        TempestadDraconica
    ]);

    public static ClassSkillCatalog CreateCatalog()
    {
        return new ClassSkillCatalog(ClassType.Sorcerer, All);
    }

    private static SkillDefinition CreateChispaIgnea()
    {
        var coefficients = CreateCoefficientSeries(1.10m, StandardProgression);
        var ascensions = CreateDamageAscensionOverrides("Chispa Ignea", "ChispaIgnea", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("chispa-ignea-heat", CombatConditionType.Heat, 0.35m, 3.5m, "Ascension 5 unlocks Heat on impact."), "Heat can now be applied on impact.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("chispa-ignea-heat", BaseDurationSeconds: 4.5m), "Heat duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("chispa-ignea-heat", BaseApplyChance: 0.45m, ApplyChanceMultiplier: 1.15m), "Heat application became more reliable.");

        return CreateSorcererSkill(
            ChispaIgneaSkillId,
            "Chispa Ignea",
            "La hechicera dispara una chispa comprimida de fuego arcano que impacta al enemigo y explota en una breve llamarada. El ataque inflige da�o m�gico inmediato y deja una ligera inestabilidad t�rmica en el objetivo. Ascensiones superiores permiten que el impacto genere acumulaciones de calor, preparando al enemigo para futuras reacciones elementales.",
            SkillSlot.Slot01,
            1,
            Elements(SkillElementType.Fire, SkillElementType.Arcane),
            Roles(SkillCombatRole.Poke, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Heat),
                Targeting: SingleTargeting(18m),
                // Chispa Ignea: poke corto, proyectil rápido. "dispara una chispa comprimida"
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 4m, CastTimeSeconds: 0.35m),
                ResourceCosts: ManaCosts(18m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "High-range fire poke with provisional internal tuning.",
            metadata: CreateMetadata("fire", "provisional-balance"));
    }

    private static SkillDefinition CreateOrbeVoltaico()
    {
        var coefficients = CreateCoefficientSeries(1.22m, StandardProgression);
        var ascensions = CreateDamageAscensionOverrides("Orbe Voltaico", "OrbeVoltaico", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("orbe-voltaico-electrified", CombatConditionType.Electrified, 0.35m, 4m, "Ascension 5 unlocks Electrified on detonation."), "Electrified can now be applied on detonation.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("orbe-voltaico-electrified", BaseDurationSeconds: 5m), "Electrified duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("orbe-voltaico-electrified", BaseApplyChance: 0.48m), "Electrified application became more consistent.");

        return CreateSorcererSkill(
            OrbeVoltaicoSkillId,
            "Orbe Voltaico",
            "La hechicera lanza una esfera de energ�a el�ctrica que se adhiere moment�neamente al enemigo antes de estallar. La descarga inflige da�o m�gico y perturba brevemente la estabilidad energ�tica del objetivo. En niveles avanzados, la explosi�n puede electrificar al enemigo, amplificando el da�o de ataques posteriores.",
            SkillSlot.Slot02,
            4,
            Elements(SkillElementType.Lightning, SkillElementType.Arcane),
            Roles(SkillCombatRole.Poke, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Electrified),
                Targeting: SingleTargeting(18m),
                // Orbe Voltaico: proyectil se adhiere y luego estalla. Gesto de "lanzar esfera + delay"
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 6m, CastTimeSeconds: 0.45m),
                ResourceCosts: ManaCosts(22m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Single-target lightning poke that evolves into an Electrified setup tool.",
            metadata: CreateMetadata("lightning", "provisional-balance"));
    }

    private static SkillDefinition CreateLanzaGlacial()
    {
        var coefficients = CreateCoefficientSeries(1.55m, StandardProgression);
        var ascensions = CreateDamageAscensionOverrides("Lanza Glacial", "LanzaGlacial", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("lanza-glacial-cold", CombatConditionType.Cold, 0.40m, 4m, "Ascension 5 unlocks Cold buildup on impact."), "Cold can now be applied on impact.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("lanza-glacial-cold", BaseDurationSeconds: 5m), "Cold duration increased.");
        ApplyAddedEffect(ascensions, 10, CreateCrowdControlEffect("lanza-glacial-freeze", CombatConditionType.Freeze, 0.18m, 1.10m, "Ascension 10 adds a direct Freeze chance to the impact."), "Freeze chance unlocked at maximum ascension.");

        return CreateSorcererSkill(
            LanzaGlacialSkillId,
            "Lanza Glacial",
            "Una lanza de hielo arcano es proyectada a gran velocidad contra el enemigo. Inflige da�o m�gico elevado y enfr�a el cuerpo del objetivo, reduciendo ligeramente su estabilidad. Ascensiones superiores permiten que el impacto genere acumulaci�n de fr�o.",
            SkillSlot.Slot03,
            7,
            Elements(SkillElementType.Ice, SkillElementType.Arcane),
            Roles(SkillCombatRole.Poke, SkillCombatRole.Burst),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Cold),
                Targeting: SingleTargeting(20m),
                // Lanza Glacial: proyectil grande a largo alcance (20m). Requiere formar el hielo
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 8m, CastTimeSeconds: 0.55m),
                ResourceCosts: ManaCosts(28m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "High-range ice lance with strong poke pressure and late Freeze access.",
            metadata: CreateMetadata("ice", "provisional-balance"));
    }

    private static SkillDefinition CreateAnilloIncandescente()
    {
        var coefficients = CreateCoefficientSeries(1.28m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Anillo Incandescente", "AnilloIncandescente", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("anillo-incandescente-heat", CombatConditionType.Heat, 0.45m, 4m, "Ascension 5 unlocks Heat around the caster."), "Heat can now be applied to nearby enemies.");
        ApplyActionOverride(ascensions, 8, CreateDamageAction(coefficients[7], CombatConditionType.Heat, [CreateConditionSynergy("anillo-incandescente-vs-heat", CombatConditionType.Heat, 1.15m, "Ascension 8 improves damage against targets already affected by Heat.")]), "Damage now scales up against Heat targets.");
        ApplyEffectOverride(ascensions, 9, new SkillConditionEffectOverride("anillo-incandescente-heat", BaseDurationSeconds: 5m), "Heat duration increased.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("anillo-incandescente-heat", BaseApplyChance: 0.55m), "Heat application became more reliable.");

        return CreateSorcererSkill(
            AnilloIncandescenteSkillId,
            "Anillo Incandescente",
            "La hechicera libera una onda circular de llamas que se expande desde su posici�n. Los enemigos cercanos reciben da�o m�gico y quedan envueltos en energ�a t�rmica. Ascensiones superiores permiten que la onda aplique calor persistente.",
            SkillSlot.Slot04,
            10,
            Elements(SkillElementType.Fire),
            Roles(SkillCombatRole.Area, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Heat),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.Area, SkillTargetAffinity.Enemy, 0m, 4m, 4, false, "Self-centered close-area fire wave. The current combat translator resolves the selected target only until spatial AoE selection is authoritative."),
                // Anillo Incandescente: AoE self-centered. Wind-up para liberar la onda
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 9m, CastTimeSeconds: 0.65m),
                ResourceCosts: ManaCosts(30m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Close-range fire pressure wave with metadata-only area footprint for now.",
            metadata: CreateMetadata("fire", "aoe", "provisional-balance"),
            pendingData: SpatialPendingData("anillo-incandescente", "The area footprint is modeled in metadata, but the current translator still resolves a selected target only.", true));
    }

    private static SkillDefinition CreateDescargaDeArco()
    {
        var coefficients = CreateCoefficientSeries(0.58m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Descarga de Arco", "DescargaDeArco", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("descarga-de-arco-electrified", CombatConditionType.Electrified, 0.28m, 3.5m, "Ascension 5 unlocks Electrified on the fragmented discharges."), "Electrified can now be applied by the fragmented hits.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("descarga-de-arco-electrified", BaseApplyChance: 0.40m), "Electrified application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(4, 0.90m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one extra discharge before chain-runtime support exists."), "One additional discharge was added to the current single-target fallback.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("descarga-de-arco-electrified", BaseDurationSeconds: 4.5m), "Electrified duration increased.");

        return CreateSorcererSkill(
            DescargaDeArcoSkillId,
            "Descarga de Arco",
            "La hechicera dispara un rayo que se fragmenta en m�ltiples descargas menores. Inflige da�o m�gico y puede rebotar hacia objetivos cercanos. En niveles avanzados, los impactos pueden electrificar a los enemigos alcanzados.",
            SkillSlot.Slot05,
            13,
            Elements(SkillElementType.Lightning),
            Roles(SkillCombatRole.MultiHit, SkillCombatRole.Pressure, SkillCombatRole.Chain),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Electrified),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.SingleTarget, SkillTargetAffinity.Enemy, 16m, null, 3, true, "The current translator executes the fragmented discharges against the selected target. Bounce distribution remains metadata-only until nearby-target runtime exists."),
                // Descarga de Arco: rayo que se fragmenta, multi-hit rápido
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 10m, CastTimeSeconds: 0.50m),
                ResourceCosts: ManaCosts(32m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(3, 0.75m, SkillHitDistributionMode.EvenlyDistributed, true, "Three rapid discharges are scheduled on the current target until bounce runtime is implemented.")),
            FreezeAscensions(ascensions),
            notes: "Fragmented lightning burst modeled as multi-hit with metadata-only bounce behavior.",
            metadata: CreateMetadata("lightning", "multihit", "chain", "provisional-balance"),
            pendingData: SpatialPendingData("descarga-de-arco", "Bounce routing toward nearby enemies is not authoritative yet; current combat execution repeats hits on the selected target.", true));
    }

    private static SkillDefinition CreatePulsoGlacial()
    {
        var coefficients = CreateCoefficientSeries(1.18m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Pulso Glacial", "PulsoGlacial", coefficients);
        ApplyAddedEffect(ascensions, 4, CreateStateEffect("pulso-glacial-cold", CombatConditionType.Cold, 0.35m, 4m, "Ascension 4 unlocks Cold on the pulse."), "Cold can now be applied by the pulse.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("pulso-glacial-cold", BaseApplyChance: 0.45m, BaseDurationSeconds: 5m), "Cold application and duration increased.");
        ApplyAddedEffect(ascensions, 9, CreateCrowdControlEffect("pulso-glacial-freeze", CombatConditionType.Freeze, 0.20m, 1.00m, "Ascension 9 unlocks a brief Freeze chance."), "Freeze chance unlocked.");

        return CreateSorcererSkill(
            PulsoGlacialSkillId,
            "Pulso Glacial",
            "La hechicera libera una onda de fr�o concentrado que ralentiza la energ�a vital del enemigo. Inflige da�o m�gico y enfr�a al objetivo. Ascensiones avanzadas permiten que el pulso inmovilice temporalmente al enemigo congel�ndolo.",
            SkillSlot.Slot06,
            16,
            Elements(SkillElementType.Ice),
            Roles(SkillCombatRole.Control, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Cold),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.Line, SkillTargetAffinity.Enemy, 14m, null, 3, true, "Modeled as a narrow forward pulse. Spatial line selection is still metadata-only in the current translator."),
                // Pulso Glacial: onda de frío que ralentiza, requiere concentración
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 11m, CastTimeSeconds: 0.60m),
                ResourceCosts: ManaCosts(34m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Ice control pulse with late Freeze unlock.",
            metadata: CreateMetadata("ice", "control", "provisional-balance"));
    }

    private static SkillDefinition CreateMeteoritoEscarlata()
    {
        var coefficients = CreateCoefficientSeries(1.90m, BurstProgression);
        var ascensions = CreateDamageAscensionOverrides("Meteorito Escarlata", "MeteoritoEscarlata", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("meteorito-escarlata-heat", CombatConditionType.Heat, 0.45m, 4.5m, "Ascension 5 unlocks Heat on the impact burst."), "Heat can now be applied on impact.");
        // Meteorito Escarlata: salida PHYSICAL — el impacto concusivo del meteorito es fuerza bruta
        ApplyActionOverride(ascensions, 8, CreatePhysDamageAction(coefficients[7], CombatConditionType.Heat, [CreateConditionSynergy("meteorito-escarlata-vs-heat", CombatConditionType.Heat, 1.20m, "Ascension 8 improves damage against already heated targets.")]), "Damage now scales up against Heat targets.");
        ApplyEffectOverride(ascensions, 9, new SkillConditionEffectOverride("meteorito-escarlata-heat", BaseApplyChance: 0.55m), "Heat application improved.");
        ApplyActionOverride(ascensions, 10, CreatePhysDamageAction(coefficients[9], CombatConditionType.Heat, [CreateConditionSynergy("meteorito-escarlata-vs-heat", CombatConditionType.Heat, 1.30m, "Ascension 10 further improves damage against already heated targets.")]), "Heat detonation multiplier increased.");

        return CreateSorcererSkill(
            MeteoritoEscarlataSkillId,
            "Meteorito Escarlata",
            "Un fragmento incandescente desciende desde el aire e impacta violentamente al enemigo. Inflige daño físico elevado y desata una explosión térmica. En niveles superiores el impacto desata calor acumulativo.",
            SkillSlot.Slot07,
            20,
            Elements(SkillElementType.Fire),
            Roles(SkillCombatRole.Burst, SkillCombatRole.Pressure),
            new SkillTuningSnapshot(
                Action: CreatePhysDamageAction(coefficients[0], CombatConditionType.Heat),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.GroundPoint, SkillTargetAffinity.Enemy, 18m, 2.5m, 3, true, "The impact point and small splash are modeled in metadata. The current translator still resolves the selected target only."),
                // Meteorito Escarlata: "fragmento desciende desde el aire". Wind-up largo
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 14m, CastTimeSeconds: 0.90m),
                ResourceCosts: ManaCosts(40m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Heavy fire burst with late Heat detonation pressure.",
            metadata: CreateMetadata("fire", "burst", "provisional-balance"),
            pendingData: SpatialPendingData("meteorito-escarlata", "The small impact splash is documented in metadata, but the current translator still resolves a selected target only.", true));
    }

    private static SkillDefinition CreatePrisionDeEscarcha()
    {
        var coefficients = CreateCoefficientSeries(1.08m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Prision de Escarcha", "PrisionDeEscarcha", coefficients);
        ApplyAddedEffect(ascensions, 4, CreateStateEffect("prision-de-escarcha-cold", CombatConditionType.Cold, 0.45m, 4.5m, "Ascension 4 unlocks Cold on the prison impact."), "Cold can now be applied on impact.");
        ApplyAddedEffect(ascensions, 8, CreateCrowdControlEffect("prision-de-escarcha-freeze", CombatConditionType.Freeze, 0.30m, 1.25m, "Ascension 8 unlocks a direct Freeze chance."), "Freeze chance unlocked.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("prision-de-escarcha-freeze", BaseApplyChance: 0.42m), "Freeze application improved.");

        return CreateSorcererSkill(
            PrisionDeEscarchaSkillId,
            "Prisi�n de Escarcha",
            "La hechicera invoca estructuras de hielo que envuelven al enemigo. Inflige da�o m�gico moderado y entumece su movimiento. En ascensiones superiores puede congelar completamente al enemigo por un breve instante.",
            SkillSlot.Slot08,
            24,
            Elements(SkillElementType.Ice),
            Roles(SkillCombatRole.Control),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Cold),
                Targeting: SingleTargeting(15m),
                // Prisión de Escarcha: "invoca estructuras de hielo", ritual
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 12m, CastTimeSeconds: 0.75m),
                ResourceCosts: ManaCosts(36m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Focused ice control tool that matures into a direct Freeze source.",
            metadata: CreateMetadata("ice", "control", "provisional-balance"));
    }

    private static SkillDefinition CreateCadenaDeTrueno()
    {
        var coefficients = CreateCoefficientSeries(0.72m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Cadena de Trueno", "CadenaDeTrueno", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("cadena-de-trueno-electrified", CombatConditionType.Electrified, 0.30m, 3.5m, "Ascension 5 unlocks Electrified on chained hits."), "Electrified can now be applied by chained hits.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("cadena-de-trueno-electrified", BaseApplyChance: 0.40m), "Electrified application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(5, 1.10m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one more chain step to the current selected-target fallback."), "One additional chain step was added to the current fallback execution.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("cadena-de-trueno-electrified", BaseDurationSeconds: 4.5m), "Electrified duration increased.");

        return CreateSorcererSkill(
            CadenaDeTruenoSkillId,
            "Cadena de Trueno",
            "Una descarga el�ctrica se conecta entre enemigos cercanos propagando energ�a inestable. Inflige da�o m�gico y puede saltar entre m�ltiples objetivos. Ascensiones avanzadas permiten que los impactos electrifiquen a cada objetivo alcanzado.",
            SkillSlot.Slot09,
            28,
            Elements(SkillElementType.Lightning),
            Roles(SkillCombatRole.Chain, SkillCombatRole.Pressure, SkillCombatRole.MultiHit),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Electrified),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.SingleTarget, SkillTargetAffinity.Enemy, 17m, null, 4, true, "Chain spread is documented in metadata. The current translator repeats hits against the selected target until nearby-target chain runtime exists."),
                // Cadena de Trueno: descarga que se conecta entre objetivos, ejecución ágil
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 13m, CastTimeSeconds: 0.55m),
                ResourceCosts: ManaCosts(38m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(4, 0.90m, SkillHitDistributionMode.EvenlyDistributed, true, "Current fallback executes four rapid chain hits on the selected target.")),
            FreezeAscensions(ascensions),
            notes: "Chain-lightning pressure skill with metadata-only propagation.",
            metadata: CreateMetadata("lightning", "chain", "multihit", "provisional-balance"),
            pendingData: SpatialPendingData("cadena-de-trueno", "Multi-target chain propagation remains metadata-only; the current combat translator repeats hits against the selected target.", true));
    }

    private static SkillDefinition CreateColapsoTermal()
    {
        var coefficients = CreateCoefficientSeries(1.72m, BurstProgression);
        var ascensions = CreateDamageAscensionOverrides("Colapso Termal", "ColapsoTermal", coefficients);
        ApplyActionOverride(ascensions, 6, CreateDamageAction(coefficients[5], CombatConditionType.Heat, [CreateConditionSynergy("colapso-termal-vs-heat", CombatConditionType.Heat, 1.50m, "Ascension 6 increases the Heat detonation multiplier.")]), "Heat detonation multiplier increased.");
        ApplyActionOverride(ascensions, 8, CreateDamageAction(coefficients[7], CombatConditionType.Heat, [CreateConditionSynergy("colapso-termal-vs-heat", CombatConditionType.Heat, 1.65m, "Ascension 8 significantly improves detonation damage against Heat targets.")]), "Heat detonation multiplier increased again.");
        ApplyActionOverride(ascensions, 10, CreateDamageAction(coefficients[9], CombatConditionType.Heat, [CreateConditionSynergy("colapso-termal-vs-heat", CombatConditionType.Heat, 1.80m, "Ascension 10 maximizes the detonation multiplier against Heat targets.")]), "Heat detonation multiplier maximized.");

        return CreateSorcererSkill(
            ColapsoTermalSkillId,
            "Colapso Termal",
            "La hechicera desestabiliza la energ�a t�rmica del objetivo provocando una implosi�n de fuego. Inflige da�o m�gico alto. Si el enemigo ya se encuentra afectado por Heat, la energ�a t�rmica colapsa causando da�o adicional.",
            SkillSlot.Slot10,
            32,
            Elements(SkillElementType.Fire),
            Roles(SkillCombatRole.Burst, SkillCombatRole.Detonation),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Heat, [CreateConditionSynergy("colapso-termal-vs-heat", CombatConditionType.Heat, 1.35m, "Base version deals extra damage when the target is already affected by Heat.")]),
                Targeting: SingleTargeting(18m),
                // Colapso Termal: detonator, "desestabiliza la energía térmica", burst preciso
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 15m, CastTimeSeconds: 0.70m),
                ResourceCosts: ManaCosts(44m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Fire detonation skill that leverages Heat already present on the target instead of applying it directly.",
            metadata: CreateMetadata("fire", "burst", "detonation", "provisional-balance"));
    }

    private static SkillDefinition CreateTormentaFractal()
    {
        var coefficients = CreateCoefficientSeries(0.48m, MultiHitProgression);
        var ascensions = CreateDamageAscensionOverrides("Tormenta Fractal", "TormentaFractal", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("tormenta-fractal-electrified", CombatConditionType.Electrified, 0.26m, 3.5m, "Ascension 5 unlocks Electrified inside the storm."), "Electrified can now be applied by the storm.");
        ApplyEffectOverride(ascensions, 8, new SkillConditionEffectOverride("tormenta-fractal-electrified", BaseApplyChance: 0.34m), "Electrified application improved.");
        ApplyMultiHitOverride(ascensions, 9, new SkillMultiHitProfile(7, 3.5m, SkillHitDistributionMode.EvenlyDistributed, true, "Ascension 9 adds one more storm pulse before persistent-area runtime exists."), "One additional storm pulse was added.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("tormenta-fractal-electrified", BaseApplyChance: 0.42m, BaseDurationSeconds: 4.5m), "Electrified became more reliable and lasted longer.");

        return CreateSorcererSkill(
            TormentaFractalSkillId,
            "Tormenta Fractal",
            "La hechicera invoca una tormenta el�ctrica localizada que golpea repetidamente el �rea. Cada impacto inflige da�o m�gico. Ascensiones superiores permiten que m�ltiples descargas electrifiquen a los enemigos atrapados en la tormenta.",
            SkillSlot.Slot11,
            36,
            Elements(SkillElementType.Lightning),
            Roles(SkillCombatRole.Area, SkillCombatRole.MultiHit, SkillCombatRole.Control),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Electrified),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.GroundPoint, SkillTargetAffinity.Enemy, 18m, 4m, 5, true, "Persistent area coverage is modeled in metadata. The current translator resolves the selected target only while still scheduling repeated hits."),
                // Tormenta Fractal: "invoca una tormenta", ritual de alto nivel
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 16m, CastTimeSeconds: 0.85m),
                ResourceCosts: ManaCosts(46m),
                Effects: Array.Empty<SkillConditionEffectDefinition>(),
                MultiHit: new SkillMultiHitProfile(6, 3m, SkillHitDistributionMode.EvenlyDistributed, true, "Six repeated storm pulses are scheduled during the active window on the current target until spatial area runtime exists.")),
            FreezeAscensions(ascensions),
            notes: "Persistent lightning storm modeled as repeated hits with metadata-only area acquisition.",
            metadata: CreateMetadata("lightning", "area", "multihit", "control", "provisional-balance"),
            pendingData: SpatialPendingData("tormenta-fractal", "Persistent area acquisition and multi-target resolution are still metadata-only; current combat execution repeats hits against the selected target.", true));
    }

    private static SkillDefinition CreateNucleoCriogenico()
    {
        var coefficients = CreateCoefficientSeries(1.52m, ControlProgression);
        var ascensions = CreateDamageAscensionOverrides("Nucleo Criogenico", "NucleoCriogenico", coefficients);
        ApplyAddedEffect(ascensions, 5, CreateStateEffect("nucleo-criogenico-cold", CombatConditionType.Cold, 0.45m, 4.5m, "Ascension 5 unlocks deep Cold buildup."), "Cold can now be applied on impact.");
        ApplyAddedEffect(ascensions, 8, CreateCrowdControlEffect("nucleo-criogenico-freeze", CombatConditionType.Freeze, 0.28m, 1.30m, "Ascension 8 allows Freeze only when the target is already saturated with Cold.", [CombatConditionType.Cold]), "Freeze can now trigger against Cold-saturated targets.");
        ApplyEffectOverride(ascensions, 9, new SkillConditionEffectOverride("nucleo-criogenico-freeze", BaseApplyChance: 0.38m), "Freeze application improved.");
        ApplyEffectOverride(ascensions, 10, new SkillConditionEffectOverride("nucleo-criogenico-freeze", BaseDurationSeconds: 1.60m), "Freeze duration increased.");

        return CreateSorcererSkill(
            NucleoCriogenicoSkillId,
            "N�cleo Criog�nico",
            "La hechicera concentra energ�a glacial en un punto que explota en un pulso helado. Inflige da�o m�gico y enfr�a profundamente al objetivo. En niveles avanzados la energ�a liberada puede congelar al enemigo si su cuerpo ya est� saturado de fr�o.",
            SkillSlot.Slot12,
            40,
            Elements(SkillElementType.Ice),
            Roles(SkillCombatRole.Control, SkillCombatRole.Burst),
            new SkillTuningSnapshot(
                Action: CreateDamageAction(coefficients[0], CombatConditionType.Cold),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.GroundPoint, SkillTargetAffinity.Enemy, 18m, 3m, 3, true, "The cryogenic pulse footprint is modeled in metadata. The current translator resolves the selected target only."),
                // Núcleo Criogénico: "concentra energía glacial", max-level burst
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 17m, CastTimeSeconds: 0.80m),
                ResourceCosts: ManaCosts(48m),
                Effects: Array.Empty<SkillConditionEffectDefinition>()),
            FreezeAscensions(ascensions),
            notes: "Heavy ice control burst with gated Freeze on Cold-saturated targets.",
            metadata: CreateMetadata("ice", "control", "burst", "provisional-balance"),
            pendingData: SpatialPendingData("nucleo-criogenico", "The cryogenic pulse is conceptually an area burst, but the current translator still resolves the selected target only.", true));
    }

    private static SkillDefinition CreateTempestadDraconica()
    {
        return new SkillDefinition(
            TempestadDraconicaSkillId,
            "Tempestad Drac�nica",
            "La hechicera convoca una tormenta ancestral que devora el campo de batalla. Durante varios segundos, un torbellino de energ�a drac�nica golpea repetidamente al objetivo con una lluvia de impactos arcanos. Cada impacto inflige da�o m�gico masivo y puede desencadenar efectos elementales dependiendo del dominio alcanzado por la hechicera. Mientras canaliza la tempestad, la hechicera entra en un estado de invulnerabilidad absoluta, ignorando da�o y efectos de control. En sus formas m�s avanzadas, la tempestad intensifica su poder elemental y puede desencadenar reacciones devastadoras en el enemigo.",
            ClassType.Sorcerer,
            SkillSlot.Slot13,
            true,
            24,
            new SkillTuningSnapshot(
                Action: CreateDamageAction(TempestadDamagePercentagesByAscension[0] / 100m, null),
                Targeting: new SkillTargetingProfile(SkillTargetingPattern.SingleTarget, SkillTargetAffinity.Enemy, 0m, null, 1, true, "The pilot resolves each hit against a selected target. Final AoE footprint and cast range are still pending combat design data."),
                // Tempestad Dracónica (ULTIMATE): "convoca una tormenta ancestral", canalización épica
                Cadence: new SkillCadenceProfile(BaseCooldownSeconds: 0m, CastTimeSeconds: 1.50m),
                ResourceCosts: Array.Empty<SkillResourceCostDefinition>(),
                Effects: Array.AsReadOnly([CreateCrowdControlEffect("tempestad-draconica-stun-proxy", CombatConditionType.Stun, null, null, "The skill's knockdown effect currently uses Stun as the combat proxy. The exact base chance and duration are still pending.")]),
                MultiHit: new SkillMultiHitProfile(10, 3m, SkillHitDistributionMode.EvenlyDistributed, true, "Ten independent magical hits are evenly distributed across the active 3-second window; each hit reuses the same action payload shape, but runtime should inject a fresh target-state snapshot before execution."),
                CastProtections: Array.AsReadOnly([CreateInvulnerabilityGrant(3m)]),
                TriggeredActions: Array.Empty<SkillTriggeredActionDefinition>()),
            CreateTempestadAscensionOverrides(),
            Elements(SkillElementType.Arcane),
            Roles(SkillCombatRole.MultiHit, SkillCombatRole.Control, SkillCombatRole.Ultimate),
            Notes: "Sorcerer pilot ultimate. Multi-hit is modeled as a scheduled repeated combat event, not as a single aggregated damage packet.",
            Metadata: new Dictionary<string, string>
            {
                ["skill.pilot"] = "true",
                ["skill.class"] = "Sorcerer",
                ["skill.kind"] = "Ultimate",
                ["skill.multi_hit.count"] = "10",
                ["skill.multi_hit.duration_seconds"] = "3"
            },
            PendingData: TempestadPendingData,
            SecurityNotes: TempestadSecurityNotes);
    }

    private static IReadOnlyDictionary<int, SkillAscensionOverrides> CreateTempestadAscensionOverrides()
    {
        return new Dictionary<int, SkillAscensionOverrides>
        {
            [2] = new SkillAscensionOverrides(2, MagnitudeProfile: CreateMagicMagnitudeProfile(TempestadDamagePercentagesByAscension[1] / 100m, "TempestadDraconicaDamagePerHit_748pct"), Note: $"Per-hit damage increased to {TempestadDamagePercentagesByAscension[1]}% of MagicAttack."),
            [3] = new SkillAscensionOverrides(3, MagnitudeProfile: CreateMagicMagnitudeProfile(TempestadDamagePercentagesByAscension[2] / 100m, "TempestadDraconicaDamagePerHit_823pct"), Note: $"Per-hit damage increased to {TempestadDamagePercentagesByAscension[2]}% of MagicAttack."),
            [4] = new SkillAscensionOverrides(4, MagnitudeProfile: CreateMagicMagnitudeProfile(TempestadDamagePercentagesByAscension[3] / 100m, "TempestadDraconicaDamagePerHit_905pct"), Note: $"Per-hit damage increased to {TempestadDamagePercentagesByAscension[3]}% of MagicAttack."),
            [5] = new SkillAscensionOverrides(5, MagnitudeProfile: CreateMagicMagnitudeProfile(TempestadDamagePercentagesByAscension[4] / 100m, "TempestadDraconicaDamagePerHit_1086pct"), CastProtections: Array.AsReadOnly([CreateInvulnerabilityGrant(4m)]), Note: $"Per-hit damage increased to {TempestadDamagePercentagesByAscension[4]}% of MagicAttack and cast invulnerability duration increased to 4 seconds."),
            [6] = new SkillAscensionOverrides(6, MagnitudeProfile: CreateMagicMagnitudeProfile(TempestadDamagePercentagesByAscension[5] / 100m, "TempestadDraconicaDamagePerHit_1303pct"), Note: $"Per-hit damage increased to {TempestadDamagePercentagesByAscension[5]}% of MagicAttack."),
            [7] = new SkillAscensionOverrides(7, MagnitudeProfile: CreateMagicMagnitudeProfile(TempestadDamagePercentagesByAscension[6] / 100m, "TempestadDraconicaDamagePerHit_1564pct"), Note: $"Per-hit damage increased to {TempestadDamagePercentagesByAscension[6]}% of MagicAttack."),
            [8] = new SkillAscensionOverrides(8, MagnitudeProfile: CreateMagicMagnitudeProfile(TempestadDamagePercentagesByAscension[7] / 100m, "TempestadDraconicaDamagePerHit_2033pct"), EffectOverrides: Array.AsReadOnly([new SkillConditionEffectOverride("tempestad-draconica-stun-proxy", ApplyChanceMultiplier: 1.5m, Note: "Ascension 8 intends to increase the base CC chance by 50%. Until the missing base value is finalized, runtime multiplies the currently available chance inputs by 1.5 as the nearest safe proxy.")]), AddedEffects: Array.AsReadOnly([CreateStateEffect("tempestad-draconica-heat", CombatConditionType.Heat, 1m, null, "Heat is added from ascension 8. The skill-specific duration is still pending, so the condition is granted without a fixed duration snapshot.")]), Note: $"Per-hit damage increased to {TempestadDamagePercentagesByAscension[7]}% of MagicAttack, the CC chance modifier increased, and Heat was added to each hit."),
            [9] = new SkillAscensionOverrides(9, MagnitudeProfile: CreateMagicMagnitudeProfile(TempestadDamagePercentagesByAscension[8] / 100m, "TempestadDraconicaDamagePerHit_2643pct"), Note: $"Per-hit damage increased to {TempestadDamagePercentagesByAscension[8]}% of MagicAttack."),
            [10] = new SkillAscensionOverrides(10, MagnitudeProfile: CreateMagicMagnitudeProfile(TempestadDamagePercentagesByAscension[9] / 100m, "TempestadDraconicaDamagePerHit_3965pct"), TriggeredActions: Array.AsReadOnly([new SkillTriggeredActionDefinition("ascension-10-self-heal", SkillExecutionTriggerPhase.OnCompletion, new SkillActionDefinition(SkillActionType.Heal, new SkillMagnitudeProfile(0m, SkillScalingType.TargetMissingHp, 0.5m, ConfigurationName: "TempestadDraconicaAscension10SelfHeal"), null, CharacterResourceType.Hp, false, false), SkillTriggeredActionTargetSelector.Self, Array.Empty<SkillConditionEffectDefinition>(), "The ascension-10 self-heal is modeled as an OnCompletion self-targeted heal for 50% of missing HP. Final trigger timing remains pending and is explicitly tracked in pending data.")]), Note: $"Per-hit damage increased to {TempestadDamagePercentagesByAscension[9]}% of MagicAttack and a completion-phase self-heal was added.")
        };
    }

    private static SkillDefinition CreateSorcererSkill(
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
        return new SkillDefinition(id, name, description, ClassType.Sorcerer, slot, false, unlockLevel, baseTuning, ascensionOverrides, elements, roles, notes, metadata, MergePendingData(id, metadata, pendingData), securityNotes);
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

    /// <summary>
    /// Crea una acción de daño FÍSICO — mismo escalado dual que la mágica,
    /// pero el tipo de salida es Physical (se mitiga con Defense, no MagicResistance).
    /// Usado por la minoría (~10%) de skills de Sorcerer que hacen daño concusivo/impacto.
    /// </summary>
    private static SkillActionDefinition CreatePhysDamageAction(decimal coefficient, CombatConditionType? damageConditionType, IReadOnlyList<SkillConditionSynergyDefinition>? synergies = null)
    {
        return new SkillActionDefinition(SkillActionType.Damage, CreateMagicMagnitudeProfile(coefficient, $"SorcPhysDamage_{FormatCoefficient(coefficient)}"), SkillDamageType.Physical, CharacterResourceType.Hp, true, true, damageConditionType, synergies);
    }

    private static SkillActionDefinition CreateDamageAction(decimal coefficient, CombatConditionType? damageConditionType, IReadOnlyList<SkillConditionSynergyDefinition>? synergies = null)
    {
        return new SkillActionDefinition(SkillActionType.Damage, CreateMagicMagnitudeProfile(coefficient, $"MagicDamage_{FormatCoefficient(coefficient)}"), SkillDamageType.Magical, CharacterResourceType.Hp, true, true, damageConditionType, synergies);
    }

    /// <summary>
    /// Sorcerer: 90% MagicAttack + 10% PhysicalAttack (dual scaling).
    /// El jugador que invierta mínimamente en físico obtiene un retorno tangible.
    /// </summary>
    private static SkillMagnitudeProfile CreateMagicMagnitudeProfile(decimal coefficient, string configurationName)
    {
        return new SkillMagnitudeProfile(
            0m,
            SkillScalingType.MagicAttack, coefficient * 0.90m,
            SkillScalingType.PhysicalAttack, coefficient * 0.10m,
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

    private static SkillProtectionGrantDefinition CreateInvulnerabilityGrant(decimal durationSeconds)
    {
        return new SkillProtectionGrantDefinition("tempestad-draconica-cast-invulnerability", CombatProtectionType.Invulnerability, CombatProtectionBlockType.Damage | CombatProtectionBlockType.NegativeConditions | CombatProtectionBlockType.CrowdControl, durationSeconds, CombatProtectionRefreshPolicy.IgnoreIfAlreadyActive, false, "Applies at cast start, does not cleanse pre-existing negative effects, and should not alter cast-cost handling.");
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
            ["skill.class"] = "Sorcerer"
        };

        for (var index = 0; index < tags.Length; index++)
        {
            metadata[$"skill.tag.{index + 1}"] = tags[index];
        }

        return metadata;
    }

    private static IReadOnlyList<SkillPendingDatum>? SpatialPendingData(string skillKey, string description, bool blocksExactCombatSimulation)
    {
        return Array.AsReadOnly([new SkillPendingDatum($"sorcerer.{skillKey}.spatial-runtime", description, blocksExactCombatSimulation)]);
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



