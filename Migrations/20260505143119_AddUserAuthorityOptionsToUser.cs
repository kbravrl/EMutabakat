using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuthorityOptionsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Kullanicilar");

            migrationBuilder.AddColumn<bool>(
                name: "IsSeedUser",
                table: "Kullanicilar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "KullaniciYetkileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<string>(type: "text", nullable: false),
                    Cariler = table.Column<int>(type: "integer", nullable: false),
                    CariGruplar = table.Column<int>(type: "integer", nullable: false),
                    DovizKodlari = table.Column<int>(type: "integer", nullable: false),
                    Mutabakatlar = table.Column<int>(type: "integer", nullable: false),
                    Firmalar = table.Column<int>(type: "integer", nullable: false),
                    Kullanicilar = table.Column<int>(type: "integer", nullable: false),
                    Loglar = table.Column<int>(type: "integer", nullable: false),
                    ImportYetki = table.Column<bool>(type: "boolean", nullable: false),
                    ExportYetki = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciYetkileri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KullaniciYetkileri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciYetkileri_KullaniciId",
                table: "KullaniciYetkileri",
                column: "KullaniciId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KullaniciYetkileri");

            migrationBuilder.DropColumn(
                name: "IsSeedUser",
                table: "Kullanicilar");

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Kullanicilar",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
