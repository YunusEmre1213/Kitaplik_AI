using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class OkumaDurumuEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OkumaBitisTarihi",
                table: "KullaniciKitapliklari",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OkumaDurumu",
                table: "KullaniciKitapliklari",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OkumayaBaslamaTarihi",
                table: "KullaniciKitapliklari",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OkumaBitisTarihi",
                table: "KullaniciKitapliklari");

            migrationBuilder.DropColumn(
                name: "OkumaDurumu",
                table: "KullaniciKitapliklari");

            migrationBuilder.DropColumn(
                name: "OkumayaBaslamaTarihi",
                table: "KullaniciKitapliklari");
        }
    }
}
