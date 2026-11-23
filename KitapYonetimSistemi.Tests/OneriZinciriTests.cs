using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Tests;

public class OneriZinciriTests
{
    // Ayşe (kaynak) -> öneri 1 -> Mehmet(kabul 10) ve Can(kabul 11)
    // Mehmet -> öneri 2 (önceki: kabul 10) -> Deniz(kabul 12) -> öneri 3 (önceki: kabul 12) -> Ela(kabul 13)
    private static readonly List<OneriKaydi> Oneriler = new()
    {
        new(1, "ayse", null),
        new(2, "mehmet", 10),
        new(3, "deniz", 12)
    };

    private static readonly List<KabulKaydi> Kabuller = new()
    {
        new(10, 1, "mehmet", true),
        new(11, 1, "can", false),
        new(12, 2, "deniz", true),
        new(13, 3, "ela", false)
    };

    [Fact]
    public void Agac_Kabul_Edenleri_Ve_Torunlari_Bulur()
    {
        var agac = OneriZinciri.Agac(1, Oneriler, Kabuller);
        Assert.Equal(new[] { "mehmet", "can" }, agac.Select(d => d.KullaniciId));
        var mehmet = agac[0];
        Assert.Equal("deniz", Assert.Single(mehmet.Cocuklar).KullaniciId);
        Assert.Equal("ela", Assert.Single(mehmet.Cocuklar[0].Cocuklar).KullaniciId);
    }

    [Fact]
    public void Ozet_Ulasan_Bitiren_Ve_Derinligi_Sayar()
    {
        var ozet = OneriZinciri.Ozet(OneriZinciri.Agac(1, Oneriler, Kabuller));
        Assert.Equal(4, ozet.Ulasan);
        Assert.Equal(2, ozet.Bitiren);
        Assert.Equal(3, ozet.Derinlik);
    }

    [Fact]
    public void Alt_Zincir_Yalnizca_Kendi_Onerisinin_Altini_Gosterir()
    {
        var ozet = OneriZinciri.Ozet(OneriZinciri.Agac(2, Oneriler, Kabuller));
        Assert.Equal(2, ozet.Ulasan);   // deniz + ela
        Assert.Equal(2, ozet.Derinlik);
    }

    [Fact]
    public void Kabulsuz_Oneri_Bos_Agac_Verir()
    {
        var agac = OneriZinciri.Agac(1, Oneriler, new List<KabulKaydi>());
        Assert.Empty(agac);
        Assert.Equal(new OneriOzeti(0, 0, 0), OneriZinciri.Ozet(agac));
    }

    [Fact]
    public void Yol_Kaynaktan_Gondereneye_Siralanir()
    {
        Assert.Equal(new string?[] { "ayse", "mehmet", "deniz" }, OneriZinciri.Yol(3, Oneriler, Kabuller));
        Assert.Equal(new string?[] { "ayse" }, OneriZinciri.Yol(1, Oneriler, Kabuller));
    }

    [Fact]
    public void Silinmis_Gonderen_Yolda_Bos_Gorunur()
    {
        var oneriler = new List<OneriKaydi> { new(1, null, null), new(2, "mehmet", 10) };
        var kabuller = new List<KabulKaydi> { new(10, 1, "mehmet", false) };
        Assert.Equal(new string?[] { null, "mehmet" }, OneriZinciri.Yol(2, oneriler, kabuller));
    }

    [Fact]
    public void Dongu_Sonsuz_Dongu_Yaratmaz()
    {
        // Bozuk veri: öneri 1'in önceki kabulü, kendi altındaki kabul
        var oneriler = new List<OneriKaydi> { new(1, "a", 10) };
        var kabuller = new List<KabulKaydi> { new(10, 1, "b", false) };
        Assert.Single(OneriZinciri.Yol(1, oneriler, kabuller));
        var agac = OneriZinciri.Agac(1, oneriler, kabuller);
        Assert.Equal(1, OneriZinciri.Ozet(agac).Ulasan);
    }
}
