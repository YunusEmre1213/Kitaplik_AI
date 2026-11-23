using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class OkumaSezonlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OkumaSezonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Baslangic = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GunSayisi = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KitapId = table.Column<int>(type: "int", nullable: false),
                    OlusturanId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OkumaSezonlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OkumaSezonlari_AspNetUsers_OlusturanId",
                        column: x => x.OlusturanId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OkumaSezonlari_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SezonMesajlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bolum = table.Column<int>(type: "int", nullable: false),
                    Metin = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SezonId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SezonMesajlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SezonMesajlari_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SezonMesajlari_OkumaSezonlari_SezonId",
                        column: x => x.SezonId,
                        principalTable: "OkumaSezonlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SezonUyeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KatilmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SonBolum = table.Column<int>(type: "int", nullable: false),
                    SezonId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SezonUyeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SezonUyeleri_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SezonUyeleri_OkumaSezonlari_SezonId",
                        column: x => x.SezonId,
                        principalTable: "OkumaSezonlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OkumaSezonlari_Baslangic",
                table: "OkumaSezonlari",
                column: "Baslangic");

            migrationBuilder.CreateIndex(
                name: "IX_OkumaSezonlari_KitapId",
                table: "OkumaSezonlari",
                column: "KitapId");

            migrationBuilder.CreateIndex(
                name: "IX_OkumaSezonlari_OlusturanId",
                table: "OkumaSezonlari",
                column: "OlusturanId");

            migrationBuilder.CreateIndex(
                name: "IX_SezonMesajlari_KullaniciId",
                table: "SezonMesajlari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SezonMesajlari_SezonId_Bolum_Tarih",
                table: "SezonMesajlari",
                columns: new[] { "SezonId", "Bolum", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_SezonUyeleri_KullaniciId",
                table: "SezonUyeleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SezonUyeleri_SezonId_KullaniciId",
                table: "SezonUyeleri",
                columns: new[] { "SezonId", "KullaniciId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SezonMesajlari");

            migrationBuilder.DropTable(
                name: "SezonUyeleri");

            migrationBuilder.DropTable(
                name: "OkumaSezonlari");
        }
    }
}
