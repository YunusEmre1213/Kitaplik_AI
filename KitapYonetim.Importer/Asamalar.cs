using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace KitapYonetim.Importer;

public static class Gunluk
{
    private static readonly object Kilit = new();
    private static string _hataDosyasi = "import-hatalari.log";

    public static void HataDosyasi(string yol) => _hataDosyasi = yol;

    public static void Bilgi(string mesaj)
    {
        lock (Kilit)
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {mesaj}");
    }

    // Hatalı kayıt atlanır, ayrıntısı dosyaya yazılır
    public static void Hata(string kaynak, long satir, string mesaj)
    {
        lock (Kilit)
            File.AppendAllText(_hataDosyasi, $"{DateTime.Now:s}\t{kaynak}\tsatir={satir}\t{mesaj.ReplaceLineEndings(" ")}{Environment.NewLine}");
    }

    public static string Bellek() => $"{Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB RAM";
}

public sealed class Aktarici
{
    private readonly Ayarlar _a;
    private readonly Veritabani _db;
    private readonly CancellationToken _iptal;

    public Aktarici(Ayarlar ayarlar, Veritabani db, CancellationToken iptal)
    {
        _a = ayarlar;
        _db = db;
        _iptal = iptal;
    }

    // =====================================================================
    // 1) EDITIONS: dil filtresi, staging'e toplu yazma, devam noktası
    // =====================================================================
    public void Editions(string dosya)
    {
        var surum = Surum(dosya);
        var log = _db.LogBaslat("editions", surum, _a.Yeniden);
        if (AtlaTamamlandiysa("editions", log)) return;

        // Dil sayaçları: devam ederken staging'de zaten bulunanlar sayılır
        var sayac = _a.Diller.ToDictionary(d => OlDil(d), _ => 0L);
        using (var c = _db.Ac())
        using (var cmd = new SqlCommand("SELECT Dil, COUNT(*) FROM import.StageEdition GROUP BY Dil", c))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                sayac[r.GetString(0)] = r.GetInt32(1);

        Gunluk.Bilgi($"editions: {surum} okunuyor. Devam noktası: satır {log.SonSatir:N0}. Mevcut: {string.Join(", ", sayac.Select(k => $"{k.Key}={k.Value:N0}"))}");

        var edT = EditionTablosu();
        var yzT = YazarTablosu("StageEditionYazar", "EditionKey", "YazarKey");
        var knT = KonuTablosu();
        long satirNo = log.SonSatir;
        var sw = Stopwatch.StartNew();
        var durum = "Devam";
        string? mesaj = null;

        try
        {
            foreach (var satir in DumpOkuyucu.Oku(dosya, log.SonSatir, m => Gunluk.Bilgi("editions: " + m), _iptal))
            {
                satirNo = satir.SatirNo;
                log.OkunanSatir = satirNo;

                if (_a.DilBasinaMax > 0 && sayac.Values.All(v => v >= _a.DilBasinaMax))
                {
                    durum = "Tamamlandi";
                    mesaj = "Dil başına sınıra ulaşıldı.";
                    break;
                }

                if (satir.Tur != "/type/edition" || !OlAyristirici.DilIcerebilir(satir.Json, _a.Diller))
                    continue;

                EditionKaydi? kayit;
                try
                {
                    kayit = OlAyristirici.Edition(satir, _a.Diller);
                }
                catch (Exception ex)
                {
                    log.HataSayisi++;
                    Gunluk.Hata("editions", satir.SatirNo, ex.Message);
                    continue;
                }

                if (kayit == null || kayit.EditionKey.Length == 0)
                {
                    log.Atlanan++;
                    continue;
                }

                var dil = OlDil(kayit.Dil);
                if (_a.DilBasinaMax > 0 && sayac.GetValueOrDefault(dil) >= _a.DilBasinaMax)
                {
                    log.Atlanan++;
                    continue;
                }

                sayac[dil] = sayac.GetValueOrDefault(dil) + 1;
                kayit = EditionKaydiDilleri(kayit, dil);
                log.KabulEdilen++;

                edT.Rows.Add(kayit.EditionKey, (object?)kayit.WorkKey ?? DBNull.Value, kayit.Baslik, dil,
                    (object?)kayit.Yayinevi ?? DBNull.Value, (object?)kayit.YayinTarihi ?? DBNull.Value,
                    (object?)kayit.SayfaSayisi ?? DBNull.Value, (object?)kayit.Yil ?? DBNull.Value,
                    (object?)kayit.Isbn10 ?? DBNull.Value, (object?)kayit.Isbn13 ?? DBNull.Value,
                    (object?)kayit.KapakId ?? DBNull.Value, (object?)kayit.Aciklama ?? DBNull.Value);

                for (var i = 0; i < kayit.YazarKeyleri.Count; i++)
                    yzT.Rows.Add(kayit.EditionKey, kayit.YazarKeyleri[i], i);
                foreach (var konu in kayit.Konular.Distinct(StringComparer.OrdinalIgnoreCase))
                    knT.Rows.Add("E", kayit.EditionKey, konu);

                if (edT.Rows.Count >= _a.Parti)
                    Bosalt(log, satirNo, edT, yzT, knT);

                if (satirNo % 200_000 == 0)
                    Ilerleme("editions", log, sw, sayac);
            }

            if (durum != "Tamamlandi")
            {
                durum = "Tamamlandi";
                mesaj = "Dosya sonuna ulaşıldı.";
            }
        }
        catch (OperationCanceledException)
        {
            mesaj = "Kullanıcı tarafından durduruldu, kaldığı yerden devam edilebilir.";
            Gunluk.Bilgi("editions: durduruluyor...");
        }
        catch (Exception ex)
        {
            durum = "Hata";
            mesaj = ex.Message;
            Gunluk.Hata("editions", satirNo, ex.ToString());
            Gunluk.Bilgi("editions: HATA: " + ex.Message);
        }

        // Kalan parti yazılır (iptal durumunda da veri kaybolmasın)
        Bosalt(log, satirNo, edT, yzT, knT);
        log.SonSatir = satirNo;
        _db.LogGuncelle(log, durum, mesaj);
        Gunluk.Bilgi($"editions: {durum}. Okunan satır {log.OkunanSatir:N0}, kabul {log.KabulEdilen:N0}, atlanan {log.Atlanan:N0}, hata {log.HataSayisi:N0}. {mesaj}");
        _iptal.ThrowIfCancellationRequested();
    }

    // =====================================================================
    // 2) WORKS: yalnızca editions'ta geçen eserler (açıklama, konu, yazar)
    // =====================================================================
    public void Works(string dosya)
    {
        var surum = Surum(dosya);
        var log = _db.LogBaslat("works", surum, _a.Yeniden);
        if (AtlaTamamlandiysa("works", log)) return;

        var gerekli = AnahtarKumesi("""
            SELECT DISTINCT e.WorkKey FROM import.StageEdition e
            WHERE e.WorkKey IS NOT NULL AND NOT EXISTS (SELECT 1 FROM import.StageWork w WHERE w.WorkKey = e.WorkKey)
            """);
        if (gerekli.Count == 0)
        {
            log.Durum = "Tamamlandi";
            _db.LogGuncelle(log, "Tamamlandi", "Aranacak yeni eser yok, dosya okunmadı.");
            Gunluk.Bilgi("works: aranacak yeni eser yok, dosya okunmadan tamamlandı.");
            return;
        }

        Gunluk.Bilgi($"works: {surum} okunuyor. Aranan eser sayısı: {gerekli.Count:N0}. Devam noktası: satır {log.SonSatir:N0}");

        var wT = new DataTable();
        wT.Columns.Add("WorkKey", typeof(string)); wT.Columns.Add("Baslik", typeof(string)); wT.Columns.Add("Aciklama", typeof(string));
        var wyT = YazarTablosu("StageWorkYazar", "WorkKey", "YazarKey");
        var knT = KonuTablosu();

        long satirNo = log.SonSatir;
        var bulunan = 0L;
        var sw = Stopwatch.StartNew();
        var durum = "Devam";
        string? mesaj = null;

        try
        {
            foreach (var satir in DumpOkuyucu.Oku(dosya, log.SonSatir, m => Gunluk.Bilgi("works: " + m), _iptal))
            {
                satirNo = satir.SatirNo;
                log.OkunanSatir = satirNo;

                // Pahalı JSON ayrıştırmasından önce yalnızca anahtara bakılır
                if (satir.Tur != "/type/work" || !gerekli.Contains(OlAyristirici.SayisalId(OlAyristirici.KisaKey(satir.Anahtar))))
                {
                    if (satirNo % 500_000 == 0) Ilerleme("works", log, sw, null, $"bulunan {bulunan:N0}/{gerekli.Count:N0}");
                    continue;
                }

                WorkKaydi? w;
                try { w = OlAyristirici.Work(satir); }
                catch (Exception ex)
                {
                    log.HataSayisi++;
                    Gunluk.Hata("works", satir.SatirNo, ex.Message);
                    continue;
                }

                if (w == null) { log.Atlanan++; continue; }

                log.KabulEdilen++;
                bulunan++;
                wT.Rows.Add(w.WorkKey, (object?)w.Baslik ?? DBNull.Value, (object?)w.Aciklama ?? DBNull.Value);
                for (var i = 0; i < w.YazarKeyleri.Count; i++)
                    wyT.Rows.Add(w.WorkKey, w.YazarKeyleri[i], i);
                foreach (var konu in w.Konular.Distinct(StringComparer.OrdinalIgnoreCase))
                    knT.Rows.Add("W", w.WorkKey, konu);

                if (wT.Rows.Count >= _a.Parti)
                    BosaltWorks(log, satirNo, wT, wyT, knT);

                // Aranan tüm eserler bulunduysa dosyanın kalanını okumaya gerek yok
                if (bulunan >= gerekli.Count)
                {
                    mesaj = "Aranan tüm eserler bulundu.";
                    break;
                }
            }

            durum = "Tamamlandi";
            mesaj ??= "Dosya sonuna ulaşıldı (bazı eserler dump içinde bulunamamış olabilir).";
        }
        catch (OperationCanceledException)
        {
            mesaj = "Kullanıcı tarafından durduruldu, kaldığı yerden devam edilebilir.";
            Gunluk.Bilgi("works: durduruluyor...");
        }
        catch (Exception ex)
        {
            durum = "Hata";
            mesaj = ex.Message;
            Gunluk.Hata("works", satirNo, ex.ToString());
            Gunluk.Bilgi("works: HATA: " + ex.Message);
        }

        BosaltWorks(log, satirNo, wT, wyT, knT);
        log.SonSatir = satirNo;
        _db.LogGuncelle(log, durum, mesaj);
        Gunluk.Bilgi($"works: {durum}. Okunan satır {log.OkunanSatir:N0}, bulunan {log.KabulEdilen:N0}/{gerekli.Count:N0}, hata {log.HataSayisi:N0}. {mesaj}");
        _iptal.ThrowIfCancellationRequested();
    }

    // =====================================================================
    // 3) AUTHORS: yalnızca kitaplarda geçen yazarlar
    // =====================================================================
    public void Authors(string dosya)
    {
        var surum = Surum(dosya);
        var log = _db.LogBaslat("authors", surum, _a.Yeniden);
        if (AtlaTamamlandiysa("authors", log)) return;

        var gerekli = AnahtarKumesi("""
            SELECT YazarKey FROM (
                SELECT YazarKey FROM import.StageEditionYazar
                UNION
                SELECT YazarKey FROM import.StageWorkYazar
            ) x
            WHERE NOT EXISTS (SELECT 1 FROM import.StageAuthor a WHERE a.AuthorKey = x.YazarKey)
            """);
        if (gerekli.Count == 0)
        {
            log.Durum = "Tamamlandi";
            _db.LogGuncelle(log, "Tamamlandi", "Aranacak yeni yazar yok, dosya okunmadı.");
            Gunluk.Bilgi("authors: aranacak yeni yazar yok, dosya okunmadan tamamlandı.");
            return;
        }

        Gunluk.Bilgi($"authors: {surum} okunuyor. Aranan yazar sayısı: {gerekli.Count:N0}. Devam noktası: satır {log.SonSatir:N0}");

        var aT = new DataTable();
        aT.Columns.Add("AuthorKey", typeof(string)); aT.Columns.Add("Ad", typeof(string));

        long satirNo = log.SonSatir;
        var bulunan = 0L;
        var sw = Stopwatch.StartNew();
        var durum = "Devam";
        string? mesaj = null;

        try
        {
            foreach (var satir in DumpOkuyucu.Oku(dosya, log.SonSatir, m => Gunluk.Bilgi("authors: " + m), _iptal))
            {
                satirNo = satir.SatirNo;
                log.OkunanSatir = satirNo;

                if (satir.Tur != "/type/author" || !gerekli.Contains(OlAyristirici.SayisalId(OlAyristirici.KisaKey(satir.Anahtar))))
                {
                    if (satirNo % 500_000 == 0) Ilerleme("authors", log, sw, null, $"bulunan {bulunan:N0}/{gerekli.Count:N0}");
                    continue;
                }

                AuthorKaydi? a;
                try { a = OlAyristirici.Author(satir); }
                catch (Exception ex)
                {
                    log.HataSayisi++;
                    Gunluk.Hata("authors", satir.SatirNo, ex.Message);
                    continue;
                }

                if (a == null) { log.Atlanan++; continue; }

                log.KabulEdilen++;
                bulunan++;
                aT.Rows.Add(a.AuthorKey, a.Ad);

                if (aT.Rows.Count >= _a.Parti)
                    BosaltAuthors(log, satirNo, aT);

                if (bulunan >= gerekli.Count)
                {
                    mesaj = "Aranan tüm yazarlar bulundu.";
                    break;
                }
            }

            durum = "Tamamlandi";
            mesaj ??= "Dosya sonuna ulaşıldı (bazı yazarlar dump içinde bulunamamış olabilir).";
        }
        catch (OperationCanceledException)
        {
            mesaj = "Kullanıcı tarafından durduruldu, kaldığı yerden devam edilebilir.";
            Gunluk.Bilgi("authors: durduruluyor...");
        }
        catch (Exception ex)
        {
            durum = "Hata";
            mesaj = ex.Message;
            Gunluk.Hata("authors", satirNo, ex.ToString());
            Gunluk.Bilgi("authors: HATA: " + ex.Message);
        }

        BosaltAuthors(log, satirNo, aT);
        log.SonSatir = satirNo;
        _db.LogGuncelle(log, durum, mesaj);
        Gunluk.Bilgi($"authors: {durum}. Okunan satır {log.OkunanSatir:N0}, bulunan {log.KabulEdilen:N0}/{gerekli.Count:N0}, hata {log.HataSayisi:N0}. {mesaj}");
        _iptal.ThrowIfCancellationRequested();
    }

    // =====================================================================
    // 4) BİRLEŞTİR: staging -> final tablolar (Kitaplar, Yazarlar, ilişkiler)
    // =====================================================================
    public void Birlestir()
    {
        var log = _db.LogBaslat("birlestirme", "staging", _a.Yeniden);
        // Birleştirme her çalıştırmada yeniden denenir (idempotent), önceki "Tamamlandi" kaydı atlama nedeni değildir.
        if (log.Durum == "Tamamlandi")
            log = _db.LogBaslat("birlestirme", "staging", yeniden: true);

        Gunluk.Bilgi("birlestir: başlıyor.");
        var durum = "Devam";
        string? mesaj = null;
        long sonSira = 0;

        try
        {
            using (var c = _db.Ac())
            {
                // Kategoriler
                foreach (var kategori in KategoriEsleme.TumKategoriler)
                {
                    using var cmd = new SqlCommand("IF NOT EXISTS (SELECT 1 FROM Kategoriler WHERE Ad = @a) INSERT INTO Kategoriler (Ad) VALUES (@a)", c);
                    cmd.Parameters.AddWithValue("@a", kategori);
                    cmd.ExecuteNonQuery();
                }
            }

            KonuEslemeleriniHazirla();

            // Yazarlar (staging'deki yeni olanlar)
            var yeniYazar = Calistir("""
                INSERT INTO Yazarlar (Ad, OpenLibraryKey)
                SELECT a.Ad, a.AuthorKey FROM import.StageAuthor a
                WHERE NOT EXISTS (SELECT 1 FROM Yazarlar y WHERE y.OpenLibraryKey = a.AuthorKey)
                """);
            Gunluk.Bilgi($"birlestir: {yeniYazar:N0} yeni yazar eklendi.");

            var enBuyuk = _db.Skaler<long?>("SELECT MAX(Sira) FROM import.StageEdition") ?? 0;
            const int Dilim = 20_000;
            long eklenen = 0;

            for (long bas = 1; bas <= enBuyuk; bas += Dilim)
            {
                _iptal.ThrowIfCancellationRequested();
                var son = bas + Dilim - 1;
                eklenen += DilimiBirlestir(bas, son);
                sonSira = son;
                log.OkunanSatir = Math.Min(son, enBuyuk);
                log.KabulEdilen = eklenen;
                log.SonSatir = sonSira;
                _db.LogGuncelle(log, "Devam");
                Gunluk.Bilgi($"birlestir: {Math.Min(son, enBuyuk):N0}/{enBuyuk:N0} staging satırı işlendi, toplam {eklenen:N0} yeni kitap. {Gunluk.Bellek()}");
            }

            durum = "Tamamlandi";
            mesaj = $"{eklenen:N0} yeni kitap, {yeniYazar:N0} yeni yazar.";
        }
        catch (OperationCanceledException)
        {
            mesaj = "Kullanıcı tarafından durduruldu, yeniden çalıştırılabilir.";
        }
        catch (Exception ex)
        {
            durum = "Hata";
            mesaj = ex.Message;
            Gunluk.Hata("birlestirme", sonSira, ex.ToString());
            Gunluk.Bilgi("birlestir: HATA: " + ex.Message);
        }

        _db.LogGuncelle(log, durum, mesaj);
        Gunluk.Bilgi($"birlestir: {durum}. {mesaj}");
        _iptal.ThrowIfCancellationRequested();
    }

    private long DilimiBirlestir(long bas, long son)
    {
        using var c = _db.Ac();
        using var islem = c.BeginTransaction();

        long Sorgu(string sql)
        {
            // Parametre kullanılırsa komut sp_executesql ile çalışır ve içinde oluşturulan geçici tablolar komut bitince silinir.
            // Sınırlar yalnızca sayıdır, doğrudan metne yazmak güvenlidir.
            sql = sql.Replace("@bas", bas.ToString()).Replace("@son", son.ToString());
            using var cmd = new SqlCommand(sql, c, islem) { CommandTimeout = 900 };
            return cmd.ExecuteNonQuery();
        }

        // a) yayınevleri
        Sorgu("""
            INSERT INTO Yayinevleri (Ad)
            SELECT DISTINCT e.Yayinevi FROM import.StageEdition e
            WHERE e.Sira BETWEEN @bas AND @son AND e.Yayinevi IS NOT NULL AND LTRIM(RTRIM(e.Yayinevi)) <> N''
              AND NOT EXISTS (SELECT 1 FROM Yayinevleri y WHERE y.Ad = e.Yayinevi)
            """);

        // b) kitaplar: her baskı ayrı kayıttır, yalnızca aynı baskı anahtarı tekrar eklenmez
        var eklenen = Sorgu("""
            INSERT INTO Kitaplar (KitapAdi, YazarAdi, YayinYili, Aciklama, EklenmeTarihi, KapakResmiUrl, SayfaSayisi, Dil,
                                  Isbn10, Isbn13, YayinTarihi, YayineviId, OpenLibraryEditionKey, OpenLibraryWorkKey)
            SELECT LEFT(e.Baslik, 400), N'', ISNULL(e.Yil, 0),
                   LEFT(COALESCE(NULLIF(e.Aciklama, N''), w.Aciklama, N''), 4000),
                   SYSDATETIME(),
                   CASE WHEN e.KapakId IS NOT NULL THEN 'https://covers.openlibrary.org/b/id/' + CAST(e.KapakId AS VARCHAR(20)) + '-M.jpg' END,
                   e.SayfaSayisi, e.Dil, e.Isbn10, e.Isbn13, e.YayinTarihi, y.Id, e.EditionKey, e.WorkKey
            FROM import.StageEdition e
            LEFT JOIN import.StageWork w ON w.WorkKey = e.WorkKey
            LEFT JOIN Yayinevleri y ON y.Ad = e.Yayinevi
            WHERE e.Sira BETWEEN @bas AND @son
              AND NOT EXISTS (SELECT 1 FROM Kitaplar k WHERE k.OpenLibraryEditionKey = e.EditionKey)
            """);

        // Dilimdeki kitaplar (yeni ya da önceden var olan, ilişkiler eksikse tamamlanır)
        Sorgu("""
            IF OBJECT_ID('tempdb..#ck') IS NOT NULL DROP TABLE #ck;
            SELECT k.Id AS KitapId, e.EditionKey, e.WorkKey
            INTO #ck
            FROM import.StageEdition e
            JOIN Kitaplar k ON k.OpenLibraryEditionKey = e.EditionKey
            WHERE e.Sira BETWEEN @bas AND @son;
            """);
        // İndeks, tablonun oluşturulduğu batch'ten ayrı bir komutta kurulmalıdır
        Sorgu("CREATE UNIQUE CLUSTERED INDEX IX_ck ON #ck (KitapId);");

        // b2) sonradan gelen eser açıklamaları mevcut kitaplara da işlenir (var olan açıklamanın üzerine yazılmaz)
        Sorgu("""
            UPDATE k SET Aciklama = LEFT(w.Aciklama, 4000)
            FROM Kitaplar k
            JOIN #ck ck ON ck.KitapId = k.Id
            JOIN import.StageWork w ON w.WorkKey = ck.WorkKey
            WHERE k.Aciklama = N'' AND w.Aciklama IS NOT NULL AND w.Aciklama <> N''
            """);

        // c) yazar ilişkileri: baskıda yazar yoksa eserdeki yazarlar kullanılır
        Sorgu("""
            INSERT INTO KitapYazarlari (KitapId, YazarId, Sira)
            SELECT ck.KitapId, y.Id, MIN(ey.Sira)
            FROM #ck ck
            JOIN import.StageEditionYazar ey ON ey.EditionKey = ck.EditionKey
            JOIN Yazarlar y ON y.OpenLibraryKey = ey.YazarKey
            WHERE NOT EXISTS (SELECT 1 FROM KitapYazarlari x WHERE x.KitapId = ck.KitapId AND x.YazarId = y.Id)
            GROUP BY ck.KitapId, y.Id
            """);
        Sorgu("""
            INSERT INTO KitapYazarlari (KitapId, YazarId, Sira)
            SELECT ck.KitapId, y.Id, MIN(wy.Sira)
            FROM #ck ck
            JOIN import.StageWorkYazar wy ON wy.WorkKey = ck.WorkKey
            JOIN Yazarlar y ON y.OpenLibraryKey = wy.YazarKey
            WHERE NOT EXISTS (SELECT 1 FROM import.StageEditionYazar ey WHERE ey.EditionKey = ck.EditionKey)
              AND NOT EXISTS (SELECT 1 FROM KitapYazarlari x WHERE x.KitapId = ck.KitapId AND x.YazarId = y.Id)
            GROUP BY ck.KitapId, y.Id
            """);

        // d) kitap kartlarında gösterilen yazar adı alanı: ilk 3 yazar, fazlası "ve diğerleri"
        Sorgu("""
            UPDATE k SET YazarAdi = ISNULL(LEFT(a.Adlar + CASE WHEN a.Sayi > 3 THEN N' ve diğerleri' ELSE N'' END, 400), N'Bilinmiyor')
            FROM Kitaplar k
            JOIN #ck ck ON ck.KitapId = k.Id
            LEFT JOIN (
                SELECT s.KitapId, MAX(s.Sayi) AS Sayi, STRING_AGG(CAST(s.Ad AS NVARCHAR(MAX)), N', ') WITHIN GROUP (ORDER BY s.Sira) AS Adlar
                FROM (
                    SELECT ky.KitapId, ky.Sira, y.Ad,
                           ROW_NUMBER() OVER (PARTITION BY ky.KitapId ORDER BY ky.Sira) AS Sn,
                           COUNT(*) OVER (PARTITION BY ky.KitapId) AS Sayi
                    FROM KitapYazarlari ky
                    JOIN Yazarlar y ON y.Id = ky.YazarId
                    WHERE ky.KitapId IN (SELECT KitapId FROM #ck)
                ) s
                WHERE s.Sn <= 3
                GROUP BY s.KitapId
            ) a ON a.KitapId = k.Id
            WHERE k.YazarAdi IN (N'', N'Bilinmiyor')
            """);

        // e) kategoriler (baskı konuları + eser konuları), "Tur" alanı için en öncelikli kategori
        Sorgu("""
            IF OBJECT_ID('tempdb..#kk') IS NOT NULL DROP TABLE #kk;
            SELECT ck.KitapId, kat.Id AS KategoriId, kat.Ad, MIN(m.Oncelik) AS Oncelik
            INTO #kk
            FROM #ck ck
            CROSS APPLY (
                SELECT Konu FROM import.StageKonu WHERE Kaynak = N'E' AND Anahtar = ck.EditionKey
                UNION
                SELECT Konu FROM import.StageKonu WHERE Kaynak = N'W' AND Anahtar = ck.WorkKey
            ) kn
            JOIN import.KonuEsleme m ON m.Konu = kn.Konu AND m.Kategori <> N'-'
            JOIN Kategoriler kat ON kat.Ad = m.Kategori
            GROUP BY ck.KitapId, kat.Id, kat.Ad;

            INSERT INTO KitapKategorileri (KitapId, KategoriId)
            SELECT KitapId, KategoriId FROM #kk q
            WHERE NOT EXISTS (SELECT 1 FROM KitapKategorileri x WHERE x.KitapId = q.KitapId AND x.KategoriId = q.KategoriId);

            UPDATE k SET Tur = s.Ad
            FROM Kitaplar k
            JOIN (SELECT KitapId, Ad, ROW_NUMBER() OVER (PARTITION BY KitapId ORDER BY Oncelik) AS sn FROM #kk) s
              ON s.KitapId = k.Id AND s.sn = 1
            WHERE k.Tur IS NULL;
            """);

        Sorgu("DROP TABLE #ck; IF OBJECT_ID('tempdb..#kk') IS NOT NULL DROP TABLE #kk;");
        islem.Commit();
        return eklenen;
    }

    // Staging'deki farklı konu başlıklarını merkezi kurallara göre kategoriye eşler
    private void KonuEslemeleriniHazirla()
    {
        var yeniKonular = new List<string>();
        using (var c = _db.Ac())
        using (var cmd = new SqlCommand("""
            SELECT DISTINCT k.Konu FROM import.StageKonu k
            WHERE NOT EXISTS (SELECT 1 FROM import.KonuEsleme m WHERE m.Konu = k.Konu)
            """, c) { CommandTimeout = 600 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                yeniKonular.Add(r.GetString(0));

        if (yeniKonular.Count == 0) return;

        var sira = KategoriEsleme.TumKategoriler.Select((k, i) => (k, i)).ToDictionary(x => x.k, x => x.i);
        var t = new DataTable();
        t.Columns.Add("Konu", typeof(string)); t.Columns.Add("Kategori", typeof(string)); t.Columns.Add("Oncelik", typeof(int));

        foreach (var konu in yeniKonular)
        {
            var eslesen = KategoriEsleme.Esle(konu);
            if (eslesen.Count == 0)
                t.Rows.Add(konu, "-", 999);   // eşleşmeyen konu bir daha taranmasın
            else
                foreach (var kategori in eslesen)
                    t.Rows.Add(konu, kategori, sira[kategori]);
        }

        using var baglanti = _db.Ac();
        using var islem = baglanti.BeginTransaction();
        _db.PartiYaz(islem, "import.KonuEsleme", t, new[] { "Konu", "Kategori" });
        islem.Commit();
        Gunluk.Bilgi($"birlestir: {yeniKonular.Count:N0} yeni konu başlığı kategoriye eşlendi.");
    }

    // ---------- yardımcılar ----------

    private bool AtlaTamamlandiysa(string ad, ImportLogKaydi log)
    {
        if (log.Durum == "Tamamlandi" && !_a.Yeniden)
        {
            Gunluk.Bilgi($"{ad}: önceki çalışmada tamamlanmış, atlanıyor (yeniden çalıştırmak için --yeniden).");
            return true;
        }
        return false;
    }

    // Dosya adı + boyutu: eksik indirilmiş dosya sonradan tamamlandığında farklı sürüm sayılır ve yeniden taranır
    private static string Surum(string dosya) => $"{Path.GetFileName(dosya)}@{new FileInfo(dosya).Length}";

    private static string OlDil(string kod) => kod switch { "tur" => "tr", "eng" => "en", _ => kod };

    private static EditionKaydi EditionKaydiDilleri(EditionKaydi k, string dil) => k;

    private HashSet<long> AnahtarKumesi(string sql)
    {
        var kume = new HashSet<long>();
        using var c = _db.Ac();
        using var cmd = new SqlCommand(sql, c) { CommandTimeout = 600 };
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var id = OlAyristirici.SayisalId(r.GetString(0));
            if (id >= 0) kume.Add(id);
        }
        return kume;
    }

    private long Calistir(string sql)
    {
        using var c = _db.Ac();
        using var cmd = new SqlCommand(sql, c) { CommandTimeout = 900 };
        return cmd.ExecuteNonQuery();
    }

    private void Ilerleme(string ad, ImportLogKaydi log, Stopwatch sw, Dictionary<string, long>? sayac, string? ek = null)
    {
        var hiz = log.OkunanSatir / Math.Max(1, sw.Elapsed.TotalSeconds);
        var detay = sayac != null ? string.Join(", ", sayac.Select(k => $"{k.Key}={k.Value:N0}")) : ek;
        Gunluk.Bilgi($"{ad}: satır {log.OkunanSatir:N0} ({hiz:N0} satır/sn), kabul {log.KabulEdilen:N0}, atlanan {log.Atlanan:N0}, hata {log.HataSayisi:N0}; {detay}; {Gunluk.Bellek()}");
    }

    private void Bosalt(ImportLogKaydi log, long satirNo, DataTable ed, DataTable yz, DataTable kn)
    {
        if (ed.Rows.Count == 0 && yz.Rows.Count == 0 && kn.Rows.Count == 0)
        {
            log.SonSatir = satirNo;
            return;
        }

        using var c = _db.Ac();
        using var islem = c.BeginTransaction();
        _db.PartiYaz(islem, "import.StageEdition", ed, new[] { "EditionKey" });
        _db.PartiYaz(islem, "import.StageEditionYazar", yz, new[] { "EditionKey", "YazarKey" });
        _db.PartiYaz(islem, "import.StageKonu", kn, new[] { "Kaynak", "Anahtar", "Konu" }, log.Id, satirNo);
        _db.LogSonSatir(islem, log.Id, satirNo);
        islem.Commit();

        log.SonSatir = satirNo;
        ed.Clear(); yz.Clear(); kn.Clear();
    }

    private void BosaltWorks(ImportLogKaydi log, long satirNo, DataTable w, DataTable wy, DataTable kn)
    {
        if (w.Rows.Count == 0 && wy.Rows.Count == 0 && kn.Rows.Count == 0)
        {
            log.SonSatir = satirNo;
            return;
        }

        using var c = _db.Ac();
        using var islem = c.BeginTransaction();
        _db.PartiYaz(islem, "import.StageWork", w, new[] { "WorkKey" });
        _db.PartiYaz(islem, "import.StageWorkYazar", wy, new[] { "WorkKey", "YazarKey" });
        _db.PartiYaz(islem, "import.StageKonu", kn, new[] { "Kaynak", "Anahtar", "Konu" });
        _db.LogSonSatir(islem, log.Id, satirNo);
        islem.Commit();

        log.SonSatir = satirNo;
        w.Clear(); wy.Clear(); kn.Clear();
    }

    private void BosaltAuthors(ImportLogKaydi log, long satirNo, DataTable a)
    {
        if (a.Rows.Count == 0)
        {
            log.SonSatir = satirNo;
            return;
        }

        using var c = _db.Ac();
        using var islem = c.BeginTransaction();
        _db.PartiYaz(islem, "import.StageAuthor", a, new[] { "AuthorKey" });
        _db.LogSonSatir(islem, log.Id, satirNo);
        islem.Commit();

        log.SonSatir = satirNo;
        a.Clear();
    }

    private static DataTable EditionTablosu()
    {
        var t = new DataTable();
        t.Columns.Add("EditionKey", typeof(string));
        t.Columns.Add("WorkKey", typeof(string));
        t.Columns.Add("Baslik", typeof(string));
        t.Columns.Add("Dil", typeof(string));
        t.Columns.Add("Yayinevi", typeof(string));
        t.Columns.Add("YayinTarihi", typeof(string));
        t.Columns.Add("SayfaSayisi", typeof(int));
        t.Columns.Add("Yil", typeof(int));
        t.Columns.Add("Isbn10", typeof(string));
        t.Columns.Add("Isbn13", typeof(string));
        t.Columns.Add("KapakId", typeof(long));
        t.Columns.Add("Aciklama", typeof(string));
        return t;
    }

    private static DataTable YazarTablosu(string _, string anahtarAdi, string yazarAdi)
    {
        var t = new DataTable();
        t.Columns.Add(anahtarAdi, typeof(string));
        t.Columns.Add(yazarAdi, typeof(string));
        t.Columns.Add("Sira", typeof(int));
        return t;
    }

    private static DataTable KonuTablosu()
    {
        var t = new DataTable();
        t.Columns.Add("Kaynak", typeof(string));
        t.Columns.Add("Anahtar", typeof(string));
        t.Columns.Add("Konu", typeof(string));
        return t;
    }
}
