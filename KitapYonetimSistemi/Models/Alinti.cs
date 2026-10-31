using KitapYonetimSistemi.Data;

namespace KitapYonetimSistemi.Models
{
    // Kullanıcının bir kitaptan paylaştığı alıntı
    public class Alinti
    {
        public const int EnFazlaUzunluk = 1000;

        public int Id { get; set; }
        public string Metin { get; set; } = string.Empty;
        public int? SayfaNo { get; set; }
        public DateTime Tarih { get; set; }

        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }

        public ICollection<AlintiBegeni> Begeniler { get; set; } = new List<AlintiBegeni>();
    }

    public class AlintiBegeni
    {
        public int Id { get; set; }

        public int AlintiId { get; set; }
        public Alinti? Alinti { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }
    }
}
