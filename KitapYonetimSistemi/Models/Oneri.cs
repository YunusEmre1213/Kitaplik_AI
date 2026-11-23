namespace KitapYonetimSistemi.Models
{
    // Bir okurun bir kitabı kişisel notla başkalarına önerdiği bağlantı. Bağlantıyı açıp kabul eden her okur zincire bir halka ekler.
    // Kullanıcılara veritabanı bağı yoktur (silinen okurun halkası zincirde "silinmiş okur" olarak kalır).
    public class Oneri
    {
        public const int NotUzunlugu = 300;
        public const int KodUzunlugu = 8;
        public const int GunlukEnCok = 20;

        public int Id { get; set; }
        public string Kod { get; set; } = string.Empty;
        public string Not { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }

        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }

        // Öneren okur; silinmişse boş
        public string? GonderenId { get; set; }

        // Önerenin bu kitabı kendisine ulaştıran kabulü: zincirdeki bir önceki halka (kaynak ise boş)
        public int? OncekiKabulId { get; set; }
        public OneriKabul? OncekiKabul { get; set; }

        public ICollection<OneriKabul> Kabuller { get; set; } = new List<OneriKabul>();
    }

    public class OneriKabul
    {
        public int Id { get; set; }
        public DateTime KabulTarihi { get; set; }

        public int OneriId { get; set; }
        public Oneri? Oneri { get; set; }

        // Kitap başına tek kabul için Oneri.KitapId'nin kopyası (veritabanı bağı yok)
        public int KitapId { get; set; }
        public string KullaniciId { get; set; } = string.Empty;
    }

    public record OneriKaydi(int Id, string? GonderenId, int? OncekiKabulId);
    public record KabulKaydi(int Id, int OneriId, string KullaniciId, bool Bitirdi);

    public class OneriDugumu
    {
        public int KabulId { get; init; }
        public string KullaniciId { get; init; } = string.Empty;
        public bool Bitirdi { get; init; }
        public List<OneriDugumu> Cocuklar { get; init; } = new();
    }

    public record OneriOzeti(int Ulasan, int Bitiren, int Derinlik);

    // Öneri zincirinin ağaç ve yol hesapları. Saf hesap, veritabanına dokunmaz.
    public static class OneriZinciri
    {
        // Bir önerinin altındaki tüm halkalar: kabul edenler, onların önerdikleri, onların kabul edenleri...
        public static List<OneriDugumu> Agac(int oneriId, IReadOnlyCollection<OneriKaydi> oneriler, IReadOnlyCollection<KabulKaydi> kabuller)
        {
            var ziyaret = new HashSet<int>();
            List<OneriDugumu> Coz(IEnumerable<int> oneriIdleri)
            {
                var kume = oneriIdleri.ToHashSet();
                var sonuc = new List<OneriDugumu>();
                foreach (var k in kabuller.Where(k => kume.Contains(k.OneriId)).OrderBy(k => k.Id))
                {
                    if (!ziyaret.Add(k.Id)) continue; // döngüye karşı koruma
                    var altOneriler = oneriler.Where(o => o.OncekiKabulId == k.Id).Select(o => o.Id).ToList();
                    sonuc.Add(new OneriDugumu
                    {
                        KabulId = k.Id,
                        KullaniciId = k.KullaniciId,
                        Bitirdi = k.Bitirdi,
                        Cocuklar = altOneriler.Count == 0 ? new() : Coz(altOneriler)
                    });
                }
                return sonuc;
            }
            return Coz(new[] { oneriId });
        }

        public static OneriOzeti Ozet(IEnumerable<OneriDugumu> kokler)
        {
            int ulasan = 0, bitiren = 0;
            int Gez(IEnumerable<OneriDugumu> dugumler)
            {
                var derin = 0;
                foreach (var d in dugumler)
                {
                    ulasan++;
                    if (d.Bitirdi) bitiren++;
                    derin = Math.Max(derin, 1 + Gez(d.Cocuklar));
                }
                return derin;
            }
            var derinlik = Gez(kokler);
            return new OneriOzeti(ulasan, bitiren, derinlik);
        }

        // Önerinin kaynağından bu öneriyi gönderene kadar olan okurlar (sıralı). Silinmiş okur null olur.
        public static List<string?> Yol(int oneriId, IReadOnlyCollection<OneriKaydi> oneriler, IReadOnlyCollection<KabulKaydi> kabuller)
        {
            var yol = new List<string?>();
            var gorulen = new HashSet<int>();
            var sira = oneriler.FirstOrDefault(o => o.Id == oneriId);
            while (sira != null && gorulen.Add(sira.Id))
            {
                yol.Add(sira.GonderenId);
                var onceki = sira.OncekiKabulId == null ? null : kabuller.FirstOrDefault(k => k.Id == sira.OncekiKabulId);
                sira = onceki == null ? null : oneriler.FirstOrDefault(o => o.Id == onceki.OneriId);
            }
            yol.Reverse();
            return yol;
        }
    }
}
