using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToCurrent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cariler_FirmaId",
                table: "Cariler");

            migrationBuilder.CreateIndex(
                name: "IX_Cariler_FirmaId_CariAdi",
                table: "Cariler",
                columns: new[] { "FirmaId", "CariAdi" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cariler_FirmaId_CariAdi",
                table: "Cariler");

            migrationBuilder.CreateIndex(
                name: "IX_Cariler_FirmaId",
                table: "Cariler",
                column: "FirmaId");
        }
    }
}
