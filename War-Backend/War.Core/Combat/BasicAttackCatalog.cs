using War.Core.Entities;
using War.Core.Resources;
using War.Core.Skills;

namespace War.Core.Combat;

public sealed record BasicAttackDefinition(
    string Id,
    string Name,
    ClassType ClassType,
    CombatDamageType DamageType,
    BasicAttackMagnitudeCoefficients BaseStageCoefficients,
    decimal CastTimeSeconds,
    BasicAttackComboProfile Combo,
    bool RequiresHitCheck = true,
    bool CanCrit = true,
    CharacterResourceType TargetResourceType = CharacterResourceType.Hp,
    decimal? RangeMeters = null,
    string? RangeProfileKey = null,
    IReadOnlyList<CombatConditionApplicationIntent>? PotentialEffects = null,
    string? Notes = null);

public sealed class ClassBasicAttackCatalog : IClassBasicAttackCatalog
{
    private const int ComboStageCount = 6;
    private const decimal ContinuationWindowSeconds = 2.0m;
    private const decimal SequentialStageMultiplier = 1.015m;

    private static readonly BasicAttackComboProfile DefaultComboProfile = new(
        StageCount: ComboStageCount,
        ContinuationWindowSeconds: ContinuationWindowSeconds,
        SequentialStageMultiplier: SequentialStageMultiplier,
        FinalStageMultiplierOverrideFromPrevious: null,
        FinalStageOverrideStage: ComboStageCount,
        FinalStageOverrideNote: "The sixth basic attack currently follows the default +1.5% sequential growth. If design confirms a special +5% final hit later, this combo profile is the single override point.");

    private readonly IReadOnlyDictionary<ClassType, BasicAttackDefinition> _definitions;

    public ClassBasicAttackCatalog()
    {
        _definitions = CreateDefinitions().ToDictionary(definition => definition.ClassType);
    }

    public static ClassBasicAttackCatalog Default { get; } = new();

    public BasicAttackDefinition GetRequired(ClassType classType)
    {
        return _definitions.TryGetValue(classType, out var definition)
            ? definition
            : throw new KeyNotFoundException($"No basic attack definition is registered for class {classType}.");
    }

    public IReadOnlyCollection<BasicAttackDefinition> GetAll()
    {
        return _definitions.Values.ToArray();
    }

    private static IReadOnlyList<BasicAttackDefinition> CreateDefinitions()
    {
        return Array.AsReadOnly(new[]
        {
            new BasicAttackDefinition(
                "basic.sorcerer.arcane-bolt",
                "Arcane Bolt",
                ClassType.Sorcerer,
                CombatDamageType.Magical,
                new BasicAttackMagnitudeCoefficients(
                    MagicAttackCoefficient: 0.10m,
                    PhysicalAttackCoefficient: 0.01m,
                    Note: "Sorcerer basic attacks are predominantly magical, with a small physical spillover contribution."),
                CastTimeSeconds: 0.30m,
                Combo: DefaultComboProfile,
                RangeMeters: 14m,
                RangeProfileKey: "ranged-single-target",
                Notes: "Sorcerer combo basics emphasize long-range magical poke. The sixth-hit ambiguity remains centralized in the combo profile override slot."),

            new BasicAttackDefinition(
                "basic.juramentada.sanctified-strike",
                "Sanctified Strike",
                ClassType.Juramentada,
                CombatDamageType.Magical,
                new BasicAttackMagnitudeCoefficients(
                    MagicAttackCoefficient: 0.06m,
                    PhysicalAttackCoefficient: 0.04m,
                    Note: "Juramentada basic attacks remain magical in delivery, but they convert a more balanced mix of magic and physical stats."),
                CastTimeSeconds: 0.25m,
                Combo: DefaultComboProfile,
                RangeMeters: 3m,
                RangeProfileKey: "melee-single-target",
                Notes: "Juramentada combo basics are modeled as close-range magical strikes with balanced dual-stat conversion."),

            new BasicAttackDefinition(
                "basic.lancero.piercing-thrust",
                "Piercing Thrust",
                ClassType.Lancero,
                CombatDamageType.Physical,
                new BasicAttackMagnitudeCoefficients(
                    MagicAttackCoefficient: 0.04m,
                    PhysicalAttackCoefficient: 0.06m,
                    Note: "Lancero basic attacks remain physical, but the class still converts a non-trivial magical component into pressure."),
                CastTimeSeconds: 0.23m,
                Combo: DefaultComboProfile,
                RangeMeters: 4m,
                RangeProfileKey: "polearm-melee",
                Notes: "Lancero combo basics are fast physical thrusts tuned for stable combo pressure."),

            new BasicAttackDefinition(
                "basic.bruiser.crushing-blow",
                "Crushing Blow",
                ClassType.Bruiser,
                CombatDamageType.Physical,
                new BasicAttackMagnitudeCoefficients(
                    MagicAttackCoefficient: 0.01m,
                    PhysicalAttackCoefficient: 0.09m,
                    Note: "Bruiser basic attacks are overwhelmingly physical, with only a trace magical contribution."),
                CastTimeSeconds: 0.20m,
                Combo: DefaultComboProfile,
                RangeMeters: 2.5m,
                RangeProfileKey: "heavy-melee",
                Notes: "Bruiser combo basics are intentionally the fastest among the four initial classes and remain clean physical pressure sources.")
        });
    }
}

public sealed class BasicAttackCombatTranslator : IBasicAttackCombatTranslator
{
    private readonly IBasicAttackComboResolver _comboResolver;
    private readonly IBasicAttackMagnitudeResolver _magnitudeResolver;

    public BasicAttackCombatTranslator(
        IBasicAttackComboResolver? comboResolver = null,
        IBasicAttackMagnitudeResolver? magnitudeResolver = null)
    {
        _comboResolver = comboResolver ?? BasicAttackComboResolver.Default;
        _magnitudeResolver = magnitudeResolver ?? BasicAttackMagnitudeResolver.Default;
    }

    public BasicAttackCombatPlan Prepare(
        BasicAttackDefinition definition,
        Character actor,
        Character target,
        BasicAttackRuntimeState runtimeState,
        DateTimeOffset startedAtUtc,
        CombatTargetClassification targetClassification = CombatTargetClassification.None,
        IReadOnlyCollection<CombatConditionType>? targetActiveConditions = null,
        IReadOnlyCollection<CombatProtectionState>? targetActiveProtections = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(target);

        if (actor.ClassType != definition.ClassType)
        {
            throw new InvalidOperationException(
                $"Basic attack '{definition.Id}' belongs to class {definition.ClassType}, but actor {actor.Id} is {actor.ClassType}.");
        }

        var comboResolution = _comboResolver.Resolve(definition, runtimeState, startedAtUtc);
        var magnitude = _magnitudeResolver.Resolve(definition, actor, comboResolution.StageToExecute);
        var actionName = $"{definition.Name} {comboResolution.StageToExecute}/{comboResolution.ComboLength}";
        var notes = new List<string>();

        if (!string.IsNullOrWhiteSpace(definition.Notes))
        {
            notes.Add(definition.Notes);
        }

        if (!string.IsNullOrWhiteSpace(definition.BaseStageCoefficients.Note))
        {
            notes.Add(definition.BaseStageCoefficients.Note);
        }

        if (!string.IsNullOrWhiteSpace(comboResolution.Note))
        {
            notes.Add(comboResolution.Note);
        }

        if (!string.IsNullOrWhiteSpace(magnitude.Note))
        {
            notes.Add(magnitude.Note);
        }

        var context = new CombatEventContext(
            actor,
            target,
            CombatActionKind.Damage,
            CombatActionSource.BasicAttack,
            BaseMagnitude: magnitude.FinalBaseMagnitude,
            DamageType: definition.DamageType,
            CanCrit: definition.CanCrit,
            RequiresHitCheck: definition.RequiresHitCheck,
            TargetResourceType: definition.TargetResourceType,
            DamageConditionType: null,
            TargetClassification: targetClassification,
            TargetActiveConditions: targetActiveConditions,
            TargetActiveProtections: targetActiveProtections,
            PotentialEffects: definition.PotentialEffects ?? Array.Empty<CombatConditionApplicationIntent>(),
            DeclaredResourceCosts: Array.Empty<CombatResourceCost>(),
            MagnitudeProfile: null,
            ActionName: actionName);

        return new BasicAttackCombatPlan(
            definition,
            comboResolution,
            magnitude,
            context,
            Array.AsReadOnly(notes.ToArray()));
    }
}
