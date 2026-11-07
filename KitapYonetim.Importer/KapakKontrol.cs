using Microsoft.Data.SqlClient;

namespace KitapYonetim.Importer;

// Open Library kapak adreslerini tarar. Boş (çok küçük dosya) ya da bulunamayan (404) kapakları siler;
// böylece arayüzde üretilmiş kapak görünür ve zenginlestir aşaması bu kitaplar için yeniden denenebilir.
// Kontrol edilen kitaplar import.KapakKontrolu tablosunda tutulur, kesintide kaldığı yerden devam edilir.
public sealed class KapakKontrol
{
    // Normal kapaklar yaklaşık 2,5-30 KB; boş sayfa taramaları ~1 KB civarındadır
    private const int EnKucukBayt = 1500;

    private readonly Veritabani _db;
    private readonly CancellationToken _iptal;
    private readonly HttpClient _http;

    public KapakKontrol(Veritabani db, CancellationToken iptal)
    {
        _db = db;
        _iptal = iptal;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("SatirArasi-Importer/1.0");
    }

    public async Task CalistirAsync()
    {
        using (var c = _db.Ac())
        using (var cmd = new SqlCommand("""
            IF OBJECT_ID('import.KapakKontrolu') IS NULL
            CREATE TABLE import.KapakKontrolu (KitapId INT NOT NULL PRIMARY KEY, Tarih DATETIME2 NOT NULL);
            IF OBJECT_ID('import.ZenginlestirmeDenemesi') IS NULL
            CREATE TABLE import.ZenginlestirmeDenemesi (KitapId INT NOT NULL PRIMARY KEY, Tarih DATETIME2 NOT NULL);
            """, c))
            cmd.ExecuteNonQuery();

        var liste = new List<(int Id, string Url)>();
        using (var c = _db.Ac())
        using (var cmd = new SqlCommand("""
            SELECT k.Id, k.KapakResmiUrl FROM Kitaplar k
            WHERE k.KapakResmiUrl LIKE 'https://covers.openlibrary.org/%'
              AND NOT EXISTS (SELECT 1 FROM import.KapakKontrolu x WHERE x.KitapId = k.Id)
            ORDER BY k.Id
            """, c))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                liste.Add((r.GetInt32(0), r.GetString(1)));

        Gunluk.Bilgi($"kapakkontrol: {liste.Count:N0} Open Library kapağı taranacak.");

        int islenen = 0, silinen = 0, hata = 0;
        var kilit = new object();
        using var sinir = new SemaphoreSlim(8);

        var isler = liste.Select(async kayit =>
        {
            await sinir.WaitAsync(_iptal);
            try
            {
                _iptal.ThrowIfCancellationRequested();
                bool? bos;
                try { bos = await BosMuAsync(kayit.Url); }
                catch (OperationCanceledException) when (_iptal.IsCancellationRequested) { throw; }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    Interlocked.Increment(ref hata);
                    Gunluk.Hata("kapakkontrol", kayit.Id, ex.Message);
                    return; // ağ hatası: kontrol edilmiş sayılmaz
                }

                lock (kilit)
                {
                    Kaydet(kayit.Id, bos == true);
                    islenen++;
                    if (bos == true) silinen++;
                    if (islenen % 250 == 0)
                        Gunluk.Bilgi($"kapakkontrol: {islenen:N0}/{liste.Count:N0} tarandı, {silinen:N0} boş kapak temizlendi.");
                }
            }
            finally { sinir.Release(); }
        });

        await Task.WhenAll(isler);
        Gunluk.Bilgi($"kapakkontrol bitti: {islenen:N0} tarandı, {silinen:N0} boş kapak temizlendi, {hata:N0} ağ hatası (yeniden çalıştırınca tekrar denenir).");
    }

    // true: boş/yok, false: kullanılabilir
    private async Task<bool?> BosMuAsync(string url)
    {
        using var yanit = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, _iptal);
        if ((int)yanit.StatusCode == 404) return true;
        if (!yanit.IsSuccessStatusCode) throw new HttpRequestException($"HTTP {(int)yanit.StatusCode}");
        var bayt = await yanit.Content.ReadAsByteArrayAsync(_iptal);
        return bayt.Length < EnKucukBayt;
    }

    private void Kaydet(int kitapId, bool sil)
    {
        using var c = _db.Ac();
        using var tx = c.BeginTransaction();
        if (sil)
        {
            using (var cmd = new SqlCommand("UPDATE Kitaplar SET KapakResmiUrl = NULL WHERE Id = @id", c, tx))
            { cmd.Parameters.AddWithValue("@id", kitapId); cmd.ExecuteNonQuery(); }
            // zenginlestir bu kitabı yeniden deneyebilsin
            using (var cmd = new SqlCommand("DELETE FROM import.ZenginlestirmeDenemesi WHERE KitapId = @id", c, tx))
            { cmd.Parameters.AddWithValue("@id", kitapId); cmd.ExecuteNonQuery(); }
        }
        using (var cmd = new SqlCommand("INSERT INTO import.KapakKontrolu (KitapId, Tarih) VALUES (@id, SYSDATETIME())", c, tx))
        { cmd.Parameters.AddWithValue("@id", kitapId); cmd.ExecuteNonQuery(); }
        tx.Commit();
    }
}
