using War.Core.Entities;

namespace War.Core.Skills;

public sealed record CharacterSkillProgress(
    Guid CharacterId,
    string SkillId,
    bool IsUnlocked,
    int CurrentAscensionLevel,
    int? UnlockedAtCharacterLevel = null);

public sealed class CharacterSkillProgressCollection
{
    private readonly IReadOnlyDictionary<string, CharacterSkillProgress> _entriesBySkillId;

    public CharacterSkillProgressCollection(IEnumerable<CharacterSkillProgress>? entries = null)
    {
        _entriesBySkillId = (entries ?? Array.Empty<CharacterSkillProgress>())
            .GroupBy(entry => entry.SkillId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<CharacterSkillProgress> Entries => _entriesBySkillId.Values.ToArray();

    public bool TryGet(string skillId, out CharacterSkillProgress? progress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);

        if (_entriesBySkillId.TryGetValue(skillId, out var storedProgress))
        {
            progress = storedProgress;
            return true;
        }

        progress = null;
        return false;
    }

    public CharacterSkillProgress? GetOrDefault(string skillId)
    {
        return TryGet(skillId, out var progress)
            ? progress
            : null;
    }
}

public enum SkillAvailabilityStatus
{
    Ready,
    LockedByClassMismatch,
    LockedByCharacterLevel,
    LockedBySkillProgress,
    InvalidSkillProgress
}

public sealed record SkillAvailabilityResult(
    SkillAvailabilityStatus Status,
    int ResolvedAscensionLevel,
    bool MeetsCharacterLevelRequirement,
    bool HasProgressRecord,
    IReadOnlyList<string>? Notes = null)
{
    public bool IsAvailable => Status == SkillAvailabilityStatus.Ready;
}

public interface ISkillAvailabilityEvaluator
{
    SkillAvailabilityResult Evaluate(
        SkillDefinition definition,
        Character actor,
        CharacterSkillProgress? progress = null);
}

public sealed class SkillAvailabilityEvaluator : ISkillAvailabilityEvaluator
{
    public SkillAvailabilityResult Evaluate(
        SkillDefinition definition,
        Character actor,
        CharacterSkillProgress? progress = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(actor);

        var notes = new List<string>();

        if (actor.ClassType != definition.ClassType)
        {
            notes.Add($"Skill '{definition.Id}' belongs to class {definition.ClassType}, but actor {actor.Id} is {actor.ClassType}.");

            return new SkillAvailabilityResult(
                SkillAvailabilityStatus.LockedByClassMismatch,
                0,
                false,
                progress is not null,
                notes);
        }

        var meetsCharacterLevelRequirement = actor.Level >= definition.UnlockLevel;
        if (!meetsCharacterLevelRequirement)
        {
            notes.Add($"Skill '{definition.Id}' unlocks at level {definition.UnlockLevel}, but actor {actor.Id} is level {actor.Level}.");

            return new SkillAvailabilityResult(
                SkillAvailabilityStatus.LockedByCharacterLevel,
                0,
                false,
                progress is not null,
                notes);
        }

        if (progress is null)
        {
            notes.Add("No persisted skill progress was supplied, so availability fell back to class and character-level requirements.");

            return new SkillAvailabilityResult(
                SkillAvailabilityStatus.Ready,
                SkillCatalogRules.MinimumAscensionLevel,
                true,
                false,
                notes);
        }

        if (progress.CharacterId != actor.Id)
        {
            notes.Add($"Skill progress for '{definition.Id}' belongs to character {progress.CharacterId}, not actor {actor.Id}.");

            return new SkillAvailabilityResult(
                SkillAvailabilityStatus.InvalidSkillProgress,
                0,
                true,
                true,
                notes);
        }

        if (!string.Equals(progress.SkillId, definition.Id, StringComparison.OrdinalIgnoreCase))
        {
            notes.Add($"Skill progress id '{progress.SkillId}' does not match skill definition '{definition.Id}'.");

            return new SkillAvailabilityResult(
                SkillAvailabilityStatus.InvalidSkillProgress,
                0,
                true,
                true,
                notes);
        }

        if (!progress.IsUnlocked)
        {
            notes.Add($"Skill '{definition.Id}' is still marked as locked in persisted progress.");

            return new SkillAvailabilityResult(
                SkillAvailabilityStatus.LockedBySkillProgress,
                0,
                true,
                true,
                notes);
        }

        if (progress.CurrentAscensionLevel < SkillCatalogRules.MinimumAscensionLevel ||
            progress.CurrentAscensionLevel > SkillCatalogRules.MaximumAscensionLevel)
        {
            notes.Add($"Skill '{definition.Id}' has persisted ascension level {progress.CurrentAscensionLevel}, but supported levels are {SkillCatalogRules.MinimumAscensionLevel} to {SkillCatalogRules.MaximumAscensionLevel}.");

            return new SkillAvailabilityResult(
                SkillAvailabilityStatus.InvalidSkillProgress,
                progress.CurrentAscensionLevel,
                true,
                true,
                notes);
        }

        return new SkillAvailabilityResult(
            SkillAvailabilityStatus.Ready,
            progress.CurrentAscensionLevel,
            true,
            true,
            notes);
    }
}
