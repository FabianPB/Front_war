namespace War.Infrastructure.Persistence;

public class CharacterSkillProgressEntity
{
    public Guid CharacterId { get; set; }

    public string SkillId { get; set; } = string.Empty;

    public bool IsUnlocked { get; set; }

    public int CurrentAscensionLevel { get; set; }

    public int? UnlockedAtCharacterLevel { get; set; }
}
