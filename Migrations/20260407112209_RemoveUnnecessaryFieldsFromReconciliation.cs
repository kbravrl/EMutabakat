using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnnecessaryFieldsFromReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MutabakatTipi",
                table: "Mutabakatlar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MutabakatTipi",
                table: "Mutabakatlar",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
