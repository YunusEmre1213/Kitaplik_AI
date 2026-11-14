using KitapYonetimSistemi.Models;
using Xunit;

namespace KitapYonetimSistemi.Tests;

public class TepkiHaritasiTests
{
    private static readonly (int, TepkiTuru)[] Ornek =
    {
        (10, TepkiTuru.Saskin), (10, TepkiTuru.Saskin), (10, TepkiTuru.Guldurdu),
        (90, TepkiTuru.Aglatti), (95, TepkiTuru.Aglatti)
    };

    [Fact]
    public void IlerlemesiOlmayanHerSeyiKilitliGorur()
    {
        var h = TepkiHaritasi.Olustur(Ornek, 100, 0);
        Assert.All(h.Dilimler, d => Assert.True(d.Kilitli));
        Assert.Equal(0, h.ToplamTepki);
    }

    [Fact]
    public void UlasilmayanSayfalarinTepkisiSizmaz()
    {
        var h = TepkiHaritasi.Olustur(Ornek, 100, 50);
        Assert.Equal(3, h.ToplamTepki);
        Assert.DoesNotContain(h.Dilimler.Where(d => d.Bas > 50), d => !d.Kilitli);
        Assert.DoesNotContain(h.Dilimler, d => d.Baskin == TepkiTuru.Aglatti);
    }

    [Fact]
    public void BitirenHepsiniGorurVeBaskinTurDogrudur()
    {
        var h = TepkiHaritasi.Olustur(Ornek, 100, int.MaxValue);
        Assert.Equal(5, h.ToplamTepki);
        Assert.Equal(TepkiTuru.Saskin, h.Dilimler.First(d => d.Bas <= 10 && d.Son >= 10).Baskin);
        Assert.Equal(TepkiTuru.Aglatti, h.Dilimler.First(d => d.Bas <= 90 && d.Son >= 90).Baskin);
    }

    [Fact]
    public void SayfaSayisiBilinmiyorsaEnYuksekTepkiyeKadarCizilir()
    {
        var h = TepkiHaritasi.Olustur(Ornek, null, int.MaxValue);
        Assert.Equal(95, h.ToplamSayfa);
    }

    [Fact]
    public void TepkiYoksaBosHaritaDoner()
    {
        var h = TepkiHaritasi.Olustur(Array.Empty<(int, TepkiTuru)>(), null, 10);
        Assert.Empty(h.Dilimler);
    }
}
