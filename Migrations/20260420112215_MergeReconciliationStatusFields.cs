using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class MergeReconciliationStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MutabakatDurum",
                table: "SilinenMutabakatlar");

            migrationBuilder.DropColumn(
                name: "MutabakatDurum",
                table: "Mutabakatlar");

            migrationBuilder.DropColumn(
                name: "MutabakatMailGonderildi",
                table: "Mutabakatlar");

            migrationBuilder.RenameColumn(
                name: "MutabakatGonderimDurumu",
                table: "SilinenMutabakatlar",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "MutabakatGonderimDurumu",
                table: "Mutabakatlar",
                newName: "Status");

            migrationBuilder.AlterColumn<DateTime>(
                name: "MutabakatGonderimTarihSaat",
                table: "Mutabakatlar",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "SilinenMutabakatlar",
                newName: "MutabakatGonderimDurumu");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Mutabakatlar",
                newName: "MutabakatGonderimDurumu");

            migrationBuilder.AddColumn<int>(
                name: "MutabakatDurum",
                table: "SilinenMutabakatlar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "MutabakatGonderimTarihSaat",
                table: "Mutabakatlar",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MutabakatDurum",
                table: "Mutabakatlar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MutabakatMailGonderildi",
                table: "Mutabakatlar",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
