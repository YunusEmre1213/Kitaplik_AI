using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class OkumaHedefiGunlukAlintiSpoiler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Spoiler",
                table: "Yorumlar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MevcutSayfa",
                table: "KullaniciKitapliklari",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SayfaSayisi",
                table: "Kitaplar",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Alintilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Metin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SayfaNo = table.Column<int>(type: "int", nullable: true),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KitapId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alintilar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alintilar_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alintilar_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OkumaGunlukleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sayfa = table.Column<int>(type: "int", nullable: true),
                    Not = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KullaniciKitaplikId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OkumaGunlukleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OkumaGunlukleri_KullaniciKitapliklari_KullaniciKitaplikId",
                        column: x => x.KullaniciKitaplikId,
                        principalTable: "KullaniciKitapliklari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OkumaHedefleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Yil = table.Column<int>(type: "int", nullable: false),
                    KitapSayisi = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OkumaHedefleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OkumaHedefleri_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlintiBegenileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlintiId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlintiBegenileri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlintiBegenileri_Alintilar_AlintiId",
                        column: x => x.AlintiId,
                        principalTable: "Alintilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlintiBegenileri_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlintiBegenileri_AlintiId_KullaniciId",
                table: "AlintiBegenileri",
                columns: new[] { "AlintiId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlintiBegenileri_KullaniciId",
                table: "AlintiBegenileri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Alintilar_KitapId",
                table: "Alintilar",
                column: "KitapId");

            migrationBuilder.CreateIndex(
                name: "IX_Alintilar_KullaniciId",
                table: "Alintilar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_OkumaGunlukleri_KullaniciKitaplikId",
                table: "OkumaGunlukleri",
                column: "KullaniciKitaplikId");

            migrationBuilder.CreateIndex(
                name: "IX_OkumaHedefleri_KullaniciId_Yil",
                table: "OkumaHedefleri",
                columns: new[] { "KullaniciId", "Yil" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlintiBegenileri");

            migrationBuilder.DropTable(
                name: "OkumaGunlukleri");

            migrationBuilder.DropTable(
                name: "OkumaHedefleri");

            migrationBuilder.DropTable(
                name: "Alintilar");

            migrationBuilder.DropColumn(
                name: "Spoiler",
                table: "Yorumlar");

            migrationBuilder.DropColumn(
                name: "MevcutSayfa",
                table: "KullaniciKitapliklari");

            migrationBuilder.DropColumn(
                name: "SayfaSayisi",
                table: "Kitaplar");
        }
    }
}
