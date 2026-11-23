namespace KitapYonetimSistemi.Models
{
    public enum SezonGorevDurumu
    {
        Yaklasan,   // sezon henüz başlamadı
        Gorev,      // okunacak bölüm var
        Tamam,      // bugüne kadarki bölümler okundu, yenisi yarın açılır
        Bitirdi     // sezonun tüm bölümleri tamam
    }

    // Sezonda bu okur için bugünün görevi: hangi bölüm, kaç bölüm geride
    public record SezonGorevi(SezonGorevDurumu Durum, int Bolum, int Geride);

    // "Bugün" ekranının saf kuralları (veritabanına dokunmaz)
    public static class BugunKurallari
    {
        public const int KapsulPenceresiGun = 14;
        public const int OneriPenceresiGun = 7;

        public static string Selam(int saat) => saat switch
        {
            >= 5 and < 12 => "Günaydın",
            >= 12 and < 18 => "İyi günler",
            >= 18 and < 22 => "İyi akşamlar",
            _ => "İyi geceler"
        };

        public static SezonGorevi SezonGorevi(SezonTakvimi takvim, int sonBolum, DateTime simdi)
        {
            if (sonBolum >= takvim.BolumSayisi) return new SezonGorevi(SezonGorevDurumu.Bitirdi, 0, 0);
            var bugun = takvim.BugunBolum(simdi);
            if (bugun == 0) return new SezonGorevi(SezonGorevDurumu.Yaklasan, 0, 0);
            if (sonBolum >= bugun) return new SezonGorevi(SezonGorevDurumu.Tamam, 0, 0);
            // Geride: okunmamış açık bölüm sayısı (sıradaki dahil)
            return new SezonGorevi(SezonGorevDurumu.Gorev, sonBolum + 1, bugun - sonBolum);
        }

        // Bir zaman kapsülü yakın zamanda mı açıldı? (açılış anı: tarih gelince ya da kitap bitince, hangisi önceyse)
        public static bool KapsulYeniAcildi(bool bitinceAc, DateTime? acilis, bool bitirdi, DateTime? bitisTarihi, DateTime simdi)
        {
            if (!KapsulKurali.Acik(bitinceAc, acilis, bitirdi, simdi)) return false;
            if (!KapsulKurali.KapsulMu(bitinceAc, acilis)) return false;

            var anlar = new List<DateTime>();
            if (acilis.HasValue && simdi >= acilis.Value) anlar.Add(acilis.Value);
            if (bitinceAc && bitirdi && bitisTarihi.HasValue) anlar.Add(bitisTarihi.Value);
            if (anlar.Count == 0) return false;

            return (simdi - anlar.Min()).TotalDays <= KapsulPenceresiGun;
        }
    }
}
