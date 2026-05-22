using War.Core.Characters;
using War.Core.Progression;
using War.Core.Skills;

namespace War.Infrastructure.Persistence;

public class CharacterEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ClassType ClassType { get; set; } = ClassType.Sorcerer;

    public CharacterGender Gender { get; set; } = CharacterGender.Male;

    public int Level { get; set; } = SkillCatalogRules.MinimumCharacterLevel;

    public long CurrentXp { get; set; }

    public long XpToNextLevel { get; set; } = CharacterLevelRules.BaseExperienceRequiredForFirstLevelUp;

    public long TotalXp { get; set; }

    public decimal CurrentHp { get; set; }

    public decimal CurrentMana { get; set; }

    public decimal UltimateCharge { get; set; }

    public int LastBasicComboStage { get; set; }

    public DateTimeOffset? LastBasicComboCompletedAtUtc { get; set; }
}
