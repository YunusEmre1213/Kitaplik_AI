using System.IO.Compression;
using System.Text;

namespace KitapYonetim.Importer;

// Open Library dump satırı: type \t key \t revision \t last_modified \t json
public readonly record struct DumpSatiri(long SatirNo, string Tur, string Anahtar, string Json);

// GZIP dosyasını belleğe yüklemeden satır satır okur.
public static class DumpOkuyucu
{
    // atlanacakSatir kadar satırı okumadan geçer (kaldığı yerden devam için).
    // Dosya eksik indirilmişse (kesik gzip) bu bir hata sayılmaz, okuma sessizce biter.
    public static IEnumerable<DumpSatiri> Oku(string dosya, long atlanacakSatir, Action<string> uyari, CancellationToken iptal)
    {
        using var dosyaAkisi = new FileStream(dosya, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20, FileOptions.SequentialScan);
        using var gzip = new GZipStream(dosyaAkisi, CompressionMode.Decompress);
        using var okuyucu = new StreamReader(gzip, Encoding.UTF8, false, 1 << 20);

        long satirNo = 0;
        while (true)
        {
            iptal.ThrowIfCancellationRequested();

            string? satir;
            try
            {
                satir = okuyucu.ReadLine();
            }
            catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException)
            {
                uyari($"Dosya beklenmedik şekilde bitti (eksik indirilmiş olabilir), {satirNo} satır okundu.");
                satir = null;
            }

            if (satir == null)
                yield break;

            satirNo++;
            if (satirNo <= atlanacakSatir)
                continue;

            // Beş sütun beklenir. Eksik bir satır (kesik dosyanın son satırı gibi) atlanır.
            var p1 = satir.IndexOf('\t');
            if (p1 < 0) continue;
            var p2 = satir.IndexOf('\t', p1 + 1);
            if (p2 < 0) continue;
            var p3 = satir.IndexOf('\t', p2 + 1);
            if (p3 < 0) continue;
            var p4 = satir.IndexOf('\t', p3 + 1);
            if (p4 < 0) continue;

            yield return new DumpSatiri(
                satirNo,
                satir[..p1],
                satir[(p1 + 1)..p2],
                satir[(p4 + 1)..]);
        }
    }
}
