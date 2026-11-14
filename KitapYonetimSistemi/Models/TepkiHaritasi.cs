namespace KitapYonetimSistemi.Models
{
    public record TepkiDilimi(int Bas, int Son, int Toplam, TepkiTuru? Baskin, bool Kilitli, IReadOnlyDictionary<TepkiTuru, int> Sayilar);

    // Kitabın duygusal eğrisi. Spoiler olmaması için okurun ulaştığı sayfanın ötesi kilitli kalır.
    public class TepkiHaritasi
    {
        public const int EnFazlaDilim = 40;

        public int ToplamSayfa { get; init; }
        public int ToplamTepki { get; init; }
        public List<TepkiDilimi> Dilimler { get; init; } = new();

        // gorunurSayfa: okurun görebildiği son sayfa (bitirenler için int.MaxValue, ilerlemesi yoksa 0)
        public static TepkiHaritasi Olustur(IEnumerable<(int Sayfa, TepkiTuru Tur)> tepkiler, int? kitapSayfaSayisi, int gorunurSayfa)
        {
            var liste = tepkiler.Where(t => t.Sayfa >= 1).ToList();
            var toplam = Math.Max(kitapSayfaSayisi ?? 0, liste.Count == 0 ? 0 : liste.Max(t => t.Sayfa));
            if (toplam == 0)
                return new TepkiHaritasi();

            var dilimSayisi = Math.Min(EnFazlaDilim, toplam);
            var dilimBoyu = (int)Math.Ceiling(toplam / (double)dilimSayisi);
            var dilimler = new List<TepkiDilimi>();
            var gorunenToplam = 0;

            for (var i = 0; i < dilimSayisi; i++)
            {
                var bas = i * dilimBoyu + 1;
                var son = Math.Min(toplam, (i + 1) * dilimBoyu);
                // Dilimin ilk sayfasına ulaşılmadıysa dilim tamamen kilitli; tepkiler hiç hesaba katılmaz
                var kilitli = bas > gorunurSayfa;
                var sayilar = new Dictionary<TepkiTuru, int>();
                if (!kilitli)
                {
                    foreach (var t in liste.Where(t => t.Sayfa >= bas && t.Sayfa <= son && t.Sayfa <= gorunurSayfa))
                        sayilar[t.Tur] = sayilar.GetValueOrDefault(t.Tur) + 1;
                }

                var dilimToplam = sayilar.Values.Sum();
                gorunenToplam += dilimToplam;
                TepkiTuru? baskin = dilimToplam == 0 ? null : sayilar.OrderByDescending(x => x.Value).ThenBy(x => (int)x.Key).First().Key;
                dilimler.Add(new TepkiDilimi(bas, son, dilimToplam, baskin, kilitli, sayilar));
            }

            return new TepkiHaritasi { ToplamSayfa = toplam, ToplamTepki = gorunenToplam, Dilimler = dilimler };
        }
    }
}
