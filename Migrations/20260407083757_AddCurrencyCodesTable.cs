using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyCodesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MutabakatDovizKodu",
                table: "Mutabakatlar",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "CariDovizKodu",
                table: "Cariler",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DovizKodlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TCMB = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DovizKodlari", x => x.Id);
                    table.UniqueConstraint("AK_DovizKodlari_TCMB", x => x.TCMB);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mutabakatlar_MutabakatDovizKodu",
                table: "Mutabakatlar",
                column: "MutabakatDovizKodu");

            migrationBuilder.CreateIndex(
                name: "IX_Cariler_CariDovizKodu",
                table: "Cariler",
                column: "CariDovizKodu");

            migrationBuilder.AddForeignKey(
                name: "FK_Cariler_DovizKodlari_CariDovizKodu",
                table: "Cariler",
                column: "CariDovizKodu",
                principalTable: "DovizKodlari",
                principalColumn: "TCMB",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mutabakatlar_DovizKodlari_MutabakatDovizKodu",
                table: "Mutabakatlar",
                column: "MutabakatDovizKodu",
                principalTable: "DovizKodlari",
                principalColumn: "TCMB",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cariler_DovizKodlari_CariDovizKodu",
                table: "Cariler");

            migrationBuilder.DropForeignKey(
                name: "FK_Mutabakatlar_DovizKodlari_MutabakatDovizKodu",
                table: "Mutabakatlar");

            migrationBuilder.DropTable(
                name: "DovizKodlari");

            migrationBuilder.DropIndex(
                name: "IX_Mutabakatlar_MutabakatDovizKodu",
                table: "Mutabakatlar");

            migrationBuilder.DropIndex(
                name: "IX_Cariler_CariDovizKodu",
                table: "Cariler");

            migrationBuilder.AlterColumn<int>(
                name: "MutabakatDovizKodu",
                table: "Mutabakatlar",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "CariDovizKodu",
                table: "Cariler",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
