namespace KitapYonetimSistemi.Models
{
    public record IkizTepkisi(int KitapId, int SayfaNo, TepkiTuru Tur);

    // İki okurun aynı yerde aynı şeyi hissettiği bir sayfa penceresi (okuyan ikisi de o sayfayı okuduğu için spoiler içermez)
    public record IkizOrnegi(int KitapId, int SayfaBasi, int SayfaSonu, TepkiTuru Tur);

    public record IkizAdayi(string KullaniciId, int OrtakKitap, int OrtakPencere, int AyniPencere, int Uyum, List<IkizOrnegi> Ornekler);

    // Okurları yıldızla değil, sayfa tepkileriyle eşleştirir: aynı kitapta aynı sayfa penceresinde aynı duyguyu bırakanlar.
    // Saf hesap, veritabanına dokunmaz.
    public static class OkumaIkizi
    {
        // Sayfa tepkileri tam sayfada nadiren çakışır; bu yüzden 10 sayfalık pencerelerle karşılaştırılır
        public const int PencereBoyu = 10;
        // Bundan az ortak veriyle yapılan eşleşme şans eseri olabilir, gösterilmez
        public const int EnAzOrtakPencere = 8;
        public const int EnAzOrtakKitap = 2;
        public const int EnCokOrnek = 3;

        private static Dictionary<(int Kitap, int Pencere), HashSet<TepkiTuru>> Pencereler(IEnumerable<IkizTepkisi> tepkiler)
        {
            var sonuc = new Dictionary<(int, int), HashSet<TepkiTuru>>();
            foreach (var t in tepkiler.Where(t => t.SayfaNo >= 1))
            {
                var anahtar = (t.KitapId, (t.SayfaNo - 1) / PencereBoyu);
                if (!sonuc.TryGetValue(anahtar, out var kume))
                    sonuc[anahtar] = kume = new HashSet<TepkiTuru>();
                kume.Add(t.Tur);
            }
            return sonuc;
        }

        public static List<IkizAdayi> Hesapla(IEnumerable<IkizTepkisi> benim, IReadOnlyDictionary<string, List<IkizTepkisi>> digerleri)
        {
            var benimPencereler = Pencereler(benim);
            var adaylar = new List<IkizAdayi>();

            foreach (var (kullaniciId, tepkiler) in digerleri)
            {
                var onun = Pencereler(tepkiler);
                var ortak = benimPencereler.Keys.Where(onun.ContainsKey).ToList();
                var ortakKitap = ortak.Select(k => k.Kitap).Distinct().Count();
                if (ortak.Count < EnAzOrtakPencere || ortakKitap < EnAzOrtakKitap)
                    continue;

                var ayni = ortak.Where(k => benimPencereler[k].Overlaps(onun[k])).ToList();
                // Az veride şişmesin diye Laplace düzeltmesi: (ayni + 1) / (ortak + 2)
                var uyum = (int)Math.Round(100.0 * (ayni.Count + 1) / (ortak.Count + 2));

                // Örnekler farklı kitaplardan seçilir, her kitaptan ilk eşleşen pencere
                var ornekler = ayni.OrderBy(k => k.Kitap).ThenBy(k => k.Pencere)
                    .GroupBy(k => k.Kitap).Select(g => g.First())
                    .Take(EnCokOrnek)
                    .Select(k => new IkizOrnegi(
                        k.Kitap,
                        k.Pencere * PencereBoyu + 1,
                        (k.Pencere + 1) * PencereBoyu,
                        benimPencereler[k].Intersect(onun[k]).OrderBy(t => t).First()))
                    .ToList();

                adaylar.Add(new IkizAdayi(kullaniciId, ortakKitap, ortak.Count, ayni.Count, uyum, ornekler));
            }

            return adaylar.OrderByDescending(a => a.Uyum).ThenByDescending(a => a.OrtakPencere).ThenBy(a => a.KullaniciId, StringComparer.Ordinal).ToList();
        }
    }
}
