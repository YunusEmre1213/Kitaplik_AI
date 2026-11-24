namespace KitapYonetimSistemi.Models
{
    // Bir okurun bir kitabı belirli bir tarihe kadar bitireceğine arkadaşlarına verdiği söz.
    // Tanık olanlar ilerlemeyi (yalnızca sayfa ve yüzde) görür ve nazik hazır mesajlarla dürtebilir.
    // Kullanıcılara veritabanı bağı yoktur (silme akışında elle temizlenir).
    public class OkumaSozu
    {
        public const int KodUzunlugu = 8;

        public int Id { get; set; }
        public string Kod { get; set; } = string.Empty;
        public string Not { get; set; } = string.Empty;
        public DateTime Bitis { get; set; }
        public DateTime Tarih { get; set; }
        // Söz verildiği andaki sayfa: gereken günlük tempoyu hesaplamak için
        public int BaslangicSayfa { get; set; }
        public int UzatmaSayisi { get; set; }

        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }

        public string SahipId { get; set; } = string.Empty;

        public ICollection<SozTanigi> Tanikler { get; set; } = new List<SozTanigi>();
        public ICollection<SozDurtmesi> Durtmeler { get; set; } = new List<SozDurtmesi>();
    }

    public class SozTanigi
    {
        public int Id { get; set; }
        public DateTime Tarih { get; set; }

        public int SozId { get; set; }
        public OkumaSozu? Soz { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
    }

    public enum DurtmeTuru
    {
        NasilGidiyor = 1,
        SanaGuveniyorum = 2,
        BirBolumDahaMi = 3,
        Tebrikler = 4
    }

    public class SozDurtmesi
    {
        public int Id { get; set; }
        public DurtmeTuru Tur { get; set; }
        public DateTime Tarih { get; set; }

        public int SozId { get; set; }
        public OkumaSozu? Soz { get; set; }

        public string GonderenId { get; set; } = string.Empty;
    }

    public enum SozDurumu
    {
        Surmekte,
        Tuttu,
        GecTuttu,
        SuresiDoldu
    }

    // Okuma sözünün saf kuralları (veritabanına dokunmaz)
    public static class SozKurali
    {
        public const int NotUzunlugu = 300;
        public const int EnFazlaGun = 180;
        public const int EnFazlaTanik = 5;
        public const int EnFazlaUzatma = 1;
        public const int UzatmaEnFazlaGun = 30;
        public const int SahipBasinaEnFazlaAcik = 3;
        public const int DurtmeAraligiSaat = 24;
        public const int DurtmeGostermeGunu = 7;

        public static string DurtmeMetni(DurtmeTuru t) => t switch
        {
            DurtmeTuru.NasilGidiyor => "Nasıl gidiyor?",
            DurtmeTuru.SanaGuveniyorum => "Sana güveniyorum 💪",
            DurtmeTuru.BirBolumDahaMi => "Bugün bir bölüm daha?",
            _ => "Tebrikler, sözünü tuttun! 🎉"
        };

        // Söz bitiş gününün sonuna kadar geçerlidir; kitap o güne kadar bittiyse tutulmuştur
        public static SozDurumu Durum(DateTime bitis, bool bitirdi, DateTime? kitapBitisTarihi, DateTime simdi)
        {
            var son = bitis.Date.AddDays(1);
            if (bitirdi)
                return (kitapBitisTarihi ?? simdi) < son ? SozDurumu.Tuttu : SozDurumu.GecTuttu;
            return simdi >= son ? SozDurumu.SuresiDoldu : SozDurumu.Surmekte;
        }

        // Bugün dahil kalan gün sayısı eksi bir: bugün son gün ise 0
        public static int KalanGun(DateTime bitis, DateTime simdi) => Math.Max(0, (bitis.Date - simdi.Date).Days);

        // Sözü tutmak için bugünden itibaren günde okunması gereken sayfa (bugün dahil)
        public static int GunlukGereken(int mevcutSayfa, int? toplamSayfa, DateTime bitis, DateTime simdi)
        {
            if (toplamSayfa is not > 0) return 0;
            var kalan = toplamSayfa.Value - mevcutSayfa;
            if (kalan <= 0) return 0;
            return (int)Math.Ceiling(kalan / (double)(KalanGun(bitis, simdi) + 1));
        }

        public static int Yuzde(int mevcutSayfa, int? toplamSayfa) =>
            toplamSayfa is > 0 ? Math.Clamp((int)Math.Round(mevcutSayfa * 100.0 / toplamSayfa.Value), 0, 100) : 0;

        public static bool TarihGecerliMi(DateTime bitis, DateTime simdi) =>
            bitis.Date > simdi.Date && bitis.Date <= simdi.Date.AddDays(EnFazlaGun);

        public static bool UzatmaGecerliMi(DateTime eskiBitis, DateTime yeniBitis, int uzatmaSayisi, DateTime simdi) =>
            uzatmaSayisi < EnFazlaUzatma
            && yeniBitis.Date > eskiBitis.Date
            && yeniBitis.Date <= eskiBitis.Date.AddDays(UzatmaEnFazlaGun)
            && yeniBitis.Date >= simdi.Date;

        public static bool DurtebilirMi(DateTime? sonDurtme, DateTime simdi) =>
            sonDurtme == null || (simdi - sonDurtme.Value).TotalHours >= DurtmeAraligiSaat;

        // Tebrik yalnızca sözü tutulmuş (zamanında ya da geç) bir söze, diğer mesajlar yalnızca sürmekte olan söze gönderilebilir
        public static bool DurtmeUygunMu(DurtmeTuru tur, SozDurumu durum) => tur == DurtmeTuru.Tebrikler
            ? durum is SozDurumu.Tuttu or SozDurumu.GecTuttu
            : durum is SozDurumu.Surmekte or SozDurumu.SuresiDoldu;
    }

    public class SozKarti
    {
        public OkumaSozu Soz { get; set; } = null!;
        public string SahipAdi { get; set; } = string.Empty;
        public SozDurumu Durum { get; set; }
        public int KalanGun { get; set; }
        public int Yuzde { get; set; }
        public int MevcutSayfa { get; set; }
        public int? ToplamSayfa { get; set; }
        public int GunlukGereken { get; set; }
        public int TanikSayisi { get; set; }
    }
}
