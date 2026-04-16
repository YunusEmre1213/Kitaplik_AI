namespace KitapYonetimSistemi.Models
{
    public class KullaniciYonetimViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public DateTime KayitTarihi { get; set; }
        public int KitaplikSayisi { get; set; }
        public int YorumSayisi { get; set; }
    }
}