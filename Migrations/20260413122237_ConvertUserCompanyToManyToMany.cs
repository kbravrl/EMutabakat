using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class ConvertUserCompanyToManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KullaniciFirmalari");

            migrationBuilder.CreateTable(
                name: "KullaniciFirmalar",
                columns: table => new
                {
                    KullaniciFirmaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciFirmalar", x => x.KullaniciFirmaId);
                    table.ForeignKey(
                        name: "FK_KullaniciFirmalar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "FirmaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KullaniciFirmalar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciFirmalar_FirmaId",
                table: "KullaniciFirmalar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciFirmalar_KullaniciId_FirmaId",
                table: "KullaniciFirmalar",
                columns: new[] { "KullaniciId", "FirmaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KullaniciFirmalar");

            migrationBuilder.CreateTable(
                name: "KullaniciFirmalari",
                columns: table => new
                {
                    KullaniciFirmaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciFirmalari", x => x.KullaniciFirmaId);
                    table.ForeignKey(
                        name: "FK_KullaniciFirmalari_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "FirmaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KullaniciFirmalari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciFirmalari_FirmaId",
                table: "KullaniciFirmalari",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciFirmalari_KullaniciId_FirmaId",
                table: "KullaniciFirmalari",
                columns: new[] { "KullaniciId", "FirmaId" },
                unique: true);
        }
    }
}
