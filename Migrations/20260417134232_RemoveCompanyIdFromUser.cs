using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMutabakat.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompanyIdFromUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KullaniciFirmalar_Firmalar_FirmaId",
                table: "KullaniciFirmalar");

            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_Firmalar_FirmaId",
                table: "Kullanicilar");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_FirmaId",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "Kullanicilar");

            migrationBuilder.AddForeignKey(
                name: "FK_KullaniciFirmalar_Firmalar_FirmaId",
                table: "KullaniciFirmalar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "FirmaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KullaniciFirmalar_Firmalar_FirmaId",
                table: "KullaniciFirmalar");

            migrationBuilder.AddColumn<int>(
                name: "FirmaId",
                table: "Kullanicilar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_FirmaId",
                table: "Kullanicilar",
                column: "FirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_KullaniciFirmalar_Firmalar_FirmaId",
                table: "KullaniciFirmalar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "FirmaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Firmalar_FirmaId",
                table: "Kullanicilar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "FirmaId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
