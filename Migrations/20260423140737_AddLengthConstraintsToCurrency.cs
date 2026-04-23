using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class AddLengthConstraintsToCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MutabakatDovizKodu",
                table: "SilinenMutabakatlar",
                type: "character varying(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "MutabakatDovizKodu",
                table: "Mutabakatlar",
                type: "character varying(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "TCMB",
                table: "DovizKodlari",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DovizKodlari",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CariDovizKodu",
                table: "Cariler",
                type: "character varying(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MutabakatDovizKodu",
                table: "SilinenMutabakatlar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)");

            migrationBuilder.AlterColumn<string>(
                name: "MutabakatDovizKodu",
                table: "Mutabakatlar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)");

            migrationBuilder.AlterColumn<string>(
                name: "TCMB",
                table: "DovizKodlari",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DovizKodlari",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CariDovizKodu",
                table: "Cariler",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)");
        }
    }
}
