using KitapYonetimSistemi.Data;

namespace KitapYonetimSistemi.Models
{
    public class KitapPuan
    {
        public int Id { get; set; }
        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }

        public int Puan { get; set; } // 1-5 arası
        public DateTime PuanTarihi { get; set; }
    }
}