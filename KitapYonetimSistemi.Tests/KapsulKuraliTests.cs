using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Tests;

public class KapsulKuraliTests
{
    private static readonly DateTime Simdi = new(2026, 11, 19, 12, 0, 0);

    [Fact]
    public void Kapsul_Olmayan_Not_Her_Zaman_Acik() =>
        Assert.True(KapsulKurali.Acik(false, null, false, Simdi));

    [Fact]
    public void Bitince_Ac_Kitap_Bitmeden_Kilitli() =>
        Assert.False(KapsulKurali.Acik(true, null, false, Simdi));

    [Fact]
    public void Bitince_Ac_Kitap_Bitince_Acilir() =>
        Assert.True(KapsulKurali.Acik(true, null, true, Simdi));

    [Fact]
    public void Tarihli_Kapsul_Tarih_Gelmeden_Kilitli_Gelince_Acik()
    {
        var acilis = Simdi.AddDays(10);
        Assert.False(KapsulKurali.Acik(false, acilis, true, Simdi));
        Assert.True(KapsulKurali.Acik(false, acilis, false, acilis));
    }

    [Fact]
    public void Ikisi_Birlikte_Hangisi_Once_Gelirse()
    {
        var acilis = Simdi.AddMonths(6);
        Assert.True(KapsulKurali.Acik(true, acilis, true, Simdi));
        Assert.True(KapsulKurali.Acik(true, acilis, false, acilis.AddDays(1)));
        Assert.False(KapsulKurali.Acik(true, acilis, false, Simdi));
    }

    [Theory]
    [InlineData("bitince", true, false)]
    [InlineData("6ay", false, true)]
    [InlineData("1yil", false, true)]
    [InlineData("yok", false, false)]
    [InlineData("rastgele", false, false)]
    [InlineData(null, false, false)]
    public void Coz_Form_Secimini_Cevirir(string? secim, bool bitinceAc, bool tarihVar)
    {
        var (b, a) = KapsulKurali.Coz(secim, Simdi);
        Assert.Equal(bitinceAc, b);
        Assert.Equal(tarihVar, a.HasValue);
    }

    [Fact]
    public void Coz_Tarihleri_Dogru_Hesaplar()
    {
        Assert.Equal(Simdi.AddMonths(6), KapsulKurali.Coz("6ay", Simdi).Acilis);
        Assert.Equal(Simdi.AddYears(1), KapsulKurali.Coz("1yil", Simdi).Acilis);
    }
}
