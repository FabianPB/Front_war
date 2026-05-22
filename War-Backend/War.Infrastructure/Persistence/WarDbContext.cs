using Microsoft.EntityFrameworkCore;
using War.Core.Characters;
using War.Core.Skills;
using War.Core.Social;

namespace War.Infrastructure.Persistence;

public class WarDbContext : DbContext
{
    public WarDbContext(DbContextOptions<WarDbContext> options)
        : base(options)
    {
    }

    public DbSet<CharacterEntity> Characters => Set<CharacterEntity>();

    public DbSet<CharacterSkillProgressEntity> CharacterSkillProgressEntries => Set<CharacterSkillProgressEntity>();

    public DbSet<AdminSkillRecordEntity> AdminSkillRecords => Set<AdminSkillRecordEntity>();

    public DbSet<SocialRelationshipEntity> SocialRelationships => Set<SocialRelationshipEntity>();

    public DbSet<FriendRequestEntity> FriendRequests => Set<FriendRequestEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<CharacterEntity>(entity =>
        {
            entity.ToTable("characters");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(x => x.ClassType)
                .HasConversion<string>()
                .HasColumnName("class_type")
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.Gender)
                .HasConversion<string>()
                .HasColumnName("gender")
                .HasMaxLength(16)
                .IsRequired();

            entity.Property(x => x.Level)
                .HasColumnName("level")
                .IsRequired();

            entity.Property(x => x.CurrentXp)
                .HasColumnName("current_xp")
                .IsRequired();

            entity.Property(x => x.XpToNextLevel)
                .HasColumnName("xp_to_next_level")
                .IsRequired();

            entity.Property(x => x.TotalXp)
                .HasColumnName("total_xp")
                .IsRequired();

            entity.Property(x => x.CurrentHp)
                .HasColumnName("current_hp")
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.CurrentMana)
                .HasColumnName("current_mana")
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.UltimateCharge)
                .HasColumnName("ultimate_charge")
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.LastBasicComboStage)
                .HasColumnName("last_basic_combo_stage")
                .IsRequired();

            entity.Property(x => x.LastBasicComboCompletedAtUtc)
                .HasColumnName("last_basic_combo_completed_at_utc");
        });

        builder.Entity<CharacterSkillProgressEntity>(entity =>
        {
            entity.ToTable("character_skill_progress");

            entity.HasKey(x => new { x.CharacterId, x.SkillId });

            entity.Property(x => x.CharacterId)
                .HasColumnName("character_id")
                .IsRequired();

            entity.Property(x => x.SkillId)
                .HasColumnName("skill_id")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(x => x.IsUnlocked)
                .HasColumnName("is_unlocked")
                .IsRequired();

            entity.Property(x => x.CurrentAscensionLevel)
                .HasColumnName("current_ascension_level")
                .IsRequired();

            entity.Property(x => x.UnlockedAtCharacterLevel)
                .HasColumnName("unlocked_at_character_level");

            entity.HasOne<CharacterEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AdminSkillRecordEntity>(entity =>
        {
            entity.ToTable("admin_skill_records");

            entity.HasKey(x => x.RecordId);

            entity.Property(x => x.RecordId)
                .HasColumnName("record_id")
                .ValueGeneratedNever();

            entity.Property(x => x.SkillId)
                .HasColumnName("skill_id")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.ClassType)
                .HasConversion<string>()
                .HasColumnName("class_type")
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.Slot)
                .HasConversion<string>()
                .HasColumnName("slot")
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.IsUltimate)
                .HasColumnName("is_ultimate")
                .IsRequired();

            entity.Property(x => x.UnlockLevel)
                .HasColumnName("unlock_level")
                .IsRequired();

            entity.Property(x => x.Origin)
                .HasColumnName("origin")
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.IsDeleted)
                .HasColumnName("is_deleted")
                .IsRequired();

            entity.Property(x => x.DraftVersion)
                .HasColumnName("draft_version")
                .IsRequired();

            entity.Property(x => x.PublishedVersion)
                .HasColumnName("published_version");

            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            entity.Property(x => x.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .IsRequired();

            entity.Property(x => x.PublishedAtUtc)
                .HasColumnName("published_at_utc");

            entity.Property(x => x.PublishedBy)
                .HasColumnName("published_by")
                .HasMaxLength(128);

            entity.Property(x => x.DeletedAtUtc)
                .HasColumnName("deleted_at_utc");

            entity.Property(x => x.DefinitionJson)
                .HasColumnName("definition_json")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.PublishedDefinitionJson)
                .HasColumnName("published_definition_json")
                .HasColumnType("text");

            entity.HasIndex(x => x.SkillId)
                .IsUnique();

            entity.HasIndex(x => new { x.ClassType, x.IsDeleted });
            entity.HasIndex(x => new { x.IsDeleted, x.PublishedVersion });
        });

        builder.Entity<SocialRelationshipEntity>(entity =>
        {
            entity.ToTable("social_relationships");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(x => x.OwnerId)
                .HasColumnName("owner_id")
                .IsRequired();

            entity.Property(x => x.TargetId)
                .HasColumnName("target_id")
                .IsRequired();

            entity.Property(x => x.Type)
                .HasConversion<string>()
                .HasColumnName("type")
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            // Decision: Unique composite index prevents duplicate relationships between the same pair.
            entity.HasIndex(x => new { x.OwnerId, x.TargetId })
                .IsUnique();

            // Decision: Separate index on TargetId for efficient reverse lookups ("who has me as friend/blocked?").
            entity.HasIndex(x => x.TargetId);

            entity.HasOne<CharacterEntity>()
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<CharacterEntity>()
                .WithMany()
                .HasForeignKey(x => x.TargetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FriendRequestEntity>(entity =>
        {
            entity.ToTable("friend_requests");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(x => x.SenderId)
                .HasColumnName("sender_id")
                .IsRequired();

            entity.Property(x => x.ReceiverId)
                .HasColumnName("receiver_id")
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasColumnName("status")
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            entity.Property(x => x.ResolvedAtUtc)
                .HasColumnName("resolved_at_utc");

            // Decision: Composite indexes on (SenderId, Status) and (ReceiverId, Status) for efficient
            // queries like "all pending requests I sent" or "all pending requests I received".
            entity.HasIndex(x => new { x.SenderId, x.Status });
            entity.HasIndex(x => new { x.ReceiverId, x.Status });

            entity.HasOne<CharacterEntity>()
                .WithMany()
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<CharacterEntity>()
                .WithMany()
                .HasForeignKey(x => x.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

