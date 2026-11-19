using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Tests;

public class RuhHaliHaritasiTests
{
    private static readonly DateTime Gece = new(2026, 11, 1, 23, 30, 0);
    private static readonly DateTime Sabah = new(2026, 11, 2, 8, 0, 0);

    private static RuhHaliOturumu O(DateTime t, OkumaRuhHali? basi, OkumaRuhHali? sonu, string? tur = null) => new(t, basi, sonu, tur);

    [Fact]
    public void Az_Veri_Yeterli_Sayilmaz()
    {
        var h = RuhHaliHaritasi.Hesapla(new[] { O(Gece, OkumaRuhHali.Yorgun, null) });
        Assert.False(h.Yeterli);
        Assert.Equal(1, h.ToplamOturum);
    }

    [Fact]
    public void Ruh_Hali_Girilmeyen_Oturumlar_Sayilmaz()
    {
        var h = RuhHaliHaritasi.Hesapla(new[] { O(Gece, null, null), O(Gece, OkumaRuhHali.Mutlu, null) });
        Assert.Equal(1, h.ToplamOturum);
    }

    [Fact]
    public void Iyilesen_Yuzde_Hesaplanir()
    {
        var oturumlar = new[]
        {
            O(Gece, OkumaRuhHali.Yorgun, OkumaRuhHali.Huzurlu),   // iyileşti
            O(Gece, OkumaRuhHali.Gergin, OkumaRuhHali.Mutlu),     // iyileşti
            O(Gece, OkumaRuhHali.Huzurlu, OkumaRuhHali.Huzurlu),  // aynı
            O(Gece, OkumaRuhHali.Mutlu, OkumaRuhHali.Yorgun),     // kötüleşti
            O(Gece, OkumaRuhHali.Merakli, null)                    // ikili değil
        };
        var h = RuhHaliHaritasi.Hesapla(oturumlar);
        Assert.True(h.Yeterli);
        Assert.Equal(4, h.CiftliOturum);
        Assert.Equal(50, h.IyilesenYuzde);
    }

    [Fact]
    public void En_Iyi_Zaman_Sonrasi_Ruh_Hali_Ortalamasina_Gore_Secilir()
    {
        var oturumlar = new List<RuhHaliOturumu>();
        for (var i = 0; i < 3; i++)
        {
            oturumlar.Add(O(Gece, OkumaRuhHali.Yorgun, OkumaRuhHali.Gergin));
            oturumlar.Add(O(Sabah, OkumaRuhHali.Yorgun, OkumaRuhHali.Mutlu));
        }
        Assert.Equal("sabah", RuhHaliHaritasi.Hesapla(oturumlar).EnIyiZaman);
    }

    [Fact]
    public void En_Iyi_Zaman_Icin_Yeterli_Oturum_Gerekir()
    {
        var oturumlar = new[]
        {
            O(Sabah, OkumaRuhHali.Yorgun, OkumaRuhHali.Mutlu),
            O(Sabah, OkumaRuhHali.Yorgun, OkumaRuhHali.Mutlu),
            O(Gece, OkumaRuhHali.Yorgun, OkumaRuhHali.Huzurlu),
            O(Gece, OkumaRuhHali.Yorgun, OkumaRuhHali.Huzurlu),
            O(Gece, OkumaRuhHali.Yorgun, OkumaRuhHali.Huzurlu)
        };
        Assert.Equal("gece", RuhHaliHaritasi.Hesapla(oturumlar).EnIyiZaman);
    }

    [Fact]
    public void Ruh_Haline_Gore_En_Cok_Okunan_Tur_Bulunur()
    {
        var oturumlar = new[]
        {
            O(Gece, OkumaRuhHali.Yorgun, null, "Roman"),
            O(Gece, OkumaRuhHali.Yorgun, null, "Roman"),
            O(Gece, OkumaRuhHali.Yorgun, null, "Roman"),
            O(Gece, OkumaRuhHali.Yorgun, null, "Felsefe"),
            O(Gece, OkumaRuhHali.Mutlu, null, "Bilim"),
            O(Gece, OkumaRuhHali.Mutlu, null, "Şiir")
        };
        var h = RuhHaliHaritasi.Hesapla(oturumlar);
        Assert.Single(h.RuhVeTur);
        Assert.Equal((OkumaRuhHali.Yorgun, "Roman"), h.RuhVeTur[0]);
    }

    [Theory]
    [InlineData(4, "gece")]
    [InlineData(5, "sabah")]
    [InlineData(11, "sabah")]
    [InlineData(12, "öğleden sonra")]
    [InlineData(18, "akşam")]
    [InlineData(22, "gece")]
    public void Zaman_Dilimi_Saate_Gore_Belirlenir(int saat, string beklenen) =>
        Assert.Equal(beklenen, RuhHaliHaritasi.ZamanDilimi(new DateTime(2026, 1, 1, saat, 0, 0)));
}
