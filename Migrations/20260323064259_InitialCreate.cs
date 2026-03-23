using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CariGruplar",
                columns: table => new
                {
                    CariGrupId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    CariGrupAdi = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariGruplar", x => x.CariGrupId);
                    table.ForeignKey(
                        name: "FK_CariGruplar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "FirmaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    KullaniciId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "text", nullable: false),
                    KullaniciSoyadi = table.Column<string>(type: "text", nullable: false),
                    KullaniciMail = table.Column<string>(type: "text", nullable: false),
                    KullaniciGsm = table.Column<string>(type: "text", nullable: false),
                    KullaniciAktifPasif = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.KullaniciId);
                    table.ForeignKey(
                        name: "FK_Kullanicilar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "FirmaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cariler",
                columns: table => new
                {
                    CariId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    CariAdi = table.Column<string>(type: "text", nullable: false),
                    CariUnvan = table.Column<string>(type: "text", nullable: false),
                    CariAdres = table.Column<string>(type: "text", nullable: false),
                    CariIlce = table.Column<string>(type: "text", nullable: false),
                    CariIl = table.Column<string>(type: "text", nullable: false),
                    CariVergiDairesi = table.Column<string>(type: "text", nullable: false),
                    CariVergiNumarasi = table.Column<string>(type: "text", nullable: false),
                    CariWebAdresi = table.Column<string>(type: "text", nullable: false),
                    CariYetkiliAdiSoyadi = table.Column<string>(type: "text", nullable: false),
                    CariYetkiliTelefon = table.Column<string>(type: "text", nullable: false),
                    CariYetkiliGsm = table.Column<string>(type: "text", nullable: false),
                    CariYetkiliMail = table.Column<string>(type: "text", nullable: false),
                    CariGrupId = table.Column<int>(type: "integer", nullable: false),
                    CariDovizKodu = table.Column<int>(type: "integer", nullable: false),
                    CariAktifPasif = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cariler", x => x.CariId);
                    table.ForeignKey(
                        name: "FK_Cariler_CariGruplar_CariGrupId",
                        column: x => x.CariGrupId,
                        principalTable: "CariGruplar",
                        principalColumn: "CariGrupId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cariler_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "FirmaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mutabakatlar",
                columns: table => new
                {
                    MutabakatId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MutabakatDonemi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MutabakatTipi = table.Column<int>(type: "integer", nullable: false),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    CariId = table.Column<int>(type: "integer", nullable: false),
                    MutabakatDovizKodu = table.Column<int>(type: "integer", nullable: false),
                    MutabakatBakiye = table.Column<decimal>(type: "numeric", nullable: false),
                    MutabakatBakiyeTipi = table.Column<string>(type: "text", nullable: false),
                    MutabakatAciklama = table.Column<string>(type: "text", nullable: false),
                    MutabakatGonderimTarihSaat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MutabakatGonderimDurumu = table.Column<int>(type: "integer", nullable: false),
                    MutabakatCevapTarihSaat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MutabakatCevapMail = table.Column<string>(type: "text", nullable: false),
                    MutabakatCevapAdSoyad = table.Column<string>(type: "text", nullable: false),
                    MutabakatCevapGsm = table.Column<string>(type: "text", nullable: false),
                    MutabakatDurum = table.Column<int>(type: "integer", nullable: false),
                    MutabakatToken = table.Column<string>(type: "text", nullable: false),
                    MutabakatReceiveStoragePath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mutabakatlar", x => x.MutabakatId);
                    table.ForeignKey(
                        name: "FK_Mutabakatlar_Cariler_CariId",
                        column: x => x.CariId,
                        principalTable: "Cariler",
                        principalColumn: "CariId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mutabakatlar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "FirmaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CariGruplar_FirmaId",
                table: "CariGruplar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cariler_CariGrupId",
                table: "Cariler",
                column: "CariGrupId");

            migrationBuilder.CreateIndex(
                name: "IX_Cariler_FirmaId",
                table: "Cariler",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_FirmaId",
                table: "Kullanicilar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_Mutabakatlar_CariId",
                table: "Mutabakatlar",
                column: "CariId");

            migrationBuilder.CreateIndex(
                name: "IX_Mutabakatlar_FirmaId",
                table: "Mutabakatlar",
                column: "FirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kullanicilar");

            migrationBuilder.DropTable(
                name: "Mutabakatlar");

            migrationBuilder.DropTable(
                name: "Cariler");

            migrationBuilder.DropTable(
                name: "CariGruplar");
        }
    }
}
