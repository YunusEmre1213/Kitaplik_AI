using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class OkumaSozleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OkumaSozleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Not = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Bitis = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaslangicSayfa = table.Column<int>(type: "int", nullable: false),
                    UzatmaSayisi = table.Column<int>(type: "int", nullable: false),
                    KitapId = table.Column<int>(type: "int", nullable: false),
                    SahipId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OkumaSozleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OkumaSozleri_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SozDurtmeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tur = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SozId = table.Column<int>(type: "int", nullable: false),
                    GonderenId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SozDurtmeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SozDurtmeleri_OkumaSozleri_SozId",
                        column: x => x.SozId,
                        principalTable: "OkumaSozleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SozTanikleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SozId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SozTanikleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SozTanikleri_OkumaSozleri_SozId",
                        column: x => x.SozId,
                        principalTable: "OkumaSozleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OkumaSozleri_KitapId",
                table: "OkumaSozleri",
                column: "KitapId");

            migrationBuilder.CreateIndex(
                name: "IX_OkumaSozleri_Kod",
                table: "OkumaSozleri",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OkumaSozleri_SahipId",
                table: "OkumaSozleri",
                column: "SahipId");

            migrationBuilder.CreateIndex(
                name: "IX_SozDurtmeleri_SozId_GonderenId_Tarih",
                table: "SozDurtmeleri",
                columns: new[] { "SozId", "GonderenId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_SozTanikleri_KullaniciId",
                table: "SozTanikleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SozTanikleri_SozId_KullaniciId",
                table: "SozTanikleri",
                columns: new[] { "SozId", "KullaniciId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SozDurtmeleri");

            migrationBuilder.DropTable(
                name: "SozTanikleri");

            migrationBuilder.DropTable(
                name: "OkumaSozleri");
        }
    }
}
