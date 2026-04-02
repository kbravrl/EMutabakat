using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerExplanationToReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MutabakatCevapAciklama",
                table: "Mutabakatlar",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MutabakatCevapAciklama",
                table: "Mutabakatlar");
        }
    }
}
