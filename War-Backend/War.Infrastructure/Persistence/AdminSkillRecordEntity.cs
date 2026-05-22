using War.Core.Skills;

namespace War.Infrastructure.Persistence;

public sealed class AdminSkillRecordEntity
{
    public Guid RecordId { get; set; }

    public string SkillId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ClassType ClassType { get; set; } = ClassType.Sorcerer;

    public SkillSlot Slot { get; set; } = SkillSlot.Slot01;

    public bool IsUltimate { get; set; }

    public int UnlockLevel { get; set; } = SkillCatalogRules.MinimumCharacterLevel;

    public string Origin { get; set; } = "CatalogImport";

    public bool IsDeleted { get; set; }

    public int DraftVersion { get; set; } = 1;

    public int? PublishedVersion { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset? PublishedAtUtc { get; set; }

    public string? PublishedBy { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public string DefinitionJson { get; set; } = string.Empty;

    public string? PublishedDefinitionJson { get; set; }
}
