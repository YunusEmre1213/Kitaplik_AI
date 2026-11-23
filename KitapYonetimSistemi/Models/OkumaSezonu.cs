using KitapYonetimSistemi.Data;

namespace KitapYonetimSistemi.Models
{
    // Topluluğun aynı kitabı belirli bir tempoda, dizi bölümleri gibi birlikte okuduğu sezon.
    // Her bölüm bir günün sayfa dilimidir; üye bölümü okuyunca o bölümün sohbeti ona açılır.
    public class OkumaSezonu
    {
        public const int AdUzunlugu = 80;
        public const int EnAzGun = 5;
        public const int EnCokGun = 90;
        public const int EnCokBaslangicGunu = 60;
        public const int KullaniciBasinaEnCokAcik = 2;

        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        // Sezonun ilk bölümünün açıldığı gün (saat önemsiz)
        public DateTime Baslangic { get; set; }
        public int GunSayisi { get; set; }
        public DateTime Tarih { get; set; }

        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }

        public string OlusturanId { get; set; } = string.Empty;
        public ApplicationUser? Olusturan { get; set; }

        public ICollection<SezonUyesi> Uyeler { get; set; } = new List<SezonUyesi>();
        public ICollection<SezonMesaji> Mesajlar { get; set; } = new List<SezonMesaji>();
    }

    public class SezonUyesi
    {
        public int Id { get; set; }
        public DateTime KatilmaTarihi { get; set; }
        // Okuduğunu onayladığı son bölüm (0 = henüz hiçbirini okumadı)
        public int SonBolum { get; set; }

        public int SezonId { get; set; }
        public OkumaSezonu? Sezon { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }
    }

    public class SezonMesaji
    {
        public const int EnFazlaUzunluk = 1000;

        public int Id { get; set; }
        public int Bolum { get; set; }
        public string Metin { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }

        public int SezonId { get; set; }
        public OkumaSezonu? Sezon { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }
    }

    public enum SezonDurumu
    {
        Yaklasan,
        Surmekte,
        Bitti
    }

    // Sezonun bölüm takvimi ve spoiler kuralları. Saf hesap, veritabanına dokunmaz.
    public class SezonTakvimi
    {
        public int ToplamSayfa { get; }
        public int GunSayisi { get; }
        public DateTime Baslangic { get; }
        public int GunlukSayfa { get; }
        public int BolumSayisi { get; }

        public SezonTakvimi(DateTime baslangic, int gunSayisi, int toplamSayfa)
        {
            Baslangic = baslangic.Date;
            GunSayisi = Math.Max(1, gunSayisi);
            ToplamSayfa = Math.Max(1, toplamSayfa);
            GunlukSayfa = (int)Math.Ceiling(ToplamSayfa / (double)GunSayisi);
            BolumSayisi = (int)Math.Ceiling(ToplamSayfa / (double)GunlukSayfa);
        }

        public int SayfaBasi(int bolum) => (Math.Clamp(bolum, 1, BolumSayisi) - 1) * GunlukSayfa + 1;
        public int SayfaSonu(int bolum) => Math.Min(ToplamSayfa, Math.Clamp(bolum, 1, BolumSayisi) * GunlukSayfa);
        public DateTime BolumGunu(int bolum) => Baslangic.AddDays(Math.Clamp(bolum, 1, BolumSayisi) - 1);

        // Bugün açık olan son bölüm; sezon başlamadıysa 0
        public int BugunBolum(DateTime simdi)
        {
            if (simdi.Date < Baslangic) return 0;
            return Math.Min(BolumSayisi, (simdi.Date - Baslangic).Days + 1);
        }

        public SezonDurumu Durum(DateTime simdi)
        {
            if (simdi.Date < Baslangic) return SezonDurumu.Yaklasan;
            return simdi.Date > BolumGunu(BolumSayisi) ? SezonDurumu.Bitti : SezonDurumu.Surmekte;
        }

        // Üye yalnızca sıradaki bölümü, o bölüm açıldıysa onaylayabilir
        public bool OnaylanabilirMi(int sonBolum, DateTime simdi) => sonBolum + 1 <= BugunBolum(simdi);

        // Bir bölümün sohbetini görebilir mi? Üye o bölümü okuduysa ya da kendi mesajıysa görür.
        public static bool SohbetAcik(int mesajBolumu, int okuyucuSonBolum, bool yazarMi) =>
            yazarMi || mesajBolumu <= okuyucuSonBolum;

        // Üye bir bölüme yalnızca okuduğu bölümler arasında yazabilir
        public bool YazabilirMi(int bolum, int sonBolum) => bolum >= 1 && bolum <= Math.Min(sonBolum, BolumSayisi);
    }

    public class SezonBolumGorunumu
    {
        public int No { get; set; }
        public int SayfaBasi { get; set; }
        public int SayfaSonu { get; set; }
        public DateTime Gun { get; set; }
        public bool Acildi { get; set; }
        public bool Bugun { get; set; }
        public int OkuyanSayisi { get; set; }
        public int MesajSayisi { get; set; }
        public bool SohbetAcik { get; set; }
        public List<SezonMesajGorunumu> Mesajlar { get; set; } = new();
    }

    public class SezonMesajGorunumu
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Metin { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }
        public bool Benim { get; set; }
    }

    public class SezonViewModel
    {
        public OkumaSezonu Sezon { get; set; } = null!;
        public SezonTakvimi Takvim { get; set; } = null!;
        public SezonDurumu Durum { get; set; }
        public int BugunBolum { get; set; }
        public int UyeSayisi { get; set; }
        public bool Uye { get; set; }
        public int BenimSonBolum { get; set; }
        public bool Onaylanabilir { get; set; }
        public int TamamlayanSayisi { get; set; }
        public List<SezonBolumGorunumu> Bolumler { get; set; } = new();
    }
}
