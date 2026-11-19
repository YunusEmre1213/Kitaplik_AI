namespace KitapYonetimSistemi.Models
{
    // Bir okurun kitaptaki durumu: kaçıncı sayfada, bitirdi mi, en son ne zaman okudu
    // AcikBirakti: okur "bıraktım" dedi (30 gün beklenmez); Neden: verdiyse sebebi
    public record OkurDurumu(int Sayfa, bool Bitirdi, DateTime SonEtkinlik, bool AcikBirakti = false, BirakmaNedeni? Neden = null);

    // Okurların bu kitabı nerede bıraktığına dair anonim, toplu istatistik.
    // "Bırakan" = bitirmemiş, ilerlemesi olan ve son 30 gündür (BirakmaGunu) hiç okumamış okur.
    public class BirakmaIstatistigi
    {
        public const int BirakmaGunu = 30;
        // Bu kadar karar vermiş okur (bitiren + bırakan) yokken gösterilmez; az veri yanıltır
        public const int EnAzKarar = 5;

        public int Bitiren { get; init; }
        public int Birakan { get; init; }
        public int DevamEden { get; init; }
        public int? MedyanSayfa { get; init; }
        public int? MedyanYuzde { get; init; }
        // Medyan sayfaya ulaşanların kaçta kaçı bitirdi (yüzde)
        public int? AsanlarinBitirmeYuzdesi { get; init; }
        // Bırakma nedenleri (yalnızca neden belirtenler); en çok seçilenden başlar
        public List<(BirakmaNedeni Neden, int Sayi)> Nedenler { get; init; } = new();
        public int NedenToplami => Nedenler.Sum(n => n.Sayi);
        // Tek tek kişiyi ele vermemek için en az bu kadar neden varken gösterilir
        public const int EnAzNeden = 3;
        public bool NedenYeterli => NedenToplami >= EnAzNeden;

        public int Karar => Bitiren + Birakan;
        public bool Yeterli => Karar >= EnAzKarar && Birakan > 0;
        public int BitirmeYuzdesi => Karar == 0 ? 0 : (int)Math.Round(Bitiren * 100.0 / Karar);

        public static BirakmaIstatistigi Hesapla(IEnumerable<OkurDurumu> okurlar, int? kitapSayfaSayisi, DateTime simdi)
        {
            var liste = okurlar.Where(o => o.Bitirdi || o.Sayfa > 0).ToList();
            var bitiren = liste.Where(o => o.Bitirdi).ToList();
            var birakan = liste.Where(o => !o.Bitirdi && (o.AcikBirakti || (simdi - o.SonEtkinlik).TotalDays >= BirakmaGunu)).ToList();
            var devam = liste.Count - bitiren.Count - birakan.Count;

            int? medyan = null, yuzde = null, asan = null;
            if (birakan.Count > 0)
            {
                var sirali = birakan.Select(o => o.Sayfa).OrderBy(s => s).ToList();
                medyan = sirali[(sirali.Count - 1) / 2];
                if (kitapSayfaSayisi is > 0)
                    yuzde = Math.Clamp((int)Math.Round(medyan.Value * 100.0 / kitapSayfaSayisi.Value), 0, 100);

                // Medyanı aşanlar arasında bitirenlerin oranı (bitirenler sayfa yerine tam kitabı okumuş sayılır)
                var asanlar = bitiren.Count + birakan.Count(o => o.Sayfa >= medyan.Value);
                asan = asanlar == 0 ? null : (int)Math.Round(bitiren.Count * 100.0 / asanlar);
            }

            return new BirakmaIstatistigi
            {
                Bitiren = bitiren.Count,
                Birakan = birakan.Count,
                DevamEden = devam,
                MedyanSayfa = medyan,
                MedyanYuzde = yuzde,
                AsanlarinBitirmeYuzdesi = asan,
                Nedenler = birakan.Where(o => o.Neden.HasValue)
                    .GroupBy(o => o.Neden!.Value)
                    .Select(g => (g.Key, g.Count()))
                    .OrderByDescending(x => x.Item2).ThenBy(x => x.Key)
                    .ToList()
            };
        }
    }
}
