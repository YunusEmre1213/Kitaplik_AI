using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class BirakmaNedeni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirakmaNedeni",
                table: "KullaniciKitapliklari",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BirakmaTarihi",
                table: "KullaniciKitapliklari",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirakmaNedeni",
                table: "KullaniciKitapliklari");

            migrationBuilder.DropColumn(
                name: "BirakmaTarihi",
                table: "KullaniciKitapliklari");
        }
    }
}
