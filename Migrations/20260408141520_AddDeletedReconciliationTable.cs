using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedReconciliationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mutabakatlar_FirmaId",
                table: "Mutabakatlar");

            migrationBuilder.CreateTable(
                name: "SilinenMutabakatlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MutabakatId = table.Column<string>(type: "text", nullable: false),
                    MutabakatTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    CariId = table.Column<string>(type: "text", nullable: false),
                    MutabakatDovizKodu = table.Column<string>(type: "text", nullable: false),
                    MutabakatBakiye = table.Column<decimal>(type: "numeric", nullable: false),
                    MutabakatBakiyeTipi = table.Column<string>(type: "text", nullable: false),
                    MutabakatAciklama = table.Column<string>(type: "text", nullable: true),
                    MutabakatGonderimTarihSaat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MutabakatGonderimDurumu = table.Column<int>(type: "integer", nullable: false),
                    MutabakatCevapTarihSaat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MutabakatCevapMail = table.Column<string>(type: "text", nullable: true),
                    MutabakatCevapAdSoyad = table.Column<string>(type: "text", nullable: true),
                    MutabakatCevapGsm = table.Column<string>(type: "text", nullable: true),
                    MutabakatCevapAciklama = table.Column<string>(type: "text", nullable: true),
                    MutabakatDurum = table.Column<int>(type: "integer", nullable: false),
                    MutabakatToken = table.Column<string>(type: "text", nullable: true),
                    MutabakatReceiveStoragePath = table.Column<string>(type: "text", nullable: true),
                    SilinmeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SilinmeNedeni = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SilinenMutabakatlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SilinenMutabakatlar_Cariler_CariId_FirmaId",
                        columns: x => new { x.CariId, x.FirmaId },
                        principalTable: "Cariler",
                        principalColumns: new[] { "CariId", "FirmaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SilinenMutabakatlar_DovizKodlari_MutabakatDovizKodu",
                        column: x => x.MutabakatDovizKodu,
                        principalTable: "DovizKodlari",
                        principalColumn: "TCMB",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SilinenMutabakatlar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "FirmaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mutabakatlar_FirmaId_CariId_MutabakatTarihi",
                table: "Mutabakatlar",
                columns: new[] { "FirmaId", "CariId", "MutabakatTarihi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SilinenMutabakatlar_CariId_FirmaId",
                table: "SilinenMutabakatlar",
                columns: new[] { "CariId", "FirmaId" });

            migrationBuilder.CreateIndex(
                name: "IX_SilinenMutabakatlar_FirmaId_CariId_MutabakatTarihi",
                table: "SilinenMutabakatlar",
                columns: new[] { "FirmaId", "CariId", "MutabakatTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_SilinenMutabakatlar_MutabakatDovizKodu",
                table: "SilinenMutabakatlar",
                column: "MutabakatDovizKodu");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SilinenMutabakatlar");

            migrationBuilder.DropIndex(
                name: "IX_Mutabakatlar_FirmaId_CariId_MutabakatTarihi",
                table: "Mutabakatlar");

            migrationBuilder.CreateIndex(
                name: "IX_Mutabakatlar_FirmaId",
                table: "Mutabakatlar",
                column: "FirmaId");
        }
    }
}
