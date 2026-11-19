namespace KitapYonetimSistemi.Models
{
    // Okuma oturumunun başında ve sonunda seçilen ruh hali
    public enum OkumaRuhHali
    {
        Gergin = 1,
        Yorgun = 2,
        Merakli = 3,
        Huzurlu = 4,
        Mutlu = 5
    }

    public static class OkumaRuhHaliMetni
    {
        public static string Etiket(OkumaRuhHali r) => r switch
        {
            OkumaRuhHali.Gergin => "Gergin",
            OkumaRuhHali.Yorgun => "Yorgun",
            OkumaRuhHali.Merakli => "Meraklı",
            OkumaRuhHali.Huzurlu => "Huzurlu",
            _ => "Mutlu"
        };

        public static string Emoji(OkumaRuhHali r) => r switch
        {
            OkumaRuhHali.Gergin => "😣",
            OkumaRuhHali.Yorgun => "😴",
            OkumaRuhHali.Merakli => "🤔",
            OkumaRuhHali.Huzurlu => "😌",
            _ => "😊"
        };
    }

    // Kapanmış bir oturumun ruh hali haritası için gereken bilgileri
    public record RuhHaliOturumu(DateTime Baslangic, OkumaRuhHali? Basi, OkumaRuhHali? Sonu, string? Tur);

    // Okurun oturumlarına bıraktığı ruh hallerinden çıkarılan içgörüler. Saf hesap, veritabanına dokunmaz.
    public class RuhHaliHaritasi
    {
        // Bu kadar ruh hali girilmeden içgörü gösterilmez
        public const int EnAzOturum = 5;
        // Bir kalıbı (saat dilimi, tür) söylemek için gereken en az oturum
        public const int EnAzKalip = 3;

        public int ToplamOturum { get; init; }
        public bool Yeterli => ToplamOturum >= EnAzOturum;
        public List<(OkumaRuhHali Ruh, int Sayi)> BaslangicDagilimi { get; init; } = new();
        // Hem başı hem sonu girilmiş oturum sayısı
        public int CiftliOturum { get; init; }
        public int IyilesenYuzde { get; init; }
        public string? EnIyiZaman { get; init; }
        public List<(OkumaRuhHali Ruh, string Tur)> RuhVeTur { get; init; } = new();

        // Ruh halinin olumluluğu: okuma sonrası değişimi ölçmek için (1 en kötü, 5 en iyi)
        public static int Puan(OkumaRuhHali r) => r switch
        {
            OkumaRuhHali.Gergin => 1,
            OkumaRuhHali.Yorgun => 2,
            OkumaRuhHali.Merakli => 3,
            OkumaRuhHali.Huzurlu => 4,
            _ => 5
        };

        public static string ZamanDilimi(DateTime t) => t.Hour switch
        {
            >= 5 and < 12 => "sabah",
            >= 12 and < 18 => "öğleden sonra",
            >= 18 and < 22 => "akşam",
            _ => "gece"
        };

        public static RuhHaliHaritasi Hesapla(IEnumerable<RuhHaliOturumu> oturumlar)
        {
            var liste = oturumlar.Where(o => o.Basi.HasValue || o.Sonu.HasValue).ToList();
            var baslayan = liste.Where(o => o.Basi.HasValue).ToList();

            var dagilim = baslayan.GroupBy(o => o.Basi!.Value)
                .Select(g => (Ruh: g.Key, Sayi: g.Count()))
                .OrderByDescending(x => x.Sayi).ThenBy(x => x.Ruh)
                .ToList();

            var ciftli = liste.Where(o => o.Basi.HasValue && o.Sonu.HasValue).ToList();
            var iyilesen = ciftli.Count == 0 ? 0 : (int)Math.Round(ciftli.Count(o => Puan(o.Sonu!.Value) > Puan(o.Basi!.Value)) * 100.0 / ciftli.Count);

            // En iyi zaman: okuma sonunda ruh halinin ortalaması en yüksek dilim
            var enIyi = liste.Where(o => o.Sonu.HasValue)
                .GroupBy(o => ZamanDilimi(o.Baslangic))
                .Where(g => g.Count() >= EnAzKalip)
                .Select(g => (Dilim: g.Key, Ort: g.Average(o => Puan(o.Sonu!.Value))))
                .OrderByDescending(x => x.Ort)
                .Select(x => x.Dilim)
                .FirstOrDefault();

            // Her başlangıç ruh hali için en çok okunan tür
            var ruhTur = baslayan.Where(o => !string.IsNullOrWhiteSpace(o.Tur))
                .GroupBy(o => o.Basi!.Value)
                .Select(g =>
                {
                    var tur = g.GroupBy(o => o.Tur!.Trim()).Select(t => (Tur: t.Key, Sayi: t.Count()))
                        .OrderByDescending(t => t.Sayi).ThenBy(t => t.Tur).First();
                    return (Ruh: g.Key, tur.Tur, tur.Sayi);
                })
                .Where(x => x.Sayi >= EnAzKalip)
                .OrderBy(x => x.Ruh)
                .Select(x => (x.Ruh, x.Tur))
                .ToList();

            return new RuhHaliHaritasi
            {
                ToplamOturum = liste.Count,
                BaslangicDagilimi = dagilim,
                CiftliOturum = ciftli.Count,
                IyilesenYuzde = iyilesen,
                EnIyiZaman = enIyi,
                RuhVeTur = ruhTur
            };
        }
    }
}
