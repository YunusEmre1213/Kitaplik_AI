namespace KitapYonetimSistemi.Models
{
    // Başlat/durdur ile kaydedilen okuma oturumu: ne zaman, kaç dakika, hangi sayfadan hangi sayfaya.
    // Kullanıcı, kitaplık kaydı üzerinden bilinir. Bitis boşsa oturum hâlâ açıktır.
    public class OkumaOturumu
    {
        // Bu süreden kısa oturumlar istatistiklerde sayılmaz (yanlışlıkla başlatma)
        public const int EnAzDakika = 1;

        public int Id { get; set; }
        public DateTime Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public int? BaslangicSayfa { get; set; }
        public int? BitisSayfa { get; set; }

        public int KullaniciKitaplikId { get; set; }
        public KullaniciKitaplik? KullaniciKitaplik { get; set; }

        public bool Acik => Bitis == null;

        public double Dakika => Bitis.HasValue ? Math.Max(0, (Bitis.Value - Baslangic).TotalMinutes) : 0;

        public int OkunanSayfa => BaslangicSayfa.HasValue && BitisSayfa.HasValue
            ? Math.Max(0, BitisSayfa.Value - BaslangicSayfa.Value)
            : 0;
    }
}
