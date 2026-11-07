using KitapYonetimSistemi.Data;

namespace KitapYonetimSistemi.Models
{
    // Kullanıcının bir kitabın belirli bir sayfasına düştüğü not. Gizli ya da herkese açık olabilir.
    public class KenarNotu
    {
        public const int EnFazlaUzunluk = 1000;
        public const int KitapBasinaEnFazla = 300;

        public int Id { get; set; }
        public int SayfaNo { get; set; }
        public string Metin { get; set; } = string.Empty;
        public bool HerkeseAcik { get; set; }
        public DateTime Tarih { get; set; }

        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }
    }
}
