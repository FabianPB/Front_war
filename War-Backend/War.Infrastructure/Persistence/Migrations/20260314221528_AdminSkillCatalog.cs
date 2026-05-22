using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace War.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminSkillCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_skill_records",
                columns: table => new
                {
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    class_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_ultimate = table.Column<bool>(type: "boolean", nullable: false),
                    unlock_level = table.Column<int>(type: "integer", nullable: false),
                    origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    definition_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_skill_records", x => x.record_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_skill_records_class_type_is_deleted",
                table: "admin_skill_records",
                columns: new[] { "class_type", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_skill_records_skill_id",
                table: "admin_skill_records",
                column: "skill_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_skill_records");
        }
    }
}
