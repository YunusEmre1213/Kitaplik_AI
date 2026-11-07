using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Tests;

public class OkumaRitmiTests
{
    private static readonly DateTime Simdi = new(2026, 11, 7, 20, 0, 0);

    private static OkumaRitmi Hesapla(
        IEnumerable<GunlukGirdi>? gunlukler = null,
        IEnumerable<OturumGirdi>? oturumlar = null,
        IEnumerable<DevamEdenKitap>? devam = null,
        IEnumerable<DateTime>? bitisler = null) =>
        OkumaRitmi.Hesapla(Simdi, gunlukler ?? [], oturumlar ?? [], devam ?? [], bitisler);

    private static GunlukGirdi G(int kitaplik, int gunOnce, int sayfa) =>
        new(kitaplik, Simdi.Date.AddDays(-gunOnce).AddHours(12), sayfa);

    [Fact]
    public void VeriYoksa_HerSeySifir()
    {
        var r = Hesapla();
        Assert.False(r.VeriVar);
        Assert.Equal(0, r.GuncelSeri);
        Assert.Equal(0, r.GunlukTempo);
        Assert.Empty(r.Tahminler);
        Assert.Equal(OkumaRitmi.GrafikGunSayisi, r.SonGunler.Count);
    }

    [Fact]
    public void Seri_BugunDahilArdisikGunleriSayar()
    {
        var r = Hesapla([G(1, 0, 10), G(1, 1, 5), G(1, 2, 3), G(1, 4, 1)]);
        Assert.Equal(3, r.GuncelSeri);          // bugün, dün, evvelsi gün; 3 gün önce boş
        Assert.True(r.BugunOkundu);
    }

    [Fact]
    public void Seri_BugunHenuzOkunmadiysaDundenSayilir()
    {
        var r = Hesapla([G(1, 1, 10), G(1, 2, 20)]);
        Assert.Equal(2, r.GuncelSeri);
        Assert.False(r.BugunOkundu);
    }

    [Fact]
    public void Seri_DunVeBugunYoksaSifirlanir()
    {
        var r = Hesapla([G(1, 3, 10), G(1, 4, 20)]);
        Assert.Equal(0, r.GuncelSeri);
        Assert.Equal(2, r.EnUzunSeri);
    }

    [Fact]
    public void GunlukSayfa_ArdisikGirdilerArasindakiArtistir()
    {
        // 0 -> 30 (2 gün önce), 30 -> 55 (dün): 30 ve 25 sayfa
        var r = Hesapla([G(1, 2, 30), G(1, 1, 55)]);
        Assert.Equal(25, r.SonGunler[^2].Sayfa);
        Assert.Equal(30, r.SonGunler[^3].Sayfa);
        Assert.Equal(55, r.SonGunlerToplamSayfa);
    }

    [Fact]
    public void GunlukSayfa_GeriyeDonenSayfaNegatifSayilmaz()
    {
        var r = Hesapla([G(1, 2, 50), G(1, 1, 40)]);
        Assert.Equal(50, r.SonGunlerToplamSayfa);
    }

    [Fact]
    public void GunlukSayfa_KitaplarAyriAyriHesaplanir()
    {
        var r = Hesapla([G(1, 1, 40), G(2, 1, 60)]);
        Assert.Equal(100, r.SonGunler[^2].Sayfa);
    }

    [Fact]
    public void Tahmin_SonOnDortGunTemposunaGoreHesaplanir()
    {
        // 14 günde 140 sayfa => 10 sayfa/gün. Kalan 100 sayfa => 10 gün
        var r = Hesapla([G(1, 5, 140)], devam: [new DevamEdenKitap(1, "Kitap", 140, 240)]);
        Assert.Equal(10, r.GunlukTempo);
        var t = Assert.Single(r.Tahminler);
        Assert.Equal(10, t.GunSayisi);
        Assert.Equal(Simdi.Date.AddDays(10), t.Tarih);
    }

    [Fact]
    public void Tahmin_TempoYoksaVeyaSayfaSayisiBilinmiyorsaYoktur()
    {
        Assert.Empty(Hesapla(devam: [new DevamEdenKitap(1, "K", 10, 100)]).Tahminler);
        Assert.Empty(Hesapla([G(1, 1, 20)], devam: [new DevamEdenKitap(1, "K", 20, null)]).Tahminler);
        Assert.Empty(Hesapla([G(1, 1, 100)], devam: [new DevamEdenKitap(1, "K", 100, 100)]).Tahminler);
    }

    [Fact]
    public void Oturum_BirDakikadanKisaSayilmaz_SureToplanir()
    {
        var bugun = Simdi.Date.AddHours(9);
        var r = Hesapla(oturumlar: [new OturumGirdi(bugun, 0.4), new OturumGirdi(bugun, 30), new OturumGirdi(bugun.AddDays(-10), 20)]);
        Assert.Equal(50, r.ToplamDakika);
        Assert.Equal(30, r.BuHaftaDakika);      // 7 Kasım 2026 Cumartesi, hafta Pazartesi 2 Kasım'da başlar
        Assert.True(r.BugunOkundu);
    }

    [Fact]
    public void EnVerimliZaman_AzindanUcOturumGerekir()
    {
        var sabah = Simdi.Date.AddHours(7);
        Assert.Null(OkumaRitmi.EnVerimliZamanDilimi([new OturumGirdi(sabah, 30), new OturumGirdi(sabah, 30)]));
        Assert.Equal("sabah", OkumaRitmi.EnVerimliZamanDilimi(
            [new OturumGirdi(sabah, 30), new OturumGirdi(sabah.AddDays(-1), 30), new OturumGirdi(Simdi.Date.AddHours(21), 20)]));
    }

    [Fact]
    public void KitapBitirmeGunuDeEtkinliktir()
    {
        var r = Hesapla(bitisler: [Simdi.Date.AddDays(-1)]);
        Assert.Equal(1, r.GuncelSeri);
    }
}

public class IsiHaritasiTests
{
    [Fact]
    public void IsaretYoksa_BosHaritaDoner()
    {
        var isi = IsiHaritasi.Olustur([], 300);
        Assert.Equal(0, isi.ToplamIsaret);
        Assert.Empty(isi.Dilimler);
    }

    [Fact]
    public void AyniSayfaCokIsaretlenirse_EnYuksekSeviyeVeEnCokListesi()
    {
        var isi = IsiHaritasi.Olustur([12, 12, 12, 88, 150], 400);
        Assert.Equal(5, isi.ToplamIsaret);
        Assert.Equal((12, 3), isi.EnCokIsaretlenen[0]);
        Assert.Equal(4, isi.Dilimler.Max(d => d.Seviye));
        Assert.Equal(3, isi.Dilimler.Count(d => d.Seviye > 0));
    }

    [Fact]
    public void KitapSayfaSayisiBilinmiyorsa_EnYuksekIsaretliSayfayaKadarCizilir()
    {
        var isi = IsiHaritasi.Olustur([10, 55], null);
        Assert.Equal(55, isi.ToplamSayfa);
    }

    [Fact]
    public void DilimSayisiSinirlidir_VeHerSayfaBirDilimeDuser()
    {
        var isi = IsiHaritasi.Olustur([1, 1000], 1000);
        Assert.Equal(IsiHaritasi.EnFazlaDilim, isi.Dilimler.Count);
        Assert.Equal(1, isi.Dilimler[0].Sayi);
        Assert.Equal(1, isi.Dilimler[^1].Sayi);
        Assert.Equal(1000, isi.Dilimler[^1].Son);
    }

    [Fact]
    public void GecersizSayfalarYoksayilir()
    {
        var isi = IsiHaritasi.Olustur([0, -4, 7], 100);
        Assert.Equal(1, isi.ToplamIsaret);
    }
}
