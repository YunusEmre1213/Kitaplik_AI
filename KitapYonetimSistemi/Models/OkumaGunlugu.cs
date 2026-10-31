namespace KitapYonetimSistemi.Models
{
    // Kitaplık kaydına bağlı ilerleme notu (hangi sayfaya gelindi, ne düşünüldü)
    public class OkumaGunlugu
    {
        public int Id { get; set; }
        public int? Sayfa { get; set; }
        public string? Not { get; set; }
        public DateTime Tarih { get; set; }

        public int KullaniciKitaplikId { get; set; }
        public KullaniciKitaplik? KullaniciKitaplik { get; set; }
    }
}
