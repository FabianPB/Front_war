using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace War.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminSkillPublicationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "draft_version",
                table: "admin_skill_records",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "published_at_utc",
                table: "admin_skill_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "published_by",
                table: "admin_skill_records",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "published_definition_json",
                table: "admin_skill_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "published_version",
                table: "admin_skill_records",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_skill_records_is_deleted_published_version",
                table: "admin_skill_records",
                columns: new[] { "is_deleted", "published_version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_admin_skill_records_is_deleted_published_version",
                table: "admin_skill_records");

            migrationBuilder.DropColumn(
                name: "draft_version",
                table: "admin_skill_records");

            migrationBuilder.DropColumn(
                name: "published_at_utc",
                table: "admin_skill_records");

            migrationBuilder.DropColumn(
                name: "published_by",
                table: "admin_skill_records");

            migrationBuilder.DropColumn(
                name: "published_definition_json",
                table: "admin_skill_records");

            migrationBuilder.DropColumn(
                name: "published_version",
                table: "admin_skill_records");
        }
    }
}
