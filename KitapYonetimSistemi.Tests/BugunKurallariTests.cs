using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Tests;

public class BugunKurallariTests
{
    private static readonly DateTime Baslangic = new(2026, 12, 1);
    private static SezonTakvimi Takvim => new(Baslangic, 10, 100); // 10 bölüm

    [Theory]
    [InlineData(4, "İyi geceler")]
    [InlineData(5, "Günaydın")]
    [InlineData(11, "Günaydın")]
    [InlineData(12, "İyi günler")]
    [InlineData(18, "İyi akşamlar")]
    [InlineData(22, "İyi geceler")]
    public void Selam_Saate_Gore(int saat, string beklenen) =>
        Assert.Equal(beklenen, BugunKurallari.Selam(saat));

    [Fact]
    public void Sezon_Baslamadiysa_Yaklasan()
    {
        var g = BugunKurallari.SezonGorevi(Takvim, 0, Baslangic.AddDays(-2));
        Assert.Equal(SezonGorevDurumu.Yaklasan, g.Durum);
    }

    [Fact]
    public void Sezonun_Ilk_Gunu_Birinci_Bolum_Gorevdir()
    {
        var g = BugunKurallari.SezonGorevi(Takvim, 0, Baslangic.AddHours(9));
        Assert.Equal(new SezonGorevi(SezonGorevDurumu.Gorev, 1, 1), g);
    }

    [Fact]
    public void Geride_Kalan_Okur_Siradaki_Bolumu_Ve_Kac_Bolum_Geride_Oldugunu_Gorur()
    {
        // 5. gün: 5 bölüm açık, 2'sini okumuş
        var g = BugunKurallari.SezonGorevi(Takvim, 2, Baslangic.AddDays(4));
        Assert.Equal(new SezonGorevi(SezonGorevDurumu.Gorev, 3, 3), g);
    }

    [Fact]
    public void Bugunun_Bolumunu_Okuyan_Tamamdir()
    {
        var g = BugunKurallari.SezonGorevi(Takvim, 5, Baslangic.AddDays(4));
        Assert.Equal(SezonGorevDurumu.Tamam, g.Durum);
    }

    [Fact]
    public void Tum_Bolumleri_Okuyan_Bitirmistir_Sezon_Bitse_De()
    {
        Assert.Equal(SezonGorevDurumu.Bitirdi, BugunKurallari.SezonGorevi(Takvim, 10, Baslangic.AddDays(5)).Durum);
        Assert.Equal(SezonGorevDurumu.Bitirdi, BugunKurallari.SezonGorevi(Takvim, 10, Baslangic.AddDays(50)).Durum);
    }

    private static readonly DateTime Simdi = new(2026, 12, 20, 10, 0, 0);

    [Fact]
    public void Tarihli_Kapsul_Yeni_Acildiysa_True()
    {
        Assert.True(BugunKurallari.KapsulYeniAcildi(false, Simdi.AddDays(-3), false, null, Simdi));
    }

    [Fact]
    public void Eski_Acilmis_Kapsul_Yeni_Sayilmaz()
    {
        Assert.False(BugunKurallari.KapsulYeniAcildi(false, Simdi.AddDays(-40), false, null, Simdi));
    }

    [Fact]
    public void Henuz_Acilmamis_Kapsul_Yeni_Acilmis_Degildir()
    {
        Assert.False(BugunKurallari.KapsulYeniAcildi(false, Simdi.AddDays(5), false, null, Simdi));
        Assert.False(BugunKurallari.KapsulYeniAcildi(true, null, false, null, Simdi));
    }

    [Fact]
    public void Kitap_Bitince_Acilan_Kapsul_Bitis_Tarihine_Gore_Yenidir()
    {
        Assert.True(BugunKurallari.KapsulYeniAcildi(true, null, true, Simdi.AddDays(-2), Simdi));
        Assert.False(BugunKurallari.KapsulYeniAcildi(true, null, true, Simdi.AddDays(-30), Simdi));
    }

    [Fact]
    public void Ikisi_Birlikte_Ilk_Acan_An_Sayilir()
    {
        // Kitap 2 ay önce bitti (kapsül o zaman açıldı), tarih ise 3 gün önce geldi
        Assert.False(BugunKurallari.KapsulYeniAcildi(true, Simdi.AddDays(-3), true, Simdi.AddDays(-60), Simdi));
    }

    [Fact]
    public void Kapsul_Olmayan_Not_Hic_Yeni_Acilmis_Degildir()
    {
        Assert.False(BugunKurallari.KapsulYeniAcildi(false, null, true, Simdi.AddDays(-1), Simdi));
    }
}
