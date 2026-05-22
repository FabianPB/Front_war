using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace War.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BasicAttackComboState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_basic_combo_completed_at_utc",
                table: "characters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "last_basic_combo_stage",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_basic_combo_completed_at_utc",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "last_basic_combo_stage",
                table: "characters");
        }
    }
}
