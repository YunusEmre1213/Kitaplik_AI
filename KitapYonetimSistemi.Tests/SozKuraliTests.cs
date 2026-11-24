using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Tests;

public class SozKuraliTests
{
    private static readonly DateTime Bitis = new(2026, 12, 20);

    [Fact]
    public void Surerken_Bitmediyse_Surmekte()
    {
        Assert.Equal(SozDurumu.Surmekte, SozKurali.Durum(Bitis, false, null, new DateTime(2026, 12, 10)));
        // Bitiş günü akşamı hâlâ süre var
        Assert.Equal(SozDurumu.Surmekte, SozKurali.Durum(Bitis, false, null, new DateTime(2026, 12, 20, 23, 30, 0)));
    }

    [Fact]
    public void Sure_Dolunca_Bitmemisse_SuresiDoldu()
    {
        Assert.Equal(SozDurumu.SuresiDoldu, SozKurali.Durum(Bitis, false, null, new DateTime(2026, 12, 21)));
    }

    [Fact]
    public void Zamaninda_Bitiren_Sozunu_Tutmustur()
    {
        Assert.Equal(SozDurumu.Tuttu, SozKurali.Durum(Bitis, true, new DateTime(2026, 12, 20, 22, 0, 0), new DateTime(2026, 12, 25)));
        Assert.Equal(SozDurumu.Tuttu, SozKurali.Durum(Bitis, true, new DateTime(2026, 12, 5), new DateTime(2026, 12, 6)));
    }

    [Fact]
    public void Gec_Bitiren_GecTuttu_Sayilir()
    {
        Assert.Equal(SozDurumu.GecTuttu, SozKurali.Durum(Bitis, true, new DateTime(2026, 12, 22), new DateTime(2026, 12, 25)));
    }

    [Fact]
    public void Kalan_Gun_Bugun_Son_Gunse_Sifir()
    {
        Assert.Equal(0, SozKurali.KalanGun(Bitis, new DateTime(2026, 12, 20, 9, 0, 0)));
        Assert.Equal(5, SozKurali.KalanGun(Bitis, new DateTime(2026, 12, 15)));
        Assert.Equal(0, SozKurali.KalanGun(Bitis, new DateTime(2026, 12, 25)));
    }

    [Fact]
    public void Gunluk_Gereken_Sayfa_Bugun_Dahil_Hesaplanir()
    {
        // 300 sayfa, 100'ündeyim, bitişe 4 gün var (bugün dahil 5 gün): 200/5 = 40
        Assert.Equal(40, SozKurali.GunlukGereken(100, 300, Bitis, new DateTime(2026, 12, 16)));
        // Son gün: kalan hepsi bugün
        Assert.Equal(200, SozKurali.GunlukGereken(100, 300, Bitis, new DateTime(2026, 12, 20)));
        // Yukarı yuvarlar
        Assert.Equal(34, SozKurali.GunlukGereken(0, 100, Bitis, new DateTime(2026, 12, 18)));
    }

    [Fact]
    public void Gunluk_Gereken_Bilinmeyen_Ya_Da_Biten_Kitapta_Sifir()
    {
        Assert.Equal(0, SozKurali.GunlukGereken(50, null, Bitis, new DateTime(2026, 12, 16)));
        Assert.Equal(0, SozKurali.GunlukGereken(300, 300, Bitis, new DateTime(2026, 12, 16)));
    }

    [Fact]
    public void Yuzde_Sinirlanir()
    {
        Assert.Equal(33, SozKurali.Yuzde(100, 300));
        Assert.Equal(100, SozKurali.Yuzde(400, 300));
        Assert.Equal(0, SozKurali.Yuzde(50, null));
    }

    [Fact]
    public void Soz_Tarihi_Yarindan_Itibaren_Ve_Sinirli()
    {
        var simdi = new DateTime(2026, 12, 1, 10, 0, 0);
        Assert.False(SozKurali.TarihGecerliMi(new DateTime(2026, 12, 1), simdi));
        Assert.True(SozKurali.TarihGecerliMi(new DateTime(2026, 12, 2), simdi));
        Assert.True(SozKurali.TarihGecerliMi(simdi.Date.AddDays(SozKurali.EnFazlaGun), simdi));
        Assert.False(SozKurali.TarihGecerliMi(simdi.Date.AddDays(SozKurali.EnFazlaGun + 1), simdi));
    }

    [Fact]
    public void Uzatma_Bir_Kez_Ve_Otuz_Gune_Kadar()
    {
        var simdi = new DateTime(2026, 12, 20, 10, 0, 0);
        Assert.True(SozKurali.UzatmaGecerliMi(Bitis, Bitis.AddDays(7), 0, simdi));
        Assert.False(SozKurali.UzatmaGecerliMi(Bitis, Bitis.AddDays(7), 1, simdi));      // hak bitti
        Assert.False(SozKurali.UzatmaGecerliMi(Bitis, Bitis.AddDays(31), 0, simdi));     // en fazla 30 gün
        Assert.False(SozKurali.UzatmaGecerliMi(Bitis, Bitis.AddDays(-1), 0, simdi));     // kısaltma değil
        Assert.False(SozKurali.UzatmaGecerliMi(Bitis, Bitis.AddDays(2), 0, new DateTime(2027, 1, 5))); // geçmişe uzatma
    }

    [Fact]
    public void Durtme_Gunde_Bir_Kez()
    {
        var simdi = new DateTime(2026, 12, 10, 12, 0, 0);
        Assert.True(SozKurali.DurtebilirMi(null, simdi));
        Assert.False(SozKurali.DurtebilirMi(simdi.AddHours(-23), simdi));
        Assert.True(SozKurali.DurtebilirMi(simdi.AddHours(-24), simdi));
    }

    [Fact]
    public void Tebrik_Yalniz_Tutulan_Soze_Digerleri_Yalniz_Acik_Soze()
    {
        Assert.True(SozKurali.DurtmeUygunMu(DurtmeTuru.Tebrikler, SozDurumu.Tuttu));
        Assert.True(SozKurali.DurtmeUygunMu(DurtmeTuru.Tebrikler, SozDurumu.GecTuttu));
        Assert.False(SozKurali.DurtmeUygunMu(DurtmeTuru.Tebrikler, SozDurumu.Surmekte));
        Assert.True(SozKurali.DurtmeUygunMu(DurtmeTuru.NasilGidiyor, SozDurumu.Surmekte));
        Assert.True(SozKurali.DurtmeUygunMu(DurtmeTuru.SanaGuveniyorum, SozDurumu.SuresiDoldu));
        Assert.False(SozKurali.DurtmeUygunMu(DurtmeTuru.NasilGidiyor, SozDurumu.Tuttu));
    }
}
