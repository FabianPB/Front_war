using War.Core.PowerScore;
using War.Core.Resources;
using War.Core.Skills;
using War.Core.Stats;

namespace War.Core.Progression;

public sealed record CharacterResourceSnapshot(
    decimal CurrentHp,
    decimal CurrentMana,
    decimal UltimateCharge);

public sealed record CharacterProfileSnapshotRequest(
    Guid CharacterId,
    ClassType ClassType,
    CharacterLevelProgress Progression,
    CharacterResources Resources,
    IEnumerable<IStatSource>? AdditionalStatSources = null,
    CharacterSkillProgressCollection? SkillProgress = null,
    SkillCatalog? SkillCatalog = null,
    bool IncludePowerScore = true);

public sealed record CharacterProfileSnapshot(
    Guid CharacterId,
    ClassType ClassType,
    CharacterLevelProgress Progression,
    IReadOnlyDictionary<StatType, decimal> FinalStats,
    CharacterResourceSnapshot Resources,
    PowerScoreResult? PowerScore = null,
    IReadOnlyList<string>? Notes = null);

