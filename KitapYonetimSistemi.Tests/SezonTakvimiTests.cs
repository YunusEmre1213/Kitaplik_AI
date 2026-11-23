using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Tests;

public class SezonTakvimiTests
{
    private static readonly DateTime Baslangic = new(2026, 12, 1);

    [Fact]
    public void Gunluk_Sayfa_Yukari_Yuvarlanir_Ve_Bolum_Sayisi_Bulunur()
    {
        var t = new SezonTakvimi(Baslangic, 21, 300);
        Assert.Equal(15, t.GunlukSayfa);
        Assert.Equal(20, t.BolumSayisi);
    }

    [Fact]
    public void Bolum_Sayfa_Araliklari_Kitabi_Eksiksiz_Kapsar()
    {
        var t = new SezonTakvimi(Baslangic, 7, 100);
        Assert.Equal(15, t.GunlukSayfa);
        Assert.Equal(7, t.BolumSayisi);
        Assert.Equal(1, t.SayfaBasi(1));
        Assert.Equal(15, t.SayfaSonu(1));
        Assert.Equal(16, t.SayfaBasi(2));
        Assert.Equal(91, t.SayfaBasi(7));
        Assert.Equal(100, t.SayfaSonu(7));
    }

    [Fact]
    public void Kisa_Kitap_Gun_Sayisindan_Az_Bolum_Uretir()
    {
        var t = new SezonTakvimi(Baslangic, 30, 100);
        Assert.Equal(4, t.GunlukSayfa);
        Assert.Equal(25, t.BolumSayisi);
        Assert.Equal(100, t.SayfaSonu(t.BolumSayisi));
    }

    [Fact]
    public void Bugun_Bolum_Gune_Gore_Artar_Ve_Sinirlanir()
    {
        var t = new SezonTakvimi(Baslangic, 10, 100);
        Assert.Equal(0, t.BugunBolum(Baslangic.AddDays(-1)));
        Assert.Equal(1, t.BugunBolum(Baslangic.AddHours(23)));
        Assert.Equal(4, t.BugunBolum(Baslangic.AddDays(3)));
        Assert.Equal(10, t.BugunBolum(Baslangic.AddDays(40)));
    }

    [Fact]
    public void Durum_Yaklasan_Surmekte_Bitti()
    {
        var t = new SezonTakvimi(Baslangic, 10, 100);
        Assert.Equal(SezonDurumu.Yaklasan, t.Durum(Baslangic.AddDays(-1)));
        Assert.Equal(SezonDurumu.Surmekte, t.Durum(Baslangic));
        Assert.Equal(SezonDurumu.Surmekte, t.Durum(Baslangic.AddDays(9)));
        Assert.Equal(SezonDurumu.Bitti, t.Durum(Baslangic.AddDays(10)));
    }

    [Fact]
    public void Onay_Yalnizca_Sirali_Ve_Acilmis_Bolum_Icin_Mumkun()
    {
        var t = new SezonTakvimi(Baslangic, 10, 100);
        var ucuncuGun = Baslangic.AddDays(2); // 3. bölüm açık
        Assert.True(t.OnaylanabilirMi(0, ucuncuGun));
        Assert.True(t.OnaylanabilirMi(2, ucuncuGun));
        Assert.False(t.OnaylanabilirMi(3, ucuncuGun));   // 4. bölüm henüz açılmadı
        Assert.False(t.OnaylanabilirMi(0, Baslangic.AddDays(-1)));
    }

    [Fact]
    public void Sohbet_Yalnizca_Okunan_Bolumler_Icin_Acik()
    {
        Assert.True(SezonTakvimi.SohbetAcik(3, 3, false));
        Assert.True(SezonTakvimi.SohbetAcik(1, 3, false));
        Assert.False(SezonTakvimi.SohbetAcik(4, 3, false));
        Assert.False(SezonTakvimi.SohbetAcik(1, 0, false));
        Assert.True(SezonTakvimi.SohbetAcik(5, 0, true)); // kendi mesajı
    }

    [Fact]
    public void Yazma_Yalnizca_Okunan_Bolumlere_Izinli()
    {
        var t = new SezonTakvimi(Baslangic, 10, 100);
        Assert.True(t.YazabilirMi(2, 2));
        Assert.False(t.YazabilirMi(3, 2));
        Assert.False(t.YazabilirMi(0, 5));
        Assert.False(t.YazabilirMi(11, 20));
    }
}
