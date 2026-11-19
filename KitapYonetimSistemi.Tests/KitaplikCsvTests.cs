using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Tests;

public class KitaplikCsvTests
{
    [Fact]
    public void Goodreads_Dosyasi_Okunur()
    {
        var csv = "Book Id,Title,Author,ISBN,ISBN13,My Rating,Exclusive Shelf,Date Read\n" +
                  "1,\"Suç ve Ceza\",\"Fyodor Dostoyevski\",\"=\"\"0140449132\"\"\",\"=\"\"9780140449136\"\"\",5,read,2023/04/12\n" +
                  "2,Simyacı,Paulo Coelho,\"=\"\"\"\"\",\"=\"\"\"\"\",0,to-read,\n" +
                  "3,Dune,Frank Herbert,,,0,currently-reading,\n";
        var s = KitaplikCsv.Oku(csv);

        Assert.True(s.BaslikBulundu);
        Assert.Equal(3, s.Satirlar.Count);
        Assert.Equal("Suç ve Ceza", s.Satirlar[0].Baslik);
        Assert.Equal("9780140449136", s.Satirlar[0].Isbn);
        Assert.Equal(5, s.Satirlar[0].Puan);
        Assert.Equal(OkumaDurumu.Okundu, s.Satirlar[0].Durum);
        Assert.Equal(new DateTime(2023, 4, 12), s.Satirlar[0].OkunmaTarihi);
        Assert.Null(s.Satirlar[1].Isbn);
        Assert.Null(s.Satirlar[1].Puan);
        Assert.Equal(OkumaDurumu.Okunacak, s.Satirlar[1].Durum);
        Assert.Equal(OkumaDurumu.Okuyorum, s.Satirlar[2].Durum);
    }

    [Fact]
    public void Turkce_Basliklar_Ve_Noktali_Virgul_Okunur()
    {
        var csv = "Kitap Adı;Yazar;Puan;Durum;Okunma Tarihi\r\n" +
                  "İnce Memed;Yaşar Kemal;4;Okundu;15.03.2022\r\n" +
                  "Kürk Mantolu Madonna;Sabahattin Ali;;Okumak istiyorum;\r\n";
        var s = KitaplikCsv.Oku(csv);

        Assert.Equal(2, s.Satirlar.Count);
        Assert.Equal("İnce Memed", s.Satirlar[0].Baslik);
        Assert.Equal(4, s.Satirlar[0].Puan);
        Assert.Equal(new DateTime(2022, 3, 15), s.Satirlar[0].OkunmaTarihi);
        Assert.Equal(OkumaDurumu.Okunacak, s.Satirlar[1].Durum);
    }

    [Fact]
    public void Tirnak_Icindeki_Virgul_Ve_Yeni_Satir_Korunur()
    {
        var csv = "Title,Author\n\"Bir, İki\nÜç\",\"Yazar \"\"X\"\"\"\n";
        var s = KitaplikCsv.Oku(csv);
        Assert.Single(s.Satirlar);
        Assert.Equal("Bir, İki\nÜç", s.Satirlar[0].Baslik);
        Assert.Equal("Yazar \"X\"", s.Satirlar[0].Yazar);
    }

    [Fact]
    public void Baslik_Sutunu_Yoksa_Okunamaz()
    {
        var s = KitaplikCsv.Oku("Foo,Bar\n1,2\n");
        Assert.False(s.BaslikBulundu);
        Assert.Empty(s.Satirlar);
    }

    [Fact]
    public void Bos_Basliklı_Satirlar_Atlanir_Ve_Bom_Temizlenir()
    {
        var s = KitaplikCsv.Oku("﻿Title,Author\nKitap A,X\n,Y\n");
        Assert.True(s.BaslikBulundu);
        Assert.Single(s.Satirlar);
        Assert.Equal(1, s.Atlanan);
    }

    [Theory]
    [InlineData("read", OkumaDurumu.Okundu)]
    [InlineData("to-read", OkumaDurumu.Okunacak)]
    [InlineData("currently-reading", OkumaDurumu.Okuyorum)]
    [InlineData("Okudum", OkumaDurumu.Okundu)]
    [InlineData("Okuyorum", OkumaDurumu.Okuyorum)]
    [InlineData("", OkumaDurumu.Okunacak)]
    [InlineData("favoriler", OkumaDurumu.Okunacak)]
    public void Durum_Cozulur(string raf, OkumaDurumu beklenen) =>
        Assert.Equal(beklenen, KitaplikCsv.DurumCoz(raf));

    [Theory]
    [InlineData("=\"9780140449136\"", "9780140449136")]
    [InlineData("0-14-044913-2", "0140449132")]
    [InlineData("12345", null)]
    [InlineData("", null)]
    public void Isbn_Temizlenir(string girdi, string? beklenen) =>
        Assert.Equal(beklenen, KitaplikCsv.IsbnTemizle(girdi));

    [Fact]
    public void Katla_Turkce_Harfleri_Esitler() =>
        Assert.Equal("kitap adi", KitaplikCsv.Katla("  KİTAP   Adı "));

    [Fact]
    public void Satir_Siniri_Asilirsa_Kirpilir()
    {
        var satirlar = string.Join("\n", Enumerable.Range(0, KitaplikCsv.EnFazlaSatir + 5).Select(i => $"Kitap {i}"));
        var s = KitaplikCsv.Oku("Title\n" + satirlar);
        Assert.Equal(KitaplikCsv.EnFazlaSatir, s.Satirlar.Count);
        Assert.True(s.Kirpildi);
    }
}
