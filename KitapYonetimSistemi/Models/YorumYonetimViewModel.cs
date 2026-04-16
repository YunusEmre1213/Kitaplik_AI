namespace KitapYonetimSistemi.Models
{
    public class YorumYonetimViewModel
    {
        public int YorumId { get; set; }
        public string YorumMetni { get; set; }
        public DateTime YorumTarihi { get; set; }

        // Kitap Bilgileri
        public int KitapId { get; set; }
        public string KitapAdi { get; set; }

        // Kullanıcı Bilgileri
        public string KullaniciId { get; set; }
        public string KullaniciEmail { get; set; }
    }
}