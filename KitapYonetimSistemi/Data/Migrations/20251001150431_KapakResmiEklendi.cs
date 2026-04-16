using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class KapakResmiEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KapakResmiUrl",
                table: "Kitaplar",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KapakResmiUrl",
                table: "Kitaplar");
        }
    }
}
