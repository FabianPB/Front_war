using System.Text.Json;
using War.Core.Combat;
using War.Core.PowerScore;
using War.Core.Skills;

namespace War.Api.Application.SkillAdmin;

public enum SkillAdminOrigin
{
    CatalogImport,
    AdminCreated
}

public enum SkillAdminCompletenessStatus
{
    Complete,
    Pending,
    Invalid,
    Archived
}

public enum SkillAdminPublicationState
{
    Draft,
    Published,
    PublishedWithDraft,
    Archived
}

public sealed record SkillAdminOptionItemDto(
    string Key,
    string Label,
    string? Description = null);

public sealed record SkillAdminConditionInteractionRuleDto(
    string Key,
    IReadOnlyList<string> Conditions,
    string Status,
    decimal? FinalDamageIncreasePercentage,
    string? AdditionalCondition,
    string Description,
    string? FutureRuleNote);

public sealed record SkillAdminOptionsDto(
    IReadOnlyList<SkillAdminOptionItemDto> Classes,
    IReadOnlyList<SkillAdminOptionItemDto> Slots,
    IReadOnlyList<SkillAdminOptionItemDto> ActionTypes,
    IReadOnlyList<SkillAdminOptionItemDto> DamageTypes,
    IReadOnlyList<SkillAdminOptionItemDto> ScalingTypes,
    IReadOnlyList<SkillAdminOptionItemDto> TargetPatterns,
    IReadOnlyList<SkillAdminOptionItemDto> TargetAffinities,
    IReadOnlyList<SkillAdminOptionItemDto> ConditionTypes,
    IReadOnlyList<SkillAdminOptionItemDto> ResourceTypes,
    IReadOnlyList<SkillAdminOptionItemDto> ProtectionTypes,
    IReadOnlyList<SkillAdminOptionItemDto> ProtectionBlockTypes,
    IReadOnlyList<SkillAdminOptionItemDto> ProtectionRefreshPolicies,
    IReadOnlyList<SkillAdminOptionItemDto> TriggerPhases,
    IReadOnlyList<SkillAdminOptionItemDto> TriggerTargets,
    IReadOnlyList<SkillAdminOptionItemDto> HitDistributionModes,
    IReadOnlyList<SkillAdminOptionItemDto> Elements,
    IReadOnlyList<SkillAdminOptionItemDto> CombatRoles,
    IReadOnlyList<SkillAdminOptionItemDto> AscensionMaterialTypes,
    IReadOnlyList<SkillAdminConditionInteractionRuleDto> ElementalMatrixRules);

public sealed record SkillAdminPublicationDto(
    SkillAdminPublicationState State,
    int DraftVersion,
    int? PublishedVersion,
    DateTimeOffset? PublishedAtUtc,
    string? PublishedBy,
    bool HasProgrammedFallback,
    bool IsRuntimePublished,
    bool HasUnpublishedChanges,
    string RuntimeResolution,
    IReadOnlyList<string> Notes);

public sealed record SkillAdminValidationIssueDto(
    string Code,
    string Message,
    string? SkillId,
    string? ClassKey,
    string? SlotKey);

public sealed record SkillAdminCombatTranslationPreviewDto(
    string Label,
    int AscensionLevel,
    bool CanTranslate,
    bool HasBlockingPendingData,
    int ScheduledEventCount,
    int EffectCount,
    int CastProtectionCount,
    int TriggeredActionCount,
    string? ActionType,
    string? DamageType,
    string? ScalingType,
    IReadOnlyList<string> Notes);

public sealed record SkillAdminPowerScoreCategoryDeltaDto(
    string CategoryKey,
    string CategoryLabel,
    decimal BaselineContribution,
    decimal ProjectedContribution,
    decimal Delta,
    string DeltaDisplay);

public sealed record SkillAdminPowerScoreImpactDto(
    string Label,
    int ReferenceLevel,
    int AscensionLevel,
    decimal BaselineTotal,
    decimal ProjectedTotal,
    decimal Delta,
    string BaselineDisplay,
    string ProjectedDisplay,
    string DeltaDisplay,
    IReadOnlyList<SkillAdminPowerScoreCategoryDeltaDto> Categories,
    IReadOnlyList<string> Notes);

public sealed record SkillAdminAscensionEntryDto(
    int Level,
    bool IsBaseLevel,
    bool HasOverride,
    SkillAscensionOverrides? Override,
    SkillAscensionUpgradeCost? UpgradeCost,
    IReadOnlyList<string> Highlights);

public sealed record SkillAdminPreviewDto(
    SkillAdminCompletenessStatus CompletenessStatus,
    bool CanSaveDraft,
    bool CanPublish,
    bool CanTranslateToCombat,
    int ValidationIssueCount,
    int RuntimeCatalogIssueCount,
    int PendingDataCount,
    bool HasBlockingPendingData,
    IReadOnlyList<SkillAdminValidationIssueDto> ValidationIssues,
    IReadOnlyList<SkillAdminValidationIssueDto> RuntimeCatalogIssues,
    IReadOnlyList<SkillAdminCombatTranslationPreviewDto> CombatPreviews,
    IReadOnlyList<SkillAdminPowerScoreImpactDto> PowerScoreImpacts,
    IReadOnlyList<string> RuntimeCatalogNotes,
    IReadOnlyList<string> PendingData,
    IReadOnlyList<string> Notes);

public sealed record SkillAdminSummaryDto(
    Guid RecordId,
    string SkillId,
    string Name,
    string ClassKey,
    string ClassLabel,
    string SlotKey,
    int SlotOrder,
    bool IsUltimate,
    int UnlockLevel,
    SkillAdminOrigin Origin,
    SkillAdminCompletenessStatus CompletenessStatus,
    SkillAdminPublicationDto Publication,
    bool CanTranslateToCombat,
    int ValidationIssueCount,
    int PendingDataCount,
    bool HasBlockingPendingData,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<string> Notes);

public sealed record SkillAdminClassOverviewDto(
    string ClassKey,
    string ClassLabel,
    int ActiveSkillCount,
    int UltimateCount,
    bool HasFullKit,
    SkillAdminCompletenessStatus CompletenessStatus,
    IReadOnlyList<SkillAdminSummaryDto> Skills,
    IReadOnlyList<string> Notes);

public sealed record SkillAdminOverviewDto(
    IReadOnlyList<SkillAdminClassOverviewDto> Classes,
    SkillAdminOptionsDto Options,
    IReadOnlyList<string> Notes);

public sealed record SkillAdminDetailDto(
    Guid RecordId,
    SkillAdminOrigin Origin,
    SkillAdminPublicationDto Publication,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    SkillDefinition Definition,
    SkillAdminPreviewDto Preview,
    IReadOnlyList<SkillAdminAscensionEntryDto> Ascensions,
    IReadOnlyList<string> Notes);

public sealed record SkillAdminUpsertRequest(
    SkillDefinition Definition);

public sealed record SkillAdminUpsertDocumentDto(
    JsonElement Definition);

public sealed record SkillAdminStateChangeResultDto(
    Guid RecordId,
    string SkillId,
    SkillAdminPublicationState PublicationState,
    int DraftVersion,
    int? PublishedVersion,
    DateTimeOffset? PublishedAtUtc,
    string Message);

public sealed record SkillAdminComparisonMetricDto(
    string Key,
    string Label,
    string LeftValue,
    string RightValue,
    string? Note = null);

public sealed record SkillAdminComparisonDto(
    SkillAdminDetailDto Left,
    SkillAdminDetailDto Right,
    IReadOnlyList<SkillAdminComparisonMetricDto> Metrics,
    IReadOnlyList<string> Notes);
