namespace KitapYonetimSistemi.Models
{
    // Bir kitapta okurların işaretlediği sayfaların yoğunluğu (herkese açık alıntılar + herkese açık kenar notları)
    public class IsiHaritasi
    {
        public const int EnFazlaDilim = 40;

        public int ToplamSayfa { get; init; }
        public int ToplamIsaret { get; init; }
        public List<IsiDilimi> Dilimler { get; init; } = new();
        public List<(int Sayfa, int Sayi)> EnCokIsaretlenen { get; init; } = new();

        // sayfalar: her işaret için bir sayfa numarası (aynı sayfa birden çok kez geçebilir)
        public static IsiHaritasi Olustur(IEnumerable<int> sayfalar, int? kitapSayfaSayisi)
        {
            var liste = sayfalar.Where(s => s >= 1).ToList();
            if (liste.Count == 0)
                return new IsiHaritasi { ToplamSayfa = kitapSayfaSayisi ?? 0 };

            // Kitabın sayfa sayısı bilinmiyorsa ya da işaretlerden küçükse, en yüksek işaretli sayfaya kadar çizilir
            var toplam = Math.Max(kitapSayfaSayisi ?? 0, liste.Max());
            var dilimSayisi = Math.Min(EnFazlaDilim, toplam);
            var dilimBoyu = (int)Math.Ceiling(toplam / (double)dilimSayisi);

            var sayimlar = new int[dilimSayisi];
            foreach (var s in liste)
                sayimlar[Math.Min(dilimSayisi - 1, (s - 1) / dilimBoyu)]++;

            var enCok = Math.Max(1, sayimlar.Max());
            var dilimler = new List<IsiDilimi>();
            for (var i = 0; i < dilimSayisi; i++)
            {
                var bas = i * dilimBoyu + 1;
                var son = Math.Min(toplam, (i + 1) * dilimBoyu);
                // 0 = işaret yok, 1-4 = göreli yoğunluk
                var seviye = sayimlar[i] == 0 ? 0 : Math.Max(1, (int)Math.Ceiling(sayimlar[i] * 4.0 / enCok));
                dilimler.Add(new IsiDilimi(bas, son, sayimlar[i], seviye));
            }

            return new IsiHaritasi
            {
                ToplamSayfa = toplam,
                ToplamIsaret = liste.Count,
                Dilimler = dilimler,
                EnCokIsaretlenen = liste.GroupBy(s => s)
                    .Select(g => (Sayfa: g.Key, Sayi: g.Count()))
                    .OrderByDescending(x => x.Sayi).ThenBy(x => x.Sayfa)
                    .Take(3).ToList()
            };
        }
    }

    public record IsiDilimi(int Bas, int Son, int Sayi, int Seviye);

    public class KenarNotlariViewModel
    {
        public int KitapId { get; set; }
        public int? KitapSayfaSayisi { get; set; }
        public string? KullaniciId { get; set; }
        public List<KenarNotu> Notlar { get; set; } = new();
        public IsiHaritasi Isi { get; set; } = new();
    }
}
