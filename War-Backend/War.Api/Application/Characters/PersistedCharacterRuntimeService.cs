using Microsoft.EntityFrameworkCore;
using War.Core.Entities;
using War.Core.Progression;
using War.Core.Resources;
using War.Core.Skills;
using War.Infrastructure.Persistence;

namespace War.Api.Application.Characters;

public sealed record PersistedCharacterRuntimeState(
    CharacterEntity Entity,
    Character Character,
    CharacterLevelProgress Progression,
    CharacterResources Resources,
    War.Core.Combat.BasicAttackRuntimeState BasicAttackRuntimeState,
    CharacterSkillProgressCollection? SkillProgress,
    IReadOnlyList<CharacterSkillProgressEntity> SkillProgressEntries);

public interface IPersistedCharacterRuntimeService
{
    Task<PersistedCharacterRuntimeState?> LoadAsync(Guid characterId, bool tracked = false, CancellationToken cancellationToken = default);
}

public sealed class PersistedCharacterRuntimeService : IPersistedCharacterRuntimeService
{
    private readonly WarDbContext _dbContext;
    private readonly ICharacterLevelProgressionService _levelProgressionService;
    private readonly ICharacterFinalStatsBuilder _finalStatsBuilder;

    public PersistedCharacterRuntimeService(
        WarDbContext dbContext,
        ICharacterLevelProgressionService levelProgressionService,
        ICharacterFinalStatsBuilder finalStatsBuilder)
    {
        _dbContext = dbContext;
        _levelProgressionService = levelProgressionService;
        _finalStatsBuilder = finalStatsBuilder;
    }

    public async Task<PersistedCharacterRuntimeState?> LoadAsync(Guid characterId, bool tracked = false, CancellationToken cancellationToken = default)
    {
        var characterQuery = tracked
            ? _dbContext.Characters.AsQueryable()
            : _dbContext.Characters.AsNoTracking();

        var entity = await characterQuery
            .SingleOrDefaultAsync(x => x.Id == characterId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var skillProgressEntriesQuery = tracked
            ? _dbContext.CharacterSkillProgressEntries.AsQueryable()
            : _dbContext.CharacterSkillProgressEntries.AsNoTracking();

        var skillProgressEntries = await skillProgressEntriesQuery
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        var progression = _levelProgressionService.CreateProgress(entity.Level, entity.CurrentXp, entity.TotalXp);
        var resources = new CharacterResources(entity.CurrentHp, entity.CurrentMana, entity.UltimateCharge);
        var finalStats = _finalStatsBuilder.Build(entity.ClassType, entity.Level);
        var character = new Character(entity.Id, finalStats, resources, entity.ClassType, entity.Level);
        var skillProgress = skillProgressEntries.Count == 0
            ? null
            : new CharacterSkillProgressCollection(skillProgressEntries.Select(MapSkillProgress));

        return new PersistedCharacterRuntimeState(
            entity,
            character,
            progression,
            resources,
            new War.Core.Combat.BasicAttackRuntimeState(entity.LastBasicComboStage, entity.LastBasicComboCompletedAtUtc),
            skillProgress,
            Array.AsReadOnly(skillProgressEntries.ToArray()));
    }

    private static CharacterSkillProgress MapSkillProgress(CharacterSkillProgressEntity entity)
    {
        return new CharacterSkillProgress(
            entity.CharacterId,
            entity.SkillId,
            entity.IsUnlocked,
            entity.CurrentAscensionLevel,
            entity.UnlockedAtCharacterLevel);
    }
}
