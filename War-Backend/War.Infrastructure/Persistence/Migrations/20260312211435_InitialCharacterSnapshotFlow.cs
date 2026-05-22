using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace War.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCharacterSnapshotFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "characters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    class_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    current_xp = table.Column<long>(type: "bigint", nullable: false),
                    xp_to_next_level = table.Column<long>(type: "bigint", nullable: false),
                    total_xp = table.Column<long>(type: "bigint", nullable: false),
                    current_hp = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    current_mana = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ultimate_charge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "character_skill_progress",
                columns: table => new
                {
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_unlocked = table.Column<bool>(type: "boolean", nullable: false),
                    current_ascension_level = table.Column<int>(type: "integer", nullable: false),
                    unlocked_at_character_level = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_skill_progress", x => new { x.character_id, x.skill_id });
                    table.ForeignKey(
                        name: "FK_character_skill_progress_characters_character_id",
                        column: x => x.character_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_skill_progress");

            migrationBuilder.DropTable(
                name: "characters");
        }
    }
}
