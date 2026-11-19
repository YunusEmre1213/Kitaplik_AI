using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class OturumRuhHali : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RuhHaliBasi",
                table: "OkumaOturumlari",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RuhHaliSonu",
                table: "OkumaOturumlari",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RuhHaliBasi",
                table: "OkumaOturumlari");

            migrationBuilder.DropColumn(
                name: "RuhHaliSonu",
                table: "OkumaOturumlari");
        }
    }
}
