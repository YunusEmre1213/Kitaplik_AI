namespace KitapYonetimSistemi.Models
{
    // API'nin dışarıya açtığı modeller. Entity sınıfları doğrudan döndürülmez.

    public class SayfaliSonuc<T>
    {
        public int Sayfa { get; set; }
        public int Boyut { get; set; }
        public int Toplam { get; set; }
        public int ToplamSayfa { get; set; }
        public List<T> Ogeler { get; set; } = new();
    }

    public class YazarDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
    }

    public class KategoriDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
    }

    public class KitapOzetDto
    {
        public int Id { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Yazar { get; set; } = string.Empty;
        public string KapakUrl { get; set; } = string.Empty;
        public int YayinYili { get; set; }
        public string? Dil { get; set; }
        public string? Tur { get; set; }
        public int? SayfaSayisi { get; set; }
        public string? Isbn13 { get; set; }
    }

    public class KitapDetayDto : KitapOzetDto
    {
        public string Aciklama { get; set; } = string.Empty;
        public string? Isbn10 { get; set; }
        public string? YayinTarihi { get; set; }
        public string? Yayinevi { get; set; }
        public string? OpenLibraryEditionKey { get; set; }
        public string? OpenLibraryWorkKey { get; set; }
        public List<YazarDto> Yazarlar { get; set; } = new();
        public List<KategoriDto> Kategoriler { get; set; } = new();
        public List<DigerBaskiDto> DigerBaskilar { get; set; } = new();
    }

    // Aynı eserin diğer baskıları
    public class DigerBaskiDto
    {
        public int Id { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string? Dil { get; set; }
        public int YayinYili { get; set; }
        public string? Yayinevi { get; set; }
    }
}
