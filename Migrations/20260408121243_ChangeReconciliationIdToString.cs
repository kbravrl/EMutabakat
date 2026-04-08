using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class ChangeReconciliationIdToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MutabakatDonemi",
                table: "Mutabakatlar",
                newName: "MutabakatTarihi");

            // Ensure any identity/sequence is removed (safe if not present) and change type to text.
            // Use raw SQL to avoid EF emitting additional ALTER statements that may try to drop/add
            // identity when the column is already in the desired state.
            migrationBuilder.Sql("ALTER TABLE \"Mutabakatlar\" ALTER COLUMN \"MutabakatId\" DROP IDENTITY IF EXISTS;");
            migrationBuilder.Sql("ALTER TABLE \"Mutabakatlar\" ALTER COLUMN \"MutabakatId\" TYPE text USING \"MutabakatId\"::text;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MutabakatTarihi",
                table: "Mutabakatlar",
                newName: "MutabakatDonemi");

            migrationBuilder.AlterColumn<int>(
                name: "MutabakatId",
                table: "Mutabakatlar",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
