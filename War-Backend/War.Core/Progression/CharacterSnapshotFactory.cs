using War.Core.Entities;
using War.Core.PowerScore;
using War.Core.Resources;
using War.Core.Skills;

namespace War.Core.Progression;

public interface ICharacterProfileSnapshotFactory
{
    CharacterProfileSnapshot Create(CharacterProfileSnapshotRequest request);
}

public sealed class CharacterProfileSnapshotFactory : ICharacterProfileSnapshotFactory
{
    private readonly ICharacterFinalStatsBuilder _finalStatsBuilder;
    private readonly IPowerScoreCalculator _powerScoreCalculator;

    public CharacterProfileSnapshotFactory(
        ICharacterFinalStatsBuilder? finalStatsBuilder = null,
        IPowerScoreCalculator? powerScoreCalculator = null)
    {
        _finalStatsBuilder = finalStatsBuilder ?? new CharacterFinalStatsBuilder();
        _powerScoreCalculator = powerScoreCalculator ?? new PowerScoreCalculator();
    }

    public CharacterProfileSnapshot Create(CharacterProfileSnapshotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Progression);
        ArgumentNullException.ThrowIfNull(request.Resources);

        var buildResult = _finalStatsBuilder.Build(new CharacterFinalStatsBuildContext(
            request.ClassType,
            request.Progression.Level,
            request.AdditionalStatSources));

        var notes = new List<string>
        {
            $"Snapshot assembled with {buildResult.Sources.Count} stat source(s), including class level growth."
        };

        if (request.Progression.IsAtMaxLevel)
        {
            notes.Add("Character is currently at max level.");
        }

        PowerScoreResult? powerScore = null;

        if (request.IncludePowerScore)
        {
            var character = new Character(
                request.CharacterId,
                buildResult.FinalStats,
                request.Resources,
                request.ClassType,
                request.Progression.Level);

            powerScore = _powerScoreCalculator.Calculate(new PowerScoreCalculationContext(
                character,
                request.SkillProgress,
                request.SkillCatalog ?? SkillCatalogRegistry.Current,
                PowerScoreClassProfileRegistry.GetRequired(request.ClassType)));
        }

        return new CharacterProfileSnapshot(
            request.CharacterId,
            request.ClassType,
            request.Progression,
            buildResult.FinalStats.GetAll(),
            new CharacterResourceSnapshot(
                request.Resources.CurrentHp,
                request.Resources.CurrentMana,
                request.Resources.UltimateCharge),
            powerScore,
            Array.AsReadOnly(notes.ToArray()));
    }
}

