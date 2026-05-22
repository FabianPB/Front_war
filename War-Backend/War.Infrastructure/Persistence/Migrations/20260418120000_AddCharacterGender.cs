using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace War.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterGender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Decision: Add 'gender' as NOT NULL with defaultValue "Male" so existing rows
            // (demo characters seeded before this migration) are populated safely and the
            // constraint can be added atomically without a two-step migration.
            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "characters",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Male");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gender",
                table: "characters");
        }
    }
}
