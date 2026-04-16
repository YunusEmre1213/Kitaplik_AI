using KitapYonetimSistemi.Data;

namespace KitapYonetimSistemi.Models
{
    public class KullaniciKitaplik
    {
        public int Id { get; set; }
        public bool HerkesGorebilir { get; set; }
        public DateTime EklenmeTarihi { get; set; }

        // YENİ: Okuma durumu ve tarihleri
        public OkumaDurumu OkumaDurumu { get; set; } = OkumaDurumu.Okunacak;
        public DateTime? OkumayaBaslamaTarihi { get; set; }
        public DateTime? OkumaBitisTarihi { get; set; }

        // Foreign Keys
        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }

        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }
    }
}