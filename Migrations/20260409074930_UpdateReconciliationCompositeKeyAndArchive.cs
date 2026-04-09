using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReconciliationCompositeKeyAndArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Mutabakatlar",
                table: "Mutabakatlar");

            migrationBuilder.DropIndex(
                name: "IX_Mutabakatlar_FirmaId_CariId_MutabakatTarihi",
                table: "Mutabakatlar");

            migrationBuilder.DropColumn(
                name: "SilinmeNedeni",
                table: "SilinenMutabakatlar");

            migrationBuilder.AlterColumn<string>(
                name: "MutabakatId",
                table: "Mutabakatlar",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Mutabakatlar",
                table: "Mutabakatlar",
                columns: new[] { "FirmaId", "CariId", "MutabakatTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Mutabakatlar_MutabakatId",
                table: "Mutabakatlar",
                column: "MutabakatId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Mutabakatlar",
                table: "Mutabakatlar");

            migrationBuilder.DropIndex(
                name: "IX_Mutabakatlar_MutabakatId",
                table: "Mutabakatlar");

            migrationBuilder.AddColumn<string>(
                name: "SilinmeNedeni",
                table: "SilinenMutabakatlar",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "MutabakatId",
                table: "Mutabakatlar",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Mutabakatlar",
                table: "Mutabakatlar",
                column: "MutabakatId");

            migrationBuilder.CreateIndex(
                name: "IX_Mutabakatlar_FirmaId_CariId_MutabakatTarihi",
                table: "Mutabakatlar",
                columns: new[] { "FirmaId", "CariId", "MutabakatTarihi" },
                unique: true);
        }
    }
}
