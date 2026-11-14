using KitapYonetimSistemi.Models;
using Xunit;

namespace KitapYonetimSistemi.Tests;

public class BirakmaIstatistigiTests
{
    private static readonly DateTime Simdi = new(2026, 11, 14);
    private static OkurDurumu Birakmis(int sayfa) => new(sayfa, false, Simdi.AddDays(-60));
    private static OkurDurumu Okuyan(int sayfa) => new(sayfa, false, Simdi.AddDays(-3));
    private static OkurDurumu Bitiren() => new(300, true, Simdi.AddDays(-10));

    [Fact]
    public void ElliGunOncekiBitmemisOkurBirakmisSayilir()
    {
        var s = BirakmaIstatistigi.Hesapla(new[] { Birakmis(50), Okuyan(40), Bitiren() }, 300, Simdi);
        Assert.Equal(1, s.Birakan);
        Assert.Equal(1, s.DevamEden);
        Assert.Equal(1, s.Bitiren);
    }

    [Fact]
    public void MedyanVeYuzdeDogruHesaplanir()
    {
        var okurlar = new[] { Birakmis(30), Birakmis(60), Birakmis(90), Birakmis(120), Bitiren(), Bitiren() };
        var s = BirakmaIstatistigi.Hesapla(okurlar, 300, Simdi);
        Assert.Equal(60, s.MedyanSayfa);
        Assert.Equal(20, s.MedyanYuzde);
        // Medyanı (60) geçenler: 2 bitiren + 3 bırakan (60, 90, 120) -> 2/5
        Assert.Equal(40, s.AsanlarinBitirmeYuzdesi);
        Assert.Equal(33, s.BitirmeYuzdesi);
    }

    [Fact]
    public void AzVeriGosterilmez()
    {
        var s = BirakmaIstatistigi.Hesapla(new[] { Birakmis(30), Bitiren() }, 300, Simdi);
        Assert.False(s.Yeterli);
    }

    [Fact]
    public void BirakanYoksaGosterilmez()
    {
        var s = BirakmaIstatistigi.Hesapla(Enumerable.Range(0, 6).Select(_ => Bitiren()), 300, Simdi);
        Assert.False(s.Yeterli);
    }

    [Fact]
    public void SayfaSayisiBilinmiyorsaYuzdeYoktur()
    {
        var s = BirakmaIstatistigi.Hesapla(new[] { Birakmis(30) }, null, Simdi);
        Assert.Null(s.MedyanYuzde);
        Assert.Equal(30, s.MedyanSayfa);
    }

    [Fact]
    public void SayfasiOlmayanBitmemisKayitYokSayilir()
    {
        var s = BirakmaIstatistigi.Hesapla(new[] { new OkurDurumu(0, false, Simdi.AddDays(-90)) }, 300, Simdi);
        Assert.Equal(0, s.Birakan);
    }
}
