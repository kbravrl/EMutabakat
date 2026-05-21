using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyInformationAndDeleteReconciliationPermition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AylikBilgilerYetki",
                table: "KullaniciYetkileri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MutabakatSilYetki",
                table: "KullaniciYetkileri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AylikBilgiler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Yil = table.Column<int>(type: "integer", nullable: false),
                    Ay = table.Column<int>(type: "integer", nullable: false),
                    AcikMi = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AylikBilgiler", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AylikBilgiler_Yil_Ay",
                table: "AylikBilgiler",
                columns: new[] { "Yil", "Ay" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AylikBilgiler");

            migrationBuilder.DropColumn(
                name: "AylikBilgilerYetki",
                table: "KullaniciYetkileri");

            migrationBuilder.DropColumn(
                name: "MutabakatSilYetki",
                table: "KullaniciYetkileri");
        }
    }
}
