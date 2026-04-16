using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class PuanSistemiEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KitapPuanlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KitapId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Puan = table.Column<int>(type: "int", nullable: false),
                    PuanTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitapPuanlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitapPuanlari_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KitapPuanlari_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitapPuanlari_KitapId_KullaniciId",
                table: "KitapPuanlari",
                columns: new[] { "KitapId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitapPuanlari_KullaniciId",
                table: "KitapPuanlari",
                column: "KullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KitapPuanlari");
        }
    }
}
