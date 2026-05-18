using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMutabakatIdUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mutabakatlar_MutabakatId",
                table: "Mutabakatlar");

            migrationBuilder.CreateIndex(
                name: "IX_Mutabakatlar_FirmaId_MutabakatId",
                table: "Mutabakatlar",
                columns: new[] { "FirmaId", "MutabakatId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mutabakatlar_FirmaId_MutabakatId",
                table: "Mutabakatlar");

            migrationBuilder.CreateIndex(
                name: "IX_Mutabakatlar_MutabakatId",
                table: "Mutabakatlar",
                column: "MutabakatId",
                unique: true);
        }
    }
}
