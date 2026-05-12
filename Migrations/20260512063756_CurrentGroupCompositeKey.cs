using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class CurrentGroupCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cariler_CariGruplar_CariGrupId",
                table: "Cariler");

            migrationBuilder.DropIndex(
                name: "IX_Cariler_CariGrupId",
                table: "Cariler");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CariGruplar",
                table: "CariGruplar");

            migrationBuilder.DropIndex(
                name: "IX_CariGruplar_FirmaId",
                table: "CariGruplar");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CariGruplar",
                table: "CariGruplar",
                columns: new[] { "CariGrupId", "FirmaId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cariler_CariGrupId_FirmaId",
                table: "Cariler",
                columns: new[] { "CariGrupId", "FirmaId" });

            migrationBuilder.CreateIndex(
                name: "IX_CariGruplar_FirmaId_CariGrupAdi",
                table: "CariGruplar",
                columns: new[] { "FirmaId", "CariGrupAdi" });

            migrationBuilder.AddForeignKey(
                name: "FK_Cariler_CariGruplar_CariGrupId_FirmaId",
                table: "Cariler",
                columns: new[] { "CariGrupId", "FirmaId" },
                principalTable: "CariGruplar",
                principalColumns: new[] { "CariGrupId", "FirmaId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cariler_CariGruplar_CariGrupId_FirmaId",
                table: "Cariler");

            migrationBuilder.DropIndex(
                name: "IX_Cariler_CariGrupId_FirmaId",
                table: "Cariler");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CariGruplar",
                table: "CariGruplar");

            migrationBuilder.DropIndex(
                name: "IX_CariGruplar_FirmaId_CariGrupAdi",
                table: "CariGruplar");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CariGruplar",
                table: "CariGruplar",
                column: "CariGrupId");

            migrationBuilder.CreateIndex(
                name: "IX_Cariler_CariGrupId",
                table: "Cariler",
                column: "CariGrupId");

            migrationBuilder.CreateIndex(
                name: "IX_CariGruplar_FirmaId",
                table: "CariGruplar",
                column: "FirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cariler_CariGruplar_CariGrupId",
                table: "Cariler",
                column: "CariGrupId",
                principalTable: "CariGruplar",
                principalColumn: "CariGrupId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
