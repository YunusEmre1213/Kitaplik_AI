using KitapYonetim.Importer;
using Xunit;

namespace KitapYonetim.Importer.Tests;

public class OlAyristiriciTests
{
    private static readonly string[] Diller = { "tur", "eng" };

    private static DumpSatiri Satir(string tur, string key, string json, long no = 1) => new(no, tur, key, json);

    [Fact]
    public void Edition_TurkceKayit_TumAlanlariOkunur()
    {
        const string json = """
            {"key": "/books/OL1M", "title": "Suç ve Ceza", "subtitle": "roman",
             "languages": [{"key": "/languages/tur"}],
             "authors": [{"key": "/authors/OL10A"}, {"key": "/authors/OL11A"}],
             "works": [{"key": "/works/OL5W"}],
             "publishers": ["Can Yayınları"], "publish_date": "March 2016",
             "number_of_pages": 521, "isbn_10": ["975-510-442-9"], "isbn_13": ["9789754589023"],
             "covers": [-1, 10736127], "description": {"type": "/type/text", "value": "Bir öğrencinin hikâyesi."},
             "subjects": ["Fiction", "Russian literature"]}
            """;

        var e = OlAyristirici.Edition(Satir("/type/edition", "/books/OL1M", json), Diller);

        Assert.NotNull(e);
        Assert.Equal("OL1M", e!.EditionKey);
        Assert.Equal("OL5W", e.WorkKey);
        Assert.Equal("Suç ve Ceza: roman", e.Baslik);
        Assert.Equal("tur", e.Dil);
        Assert.Equal(new[] { "OL10A", "OL11A" }, e.YazarKeyleri);
        Assert.Equal("Can Yayınları", e.Yayinevi);
        Assert.Equal(2016, e.Yil);
        Assert.Equal(521, e.SayfaSayisi);
        Assert.Equal("9755104429", e.Isbn10);          // tire temizlenir
        Assert.Equal("9789754589023", e.Isbn13);
        Assert.Equal(10736127, e.KapakId);              // -1 geçersiz kapak atlanır
        Assert.Equal("Bir öğrencinin hikâyesi.", e.Aciklama);
        Assert.Contains("Fiction", e.Konular);
    }

    [Fact]
    public void Edition_EksikAlanlar_KayitSilinmezAlanlarBosKalir()
    {
        const string json = """{"title": "Sadece Başlık", "languages": [{"key": "/languages/eng"}]}""";

        var e = OlAyristirici.Edition(Satir("/type/edition", "/books/OL2M", json), Diller);

        Assert.NotNull(e);
        Assert.Null(e!.SayfaSayisi);
        Assert.Null(e.Isbn10);
        Assert.Null(e.Isbn13);
        Assert.Null(e.KapakId);
        Assert.Null(e.Aciklama);
        Assert.Null(e.Yil);
        Assert.Empty(e.YazarKeyleri);
    }

    [Fact]
    public void Edition_BaslikYoksa_Atlanir()
    {
        const string json = """{"languages": [{"key": "/languages/tur"}]}""";
        Assert.Null(OlAyristirici.Edition(Satir("/type/edition", "/books/OL3M", json), Diller));
    }

    [Fact]
    public void Edition_IstenmeyenDil_Atlanir()
    {
        const string json = """{"title": "Le Petit Prince", "languages": [{"key": "/languages/fre"}]}""";
        Assert.Null(OlAyristirici.Edition(Satir("/type/edition", "/books/OL4M", json), Diller));
    }

    [Theory]
    [InlineData("\"320\"", 320)]       // sayfa sayısı bazen metin olarak gelir
    [InlineData("320", 320)]
    [InlineData("0", null)]            // mantıksız değerler yok sayılır
    [InlineData("99999", null)]
    [InlineData("\"yüz sayfa\"", null)]
    public void Edition_SayfaSayisi_FarkliBicimleriTolereEder(string ham, int? beklenen)
    {
        var json = $$"""{"title": "X", "languages": [{"key": "/languages/eng"}], "number_of_pages": {{ham}}}""";
        var e = OlAyristirici.Edition(Satir("/type/edition", "/books/OL5M", json), Diller);
        Assert.Equal(beklenen, e!.SayfaSayisi);
    }

    [Fact]
    public void Edition_AyrikHarfler_NfcBicimineCevrilir()
    {
        // "u" + birleşik nokta işareti (ayrık ü), Open Library'nin eski kayıtlarındaki gibi
        var json = "{\"title\": \"Tu\\u0308rk yazarlar\", \"languages\": [{\"key\": \"/languages/tur\"}]}";
        var e = OlAyristirici.Edition(Satir("/type/edition", "/books/OL6M", json), Diller);
        Assert.Equal("Türk yazarlar", e!.Baslik);
        Assert.Contains('ü', e.Baslik);
    }

    [Theory]
    [InlineData("1992", 1992)]
    [InlineData("March 3, 2016", 2016)]
    [InlineData("c1999", 1999)]
    [InlineData("2016-03-01", 2016)]
    [InlineData("bilinmiyor", null)]
    public void Edition_YayinYili_DegisikBicimlerdenCikarilir(string tarih, int? beklenen)
    {
        var json = $$"""{"title": "X", "languages": [{"key": "/languages/eng"}], "publish_date": "{{tarih}}"}""";
        var e = OlAyristirici.Edition(Satir("/type/edition", "/books/OL7M", json), Diller);
        Assert.Equal(beklenen, e!.Yil);
    }

    [Fact]
    public void Work_AciklamaVeYazarlariOkunur()
    {
        const string json = """
            {"key": "/works/OL5W", "title": "Crime and Punishment", "description": "Klasik.",
             "subjects": ["Fiction"], "authors": [{"author": {"key": "/authors/OL10A"}, "type": {"key": "/type/author_role"}}]}
            """;

        var w = OlAyristirici.Work(Satir("/type/work", "/works/OL5W", json));

        Assert.NotNull(w);
        Assert.Equal("OL5W", w!.WorkKey);
        Assert.Equal("Klasik.", w.Aciklama);
        Assert.Equal(new[] { "OL10A" }, w.YazarKeyleri);
    }

    [Fact]
    public void Author_AdYoksaAtlanir_KisiselAdYedekOlur()
    {
        Assert.Null(OlAyristirici.Author(Satir("/type/author", "/authors/OL1A", "{}")));

        var a = OlAyristirici.Author(Satir("/type/author", "/authors/OL2A", """{"personal_name": "Cemal Süreya"}"""));
        Assert.Equal("Cemal Süreya", a!.Ad);
    }

    [Theory]
    [InlineData("OL123W", 123)]
    [InlineData("OL7A", 7)]
    [InlineData("/works/OL99W", 99)]
    [InlineData("kimlik", -1)]
    public void SayisalId_AnahtardanSayiCikarir(string key, long beklenen) =>
        Assert.Equal(beklenen, OlAyristirici.SayisalId(OlAyristirici.KisaKey(key)));

    [Fact]
    public void Edition_BozukJson_IstisnaFirlatir_CagiranAtlar()
    {
        Assert.ThrowsAny<System.Text.Json.JsonException>(() =>
            OlAyristirici.Edition(Satir("/type/edition", "/books/OL8M", "{bozuk"), Diller));
    }
}
