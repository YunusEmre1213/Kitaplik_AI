namespace KitapYonetimSistemi.Models
{
    // Open Library gibi dış kaynaklardan toplu aktarılan kitap verisi için ilişkili tablolar.
    // Kitap bir "baskıdır": aynı eserin farklı baskıları ayrı kayıt olarak tutulur.

    public class Yazar
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string? OpenLibraryKey { get; set; }

        public ICollection<KitapYazar> Kitaplar { get; set; } = new List<KitapYazar>();
    }

    public class KitapYazar
    {
        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }

        public int YazarId { get; set; }
        public Yazar? Yazar { get; set; }

        // Yazarın kitaptaki sırası (0 = ilk yazar)
        public int Sira { get; set; }
    }

    public class Kategori
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;

        public ICollection<KitapKategori> Kitaplar { get; set; } = new List<KitapKategori>();
    }

    public class KitapKategori
    {
        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }

        public int KategoriId { get; set; }
        public Kategori? Kategori { get; set; }
    }

    public class Yayinevi
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
    }

    // Toplu aktarım çalışmalarının kaydı. Kesinti sonrası kaldığı yerden devam etmek için de kullanılır.
    public class ImportLog
    {
        public int Id { get; set; }
        public string Kaynak { get; set; } = string.Empty;       // örn. "editions", "works", "authors", "birlestirme"
        public string Surum { get; set; } = string.Empty;        // dump dosyasının sürümü/adı
        public DateTime Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public string Durum { get; set; } = "Devam";             // Devam, Tamamlandi, Hata
        public long SonSatir { get; set; }                       // en son güvenle yazılan satır (devam noktası)
        public long OkunanSatir { get; set; }
        public long KabulEdilen { get; set; }
        public long Atlanan { get; set; }
        public long HataSayisi { get; set; }
        public string? Mesaj { get; set; }
    }
}
