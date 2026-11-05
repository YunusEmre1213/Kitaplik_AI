using System.IO.Compression;
using System.Text;
using KitapYonetim.Importer;
using Xunit;

namespace KitapYonetim.Importer.Tests;

public class DumpOkuyucuTests : IDisposable
{
    private readonly string _klasor = Path.Combine(Path.GetTempPath(), "dump-test-" + Guid.NewGuid().ToString("N"));

    public DumpOkuyucuTests() => Directory.CreateDirectory(_klasor);

    public void Dispose() => Directory.Delete(_klasor, true);

    private string Olustur(IEnumerable<string> satirlar, bool kes = false)
    {
        var yol = Path.Combine(_klasor, "dump.txt.gz");
        using (var dosya = File.Create(yol))
        using (var gzip = new GZipStream(dosya, CompressionLevel.SmallestSize))
        {
            var bytes = Encoding.UTF8.GetBytes(string.Join("\n", satirlar) + "\n");
            gzip.Write(bytes);
        }

        if (kes)
        {
            // Eksik indirilmiş dosyayı taklit etmek için dosyanın yarısını at
            var tum = File.ReadAllBytes(yol);
            File.WriteAllBytes(yol, tum[..(tum.Length / 2)]);
        }
        return yol;
    }

    private static string Satir(int n) => $"/type/edition\t/books/OL{n}M\t1\t2020-01-01T00:00:00\t{{\"title\": \"Kitap {n}\"}}";

    [Fact]
    public void Oku_TumSatirlariSirayla_BesSutunuAyirir()
    {
        var yol = Olustur(Enumerable.Range(1, 5).Select(Satir));

        var satirlar = DumpOkuyucu.Oku(yol, 0, _ => { }, CancellationToken.None).ToList();

        Assert.Equal(5, satirlar.Count);
        Assert.Equal(1, satirlar[0].SatirNo);
        Assert.Equal("/type/edition", satirlar[0].Tur);
        Assert.Equal("/books/OL1M", satirlar[0].Anahtar);
        Assert.Equal("{\"title\": \"Kitap 1\"}", satirlar[0].Json);
    }

    [Fact]
    public void Oku_KaldigiYerdenDevam_VerilenSatiriAtlar()
    {
        var yol = Olustur(Enumerable.Range(1, 10).Select(Satir));

        var satirlar = DumpOkuyucu.Oku(yol, 7, _ => { }, CancellationToken.None).ToList();

        Assert.Equal(new long[] { 8, 9, 10 }, satirlar.Select(s => s.SatirNo));
    }

    [Fact]
    public void Oku_EksikSutunluSatir_Atlanir()
    {
        var yol = Olustur(new[] { Satir(1), "bozuk satir", Satir(3) });

        var satirlar = DumpOkuyucu.Oku(yol, 0, _ => { }, CancellationToken.None).ToList();

        Assert.Equal(2, satirlar.Count);
        Assert.Equal(new long[] { 1, 3 }, satirlar.Select(s => s.SatirNo));
    }

    [Fact]
    public void Oku_KesikGzipDosyasi_HataFirlatmazOkunabilenKadarVerir()
    {
        var yol = Olustur(Enumerable.Range(1, 5000).Select(Satir), kes: true);

        var satirlar = DumpOkuyucu.Oku(yol, 0, _ => { }, CancellationToken.None).ToList();

        // Yarım dosya çökmeden okunur. Son satır yarım kalabileceği için tüm satırlar beklenmez.
        Assert.NotEmpty(satirlar);
        Assert.True(satirlar.Count < 5000);
        Assert.All(satirlar, s => Assert.StartsWith("/books/OL", s.Anahtar));
    }

    [Fact]
    public void Oku_IptalIstenirse_Durur()
    {
        var yol = Olustur(Enumerable.Range(1, 100).Select(Satir));
        using var iptal = new CancellationTokenSource();

        Assert.Throws<OperationCanceledException>(() =>
        {
            foreach (var s in DumpOkuyucu.Oku(yol, 0, _ => { }, iptal.Token))
                if (s.SatirNo == 3) iptal.Cancel();
        });
    }
}
