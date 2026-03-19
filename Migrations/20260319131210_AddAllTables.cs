using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class AddAllTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Firmalar",
                columns: table => new
                {
                    FirmaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmaAdi = table.Column<string>(type: "text", nullable: false),
                    FirmaUnvan = table.Column<string>(type: "text", nullable: false),
                    FirmaAdres = table.Column<string>(type: "text", nullable: false),
                    FirmaIlce = table.Column<string>(type: "text", nullable: false),
                    FirmaIl = table.Column<string>(type: "text", nullable: false),
                    FirmaVergiDairesi = table.Column<string>(type: "text", nullable: false),
                    FirmaVergiNumarasi = table.Column<string>(type: "text", nullable: false),
                    FirmaMersisNumarasi = table.Column<string>(type: "text", nullable: false),
                    FirmaWebAdresi = table.Column<string>(type: "text", nullable: false),
                    FirmaYetkiliAdiSoyadi = table.Column<string>(type: "text", nullable: false),
                    FirmaMail = table.Column<string>(type: "text", nullable: false),
                    FirmaTelefon = table.Column<string>(type: "text", nullable: false),
                    FirmaGsm = table.Column<string>(type: "text", nullable: false),
                    FirmaSmtpHost = table.Column<string>(type: "text", nullable: false),
                    FirmaSmtpPort = table.Column<int>(type: "integer", nullable: false),
                    FirmaSmtpUser = table.Column<string>(type: "text", nullable: false),
                    FirmaSmtpPassword = table.Column<string>(type: "text", nullable: false),
                    FirmaSmtpSecure = table.Column<string>(type: "text", nullable: false),
                    FirmaAktifPasif = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Firmalar", x => x.FirmaId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Firmalar");
        }
    }
}
