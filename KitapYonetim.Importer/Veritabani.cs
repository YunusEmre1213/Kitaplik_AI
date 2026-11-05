using System.Data;
using Microsoft.Data.SqlClient;

namespace KitapYonetim.Importer;

// Staging tabloları, toplu yazma ve ImportLog (devam noktası) işlemleri
public sealed class Veritabani
{
    private readonly string _baglanti;

    public Veritabani(string baglanti) => _baglanti = baglanti;

    public SqlConnection Ac()
    {
        var c = new SqlConnection(_baglanti);
        c.Open();
        return c;
    }

    // Staging şeması ve tabloları. Final tablolar EF Core migration'ı ile oluşturulur.
    public void StagingHazirla()
    {
        const string ddl = """
            IF SCHEMA_ID('import') IS NULL EXEC('CREATE SCHEMA import');

            IF OBJECT_ID('import.StageEdition') IS NULL
            CREATE TABLE import.StageEdition (
                Sira            BIGINT IDENTITY(1,1) NOT NULL,
                EditionKey      VARCHAR(20)    NOT NULL PRIMARY KEY,
                WorkKey         VARCHAR(20)    NULL,
                Baslik          NVARCHAR(400)  NOT NULL,
                Dil             VARCHAR(10)    NOT NULL,
                Yayinevi        NVARCHAR(300)  NULL,
                YayinTarihi     NVARCHAR(50)   NULL,
                SayfaSayisi     INT            NULL,
                Yil             INT            NULL,
                Isbn10          VARCHAR(10)    NULL,
                Isbn13          VARCHAR(13)    NULL,
                KapakId         BIGINT         NULL,
                Aciklama        NVARCHAR(MAX)  NULL
            );
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StageEdition_Sira' AND object_id = OBJECT_ID('import.StageEdition'))
                CREATE UNIQUE INDEX IX_StageEdition_Sira ON import.StageEdition (Sira);

            IF OBJECT_ID('import.StageEditionYazar') IS NULL
            CREATE TABLE import.StageEditionYazar (
                EditionKey VARCHAR(20) NOT NULL,
                YazarKey   VARCHAR(20) NOT NULL,
                Sira       INT         NOT NULL,
                PRIMARY KEY (EditionKey, YazarKey)
            );

            IF OBJECT_ID('import.StageKonu') IS NULL
            CREATE TABLE import.StageKonu (
                Kaynak NVARCHAR(10)  NOT NULL,   -- 'E' = edition, 'W' = work
                Anahtar VARCHAR(20)  NOT NULL,
                Konu   NVARCHAR(200) NOT NULL,
                PRIMARY KEY (Kaynak, Anahtar, Konu)
            );

            IF OBJECT_ID('import.StageWork') IS NULL
            CREATE TABLE import.StageWork (
                WorkKey  VARCHAR(20)   NOT NULL PRIMARY KEY,
                Baslik   NVARCHAR(400) NULL,
                Aciklama NVARCHAR(MAX) NULL
            );

            IF OBJECT_ID('import.StageWorkYazar') IS NULL
            CREATE TABLE import.StageWorkYazar (
                WorkKey  VARCHAR(20) NOT NULL,
                YazarKey VARCHAR(20) NOT NULL,
                Sira     INT         NOT NULL,
                PRIMARY KEY (WorkKey, YazarKey)
            );

            IF OBJECT_ID('import.StageAuthor') IS NULL
            CREATE TABLE import.StageAuthor (
                AuthorKey VARCHAR(20)   NOT NULL PRIMARY KEY,
                Ad        NVARCHAR(300) NOT NULL
            );

            IF OBJECT_ID('import.KonuEsleme') IS NULL
            CREATE TABLE import.KonuEsleme (
                Konu     NVARCHAR(200) NOT NULL,
                Kategori NVARCHAR(100) NOT NULL,
                Oncelik  INT           NOT NULL,
                PRIMARY KEY (Konu, Kategori)
            );
            """;

        using var c = Ac();
        using var cmd = new SqlCommand(ddl, c);
        cmd.ExecuteNonQuery();
    }

    // Parti verisini önce geçici tabloya yazar, sonra hedefe "yoksa ekle" mantığıyla taşır.
    // Aynı parti iki kez yazılsa bile mükerrer kayıt oluşmaz. Devam noktası aynı işlemde güncellenir.
    public void PartiYaz(SqlTransaction islem, string hedefTablo, DataTable tablo, string[] anahtarSutunlari, long? logId = null, long? sonSatir = null)
    {
        if (tablo.Rows.Count == 0) return;

        var baglanti = islem.Connection!;
        var gecici = "#gecici_" + hedefTablo.Replace(".", "_");
        var sutunlar = tablo.Columns.Cast<DataColumn>().Select(x => $"[{x.ColumnName}]").ToList();
        var sutunListesi = string.Join(", ", sutunlar);
        var kosul = string.Join(" AND ", anahtarSutunlari.Select(k => $"h.[{k}] = g.[{k}]"));

        using (var olustur = new SqlCommand($"IF OBJECT_ID('tempdb..{gecici}') IS NOT NULL DROP TABLE {gecici}; SELECT TOP 0 {sutunListesi} INTO {gecici} FROM {hedefTablo};", baglanti, islem))
            olustur.ExecuteNonQuery();

        using (var bulk = new SqlBulkCopy(baglanti, SqlBulkCopyOptions.Default, islem) { DestinationTableName = gecici, BulkCopyTimeout = 600, BatchSize = 5000 })
        {
            foreach (var s in sutunlar)
            {
                var ad = s.Trim('[', ']');
                bulk.ColumnMappings.Add(ad, ad);
            }
            bulk.WriteToServer(tablo);
        }

        // Parti içinde aynı anahtar birden fazla kez geçebilir: yalnızca ilkini al
        var anahtarListesi = string.Join(", ", anahtarSutunlari.Select(k => $"[{k}]"));
        var ekle = $"""
            INSERT INTO {hedefTablo} ({sutunListesi})
            SELECT {string.Join(", ", sutunlar.Select(s => "g." + s))}
            FROM (
                SELECT *, ROW_NUMBER() OVER (PARTITION BY {anahtarListesi} ORDER BY (SELECT 1)) AS sn FROM {gecici}
            ) g
            WHERE g.sn = 1 AND NOT EXISTS (SELECT 1 FROM {hedefTablo} h WHERE {kosul});
            DROP TABLE {gecici};
            """;
        using (var cmd = new SqlCommand(ekle, baglanti, islem) { CommandTimeout = 600 })
            cmd.ExecuteNonQuery();

        if (logId.HasValue && sonSatir.HasValue)
            LogSonSatir(islem, logId.Value, sonSatir.Value);
    }

    // ---------- ImportLog ----------

    public ImportLogKaydi LogBaslat(string kaynak, string surum, bool yeniden)
    {
        using var c = Ac();

        if (!yeniden)
        {
            using var oku = new SqlCommand("""
                SELECT TOP 1 Id, Durum, SonSatir, OkunanSatir, KabulEdilen, Atlanan, HataSayisi
                FROM ImportLoglari WHERE Kaynak = @k AND Surum = @s ORDER BY Id DESC
                """, c);
            oku.Parameters.AddWithValue("@k", kaynak);
            oku.Parameters.AddWithValue("@s", surum);
            using var r = oku.ExecuteReader();
            if (r.Read())
            {
                var kayit = new ImportLogKaydi
                {
                    Id = r.GetInt32(0),
                    Durum = r.GetString(1),
                    SonSatir = r.GetInt64(2),
                    OkunanSatir = r.GetInt64(3),
                    KabulEdilen = r.GetInt64(4),
                    Atlanan = r.GetInt64(5),
                    HataSayisi = r.GetInt64(6)
                };
                // Tamamlanmış çalışma da döndürülür, çağıran atlar. Yarım kalan devam eder.
                return kayit;
            }
        }

        using var ekle = new SqlCommand("""
            INSERT INTO ImportLoglari (Kaynak, Surum, Baslangic, Durum, SonSatir, OkunanSatir, KabulEdilen, Atlanan, HataSayisi)
            OUTPUT INSERTED.Id VALUES (@k, @s, SYSDATETIME(), 'Devam', 0, 0, 0, 0, 0)
            """, c);
        ekle.Parameters.AddWithValue("@k", kaynak);
        ekle.Parameters.AddWithValue("@s", surum);
        return new ImportLogKaydi { Id = (int)ekle.ExecuteScalar()!, Durum = "Devam" };
    }

    public void LogSonSatir(SqlTransaction islem, long logId, long sonSatir)
    {
        using var cmd = new SqlCommand("UPDATE ImportLoglari SET SonSatir = @s WHERE Id = @id", islem.Connection!, islem);
        cmd.Parameters.AddWithValue("@s", sonSatir);
        cmd.Parameters.AddWithValue("@id", logId);
        cmd.ExecuteNonQuery();
    }

    public void LogGuncelle(ImportLogKaydi k, string durum, string? mesaj = null)
    {
        using var c = Ac();
        using var cmd = new SqlCommand("""
            UPDATE ImportLoglari
            SET Durum = @d, OkunanSatir = @o, KabulEdilen = @ke, Atlanan = @a, HataSayisi = @h, Mesaj = @m,
                SonSatir = CASE WHEN @ss > SonSatir THEN @ss ELSE SonSatir END,
                Bitis = CASE WHEN @d = 'Devam' THEN NULL ELSE SYSDATETIME() END
            WHERE Id = @id
            """, c);
        cmd.Parameters.AddWithValue("@d", durum);
        cmd.Parameters.AddWithValue("@o", k.OkunanSatir);
        cmd.Parameters.AddWithValue("@ke", k.KabulEdilen);
        cmd.Parameters.AddWithValue("@a", k.Atlanan);
        cmd.Parameters.AddWithValue("@h", k.HataSayisi);
        cmd.Parameters.AddWithValue("@m", (object?)mesaj ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ss", k.SonSatir);
        cmd.Parameters.AddWithValue("@id", k.Id);
        cmd.ExecuteNonQuery();
    }

    public T Skaler<T>(string sql)
    {
        using var c = Ac();
        using var cmd = new SqlCommand(sql, c) { CommandTimeout = 600 };
        var v = cmd.ExecuteScalar();
        return v == null || v is DBNull ? default! : (T)Convert.ChangeType(v, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }
}

public sealed class ImportLogKaydi
{
    public int Id { get; set; }
    public string Durum { get; set; } = "Devam";
    public long SonSatir { get; set; }
    public long OkunanSatir { get; set; }
    public long KabulEdilen { get; set; }
    public long Atlanan { get; set; }
    public long HataSayisi { get; set; }
}
