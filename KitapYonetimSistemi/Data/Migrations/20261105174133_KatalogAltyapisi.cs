using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapYonetimSistemi.Data.Migrations
{
    /// <inheritdoc />
    public partial class KatalogAltyapisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "YazarAdi",
                table: "Kitaplar",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Tur",
                table: "Kitaplar",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "KitapAdi",
                table: "Kitaplar",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Dil",
                table: "Kitaplar",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Isbn10",
                table: "Kitaplar",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Isbn13",
                table: "Kitaplar",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenLibraryEditionKey",
                table: "Kitaplar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenLibraryWorkKey",
                table: "Kitaplar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YayinTarihi",
                table: "Kitaplar",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YayineviId",
                table: "Kitaplar",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportLoglari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kaynak = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Surum = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Baslangic = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Bitis = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Durum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SonSatir = table.Column<long>(type: "bigint", nullable: false),
                    OkunanSatir = table.Column<long>(type: "bigint", nullable: false),
                    KabulEdilen = table.Column<long>(type: "bigint", nullable: false),
                    Atlanan = table.Column<long>(type: "bigint", nullable: false),
                    HataSayisi = table.Column<long>(type: "bigint", nullable: false),
                    Mesaj = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportLoglari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kategoriler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategoriler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Yayinevleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Yayinevleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Yazarlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    OpenLibraryKey = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Yazarlar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KitapKategorileri",
                columns: table => new
                {
                    KitapId = table.Column<int>(type: "int", nullable: false),
                    KategoriId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitapKategorileri", x => new { x.KitapId, x.KategoriId });
                    table.ForeignKey(
                        name: "FK_KitapKategorileri_Kategoriler_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KitapKategorileri_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KitapYazarlari",
                columns: table => new
                {
                    KitapId = table.Column<int>(type: "int", nullable: false),
                    YazarId = table.Column<int>(type: "int", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitapYazarlari", x => new { x.KitapId, x.YazarId });
                    table.ForeignKey(
                        name: "FK_KitapYazarlari_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KitapYazarlari_Yazarlar_YazarId",
                        column: x => x.YazarId,
                        principalTable: "Yazarlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_Dil",
                table: "Kitaplar",
                column: "Dil");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_EklenmeTarihi",
                table: "Kitaplar",
                column: "EklenmeTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_Isbn10",
                table: "Kitaplar",
                column: "Isbn10");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_Isbn13",
                table: "Kitaplar",
                column: "Isbn13");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_KitapAdi",
                table: "Kitaplar",
                column: "KitapAdi");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_OpenLibraryEditionKey",
                table: "Kitaplar",
                column: "OpenLibraryEditionKey",
                unique: true,
                filter: "[OpenLibraryEditionKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_OpenLibraryWorkKey",
                table: "Kitaplar",
                column: "OpenLibraryWorkKey");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_Tur",
                table: "Kitaplar",
                column: "Tur");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_YayineviId",
                table: "Kitaplar",
                column: "YayineviId");

            migrationBuilder.CreateIndex(
                name: "IX_Kitaplar_YazarAdi",
                table: "Kitaplar",
                column: "YazarAdi");

            migrationBuilder.CreateIndex(
                name: "IX_ImportLoglari_Kaynak_Surum",
                table: "ImportLoglari",
                columns: new[] { "Kaynak", "Surum" });

            migrationBuilder.CreateIndex(
                name: "IX_Kategoriler_Ad",
                table: "Kategoriler",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitapKategorileri_KategoriId",
                table: "KitapKategorileri",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_KitapYazarlari_YazarId",
                table: "KitapYazarlari",
                column: "YazarId");

            migrationBuilder.CreateIndex(
                name: "IX_Yayinevleri_Ad",
                table: "Yayinevleri",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Yazarlar_Ad",
                table: "Yazarlar",
                column: "Ad");

            migrationBuilder.CreateIndex(
                name: "IX_Yazarlar_OpenLibraryKey",
                table: "Yazarlar",
                column: "OpenLibraryKey",
                unique: true,
                filter: "[OpenLibraryKey] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Kitaplar_Yayinevleri_YayineviId",
                table: "Kitaplar",
                column: "YayineviId",
                principalTable: "Yayinevleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kitaplar_Yayinevleri_YayineviId",
                table: "Kitaplar");

            migrationBuilder.DropTable(
                name: "ImportLoglari");

            migrationBuilder.DropTable(
                name: "KitapKategorileri");

            migrationBuilder.DropTable(
                name: "KitapYazarlari");

            migrationBuilder.DropTable(
                name: "Yayinevleri");

            migrationBuilder.DropTable(
                name: "Kategoriler");

            migrationBuilder.DropTable(
                name: "Yazarlar");

            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_Dil",
                table: "Kitaplar");

            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_EklenmeTarihi",
                table: "Kitaplar");

            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_Isbn10",
                table: "Kitaplar");

            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_Isbn13",
                table: "Kitaplar");

            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_KitapAdi",
                table: "Kitaplar");

            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_OpenLibraryEditionKey",
                table: "Kitaplar");

            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_OpenLibraryWorkKey",
                table: "Kitaplar");

            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_Tur",
                table: "Kitaplar");

            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_YayineviId",
                table: "Kitaplar");

            migrationBuilder.DropIndex(
                name: "IX_Kitaplar_YazarAdi",
                table: "Kitaplar");

            migrationBuilder.DropColumn(
                name: "Dil",
                table: "Kitaplar");

            migrationBuilder.DropColumn(
                name: "Isbn10",
                table: "Kitaplar");

            migrationBuilder.DropColumn(
                name: "Isbn13",
                table: "Kitaplar");

            migrationBuilder.DropColumn(
                name: "OpenLibraryEditionKey",
                table: "Kitaplar");

            migrationBuilder.DropColumn(
                name: "OpenLibraryWorkKey",
                table: "Kitaplar");

            migrationBuilder.DropColumn(
                name: "YayinTarihi",
                table: "Kitaplar");

            migrationBuilder.DropColumn(
                name: "YayineviId",
                table: "Kitaplar");

            migrationBuilder.AlterColumn<string>(
                name: "YazarAdi",
                table: "Kitaplar",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "Tur",
                table: "Kitaplar",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "KitapAdi",
                table: "Kitaplar",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);
        }
    }
}
