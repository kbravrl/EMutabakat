using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class AddLogAuthorityToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Loglar",
                table: "KullaniciYetkileri");

            migrationBuilder.AddColumn<bool>(
                name: "LogYetki",
                table: "KullaniciYetkileri",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogYetki",
                table: "KullaniciYetkileri");

            migrationBuilder.AddColumn<int>(
                name: "Loglar",
                table: "KullaniciYetkileri",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
