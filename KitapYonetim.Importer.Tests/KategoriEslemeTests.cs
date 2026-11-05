using KitapYonetim.Importer;
using Xunit;

namespace KitapYonetim.Importer.Tests;

public class KategoriEslemeTests
{
    [Theory]
    [InlineData("Science fiction", "Bilim Kurgu")]
    [InlineData("Detective and mystery stories", "Polisiye")]
    [InlineData("Horror tales", "Korku")]
    [InlineData("Love stories", "Romantik")]
    [InlineData("Philosophy", "Felsefe")]
    [InlineData("World War, 1939-1945 -- History", "Tarih")]
    [InlineData("Juvenile fiction", "Çocuk")]
    [InlineData("Self-help", "Kişisel Gelişim")]
    [InlineData("Şiir", "Şiir")]
    [InlineData("Fiction", "Roman")]
    public void Esle_BilinenKonuBasliklarini_TurkceKategoriyeEsler(string konu, string beklenen) =>
        Assert.Contains(beklenen, KategoriEsleme.Esle(konu));

    [Fact]
    public void Esle_EdebiyatEleştirisi_TarihSayilmaz()
    {
        var sonuc = KategoriEsleme.Esle("Turkish poetry -- History and criticism");
        Assert.DoesNotContain("Tarih", sonuc);
        Assert.Contains("Şiir", sonuc);
    }

    [Fact]
    public void Esle_SosyalBilimler_BilimSayilmaz() =>
        Assert.DoesNotContain("Bilim", KategoriEsleme.Esle("Social sciences"));

    [Fact]
    public void Esle_BirKonuBirdenFazlaKategoriyeDusebilir()
    {
        var sonuc = KategoriEsleme.Esle("Science fiction adventure stories");
        Assert.Contains("Bilim Kurgu", sonuc);
        Assert.Contains("Macera", sonuc);
    }

    [Fact]
    public void Esle_TanimsizKonu_BosListeDoner() =>
        Assert.Empty(KategoriEsleme.Esle("Xyzzy qwerty"));

    [Fact]
    public void Esle_SonucOncelikSirasinaGoreGelir()
    {
        // Bilim Kurgu kuralı Roman kuralından önce tanımlıdır, "Tur" alanı için ilk sıradaki kullanılır
        var sonuc = KategoriEsleme.Esle("Science fiction novels");
        Assert.True(sonuc.IndexOf("Bilim Kurgu") < sonuc.IndexOf("Roman"));
    }
}
