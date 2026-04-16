using KitapYonetimSistemi.Data;

namespace KitapYonetimSistemi.Models
{
    public class Yorum
    {
        public int Id { get; set; }
        public string YorumMetni { get; set; } = string.Empty;
        public DateTime YorumTarihi { get; set; }

        // Foreign Keys
        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }
    }
}