using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class KitapIsbnDisKaynak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisKaynakId",
                table: "Kitaplar",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Isbn",
                table: "Kitaplar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_DisKaynakId",
                table: "Kitaplar",
                column: "DisKaynakId",
                unique: true,
                filter: "[DisKaynakId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_Isbn",
                table: "Kitaplar",
                column: "Isbn",
                unique: true,
                filter: "[Isbn] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_DisKaynakId",
                table: "Kitaplar");

            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_Isbn",
                table: "Kitaplar");

            migrationBuilder.DropColumn(
                name: "DisKaynakId",
                table: "Kitaplar");

            migrationBuilder.DropColumn(
                name: "Isbn",
                table: "Kitaplar");
        }
    }
}
