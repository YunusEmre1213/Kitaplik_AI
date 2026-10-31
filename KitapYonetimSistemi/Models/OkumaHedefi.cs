using KitapYonetimSistemi.Data;

namespace KitapYonetimSistemi.Models
{
    // Kullanıcının bir yıl için belirlediği okuma hedefi (kaç kitap)
    public class OkumaHedefi
    {
        public int Id { get; set; }
        public int Yil { get; set; }
        public int KitapSayisi { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }
    }
}
