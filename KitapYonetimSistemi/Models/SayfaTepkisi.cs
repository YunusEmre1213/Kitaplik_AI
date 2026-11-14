using KitapYonetimSistemi.Data;

namespace KitapYonetimSistemi.Models
{
    public enum TepkiTuru
    {
        Saskin = 1,
        Aglatti = 2,
        Guldurdu = 3,
        Sikici = 4
    }

    // Bir okurun kitabın belirli bir sayfasına bıraktığı tek dokunuşluk duygu işareti (kullanıcı başına sayfada bir tane)
    public class SayfaTepkisi
    {
        public int Id { get; set; }
        public int SayfaNo { get; set; }
        public TepkiTuru Tur { get; set; }
        public DateTime Tarih { get; set; }

        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }
    }
}
