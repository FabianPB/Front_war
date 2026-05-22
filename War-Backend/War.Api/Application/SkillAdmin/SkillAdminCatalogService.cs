using Microsoft.EntityFrameworkCore;
using War.Api.Application.Characters;
using War.Api.Application.SkillRuntime;
using War.Core.Combat;
using War.Core.Entities;
using War.Core.PowerScore;
using War.Core.Progression;
using War.Core.Resources;
using War.Core.Skills;
using War.Core.Stats;
using War.Infrastructure.Persistence;

namespace War.Api.Application.SkillAdmin;

public interface ISkillAdminCatalogService
{
    Task EnsureCatalogImportedAsync(CancellationToken cancellationToken = default);

    Task<SkillAdminOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<SkillAdminDetailDto?> GetDetailAsync(Guid recordId, CancellationToken cancellationToken = default);

    Task<SkillAdminDetailDto> CreateAsync(SkillAdminUpsertRequest request, CancellationToken cancellationToken = default);

    Task<SkillAdminDetailDto> UpdateAsync(Guid recordId, SkillAdminUpsertRequest request, CancellationToken cancellationToken = default);

    Task<SkillAdminStateChangeResultDto> PublishAsync(Guid recordId, CancellationToken cancellationToken = default);

    Task<SkillAdminStateChangeResultDto> UnpublishAsync(Guid recordId, CancellationToken cancellationToken = default);

    Task<SkillAdminStateChangeResultDto> ArchiveAsync(Guid recordId, CancellationToken cancellationToken = default);

    Task<SkillAdminPreviewDto> PreviewAsync(SkillAdminUpsertRequest request, Guid? currentRecordId = null, CancellationToken cancellationToken = default);

    Task<SkillAdminComparisonDto> CompareAsync(Guid leftRecordId, Guid rightRecordId, CancellationToken cancellationToken = default);
}

public sealed class SkillAdminCatalogService : ISkillAdminCatalogService
{
    private const string DefaultPublishedBy = "admin-panel";

    private static readonly IReadOnlySet<string> DraftBlockingIssueCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "missing-skill-id",
        "missing-skill-name",
        "missing-skill-description",
        "invalid-unlock-level",
        "duplicate-skill-id-global"
    };

    private readonly WarDbContext _dbContext;
    private readonly ISkillAdminOptionsService _optionsService;
    private readonly ICharacterFinalStatsBuilder _finalStatsBuilder;
    private readonly IPowerScoreCalculator _powerScoreCalculator;
    private readonly ISkillCombatTranslator _skillCombatTranslator;
    private readonly IProgrammedSkillCatalogSource _programmedCatalogSource;

    public SkillAdminCatalogService(
        WarDbContext dbContext,
        ISkillAdminOptionsService optionsService,
        ICharacterFinalStatsBuilder finalStatsBuilder,
        IPowerScoreCalculator powerScoreCalculator,
        ISkillCombatTranslator skillCombatTranslator,
        IProgrammedSkillCatalogSource programmedCatalogSource)
    {
        _dbContext = dbContext;
        _optionsService = optionsService;
        _finalStatsBuilder = finalStatsBuilder;
        _powerScoreCalculator = powerScoreCalculator;
        _skillCombatTranslator = skillCombatTranslator;
        _programmedCatalogSource = programmedCatalogSource;
    }

    public async Task EnsureCatalogImportedAsync(CancellationToken cancellationToken = default)
    {
        var persistedSkillIds = await _dbContext.AdminSkillRecords
            .Select(record => record.SkillId)
            .ToListAsync(cancellationToken);

        var knownIds = new HashSet<string>(persistedSkillIds, StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var importedRecords = _programmedCatalogSource.GetCatalog().ClassCatalogs
            .SelectMany(classCatalog => classCatalog.Skills)
            .Where(definition => !knownIds.Contains(definition.Id))
            .Select(definition => CreateEntity(definition, SkillAdminOrigin.CatalogImport, now))
            .ToArray();

        if (importedRecords.Length == 0)
        {
            return;
        }

        _dbContext.AdminSkillRecords.AddRange(importedRecords);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SkillAdminOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var entities = await LoadAllEntitiesAsync(cancellationToken);
        var options = _optionsService.GetOptions();
        var notes = new List<string>
        {
            "The admin panel stores editable draft skill definitions separately from runtime-published snapshots.",
            "Runtime resolution policy: published persisted skills override the programmed catalog only when the merged class catalog stays valid.",
            "Drafts never enter combat runtime until they are explicitly published."
        };

        var classOverviews = new List<SkillAdminClassOverviewDto>();

        foreach (var classType in SkillCatalogRules.InitialClasses)
        {
            var classEntities = entities
                .Where(entity => entity.ClassType == classType)
                .OrderBy(entity => entity.IsDeleted)
                .ThenBy(entity => entity.Slot.GetOrder())
                .ThenBy(entity => entity.Name, StringComparer.Ordinal)
                .ToArray();
            var activeDefinitions = classEntities
                .Where(entity => !entity.IsDeleted)
                .Select(DeserializeDefinition)
                .ToArray();
            var classIssues = BuildDraftCatalogIssues(activeDefinitions);
            var summaries = new List<SkillAdminSummaryDto>();

            foreach (var entity in classEntities)
            {
                var definition = DeserializeDefinition(entity);
                var preview = await EvaluatePreviewAsync(definition, entity.RecordId, entities, cancellationToken);
                summaries.Add(BuildSummary(entity, definition, preview));
            }

            var activeSkillCount = activeDefinitions.Length;
            var ultimateCount = activeDefinitions.Count(definition => definition.IsUltimate);
            var hasFullKit = activeSkillCount == SkillCatalogRules.SkillsPerClass && ultimateCount == 1;
            var completeness = ResolveClassCompleteness(summaries, activeSkillCount, ultimateCount, classIssues.Count);
            var classNotes = new List<string>();

            if (activeSkillCount == 0)
            {
                classNotes.Add("No active draft skills are currently registered for this class.");
            }

            if (!hasFullKit)
            {
                classNotes.Add($"This class currently has {activeSkillCount} active draft skills and {ultimateCount} active ultimate entries in the admin catalog.");
            }

            classNotes.AddRange(classIssues.Select(issue => issue.Message));

            classOverviews.Add(new SkillAdminClassOverviewDto(
                classType.ToString().ToLowerInvariant(),
                classType.ToString(),
                activeSkillCount,
                ultimateCount,
                hasFullKit,
                completeness,
                Array.AsReadOnly(summaries.ToArray()),
                Array.AsReadOnly(classNotes.Distinct(StringComparer.Ordinal).ToArray())));
        }

        return new SkillAdminOverviewDto(
            Array.AsReadOnly(classOverviews.ToArray()),
            options,
            Array.AsReadOnly(notes.ToArray()));
    }

    public async Task<SkillAdminDetailDto?> GetDetailAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        var entities = await LoadAllEntitiesAsync(cancellationToken);
        var entity = entities.FirstOrDefault(item => item.RecordId == recordId);
        if (entity is null)
        {
            return null;
        }

        var definition = DeserializeDefinition(entity);
        var preview = await EvaluatePreviewAsync(definition, entity.RecordId, entities, cancellationToken);
        return BuildDetail(entity, definition, preview);
    }
    public async Task<SkillAdminDetailDto> CreateAsync(SkillAdminUpsertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Definition);

        var allEntities = await LoadAllEntitiesAsync(cancellationToken);
        if (allEntities.Any(entity => string.Equals(entity.SkillId, request.Definition.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A persisted admin skill with id '{request.Definition.Id}' already exists.");
        }

        var preview = await EvaluatePreviewAsync(request.Definition, null, allEntities, cancellationToken);
        EnsureCanSaveDraft(request.Definition, preview);

        var entity = CreateEntity(request.Definition, SkillAdminOrigin.AdminCreated, DateTimeOffset.UtcNow);
        _dbContext.AdminSkillRecords.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildDetail(entity, request.Definition, preview);
    }

    public async Task<SkillAdminDetailDto> UpdateAsync(Guid recordId, SkillAdminUpsertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Definition);

        var entity = await _dbContext.AdminSkillRecords.SingleOrDefaultAsync(item => item.RecordId == recordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Admin skill record '{recordId}' was not found.");

        if (entity.IsDeleted)
        {
            throw new InvalidOperationException("Archived skills cannot be edited in this first publication phase.");
        }

        EnsureDraftCanCoexistWithPublishedSnapshot(entity, request.Definition);

        var allEntities = await LoadAllEntitiesAsync(cancellationToken);
        if (allEntities.Any(item => item.RecordId != recordId && string.Equals(item.SkillId, request.Definition.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Another persisted admin skill already uses id '{request.Definition.Id}'.");
        }

        var preview = await EvaluatePreviewAsync(request.Definition, recordId, allEntities, cancellationToken);
        EnsureCanSaveDraft(request.Definition, preview);

        var serializedDefinition = SerializeDefinition(request.Definition);
        var hasDefinitionChanged = !string.Equals(entity.DefinitionJson, serializedDefinition, StringComparison.Ordinal);
        UpdateEntityFromDefinition(entity, request.Definition, serializedDefinition, DateTimeOffset.UtcNow, hasDefinitionChanged);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildDetail(entity, request.Definition, preview);
    }

    public async Task<SkillAdminStateChangeResultDto> PublishAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AdminSkillRecords.SingleOrDefaultAsync(item => item.RecordId == recordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Admin skill record '{recordId}' was not found.");

        if (entity.IsDeleted)
        {
            throw new InvalidOperationException("Archived skills cannot be published.");
        }

        if (HasPublishedSnapshot(entity) &&
            entity.PublishedVersion == entity.DraftVersion &&
            string.Equals(entity.PublishedDefinitionJson, entity.DefinitionJson, StringComparison.Ordinal))
        {
            return BuildStateChangeResult(entity, "The latest draft is already published and available to runtime.");
        }

        var allEntities = await LoadAllEntitiesAsync(cancellationToken);
        var definition = DeserializeDefinition(entity);
        var preview = await EvaluatePreviewAsync(definition, entity.RecordId, allEntities, cancellationToken);
        EnsureCanPublish(definition, preview);

        var now = DateTimeOffset.UtcNow;
        entity.PublishedDefinitionJson = entity.DefinitionJson;
        entity.PublishedVersion = entity.DraftVersion;
        entity.PublishedAtUtc = now;
        entity.PublishedBy = DefaultPublishedBy;
        entity.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildStateChangeResult(entity, "The draft was published and is now eligible for runtime resolution.");
    }

    public async Task<SkillAdminStateChangeResultDto> UnpublishAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AdminSkillRecords.SingleOrDefaultAsync(item => item.RecordId == recordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Admin skill record '{recordId}' was not found.");

        if (entity.IsDeleted)
        {
            throw new InvalidOperationException("Archived skills cannot be unpublished because they are already outside runtime resolution.");
        }

        if (!HasPublishedSnapshot(entity))
        {
            return BuildStateChangeResult(entity, "The skill already has no published runtime snapshot.");
        }

        if (!HasProgrammedFallback(entity.SkillId))
        {
            throw new InvalidOperationException("This skill cannot be unpublished because runtime has no programmed fallback for its id.");
        }

        entity.PublishedDefinitionJson = null;
        entity.PublishedVersion = null;
        entity.PublishedAtUtc = null;
        entity.PublishedBy = null;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildStateChangeResult(entity, "The published runtime snapshot was removed. Runtime will now fall back to the programmed catalog.");
    }

    public async Task<SkillAdminStateChangeResultDto> ArchiveAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AdminSkillRecords.SingleOrDefaultAsync(item => item.RecordId == recordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Admin skill record '{recordId}' was not found.");

        if (entity.IsDeleted)
        {
            return BuildStateChangeResult(entity, "The skill was already archived.");
        }

        if (HasPublishedSnapshot(entity) && !HasProgrammedFallback(entity.SkillId))
        {
            throw new InvalidOperationException("This skill cannot be archived because runtime has no programmed fallback for the currently published id.");
        }

        var now = DateTimeOffset.UtcNow;
        entity.IsDeleted = true;
        entity.DeletedAtUtc = now;
        entity.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildStateChangeResult(
            entity,
            HasProgrammedFallback(entity.SkillId)
                ? "The skill was archived. Runtime will ignore the admin record and fall back to the programmed catalog if available."
                : "The skill was archived and removed from runtime consideration.");
    }

    public async Task<SkillAdminPreviewDto> PreviewAsync(SkillAdminUpsertRequest request, Guid? currentRecordId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Definition);

        var allEntities = await LoadAllEntitiesAsync(cancellationToken);
        return await EvaluatePreviewAsync(request.Definition, currentRecordId, allEntities, cancellationToken);
    }

    public async Task<SkillAdminComparisonDto> CompareAsync(Guid leftRecordId, Guid rightRecordId, CancellationToken cancellationToken = default)
    {
        var left = await GetDetailAsync(leftRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Admin skill record '{leftRecordId}' was not found.");
        var right = await GetDetailAsync(rightRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Admin skill record '{rightRecordId}' was not found.");

        var metrics = new List<SkillAdminComparisonMetricDto>
        {
            new("class", "Class", left.Definition.ClassType.ToString(), right.Definition.ClassType.ToString()),
            new("slot", "Slot", left.Definition.Slot.ToString(), right.Definition.Slot.ToString()),
            new("ultimate", "Ultimate", left.Definition.IsUltimate ? "Yes" : "No", right.Definition.IsUltimate ? "Yes" : "No"),
            new("unlock-level", "Unlock Level", left.Definition.UnlockLevel.ToString(), right.Definition.UnlockLevel.ToString()),
            new("publication", "Publication", left.Publication.State.ToString(), right.Publication.State.ToString()),
            new("base-cooldown", "Base Cooldown", left.Definition.BaseTuning.Cadence.BaseCooldownSeconds.ToString("N2"), right.Definition.BaseTuning.Cadence.BaseCooldownSeconds.ToString("N2")),
            new("base-cost", "Base Resource Cost", FormatCosts(left.Definition.BaseTuning.ResourceCosts), FormatCosts(right.Definition.BaseTuning.ResourceCosts)),
            new("base-scaling", "Base Scaling", FormatMagnitude(left.Definition.BaseTuning.Action.MagnitudeProfile), FormatMagnitude(right.Definition.BaseTuning.Action.MagnitudeProfile)),
            new("effects", "Base Effects", (left.Definition.BaseTuning.Effects?.Count ?? 0).ToString(), (right.Definition.BaseTuning.Effects?.Count ?? 0).ToString()),
            new("multi-hit", "Multi-Hit", FormatMultiHit(left.Definition.BaseTuning.MultiHit), FormatMultiHit(right.Definition.BaseTuning.MultiHit)),
            new("power-a1", "PowerScore Delta (Asc 1)", ResolvePowerDeltaDisplay(left.Preview, 1), ResolvePowerDeltaDisplay(right.Preview, 1)),
            new("power-a10", "PowerScore Delta (Asc 10)", ResolvePowerDeltaDisplay(left.Preview, 10), ResolvePowerDeltaDisplay(right.Preview, 10))
        };

        var notes = new List<string>
        {
            "Comparison is referential and uses the persisted admin catalog plus the current Power Score policies.",
            "Runtime resolution still follows the publication policy: only published persisted snapshots may override the programmed catalog."
        };

        return new SkillAdminComparisonDto(left, right, Array.AsReadOnly(metrics.ToArray()), Array.AsReadOnly(notes.ToArray()));
    }
    private async Task<SkillAdminPreviewDto> EvaluatePreviewAsync(
        SkillDefinition candidate,
        Guid? currentRecordId,
        IReadOnlyList<AdminSkillRecordEntity> allEntities,
        CancellationToken cancellationToken)
    {
        await Task.Yield();

        var activeDefinitions = allEntities
            .Where(entity => !entity.IsDeleted && entity.RecordId != currentRecordId)
            .Select(DeserializeDefinition)
            .ToList();
        activeDefinitions.Add(candidate);

        var issues = new List<SkillValidationIssue>();
        issues.AddRange(SkillDefinitionValidator.Validate(candidate));
        issues.AddRange(BuildDraftCatalogIssues(activeDefinitions));

        if (allEntities.Any(entity => entity.RecordId != currentRecordId && string.Equals(entity.SkillId, candidate.Id, StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add(new SkillValidationIssue(
                "duplicate-skill-id-global",
                $"Another persisted admin record already uses skill id '{candidate.Id}'.",
                candidate.Id,
                candidate.ClassType,
                candidate.Slot));
        }

        var combatPreviews = BuildCombatPreviews(candidate);
        var runtimeCatalogIssues = BuildProjectedRuntimeCatalogIssues(candidate, currentRecordId, allEntities);
        var runtimeCatalogNotes = BuildProjectedRuntimeCatalogNotes(candidate, currentRecordId, allEntities);
        var pendingData = (candidate.PendingData ?? Array.Empty<SkillPendingDatum>())
            .Select(item => item.Description)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var hasBlockingPendingData = (candidate.PendingData ?? Array.Empty<SkillPendingDatum>()).Any(item => item.BlocksExactCombatSimulation)
            || combatPreviews.Any(preview => preview.HasBlockingPendingData);
        var canTranslate = combatPreviews.All(preview => preview.CanTranslate);
        var canSaveDraft = CanSaveDraft(issues);
        var canPublish = canSaveDraft && issues.Count == 0 && runtimeCatalogIssues.Count == 0 && canTranslate && !hasBlockingPendingData;
        var notes = new List<string>();

        if (!canSaveDraft)
        {
            notes.Add("This draft cannot be saved yet because core identity or uniqueness fields are still invalid.");
        }

        if (!canPublish)
        {
            notes.Add("This skill cannot be published until definition validation, runtime catalog projection, and combat translation all succeed without blocking issues.");
        }

        if (pendingData.Length > 0)
        {
            notes.Add("The skill remains pending because at least one data point is still explicitly marked as unresolved.");
        }

        IReadOnlyList<SkillAdminPowerScoreImpactDto> powerScoreImpacts;
        if (issues.Count == 0)
        {
            powerScoreImpacts = BuildPowerScoreImpacts(candidate, currentRecordId, allEntities);
        }
        else
        {
            powerScoreImpacts = Array.AsReadOnly(Array.Empty<SkillAdminPowerScoreImpactDto>());
            notes.Add("Power Score preview was skipped because the candidate definition is not yet valid.");
        }

        return new SkillAdminPreviewDto(
            ResolveSkillCompleteness(issues.Count, pendingData.Length, hasBlockingPendingData),
            canSaveDraft,
            canPublish,
            canTranslate,
            issues.Count,
            runtimeCatalogIssues.Count,
            pendingData.Length,
            hasBlockingPendingData,
            Array.AsReadOnly(issues.Select(ToDto).ToArray()),
            Array.AsReadOnly(runtimeCatalogIssues.Select(ToDto).ToArray()),
            Array.AsReadOnly(combatPreviews.ToArray()),
            powerScoreImpacts,
            runtimeCatalogNotes,
            Array.AsReadOnly(pendingData),
            Array.AsReadOnly(notes.Distinct(StringComparer.Ordinal).ToArray()));
    }

    private IReadOnlyList<SkillAdminCombatTranslationPreviewDto> BuildCombatPreviews(SkillDefinition definition)
    {
        var previews = new List<SkillAdminCombatTranslationPreviewDto>();

        foreach (var ascensionLevel in new[] { SkillCatalogRules.MinimumAscensionLevel, SkillCatalogRules.MaximumAscensionLevel })
        {
            try
            {
                var actor = CreateReferenceCharacter(Guid.NewGuid(), definition.ClassType, 80);
                var targetClass = definition.ClassType == ClassType.Bruiser ? ClassType.Sorcerer : ClassType.Bruiser;
                var target = CreateReferenceCharacter(Guid.NewGuid(), targetClass, 80);
                var progress = new CharacterSkillProgress(actor.Id, definition.Id, true, ascensionLevel, definition.UnlockLevel);
                var plan = _skillCombatTranslator.Prepare(
                    definition,
                    new SkillCombatTranslationContext(
                        actor,
                        target,
                        progress,
                        CombatTargetClassification.Player,
                        Array.Empty<CombatConditionType>(),
                        Array.Empty<CombatProtectionState>()));

                var combatContext = plan.CombatEventContext;

                previews.Add(new SkillAdminCombatTranslationPreviewDto(
                    ascensionLevel == SkillCatalogRules.MinimumAscensionLevel ? "Base combat translation" : "Max ascension combat translation",
                    ascensionLevel,
                    plan.CanExecute,
                    plan.HasBlockingPendingData,
                    plan.ScheduledEvents?.Sum(item => item.RepeatCount) ?? 0,
                    combatContext?.PotentialEffects?.Count ?? 0,
                    plan.CastProtectionGrants?.Count ?? 0,
                    plan.ScheduledEvents?.Count(item => item.TriggerPhase != SkillExecutionTriggerPhase.DuringActiveWindow || item.EventKey.Contains('.')) ?? 0,
                    combatContext?.ActionKind.ToString(),
                    combatContext?.DamageType?.ToString(),
                    combatContext?.MagnitudeProfile?.ScalingType.ToString(),
                    Array.AsReadOnly((plan.Notes ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray())));
            }
            catch (Exception exception)
            {
                previews.Add(new SkillAdminCombatTranslationPreviewDto(
                    ascensionLevel == SkillCatalogRules.MinimumAscensionLevel ? "Base combat translation" : "Max ascension combat translation",
                    ascensionLevel,
                    false,
                    true,
                    0,
                    0,
                    0,
                    0,
                    null,
                    null,
                    null,
                    Array.AsReadOnly(new[] { exception.Message })));
            }
        }

        return Array.AsReadOnly(previews.ToArray());
    }

    private IReadOnlyList<SkillAdminPowerScoreImpactDto> BuildPowerScoreImpacts(
        SkillDefinition candidate,
        Guid? currentRecordId,
        IReadOnlyList<AdminSkillRecordEntity> allEntities)
    {
        var impacts = new List<SkillAdminPowerScoreImpactDto>();
        var referenceLevel = 80;

        foreach (var ascensionLevel in new[] { SkillCatalogRules.MinimumAscensionLevel, SkillCatalogRules.MaximumAscensionLevel })
        {
            var baselineDefinitions = allEntities
                .Where(entity => !entity.IsDeleted)
                .Select(DeserializeDefinition)
                .ToList();
            var projectedDefinitions = allEntities
                .Where(entity => !entity.IsDeleted && entity.RecordId != currentRecordId)
                .Select(DeserializeDefinition)
                .ToList();
            projectedDefinitions.Add(candidate);

            var baselineCatalog = SkillCatalog.FromDefinitions(baselineDefinitions);
            var projectedCatalog = SkillCatalog.FromDefinitions(projectedDefinitions);
            var character = CreateReferenceCharacter(Guid.NewGuid(), candidate.ClassType, referenceLevel);
            var baselineProgress = BuildReferenceSkillProgress(baselineCatalog, candidate.ClassType, character.Id, ascensionLevel);
            var projectedProgress = BuildReferenceSkillProgress(projectedCatalog, candidate.ClassType, character.Id, ascensionLevel);
            var baselineResult = _powerScoreCalculator.Calculate(new PowerScoreCalculationContext(character, baselineProgress, baselineCatalog));
            var projectedResult = _powerScoreCalculator.Calculate(new PowerScoreCalculationContext(character, projectedProgress, projectedCatalog));
            var categoryDeltas = BuildCategoryDeltas(baselineResult, projectedResult);
            var notes = new List<string>
            {
                "Reference preview uses a level-80 synthetic character built from class growth only.",
                "The delta is referential: it shows how much this skill changes the class valuation under the current Power Score policy."
            };

            impacts.Add(new SkillAdminPowerScoreImpactDto(
                ascensionLevel == SkillCatalogRules.MinimumAscensionLevel ? "PowerScore preview at ascension 1" : "PowerScore preview at ascension 10",
                referenceLevel,
                ascensionLevel,
                baselineResult.TotalScore,
                projectedResult.TotalScore,
                projectedResult.TotalScore - baselineResult.TotalScore,
                CharacterSnapshotPresentationCatalog.FormatPowerScore(baselineResult.TotalScore),
                CharacterSnapshotPresentationCatalog.FormatPowerScore(projectedResult.TotalScore),
                FormatSigned(projectedResult.TotalScore - baselineResult.TotalScore),
                categoryDeltas,
                Array.AsReadOnly(notes.ToArray())));
        }

        return Array.AsReadOnly(impacts.ToArray());
    }

    private IReadOnlyList<SkillAdminPowerScoreCategoryDeltaDto> BuildCategoryDeltas(PowerScoreResult baselineResult, PowerScoreResult projectedResult)
    {
        var baselineByCategory = baselineResult.CategoryContributions.ToDictionary(item => item.Category, item => item);
        var projectedByCategory = projectedResult.CategoryContributions.ToDictionary(item => item.Category, item => item);

        return Array.AsReadOnly(Enum.GetValues<PowerScoreCategory>()
            .Select(category =>
            {
                var baseline = baselineByCategory.TryGetValue(category, out var baselineContribution)
                    ? baselineContribution.Contribution
                    : 0m;
                var projected = projectedByCategory.TryGetValue(category, out var projectedContribution)
                    ? projectedContribution.Contribution
                    : 0m;

                return new SkillAdminPowerScoreCategoryDeltaDto(
                    CharacterSnapshotPresentationCatalog.GetPowerScoreCategoryKey(category),
                    CharacterSnapshotPresentationCatalog.GetPowerScoreCategoryLabel(category),
                    baseline,
                    projected,
                    projected - baseline,
                    FormatSigned(projected - baseline));
            })
            .Where(item => item.BaselineContribution != 0m || item.ProjectedContribution != 0m || item.Delta != 0m)
            .ToArray());
    }

    private CharacterSkillProgressCollection BuildReferenceSkillProgress(SkillCatalog catalog, ClassType classType, Guid characterId, int ascensionLevel)
    {
        var classCatalog = catalog.ClassCatalogs.FirstOrDefault(item => item.ClassType == classType)
            ?? new ClassSkillCatalog(classType);

        var entries = classCatalog.Skills.Select(skill => new CharacterSkillProgress(
            characterId,
            skill.Id,
            true,
            ascensionLevel,
            skill.UnlockLevel));

        return new CharacterSkillProgressCollection(entries);
    }

    private Character CreateReferenceCharacter(Guid characterId, ClassType classType, int level)
    {
        var stats = _finalStatsBuilder.Build(classType, level);
        var resources = new CharacterResources(
            stats.Get(StatType.MaxHp),
            stats.Get(StatType.MaxMana),
            stats.Get(StatType.UltimateChargeMax));

        return new Character(characterId, stats, resources, classType, level);
    }

    private SkillAdminDetailDto BuildDetail(AdminSkillRecordEntity entity, SkillDefinition definition, SkillAdminPreviewDto preview)
    {
        var publication = BuildPublication(entity);
        var notes = new List<string>
        {
            entity.Origin == SkillAdminOrigin.CatalogImport.ToString()
                ? "This record started as an imported copy of the programmed skill catalog."
                : "This record was created directly from the admin panel.",
            publication.RuntimeResolution
        };

        notes.AddRange(publication.Notes);

        return new SkillAdminDetailDto(
            entity.RecordId,
            Enum.Parse<SkillAdminOrigin>(entity.Origin, ignoreCase: true),
            publication,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            definition,
            preview,
            BuildAscensionEntries(definition),
            Array.AsReadOnly(notes.Distinct(StringComparer.Ordinal).ToArray()));
    }

    private IReadOnlyList<SkillAdminAscensionEntryDto> BuildAscensionEntries(SkillDefinition definition)
    {
        var entries = new List<SkillAdminAscensionEntryDto>();
        var overrides = definition.AscensionOverrides ?? new Dictionary<int, SkillAscensionOverrides>();

        entries.Add(new SkillAdminAscensionEntryDto(
            SkillCatalogRules.MinimumAscensionLevel,
            true,
            false,
            null,
            null,
            Array.AsReadOnly(new[] { "Base tuning comes directly from the skill definition and is always cumulative origin." })));

        for (var level = SkillCatalogRules.MinimumAscensionLevel + 1; level <= SkillCatalogRules.MaximumAscensionLevel; level++)
        {
            overrides.TryGetValue(level, out var overrideValue);
            entries.Add(new SkillAdminAscensionEntryDto(
                level,
                false,
                overrideValue is not null,
                overrideValue,
                overrideValue?.UpgradeCost,
                BuildAscensionHighlights(overrideValue, level)));
        }

        return Array.AsReadOnly(entries.ToArray());
    }
    private IReadOnlyList<string> BuildAscensionHighlights(SkillAscensionOverrides? overrideValue, int level)
    {
        if (overrideValue is null)
        {
            return Array.AsReadOnly(new[] { $"Ascension {level} currently has no override and inherits all prior state." });
        }

        var highlights = new List<string>();

        if (overrideValue.Action is not null || overrideValue.MagnitudeProfile is not null)
        {
            highlights.Add("Magnitude or base action tuning changes at this ascension.");
        }

        if (overrideValue.Targeting is not null)
        {
            highlights.Add("Targeting changes at this ascension.");
        }

        if (overrideValue.ResourceCosts is not null)
        {
            highlights.Add("Resource costs are overridden at this ascension.");
        }

        if (overrideValue.Cadence is not null)
        {
            highlights.Add("Cooldown cadence changes at this ascension.");
        }

        if ((overrideValue.AddedEffects?.Count ?? 0) > 0 || (overrideValue.EffectOverrides?.Count ?? 0) > 0 || (overrideValue.RemovedEffectKeys?.Count ?? 0) > 0)
        {
            highlights.Add("Condition effects change at this ascension.");
        }

        if (overrideValue.MultiHit is not null)
        {
            highlights.Add("Multi-hit behavior changes at this ascension.");
        }

        if ((overrideValue.CastProtections?.Count ?? 0) > 0)
        {
            highlights.Add("Cast protections are overridden at this ascension.");
        }

        if ((overrideValue.TriggeredActions?.Count ?? 0) > 0)
        {
            highlights.Add("Triggered actions are added or replaced at this ascension.");
        }

        if (overrideValue.UpgradeCost is not null)
        {
            highlights.Add(overrideValue.UpgradeCost.HasPendingData
                ? "Upgrade material data is present but still partially pending."
                : "Upgrade material data is defined for this ascension.");
        }

        if (!string.IsNullOrWhiteSpace(overrideValue.Note))
        {
            highlights.Add(overrideValue.Note!);
        }

        return Array.AsReadOnly(highlights.ToArray());
    }

    private SkillAdminSummaryDto BuildSummary(AdminSkillRecordEntity entity, SkillDefinition definition, SkillAdminPreviewDto preview)
    {
        var publication = BuildPublication(entity);
        var completeness = entity.IsDeleted ? SkillAdminCompletenessStatus.Archived : preview.CompletenessStatus;
        var notes = new List<string>(preview.Notes ?? Array.Empty<string>())
        {
            publication.RuntimeResolution
        };

        return new SkillAdminSummaryDto(
            entity.RecordId,
            definition.Id,
            definition.Name,
            definition.ClassType.ToString().ToLowerInvariant(),
            definition.ClassType.ToString(),
            definition.Slot.ToString(),
            definition.Slot.GetOrder(),
            definition.IsUltimate,
            definition.UnlockLevel,
            Enum.Parse<SkillAdminOrigin>(entity.Origin, ignoreCase: true),
            completeness,
            publication,
            preview.CanTranslateToCombat,
            preview.ValidationIssueCount,
            preview.PendingDataCount,
            preview.HasBlockingPendingData,
            entity.UpdatedAtUtc,
            Array.AsReadOnly(notes.Distinct(StringComparer.Ordinal).Take(3).ToArray()));
    }

    private IReadOnlyList<SkillValidationIssue> BuildDraftCatalogIssues(IReadOnlyList<SkillDefinition> definitions)
    {
        var issues = new List<SkillValidationIssue>();
        var catalog = SkillCatalog.FromDefinitions(definitions);
        issues.AddRange(catalog.Validate(requireFullKit: false));

        foreach (var duplicateSkillId in definitions
                     .GroupBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            issues.Add(new SkillValidationIssue(
                "duplicate-skill-id-global",
                $"The admin catalog contains duplicate skill id '{duplicateSkillId}'.",
                duplicateSkillId));
        }

        return Array.AsReadOnly(issues.ToArray());
    }

    private IReadOnlyList<SkillValidationIssue> BuildProjectedRuntimeCatalogIssues(
        SkillDefinition candidate,
        Guid? currentRecordId,
        IReadOnlyList<AdminSkillRecordEntity> allEntities)
    {
        var programmedCatalog = _programmedCatalogSource.GetCatalog();
        var projectedPublishedEntries = BuildProjectedPublishedEntries(candidate, currentRecordId, allEntities);

        var issues = SkillCatalogRules.InitialClasses
            .SelectMany(classType => RuntimeSkillCatalogComposer.GetMergedClassIssues(programmedCatalog, projectedPublishedEntries, classType))
            .ToArray();

        return Array.AsReadOnly(issues);
    }

    private IReadOnlyList<string> BuildProjectedRuntimeCatalogNotes(
        SkillDefinition candidate,
        Guid? currentRecordId,
        IReadOnlyList<AdminSkillRecordEntity> allEntities)
    {
        var programmedCatalog = _programmedCatalogSource.GetCatalog();
        var projectedPublishedEntries = BuildProjectedPublishedEntries(candidate, currentRecordId, allEntities);
        return RuntimeSkillCatalogComposer.Compose(programmedCatalog, projectedPublishedEntries).Notes;
    }

    private IReadOnlyList<RuntimeResolvedSkillDefinition> BuildProjectedPublishedEntries(
        SkillDefinition candidate,
        Guid? currentRecordId,
        IReadOnlyList<AdminSkillRecordEntity> allEntities)
    {
        var entries = allEntities
            .Where(entity => !entity.IsDeleted && HasPublishedSnapshot(entity) && entity.RecordId != currentRecordId)
            .Select(CreatePublishedRuntimeEntry)
            .ToList();

        entries.Add(new RuntimeResolvedSkillDefinition(
            candidate.Id,
            candidate,
            RuntimeSkillSourceKind.PublishedPersisted,
            currentRecordId,
            DraftVersion: null,
            PublishedVersion: null,
            PublishedAtUtc: null,
            PublishedBy: DefaultPublishedBy,
            ResolutionNote: "Projected publication candidate."));

        return Array.AsReadOnly(entries.ToArray());
    }

    private SkillAdminPublicationDto BuildPublication(AdminSkillRecordEntity entity)
    {
        var hasProgrammedFallback = HasProgrammedFallback(entity.SkillId);
        var hasPublishedSnapshot = HasPublishedSnapshot(entity);
        var hasUnpublishedChanges = hasPublishedSnapshot &&
                                    (entity.PublishedVersion != entity.DraftVersion ||
                                     !string.Equals(entity.PublishedDefinitionJson, entity.DefinitionJson, StringComparison.Ordinal));
        var state = ResolvePublicationState(entity, hasUnpublishedChanges);
        var notes = new List<string>();

        if (hasPublishedSnapshot && hasUnpublishedChanges)
        {
            notes.Add("Runtime still uses the published snapshot. The current draft contains unpublished changes.");
        }

        if (!hasPublishedSnapshot && !entity.IsDeleted)
        {
            notes.Add(hasProgrammedFallback
                ? "Runtime currently falls back to the programmed catalog because this record has no published snapshot."
                : "This draft has no published snapshot and no programmed fallback, so runtime will not resolve it yet.");
        }

        if (entity.IsDeleted)
        {
            notes.Add(hasProgrammedFallback
                ? "Archived records are ignored by runtime and fall back to the programmed catalog when available."
                : "Archived records are ignored by runtime and remain unavailable until restored in a future phase.");
        }

        var runtimeResolution = state switch
        {
            SkillAdminPublicationState.Archived when hasProgrammedFallback => "Archived in admin. Runtime ignores this record and falls back to the programmed catalog.",
            SkillAdminPublicationState.Archived => "Archived in admin. Runtime ignores this record.",
            SkillAdminPublicationState.PublishedWithDraft => "Runtime uses the published snapshot. A newer draft exists but does not affect runtime yet.",
            SkillAdminPublicationState.Published => "Runtime can resolve this skill from the published admin snapshot.",
            _ when hasProgrammedFallback => "Runtime currently resolves this skill from the programmed catalog until an admin snapshot is published.",
            _ => "Runtime currently cannot resolve this skill because no published admin snapshot is active."
        };

        return new SkillAdminPublicationDto(
            state,
            entity.DraftVersion,
            entity.PublishedVersion,
            entity.PublishedAtUtc,
            entity.PublishedBy,
            hasProgrammedFallback,
            !entity.IsDeleted && hasPublishedSnapshot,
            hasUnpublishedChanges,
            runtimeResolution,
            Array.AsReadOnly(notes.Distinct(StringComparer.Ordinal).ToArray()));
    }

    private static SkillAdminPublicationState ResolvePublicationState(AdminSkillRecordEntity entity, bool hasUnpublishedChanges)
    {
        if (entity.IsDeleted)
        {
            return SkillAdminPublicationState.Archived;
        }

        if (entity.PublishedVersion is null || string.IsNullOrWhiteSpace(entity.PublishedDefinitionJson))
        {
            return SkillAdminPublicationState.Draft;
        }

        return hasUnpublishedChanges
            ? SkillAdminPublicationState.PublishedWithDraft
            : SkillAdminPublicationState.Published;
    }

    private bool HasProgrammedFallback(string skillId)
    {
        return _programmedCatalogSource.GetCatalog().ClassCatalogs
            .SelectMany(classCatalog => classCatalog.Skills)
            .Any(skill => string.Equals(skill.Id, skillId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasPublishedSnapshot(AdminSkillRecordEntity entity)
    {
        return entity.PublishedVersion is not null && !string.IsNullOrWhiteSpace(entity.PublishedDefinitionJson);
    }

    private static SkillAdminCompletenessStatus ResolveClassCompleteness(
        IReadOnlyList<SkillAdminSummaryDto> summaries,
        int activeSkillCount,
        int ultimateCount,
        int classIssueCount)
    {
        if (summaries.Count > 0 && summaries.All(summary => summary.Publication.State == SkillAdminPublicationState.Archived))
        {
            return SkillAdminCompletenessStatus.Archived;
        }

        if (classIssueCount > 0 || summaries.Any(summary => summary.Publication.State != SkillAdminPublicationState.Archived && summary.CompletenessStatus == SkillAdminCompletenessStatus.Invalid))
        {
            return SkillAdminCompletenessStatus.Invalid;
        }

        if (activeSkillCount != SkillCatalogRules.SkillsPerClass ||
            ultimateCount != 1 ||
            summaries.Any(summary => summary.Publication.State != SkillAdminPublicationState.Archived && summary.CompletenessStatus == SkillAdminCompletenessStatus.Pending))
        {
            return SkillAdminCompletenessStatus.Pending;
        }

        return SkillAdminCompletenessStatus.Complete;
    }

    private static SkillAdminCompletenessStatus ResolveSkillCompleteness(int validationIssueCount, int pendingDataCount, bool hasBlockingPendingData)
    {
        if (validationIssueCount > 0)
        {
            return SkillAdminCompletenessStatus.Invalid;
        }

        if (pendingDataCount > 0 || hasBlockingPendingData)
        {
            return SkillAdminCompletenessStatus.Pending;
        }

        return SkillAdminCompletenessStatus.Complete;
    }
    private static bool CanSaveDraft(IReadOnlyList<SkillValidationIssue> issues)
    {
        return issues.All(issue => !DraftBlockingIssueCodes.Contains(issue.Code));
    }

    private static void EnsureCanSaveDraft(SkillDefinition definition, SkillAdminPreviewDto preview)
    {
        if (preview.CanSaveDraft)
        {
            return;
        }

        var blockingIssues = preview.ValidationIssues
            .Where(issue => DraftBlockingIssueCodes.Contains(issue.Code))
            .Select(issue => issue.Message)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var message = blockingIssues.Length > 0
            ? string.Join(" ", blockingIssues)
            : $"Skill '{definition.Id}' cannot be saved as a draft yet because a required identity or uniqueness field is still invalid.";

        throw new InvalidOperationException(message);
    }

    private static void EnsureCanPublish(SkillDefinition definition, SkillAdminPreviewDto preview)
    {
        if (preview.CanPublish)
        {
            return;
        }

        var messages = new List<string>();
        messages.AddRange(preview.ValidationIssues.Select(issue => issue.Message));
        messages.AddRange(preview.RuntimeCatalogIssues.Select(issue => issue.Message));
        messages.AddRange(preview.PendingData.Select(item => $"Pending data: {item}"));

        if (!preview.CanTranslateToCombat)
        {
            messages.Add("Combat translation preview is blocked for at least one ascension preview.");
        }

        if (messages.Count == 0)
        {
            messages.Add($"Skill '{definition.Id}' is not publishable yet.");
        }

        throw new InvalidOperationException(string.Join(" ", messages.Distinct(StringComparer.Ordinal)));
    }

    private static void EnsureDraftCanCoexistWithPublishedSnapshot(AdminSkillRecordEntity entity, SkillDefinition draftDefinition)
    {
        if (!HasPublishedSnapshot(entity))
        {
            return;
        }

        var publishedDefinition = SkillAdminJsonSerializer.Deserialize<SkillDefinition>(entity.PublishedDefinitionJson!);
        if (publishedDefinition is null)
        {
            throw new InvalidOperationException("The published snapshot for this skill record could not be read.");
        }

        if (!string.Equals(publishedDefinition.Id, draftDefinition.Id, StringComparison.OrdinalIgnoreCase) ||
            publishedDefinition.ClassType != draftDefinition.ClassType ||
            publishedDefinition.Slot != draftDefinition.Slot ||
            publishedDefinition.IsUltimate != draftDefinition.IsUltimate)
        {
            throw new InvalidOperationException("Published skills cannot change id, class, slot, or ultimate flag while a runtime snapshot is active. Unpublish the skill first, then edit those identity fields.");
        }
    }

    private RuntimeResolvedSkillDefinition CreatePublishedRuntimeEntry(AdminSkillRecordEntity entity)
    {
        var definition = DeserializePublishedDefinition(entity);
        return new RuntimeResolvedSkillDefinition(
            definition.Id,
            definition,
            RuntimeSkillSourceKind.PublishedPersisted,
            entity.RecordId,
            entity.DraftVersion,
            entity.PublishedVersion,
            entity.PublishedAtUtc,
            entity.PublishedBy,
            "Projected currently published snapshot.");
    }

    private static SkillDefinition DeserializeDefinition(AdminSkillRecordEntity entity)
    {
        return SkillAdminJsonSerializer.Deserialize<SkillDefinition>(entity.DefinitionJson)
            ?? throw new InvalidOperationException($"Admin skill record '{entity.RecordId}' could not be deserialized.");
    }

    private static SkillDefinition DeserializePublishedDefinition(AdminSkillRecordEntity entity)
    {
        return SkillAdminJsonSerializer.Deserialize<SkillDefinition>(entity.PublishedDefinitionJson!)
            ?? throw new InvalidOperationException($"Published snapshot for admin skill record '{entity.RecordId}' could not be deserialized.");
    }

    private static string SerializeDefinition(SkillDefinition definition)
    {
        return SkillAdminJsonSerializer.Serialize(definition);
    }

    private static AdminSkillRecordEntity CreateEntity(SkillDefinition definition, SkillAdminOrigin origin, DateTimeOffset now)
    {
        var serializedDefinition = SerializeDefinition(definition);
        return new AdminSkillRecordEntity
        {
            RecordId = Guid.NewGuid(),
            SkillId = definition.Id,
            Name = definition.Name,
            Description = definition.Description,
            ClassType = definition.ClassType,
            Slot = definition.Slot,
            IsUltimate = definition.IsUltimate,
            UnlockLevel = definition.UnlockLevel,
            Origin = origin.ToString(),
            IsDeleted = false,
            DraftVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            DefinitionJson = serializedDefinition,
            PublishedVersion = null,
            PublishedAtUtc = null,
            PublishedBy = null,
            PublishedDefinitionJson = null
        };
    }

    private static void UpdateEntityFromDefinition(
        AdminSkillRecordEntity entity,
        SkillDefinition definition,
        string serializedDefinition,
        DateTimeOffset now,
        bool hasDefinitionChanged)
    {
        entity.SkillId = definition.Id;
        entity.Name = definition.Name;
        entity.Description = definition.Description;
        entity.ClassType = definition.ClassType;
        entity.Slot = definition.Slot;
        entity.IsUltimate = definition.IsUltimate;
        entity.UnlockLevel = definition.UnlockLevel;
        entity.DefinitionJson = serializedDefinition;
        entity.UpdatedAtUtc = now;

        if (hasDefinitionChanged)
        {
            entity.DraftVersion = Math.Max(1, entity.DraftVersion) + 1;
        }
    }

    private SkillAdminStateChangeResultDto BuildStateChangeResult(AdminSkillRecordEntity entity, string message)
    {
        var publication = BuildPublication(entity);
        return new SkillAdminStateChangeResultDto(
            entity.RecordId,
            entity.SkillId,
            publication.State,
            entity.DraftVersion,
            entity.PublishedVersion,
            entity.PublishedAtUtc,
            message);
    }

    private async Task<IReadOnlyList<AdminSkillRecordEntity>> LoadAllEntitiesAsync(CancellationToken cancellationToken)
    {
        var entities = await _dbContext.AdminSkillRecords
            .AsNoTracking()
            .OrderBy(entity => entity.ClassType)
            .ThenBy(entity => entity.IsDeleted)
            .ThenBy(entity => entity.Slot)
            .ThenBy(entity => entity.Name)
            .ToListAsync(cancellationToken);

        return Array.AsReadOnly(entities.ToArray());
    }

    private static SkillAdminValidationIssueDto ToDto(SkillValidationIssue issue)
    {
        return new SkillAdminValidationIssueDto(
            issue.Code,
            issue.Message,
            issue.SkillId,
            issue.ClassType?.ToString().ToLowerInvariant(),
            issue.Slot?.ToString());
    }

    private static string FormatCosts(IReadOnlyList<SkillResourceCostDefinition>? costs)
    {
        if (costs is null || costs.Count == 0)
        {
            return "None";
        }

        return string.Join(" | ", costs.Select(cost => $"{cost.ResourceType} {CharacterSnapshotPresentationCatalog.FormatNumber(cost.Amount)}"));
    }

    private static string FormatMagnitude(SkillMagnitudeProfile magnitude)
    {
        return $"{CharacterSnapshotPresentationCatalog.FormatNumber(magnitude.BaseMagnitude)} + {magnitude.ScalingType} x {CharacterSnapshotPresentationCatalog.FormatNumber(magnitude.ScalingCoefficient)}";
    }

    private static string FormatMultiHit(SkillMultiHitProfile? multiHit)
    {
        return multiHit is null
            ? "Single"
            : $"{multiHit.HitCount} hits / {CharacterSnapshotPresentationCatalog.FormatNumber(multiHit.ActiveDurationSeconds)}s";
    }

    private static string ResolvePowerDeltaDisplay(SkillAdminPreviewDto preview, int ascensionLevel)
    {
        return preview.PowerScoreImpacts.FirstOrDefault(item => item.AscensionLevel == ascensionLevel)?.DeltaDisplay ?? "N/A";
    }

    private static string FormatSigned(decimal value)
    {
        var formatted = CharacterSnapshotPresentationCatalog.FormatPowerScore(Math.Abs(value));
        if (value > 0m)
        {
            return $"+{formatted}";
        }

        if (value < 0m)
        {
            return $"-{formatted}";
        }

        return formatted;
    }
}
