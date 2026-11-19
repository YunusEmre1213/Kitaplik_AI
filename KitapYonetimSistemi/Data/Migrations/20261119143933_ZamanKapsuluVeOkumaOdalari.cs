using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class ZamanKapsuluVeOkumaOdalari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "KapsulAcilisTarihi",
                table: "KenarNotlari",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "KapsulBitinceAc",
                table: "KenarNotlari",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "OkumaOdalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KitapId = table.Column<int>(type: "int", nullable: false),
                    OlusturanId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OkumaOdalari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OkumaOdalari_AspNetUsers_OlusturanId",
                        column: x => x.OlusturanId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OkumaOdalari_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OdaMesajlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayfaNo = table.Column<int>(type: "int", nullable: false),
                    Metin = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OdaId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdaMesajlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdaMesajlari_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OdaMesajlari_OkumaOdalari_OdaId",
                        column: x => x.OdaId,
                        principalTable: "OkumaOdalari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OdaUyeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KatilmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OdaId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdaUyeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdaUyeleri_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OdaUyeleri_OkumaOdalari_OdaId",
                        column: x => x.OdaId,
                        principalTable: "OkumaOdalari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OdaMesajlari_KullaniciId",
                table: "OdaMesajlari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_OdaMesajlari_OdaId_Tarih",
                table: "OdaMesajlari",
                columns: new[] { "OdaId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_OdaUyeleri_KullaniciId",
                table: "OdaUyeleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_OdaUyeleri_OdaId_KullaniciId",
                table: "OdaUyeleri",
                columns: new[] { "OdaId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OkumaOdalari_KitapId",
                table: "OkumaOdalari",
                column: "KitapId");

            migrationBuilder.CreateIndex(
                name: "IX_OkumaOdalari_Kod",
                table: "OkumaOdalari",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OkumaOdalari_OlusturanId",
                table: "OkumaOdalari",
                column: "OlusturanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OdaMesajlari");

            migrationBuilder.DropTable(
                name: "OdaUyeleri");

            migrationBuilder.DropTable(
                name: "OkumaOdalari");

            migrationBuilder.DropColumn(
                name: "KapsulAcilisTarihi",
                table: "KenarNotlari");

            migrationBuilder.DropColumn(
                name: "KapsulBitinceAc",
                table: "KenarNotlari");
        }
    }
}
