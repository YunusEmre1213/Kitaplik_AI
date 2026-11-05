using System.Text.Json;
using System.Text.RegularExpressions;

namespace KitapYonetim.Importer;

public sealed class EditionKaydi
{
    public string EditionKey { get; init; } = "";
    public string? WorkKey { get; init; }
    public string Baslik { get; init; } = "";
    public string Dil { get; init; } = "";
    public List<string> YazarKeyleri { get; init; } = new();
    public string? Yayinevi { get; init; }
    public string? YayinTarihi { get; init; }
    public int? SayfaSayisi { get; init; }
    public int? Yil { get; init; }
    public string? Isbn10 { get; init; }
    public string? Isbn13 { get; init; }
    public long? KapakId { get; init; }
    public string? Aciklama { get; init; }
    public List<string> Konular { get; init; } = new();
    public long SatirNo { get; init; }
}

public sealed class WorkKaydi
{
    public string WorkKey { get; init; } = "";
    public string? Baslik { get; init; }
    public string? Aciklama { get; init; }
    public List<string> Konular { get; init; } = new();
    public List<string> YazarKeyleri { get; init; } = new();
}

public sealed class AuthorKaydi
{
    public string AuthorKey { get; init; } = "";
    public string Ad { get; init; } = "";
}

// Open Library JSON yapısındaki farklılıkları (alanın metin/nesne olması, eksik alanlar, bozuk sayılar) tolere eder.
public static class OlAyristirici
{
    private static readonly Regex YilDeseni = new(@"(?<!\d)(1[0-9]{3}|20[0-9]{2})(?!\d)", RegexOptions.Compiled);
    private static readonly Regex IsbnHarfleri = new("[^0-9Xx]", RegexOptions.Compiled);

    // "/books/OL123M" -> "OL123M"
    public static string KisaKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        var son = key.LastIndexOf('/');
        return son >= 0 ? key[(son + 1)..] : key;
    }

    // "OL123W" -> 123 (kısa ve hızlı kümelerde saklamak için)
    public static long SayisalId(string kisaKey)
    {
        long sonuc = 0;
        var basamak = false;
        foreach (var c in kisaKey)
        {
            if (c is >= '0' and <= '9') { sonuc = sonuc * 10 + (c - '0'); basamak = true; }
            else if (basamak) break;
        }
        return basamak ? sonuc : -1;
    }

    // Satır dil filtresinden geçer mi? Pahalı JSON ayrıştırmadan önce hızlı metin kontrolü.
    public static bool DilIcerebilir(string json, IReadOnlyList<string> diller)
    {
        foreach (var dil in diller)
            if (json.Contains($"\"/languages/{dil}\"", StringComparison.Ordinal))
                return true;
        return false;
    }

    public static EditionKaydi? Edition(DumpSatiri satir, IReadOnlyList<string> diller)
    {
        using var doc = JsonDocument.Parse(satir.Json);
        var kok = doc.RootElement;

        var baslik = Metin(kok, "title")?.Trim();
        if (string.IsNullOrEmpty(baslik))
            return null;

        // İlk eşleşen dil seçilir (diller listesindeki öncelik sırasına göre)
        string? secilenDil = null;
        if (kok.TryGetProperty("languages", out var diller1) && diller1.ValueKind == JsonValueKind.Array)
        {
            var bulunanlar = diller1.EnumerateArray().Select(d => KisaKey(Metin(d, "key"))).ToHashSet();
            secilenDil = diller.FirstOrDefault(bulunanlar.Contains);
        }
        if (secilenDil == null)
            return null;

        var altBaslik = Metin(kok, "subtitle")?.Trim();
        if (!string.IsNullOrEmpty(altBaslik) && baslik.Length + altBaslik.Length < 380)
            baslik = $"{baslik}: {altBaslik}";

        return new EditionKaydi
        {
            EditionKey = satir.Anahtar.Length > 0 ? KisaKey(satir.Anahtar) : "",
            WorkKey = IlkKey(kok, "works"),
            Baslik = Kisalt(baslik, 400),
            Dil = secilenDil,
            YazarKeyleri = Keyler(kok, "authors"),
            Yayinevi = Kisalt(IlkMetin(kok, "publishers")?.Trim(), 300),
            YayinTarihi = Kisalt(Metin(kok, "publish_date")?.Trim(), 50),
            SayfaSayisi = Sayfa(kok),
            Yil = Yil(Metin(kok, "publish_date")),
            Isbn10 = Isbn(kok, "isbn_10", 10),
            Isbn13 = Isbn(kok, "isbn_13", 13),
            KapakId = Kapak(kok),
            Aciklama = Aciklama(kok),
            Konular = MetinListesi(kok, "subjects", 30),
            SatirNo = satir.SatirNo
        };
    }

    public static WorkKaydi? Work(DumpSatiri satir)
    {
        using var doc = JsonDocument.Parse(satir.Json);
        var kok = doc.RootElement;

        var yazarlar = new List<string>();
        if (kok.TryGetProperty("authors", out var yz) && yz.ValueKind == JsonValueKind.Array)
        {
            foreach (var y in yz.EnumerateArray())
            {
                if (y.ValueKind == JsonValueKind.Object && y.TryGetProperty("author", out var a))
                {
                    var key = KisaKey(Metin(a, "key"));
                    if (key.Length > 0) yazarlar.Add(key);
                }
            }
        }

        return new WorkKaydi
        {
            WorkKey = KisaKey(satir.Anahtar),
            Baslik = Kisalt(Metin(kok, "title")?.Trim(), 400),
            Aciklama = Aciklama(kok),
            Konular = MetinListesi(kok, "subjects", 30),
            YazarKeyleri = yazarlar
        };
    }

    public static AuthorKaydi? Author(DumpSatiri satir)
    {
        using var doc = JsonDocument.Parse(satir.Json);
        var kok = doc.RootElement;

        var ad = Metin(kok, "name")?.Trim();
        if (string.IsNullOrEmpty(ad)) ad = Metin(kok, "personal_name")?.Trim();
        if (string.IsNullOrEmpty(ad)) return null;

        return new AuthorKaydi { AuthorKey = KisaKey(satir.Anahtar), Ad = Kisalt(ad, 300)! };
    }

    // ---------- yardımcılar ----------

    // Eski kayıtlarda harfler "ayrık" saklanabilir (u + ayrı nokta işareti). Aramanın çalışması için NFC biçimine çevrilir.
    private static string? Nfc(string? metin) =>
        metin is null ? null : metin.Normalize(System.Text.NormalizationForm.FormC);

    private static string? Metin(JsonElement e, string ad) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(ad, out var p) && p.ValueKind == JsonValueKind.String
            ? Nfc(p.GetString())
            : null;

    private static string? Kisalt(string? metin, int uzunluk) =>
        metin is null || metin.Length <= uzunluk ? metin : metin[..uzunluk];

    private static string? IlkKey(JsonElement e, string ad) => Keyler(e, ad).FirstOrDefault();

    private static List<string> Keyler(JsonElement e, string ad)
    {
        var liste = new List<string>();
        if (e.TryGetProperty(ad, out var dizi) && dizi.ValueKind == JsonValueKind.Array)
        {
            foreach (var oge in dizi.EnumerateArray())
            {
                var key = KisaKey(Metin(oge, "key"));
                if (key.Length > 0 && !liste.Contains(key)) liste.Add(key);
            }
        }
        return liste;
    }

    private static string? IlkMetin(JsonElement e, string ad) =>
        e.TryGetProperty(ad, out var dizi) && dizi.ValueKind == JsonValueKind.Array
            ? dizi.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => Nfc(x.GetString())).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            : null;

    private static List<string> MetinListesi(JsonElement e, string ad, int enFazla)
    {
        var liste = new List<string>();
        if (e.TryGetProperty(ad, out var dizi) && dizi.ValueKind == JsonValueKind.Array)
        {
            foreach (var oge in dizi.EnumerateArray())
            {
                if (liste.Count >= enFazla) break;
                if (oge.ValueKind == JsonValueKind.String && oge.GetString() is { Length: > 0 and <= 200 } s)
                    liste.Add(Nfc(s.Trim())!);
            }
        }
        return liste;
    }

    // Sayfa sayısı bazen metin ("320") bazen sayı gelir. Mantıksız değerler yok sayılır.
    private static int? Sayfa(JsonElement e)
    {
        if (!e.TryGetProperty("number_of_pages", out var p))
            return null;

        int? deger = null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n)) deger = n;
        else if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString()?.Trim(), out var m)) deger = m;

        return deger is >= 1 and <= 20000 ? deger : null;
    }

    // "March 2016", "2016-03-01", "c1999" gibi değişik biçimlerden yılı çıkarır
    private static int? Yil(string? tarih)
    {
        if (string.IsNullOrWhiteSpace(tarih)) return null;
        var m = YilDeseni.Match(tarih);
        return m.Success ? int.Parse(m.Value) : null;
    }

    private static string? Isbn(JsonElement e, string ad, int uzunluk)
    {
        if (!e.TryGetProperty(ad, out var dizi) || dizi.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var oge in dizi.EnumerateArray())
        {
            if (oge.ValueKind != JsonValueKind.String) continue;
            var temiz = IsbnHarfleri.Replace(oge.GetString() ?? "", "").ToUpperInvariant();
            if (temiz.Length == uzunluk) return temiz;
        }
        return null;
    }

    private static long? Kapak(JsonElement e)
    {
        if (!e.TryGetProperty("covers", out var dizi) || dizi.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var oge in dizi.EnumerateArray())
            if (oge.ValueKind == JsonValueKind.Number && oge.TryGetInt64(out var id) && id > 0)
                return id;
        return null;
    }

    // description alanı düz metin ya da {"type": "/type/text", "value": "..."} olabilir
    private static string? Aciklama(JsonElement e)
    {
        if (!e.TryGetProperty("description", out var d))
            return null;

        var ham = d.ValueKind switch
        {
            JsonValueKind.String => Nfc(d.GetString()),
            JsonValueKind.Object => Metin(d, "value"),
            _ => null
        };

        return string.IsNullOrWhiteSpace(ham) ? null : ham.Trim();
    }
}
