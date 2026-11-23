using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Tests;

public class OkumaIkiziTests
{
    private static IkizTepkisi T(int kitap, int sayfa, TepkiTuru tur) => new(kitap, sayfa, tur);

    // 10 sayfalık pencerelerde, iki kitaba yayılmış 10 ortak pencere üretir
    private static List<IkizTepkisi> Tepkiler(Func<int, TepkiTuru> tur) =>
        Enumerable.Range(0, 10).Select(i => T(i < 5 ? 1 : 2, (i % 5) * 10 + 5, tur(i))).ToList();

    private static Dictionary<string, List<IkizTepkisi>> Digerleri(params (string Id, List<IkizTepkisi> Tepkiler)[] liste) =>
        liste.ToDictionary(x => x.Id, x => x.Tepkiler);

    [Fact]
    public void Ayni_Hisseden_Okur_Yuksek_Uyum_Alir()
    {
        var benim = Tepkiler(_ => TepkiTuru.Aglatti);
        var sonuc = OkumaIkizi.Hesapla(benim, Digerleri(("a", Tepkiler(_ => TepkiTuru.Aglatti))));
        var a = Assert.Single(sonuc);
        Assert.Equal(10, a.OrtakPencere);
        Assert.Equal(10, a.AyniPencere);
        Assert.Equal(92, a.Uyum);   // (10+1)/(10+2)
        Assert.Equal(2, a.OrtakKitap);
    }

    [Fact]
    public void Farkli_Hisseden_Okur_Dusuk_Uyum_Alir()
    {
        var benim = Tepkiler(_ => TepkiTuru.Aglatti);
        var sonuc = OkumaIkizi.Hesapla(benim, Digerleri(("a", Tepkiler(_ => TepkiTuru.Sikici))));
        Assert.Equal(8, Assert.Single(sonuc).Uyum);   // (0+1)/(10+2)
    }

    [Fact]
    public void Siralama_Uyuma_Gore_Azalandir()
    {
        var benim = Tepkiler(_ => TepkiTuru.Aglatti);
        var sonuc = OkumaIkizi.Hesapla(benim, Digerleri(
            ("dusuk", Tepkiler(_ => TepkiTuru.Sikici)),
            ("yuksek", Tepkiler(_ => TepkiTuru.Aglatti)),
            ("orta", Tepkiler(i => i < 5 ? TepkiTuru.Aglatti : TepkiTuru.Sikici))));
        Assert.Equal(new[] { "yuksek", "orta", "dusuk" }, sonuc.Select(a => a.KullaniciId));
    }

    [Fact]
    public void Az_Ortak_Pencere_Varsa_Eslesme_Gosterilmez()
    {
        var benim = Tepkiler(_ => TepkiTuru.Aglatti);
        var az = Tepkiler(_ => TepkiTuru.Aglatti).Take(7).ToList();
        Assert.Empty(OkumaIkizi.Hesapla(benim, Digerleri(("a", az))));
    }

    [Fact]
    public void Tek_Kitaptan_Eslesme_Gosterilmez()
    {
        var benim = Enumerable.Range(0, 12).Select(i => T(1, i * 10 + 5, TepkiTuru.Guldurdu)).ToList();
        Assert.Empty(OkumaIkizi.Hesapla(benim, Digerleri(("a", benim.ToList()))));
    }

    [Fact]
    public void Ayni_Pencerede_Yakin_Sayfalar_Eslesir()
    {
        var benim = Tepkiler(_ => TepkiTuru.Saskin);
        // Her tepki 5. sayfa yerine 8. sayfada: aynı 10 sayfalık pencerede
        var onun = benim.Select(t => t with { SayfaNo = t.SayfaNo + 3 }).ToList();
        Assert.Equal(10, Assert.Single(OkumaIkizi.Hesapla(benim, Digerleri(("a", onun)))).AyniPencere);
    }

    [Fact]
    public void Ornekler_Farkli_Kitaplardan_Ve_Sinirli_Gelir()
    {
        var benim = Tepkiler(_ => TepkiTuru.Aglatti);
        var a = Assert.Single(OkumaIkizi.Hesapla(benim, Digerleri(("a", Tepkiler(_ => TepkiTuru.Aglatti)))));
        Assert.Equal(2, a.Ornekler.Count);   // yalnızca iki kitap var
        Assert.Equal(new[] { 1, 2 }, a.Ornekler.Select(o => o.KitapId));
        Assert.All(a.Ornekler, o => Assert.Equal(TepkiTuru.Aglatti, o.Tur));
        Assert.Equal((1, 10), (a.Ornekler[0].SayfaBasi, a.Ornekler[0].SayfaSonu));
    }

    [Fact]
    public void Ayni_Pencerede_Birden_Cok_Tepki_Kesisirse_Eslesir()
    {
        var benim = Tepkiler(_ => TepkiTuru.Aglatti).Concat(new[] { T(1, 6, TepkiTuru.Guldurdu) }).ToList();
        var onun = Tepkiler(_ => TepkiTuru.Guldurdu);
        var a = Assert.Single(OkumaIkizi.Hesapla(benim, Digerleri(("a", onun))));
        Assert.Equal(1, a.AyniPencere);
    }
}
