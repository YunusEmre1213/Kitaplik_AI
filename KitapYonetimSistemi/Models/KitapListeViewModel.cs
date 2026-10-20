namespace KitapYonetimSistemi.Models
{
    public class KitapListeViewModel
    {
        public const int SayfaBoyutu = 12;

        public List<Kitap> Kitaplar { get; set; } = new();
        public string? Arama { get; set; }
        public string? Tur { get; set; }
        public string Siralama { get; set; } = "yeni";
        public int Sayfa { get; set; } = 1;
        public int ToplamSayfa { get; set; }
        public int ToplamKayit { get; set; }
    }
}
