using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class OneriZinciri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OneriKabulleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KabulTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OneriId = table.Column<int>(type: "int", nullable: false),
                    KitapId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneriKabulleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Oneriler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Not = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KitapId = table.Column<int>(type: "int", nullable: false),
                    GonderenId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OncekiKabulId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oneriler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Oneriler_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Oneriler_OneriKabulleri_OncekiKabulId",
                        column: x => x.OncekiKabulId,
                        principalTable: "OneriKabulleri",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OneriKabulleri_KullaniciId_KitapId",
                table: "OneriKabulleri",
                columns: new[] { "KullaniciId", "KitapId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OneriKabulleri_OneriId",
                table: "OneriKabulleri",
                column: "OneriId");

            migrationBuilder.CreateIndex(
                name: "IX_Oneriler_GonderenId",
                table: "Oneriler",
                column: "GonderenId");

            migrationBuilder.CreateIndex(
                name: "IX_Oneriler_KitapId",
                table: "Oneriler",
                column: "KitapId");

            migrationBuilder.CreateIndex(
                name: "IX_Oneriler_Kod",
                table: "Oneriler",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Oneriler_OncekiKabulId",
                table: "Oneriler",
                column: "OncekiKabulId");

            migrationBuilder.AddForeignKey(
                name: "FK_OneriKabulleri_Oneriler_OneriId",
                table: "OneriKabulleri",
                column: "OneriId",
                principalTable: "Oneriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OneriKabulleri_Oneriler_OneriId",
                table: "OneriKabulleri");

            migrationBuilder.DropTable(
                name: "Oneriler");

            migrationBuilder.DropTable(
                name: "OneriKabulleri");
        }
    }
}
