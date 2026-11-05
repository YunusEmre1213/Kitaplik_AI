namespace KitapYonetim.Importer;

// Komut satırı seçenekleri
public sealed class Ayarlar
{
    public string? EditionsDosyasi { get; set; }
    public string? WorksDosyasi { get; set; }
    public string? AuthorsDosyasi { get; set; }

    // Aktarılacak diller (Open Library dil kodları)
    public List<string> Diller { get; set; } = new() { "tur", "eng" };

    // Dil başına en fazla kaç baskı alınacağı (0 = sınırsız)
    public int DilBasinaMax { get; set; }

    // Çalıştırılacak aşamalar
    public HashSet<string> Asamalar { get; set; } = new() { "editions", "works", "authors", "birlestir" };

    // Tamamlanmış aşamaları da yeniden çalıştır
    public bool Yeniden { get; set; }

    public int Parti { get; set; } = 2000;
    public string? Baglanti { get; set; }
    public string HataLogYolu { get; set; } = "import-hatalari.log";

    public static Ayarlar Oku(string[] args)
    {
        var a = new Ayarlar();
        for (var i = 0; i < args.Length; i++)
        {
            string Deger() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} için değer eksik.");

            switch (args[i])
            {
                case "--editions": a.EditionsDosyasi = Deger(); break;
                case "--works": a.WorksDosyasi = Deger(); break;
                case "--authors": a.AuthorsDosyasi = Deger(); break;
                case "--diller": a.Diller = Deger().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(); break;
                case "--dil-basina-max": a.DilBasinaMax = int.Parse(Deger()); break;
                case "--asamalar": a.Asamalar = Deger().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase); break;
                case "--parti": a.Parti = int.Parse(Deger()); break;
                case "--baglanti": a.Baglanti = Deger(); break;
                case "--hata-log": a.HataLogYolu = Deger(); break;
                case "--yeniden": a.Yeniden = true; break;
                case "--yardim": case "-h": case "--help": throw new ArgumentException("yardim");
                default: throw new ArgumentException($"Bilinmeyen seçenek: {args[i]}");
            }
        }
        return a;
    }

    public const string Yardim = """
        Open Library toplu veri aktarıcısı

        Kullanım:
          dotnet run --project KitapYonetim.Importer -- [seçenekler]

        Seçenekler:
          --editions <dosya.txt.gz>   Open Library editions dump dosyası
          --works <dosya.txt.gz>      Open Library works dump dosyası
          --authors <dosya.txt.gz>    Open Library authors dump dosyası
          --diller tur,eng            Aktarılacak diller (varsayılan: tur,eng)
          --dil-basina-max <N>        Dil başına en fazla N baskı (varsayılan: sınırsız)
          --asamalar a,b,c            editions,works,authors,birlestir (varsayılan: hepsi)
          --parti <N>                 Toplu yazma parti boyutu (varsayılan: 2000)
          --baglanti "<cs>"           Bağlantı dizesi (varsayılan: user-secrets / ortam değişkeni)
          --hata-log <yol>            Hatalı kayıtların yazılacağı dosya
          --yeniden                   Tamamlanmış aşamaları da yeniden çalıştır

        Kesintiden sonra aynı komutu tekrar çalıştırmak kaldığı yerden devam eder.
        """;
}
