using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class AddIsEmailSentStatusToReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MutabakatMailGonderildi",
                table: "Mutabakatlar",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MutabakatMailGonderildi",
                table: "Mutabakatlar");
        }
    }
}
