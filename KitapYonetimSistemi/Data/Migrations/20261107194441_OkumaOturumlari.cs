using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class OkumaOturumlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OkumaOturumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Baslangic = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Bitis = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaslangicSayfa = table.Column<int>(type: "int", nullable: true),
                    BitisSayfa = table.Column<int>(type: "int", nullable: true),
                    KullaniciKitaplikId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OkumaOturumlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OkumaOturumlari_KullaniciKitapliklari_KullaniciKitaplikId",
                        column: x => x.KullaniciKitaplikId,
                        principalTable: "KullaniciKitapliklari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OkumaOturumlari_Bitis",
                table: "OkumaOturumlari",
                column: "Bitis");

            migrationBuilder.CreateIndex(
                name: "IX_OkumaOturumlari_KullaniciKitaplikId_Baslangic",
                table: "OkumaOturumlari",
                columns: new[] { "KullaniciKitaplikId", "Baslangic" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OkumaOturumlari");
        }
    }
}
