using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace KitapYonetim.Importer;

// Kapağı ya da açıklaması eksik kitapları ISBN ile Open Library ve Google Books'tan tamamlar.
// Yalnızca boş alanlar doldurulur, var olan veri ezilmez. Hiçbir şey uydurulmaz.
// Denenen kitaplar import.ZenginlestirmeDenemesi tablosuna yazılır, böylece kesintide kaldığı yerden devam edilir.
public sealed partial class Zenginlestirici
{
    private readonly Veritabani _db;
    private readonly Ayarlar _a;
    private readonly string? _googleAnahtar;
    private readonly CancellationToken _iptal;
    private readonly HttpClient _http;

    public Zenginlestirici(Veritabani db, Ayarlar ayarlar, string? googleAnahtar, CancellationToken iptal)
    {
        _db = db;
        _a = ayarlar;
        _googleAnahtar = googleAnahtar;
        _iptal = iptal;
        _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }) { Timeout = TimeSpan.FromSeconds(20) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("SatirArasi-Importer/1.0");
    }

    private sealed record Aday(int Id, string Isbn, bool KapakGerek, bool AciklamaGerek);

    public async Task CalistirAsync()
    {
        using (var c = _db.Ac())
        using (var cmd = new SqlCommand("""
            IF OBJECT_ID('import.ZenginlestirmeDenemesi') IS NULL
            CREATE TABLE import.ZenginlestirmeDenemesi (KitapId INT NOT NULL PRIMARY KEY, Tarih DATETIME2 NOT NULL);
            """, c))
            cmd.ExecuteNonQuery();

        if (_a.ZenginlestirYeniden)
        {
            using var c = _db.Ac();
            using var cmd = new SqlCommand("DELETE FROM import.ZenginlestirmeDenemesi", c);
            cmd.ExecuteNonQuery();
        }

        var adaylar = AdaylariOku();
        Gunluk.Bilgi($"zenginlestir: {adaylar.Count:N0} aday kitap (ISBN'li, kapağı ya da açıklaması eksik, daha önce denenmemiş).");

        int islenen = 0, kapakBulunan = 0, aciklamaBulunan = 0;
        var sonKontrol = DateTime.UtcNow;

        foreach (var aday in adaylar)
        {
            _iptal.ThrowIfCancellationRequested();

            string? kapak = null, aciklama = null;
            var googleGerekti = false;
            try
            {
                if (aday.KapakGerek)
                    kapak = await OpenLibraryKapakAsync(aday.Isbn);

                if (aday.AciklamaGerek || (aday.KapakGerek && kapak == null))
                {
                    googleGerekti = true;
                    if (!_googleKapali)
                    {
                        var g = await GoogleAsync(aday.Isbn);
                        if (g.KotaDoldu)
                        {
                            _googleKapali = true;
                            Gunluk.Bilgi("Google Books istek sınırı doldu (anahtarsız kullanımda günlük kota çok düşüktür). Bu çalıştırmada yalnızca Open Library kapakları denenecek; Google gerektiren kitaplar denenmedi olarak kalır.");
                        }
                        else
                        {
                            googleGerekti = false;
                            if (aday.KapakGerek && kapak == null) kapak = g.Kapak;
                            if (aday.AciklamaGerek) aciklama = g.Aciklama;
                        }
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                Gunluk.Hata("zenginlestir", aday.Id, ex.Message);
                continue; // ağ hatası: denendi sayılmaz, sonraki çalıştırmada tekrar denenir
            }

            Kaydet(aday.Id, kapak, aciklama, denendi: !googleGerekti);
            islenen++;
            if (kapak != null) kapakBulunan++;
            if (aciklama != null) aciklamaBulunan++;

            if (islenen % 100 == 0 || (DateTime.UtcNow - sonKontrol).TotalSeconds > 60)
            {
                sonKontrol = DateTime.UtcNow;
                Gunluk.Bilgi($"zenginlestir: {islenen:N0}/{adaylar.Count:N0} işlendi. Kapak bulunan: {kapakBulunan:N0}, açıklama bulunan: {aciklamaBulunan:N0}");
            }

            await Task.Delay(_googleKapali ? 100 : _a.ZenginlestirBeklemeMs, _iptal);
        }

        Gunluk.Bilgi($"zenginlestir bitti: {islenen:N0} kitap işlendi. Kapak bulunan: {kapakBulunan:N0}, açıklama bulunan: {aciklamaBulunan:N0}");
    }

    private List<Aday> AdaylariOku()
    {
        var liste = new List<Aday>();
        var ust = _a.ZenginlestirMax > 0 ? $"TOP ({_a.ZenginlestirMax})" : "";
        using var c = _db.Ac();
        using var cmd = new SqlCommand($"""
            SELECT {ust} k.Id, COALESCE(k.Isbn13, k.Isbn10),
                   CASE WHEN k.KapakResmiUrl IS NULL OR k.KapakResmiUrl = N'' THEN 1 ELSE 0 END,
                   CASE WHEN k.Aciklama = N'' THEN 1 ELSE 0 END
            FROM Kitaplar k
            WHERE (k.Isbn13 IS NOT NULL OR k.Isbn10 IS NOT NULL)
              AND (k.KapakResmiUrl IS NULL OR k.KapakResmiUrl = N'' OR k.Aciklama = N'')
              AND NOT EXISTS (SELECT 1 FROM import.ZenginlestirmeDenemesi d WHERE d.KitapId = k.Id)
            ORDER BY k.Id
            """, c);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            liste.Add(new Aday(r.GetInt32(0), r.GetString(1), r.GetInt32(2) == 1, r.GetInt32(3) == 1));
        return liste;
    }

    private bool _googleKapali;

    private void Kaydet(int kitapId, string? kapak, string? aciklama, bool denendi)
    {
        using var c = _db.Ac();
        using var tx = c.BeginTransaction();
        if (kapak != null)
        {
            using var cmd = new SqlCommand("UPDATE Kitaplar SET KapakResmiUrl = @u WHERE Id = @id AND (KapakResmiUrl IS NULL OR KapakResmiUrl = N'')", c, tx);
            cmd.Parameters.AddWithValue("@u", kapak);
            cmd.Parameters.AddWithValue("@id", kitapId);
            cmd.ExecuteNonQuery();
        }
        if (aciklama != null)
        {
            using var cmd = new SqlCommand("UPDATE Kitaplar SET Aciklama = @a WHERE Id = @id AND Aciklama = N''", c, tx);
            cmd.Parameters.AddWithValue("@a", aciklama);
            cmd.Parameters.AddWithValue("@id", kitapId);
            cmd.ExecuteNonQuery();
        }
        if (denendi)
        {
            using var cmd = new SqlCommand("INSERT INTO import.ZenginlestirmeDenemesi (KitapId, Tarih) VALUES (@id, SYSDATETIME())", c, tx);
            cmd.Parameters.AddWithValue("@id", kitapId);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    // Open Library'de bu ISBN için kapak varsa adresini döner. default=false: kapak yoksa 404 verir.
    private async Task<string?> OpenLibraryKapakAsync(string isbn)
    {
        var adres = $"https://covers.openlibrary.org/b/isbn/{Uri.EscapeDataString(isbn)}-M.jpg?default=false";
        using var yanit = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Head, adres), HttpCompletionOption.ResponseHeadersRead, _iptal);
        return yanit.IsSuccessStatusCode ? adres.Replace("?default=false", "") : null;
    }

    private sealed record GoogleSonuc(string? Kapak, string? Aciklama, bool KotaDoldu);

    private async Task<GoogleSonuc> GoogleAsync(string isbn)
    {
        var adres = $"https://www.googleapis.com/books/v1/volumes?q=isbn:{Uri.EscapeDataString(isbn)}&maxResults=1&fields=items(volumeInfo(description,imageLinks))";
        if (!string.IsNullOrEmpty(_googleAnahtar))
            adres += "&key=" + Uri.EscapeDataString(_googleAnahtar);

        for (var deneme = 0; deneme < 2; deneme++)
        {
            using var yanit = await _http.GetAsync(adres, _iptal);
            if (yanit.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (deneme == 0) { await Task.Delay(TimeSpan.FromSeconds(30), _iptal); continue; }
                return new GoogleSonuc(null, null, true);
            }
            if (!yanit.IsSuccessStatusCode)
                return new GoogleSonuc(null, null, false);

            using var belge = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync(_iptal));
            if (!belge.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
                return new GoogleSonuc(null, null, false);

            var vi = items[0].GetProperty("volumeInfo");

            string? kapak = null;
            if (vi.TryGetProperty("imageLinks", out var img))
            {
                var ham = img.TryGetProperty("thumbnail", out var t) ? t.GetString()
                        : img.TryGetProperty("smallThumbnail", out var st) ? st.GetString() : null;
                if (!string.IsNullOrEmpty(ham))
                    kapak = ham.Replace("http://", "https://").Replace("&edge=curl", "");
            }

            string? aciklama = null;
            if (vi.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String)
            {
                var temiz = Temizle(d.GetString() ?? "");
                if (temiz.Length >= 20)
                    aciklama = temiz.Length > 4000 ? temiz[..4000] : temiz;
            }

            return new GoogleSonuc(kapak, aciklama, false);
        }
        return new GoogleSonuc(null, null, true);
    }

    // Google açıklamaları basit HTML içerebilir
    private static string Temizle(string html)
    {
        var s = HtmlEtiketi().Replace(html.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("</p>", "\n"), "");
        s = WebUtility.HtmlDecode(s);
        return s.Normalize(System.Text.NormalizationForm.FormC).Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlEtiketi();
}
