namespace War.Api.Application.Characters;

public sealed record CharacterSnapshotDto(
    Guid Id,
    string Name,
    string ClassKey,
    string ClassLabel,
    CharacterProgressDto Progress,
    CharacterBasicComboDto? BasicCombo,
    IReadOnlyList<CharacterResourceMetricDto> Resources,
    IReadOnlyList<CharacterStatSectionDto> StatSections,
    CharacterPowerScoreDto? PowerScore,
    bool IsDefeated,
    IReadOnlyList<string> Notes);

public sealed record CharacterProgressDto(
    int Level,
    long CurrentXp,
    long XpToNextLevel,
    long TotalXp,
    long RemainingXpToNextLevel,
    decimal ProgressRatio);

public sealed record CharacterResourceMetricDto(
    string Key,
    string Label,
    decimal Current,
    decimal? Maximum,
    string CurrentDisplay,
    string? MaximumDisplay,
    string DisplayValue);

public sealed record CharacterStatSectionDto(
    string Key,
    string Label,
    IReadOnlyList<CharacterStatEntryDto> Stats);

public sealed record CharacterStatEntryDto(
    string Key,
    string Label,
    decimal Value,
    string DisplayValue,
    bool IsZero,
    string DisplayKind);

public sealed record CharacterPowerScoreDto(
    decimal Total,
    string TotalDisplay,
    IReadOnlyList<CharacterPowerScoreCategoryDto> Categories,
    IReadOnlyList<CharacterPowerScoreTopStatDto> TopStats,
    IReadOnlyList<string> Notes);

public sealed record CharacterPowerScoreCategoryDto(
    string Key,
    string Label,
    decimal Contribution,
    string ContributionDisplay,
    decimal Share,
    string ShareDisplay);

public sealed record CharacterPowerScoreTopStatDto(
    string Key,
    string Label,
    decimal Contribution,
    string ContributionDisplay,
    string StatValueDisplay);

public sealed record CharacterBasicComboDto(
    int LastCompletedStage,
    int NextStage,
    int ComboLength,
    bool IsContinuationWindowActive,
    decimal ContinuationWindowSeconds,
    decimal CastTimeSeconds,
    decimal? WindowRemainingSeconds,
    string StatusLabel,
    IReadOnlyList<string> Notes);
