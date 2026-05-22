using War.Api.Application.SkillRuntime;
using War.Core.Combat;
using War.Core.Progression;
using War.Core.Resources;
using War.Core.Stats;

namespace War.Api.Application.Characters;

public interface ICharacterSnapshotQueryService
{
    Task<CharacterSnapshotDto?> GetSnapshotAsync(Guid characterId, CancellationToken cancellationToken = default);
}

public sealed class CharacterSnapshotQueryService : ICharacterSnapshotQueryService
{
    private readonly IPersistedCharacterRuntimeService _runtimeService;
    private readonly ICharacterProfileSnapshotFactory _snapshotFactory;
    private readonly ISkillRuntimeCatalogProvider _skillRuntimeCatalogProvider;
    private readonly IClassBasicAttackCatalog _basicAttackCatalog;
    private readonly IBasicAttackComboResolver _basicAttackComboResolver;

    public CharacterSnapshotQueryService(
        IPersistedCharacterRuntimeService runtimeService,
        ICharacterProfileSnapshotFactory snapshotFactory,
        ISkillRuntimeCatalogProvider skillRuntimeCatalogProvider,
        IClassBasicAttackCatalog basicAttackCatalog,
        IBasicAttackComboResolver basicAttackComboResolver)
    {
        _runtimeService = runtimeService;
        _snapshotFactory = snapshotFactory;
        _skillRuntimeCatalogProvider = skillRuntimeCatalogProvider;
        _basicAttackCatalog = basicAttackCatalog;
        _basicAttackComboResolver = basicAttackComboResolver;
    }

    public async Task<CharacterSnapshotDto?> GetSnapshotAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        var runtime = await _runtimeService.LoadAsync(characterId, tracked: false, cancellationToken);
        if (runtime is null)
        {
            return null;
        }

        var runtimeSkillCatalog = await _skillRuntimeCatalogProvider.GetRuntimeCatalogAsync(cancellationToken);
        var snapshot = _snapshotFactory.Create(new CharacterProfileSnapshotRequest(
            runtime.Character.Id,
            runtime.Character.ClassType,
            runtime.Progression,
            runtime.Resources,
            SkillProgress: runtime.SkillProgress,
            SkillCatalog: runtimeSkillCatalog.Catalog,
            IncludePowerScore: true));

        var notes = new List<string>();
        notes.AddRange(snapshot.Notes ?? Array.Empty<string>());

        if (runtime.Entity.XpToNextLevel != runtime.Progression.XpToNextLevel)
        {
            notes.Add($"Persisted xp_to_next_level ({runtime.Entity.XpToNextLevel}) was resynchronized to the progression service result ({runtime.Progression.XpToNextLevel}) during snapshot assembly.");
        }

        notes.AddRange(runtimeSkillCatalog.Notes ?? Array.Empty<string>());

        var basicAttackDefinition = _basicAttackCatalog.GetRequired(runtime.Entity.ClassType);
        var basicComboStatus = _basicAttackComboResolver.Describe(
            basicAttackDefinition,
            runtime.BasicAttackRuntimeState,
            DateTimeOffset.UtcNow);

        return CharacterSnapshotDtoFactory.Create(runtime.Entity.Name, snapshot, basicComboStatus, notes);
    }
}

internal static class CharacterSnapshotDtoFactory
{
    public static CharacterSnapshotDto Create(
        string characterName,
        CharacterProfileSnapshot snapshot,
        BasicAttackComboStatus? basicComboStatus = null,
        IReadOnlyList<string>? extraNotes = null)
    {
        var resourceMetrics = CreateResourceMetrics(snapshot);
        var statSections = CreateStatSections(snapshot.FinalStats);
        var powerScore = CreatePowerScore(snapshot);
        var notes = (snapshot.Notes ?? Array.Empty<string>())
            .Concat(extraNotes ?? Array.Empty<string>())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var isDefeated = resourceMetrics.First(metric => metric.Key == "hp").Current <= 0m;

        return new CharacterSnapshotDto(
            snapshot.CharacterId,
            characterName,
            CharacterSnapshotPresentationCatalog.GetClassKey(snapshot.ClassType.ToString()),
            snapshot.ClassType.ToString(),
            new CharacterProgressDto(
                snapshot.Progression.Level,
                snapshot.Progression.CurrentXp,
                snapshot.Progression.XpToNextLevel,
                snapshot.Progression.TotalXp,
                snapshot.Progression.RemainingXpToNextLevel,
                snapshot.Progression.IsAtMaxLevel || snapshot.Progression.XpToNextLevel <= 0
                    ? 1m
                    : decimal.Round(snapshot.Progression.CurrentXp / (decimal)snapshot.Progression.XpToNextLevel, 4, MidpointRounding.AwayFromZero)),
            CreateBasicCombo(basicComboStatus),
            resourceMetrics,
            statSections,
            powerScore,
            isDefeated,
            Array.AsReadOnly(notes));
    }

    private static IReadOnlyList<CharacterResourceMetricDto> CreateResourceMetrics(CharacterProfileSnapshot snapshot)
    {
        var maxHp = GetFinalStat(snapshot.FinalStats, StatType.MaxHp);
        var maxMana = GetFinalStat(snapshot.FinalStats, StatType.MaxMana);
        var ultimateChargeMax = GetFinalStat(snapshot.FinalStats, StatType.UltimateChargeMax);

        return Array.AsReadOnly(new[]
        {
            CreateResourceMetric("hp", "HP", snapshot.Resources.CurrentHp, maxHp),
            CreateResourceMetric("mana", "Mana", snapshot.Resources.CurrentMana, maxMana),
            CreateResourceMetric("ultimate-charge", "Ultimate Charge", snapshot.Resources.UltimateCharge, ultimateChargeMax > 0m ? ultimateChargeMax : null)
        });
    }

    private static IReadOnlyList<CharacterStatSectionDto> CreateStatSections(IReadOnlyDictionary<StatType, decimal> finalStats)
    {
        return CharacterSnapshotPresentationCatalog.GetAllStatMetadata()
            .GroupBy(metadata => new { metadata.SectionKey, metadata.SectionLabel, metadata.SectionOrder })
            .OrderBy(group => group.Key.SectionOrder)
            .Select(group => new CharacterStatSectionDto(
                group.Key.SectionKey,
                group.Key.SectionLabel,
                Array.AsReadOnly(group
                    .OrderBy(metadata => metadata.StatOrder)
                    .Select(metadata =>
                    {
                        var value = GetFinalStat(finalStats, metadata.StatType);
                        return new CharacterStatEntryDto(
                            metadata.Key,
                            metadata.Label,
                            value,
                            CharacterSnapshotPresentationCatalog.FormatStatValue(metadata.StatType, value),
                            value == 0m,
                            metadata.ValueKind == CharacterSnapshotValueKind.Percentage ? "percentage" : "number");
                    })
                    .ToArray())))
            .ToArray();
    }

    private static CharacterPowerScoreDto? CreatePowerScore(CharacterProfileSnapshot snapshot)
    {
        if (snapshot.PowerScore is null)
        {
            return null;
        }

        var categories = snapshot.PowerScore.CategoryContributions
            .Select(category => new CharacterPowerScoreCategoryDto(
                CharacterSnapshotPresentationCatalog.GetPowerScoreCategoryKey(category.Category),
                CharacterSnapshotPresentationCatalog.GetPowerScoreCategoryLabel(category.Category),
                category.Contribution,
                CharacterSnapshotPresentationCatalog.FormatPowerScore(category.Contribution),
                category.ShareOfTotal,
                CharacterSnapshotPresentationCatalog.FormatShare(category.ShareOfTotal)))
            .ToArray();

        var topStats = snapshot.PowerScore.StatContributions
            .Where(contribution => contribution.FinalContribution > 0m)
            .Take(6)
            .Select(contribution =>
            {
                var metadata = CharacterSnapshotPresentationCatalog.GetMetadata(contribution.StatType);
                return new CharacterPowerScoreTopStatDto(
                    metadata.Key,
                    metadata.Label,
                    contribution.FinalContribution,
                    CharacterSnapshotPresentationCatalog.FormatPowerScore(contribution.FinalContribution),
                    CharacterSnapshotPresentationCatalog.FormatStatValue(contribution.StatType, contribution.ActualStatValue));
            })
            .ToArray();

        return new CharacterPowerScoreDto(
            snapshot.PowerScore.TotalScore,
            CharacterSnapshotPresentationCatalog.FormatPowerScore(snapshot.PowerScore.TotalScore),
            Array.AsReadOnly(categories),
            Array.AsReadOnly(topStats),
            Array.AsReadOnly((snapshot.PowerScore.Notes ?? Array.Empty<string>()).ToArray()));
    }

    private static CharacterBasicComboDto? CreateBasicCombo(BasicAttackComboStatus? status)
    {
        if (status is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var windowRemainingSeconds = status.IsContinuationWindowActive && status.ContinuationWindowExpiresAtUtc.HasValue
            ? (decimal?)Math.Max(0m, decimal.Round(Convert.ToDecimal((status.ContinuationWindowExpiresAtUtc.Value - now).TotalSeconds), 2, MidpointRounding.AwayFromZero))
            : null;
        var statusLabel = status.IsContinuationWindowActive
            ? $"Next basic: {status.NextStage}/{status.ComboLength}"
            : $"Combo resets to stage 1/{status.ComboLength}";
        var notes = new List<string>();

        if (status.LastCompletedStage > 0 && status.LastCompletedAtUtc.HasValue)
        {
            notes.Add($"Last completed basic attack stage: {status.LastCompletedStage}/{status.ComboLength}.");
        }
        else
        {
            notes.Add("No basic attack has completed yet in the current combo sequence.");
        }

        if (status.IsContinuationWindowActive && windowRemainingSeconds.HasValue)
        {
            notes.Add($"The continuation window remains active for {CharacterSnapshotPresentationCatalog.FormatNumber(windowRemainingSeconds.Value)}s.");
        }

        if (!string.IsNullOrWhiteSpace(status.Note))
        {
            notes.Add(status.Note);
        }

        return new CharacterBasicComboDto(
            status.LastCompletedStage,
            status.NextStage,
            status.ComboLength,
            status.IsContinuationWindowActive,
            status.ContinuationWindowSeconds,
            status.CastTimeSeconds,
            windowRemainingSeconds,
            statusLabel,
            Array.AsReadOnly(notes.ToArray()));
    }

    private static CharacterResourceMetricDto CreateResourceMetric(string key, string label, decimal current, decimal? maximum)
    {
        var currentDisplay = CharacterSnapshotPresentationCatalog.FormatNumber(current);
        var maximumDisplay = maximum.HasValue ? CharacterSnapshotPresentationCatalog.FormatNumber(maximum.Value) : null;
        var displayValue = maximum.HasValue
            ? $"{currentDisplay} / {maximumDisplay}"
            : currentDisplay;

        return new CharacterResourceMetricDto(
            key,
            label,
            current,
            maximum,
            currentDisplay,
            maximumDisplay,
            displayValue);
    }

    private static decimal GetFinalStat(IReadOnlyDictionary<StatType, decimal> finalStats, StatType statType)
    {
        return finalStats.TryGetValue(statType, out var value)
            ? value
            : 0m;
    }
}
