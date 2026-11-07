namespace KitapYonetimSistemi.Models
{
    // Okuma günlüğü ve oturumlarından türetilen ritim bilgileri: seri, günlük tempo, tahmini bitiş.
    // Hesap saf bir fonksiyondur (veritabanına dokunmaz), bu yüzden kolayca test edilir.
    public class OkumaRitmi
    {
        public const int GrafikGunSayisi = 30;
        public const int TempoGunSayisi = 14;

        public int GuncelSeri { get; init; }
        public int EnUzunSeri { get; init; }
        public bool BugunOkundu { get; init; }
        public List<GunlukOkuma> SonGunler { get; init; } = new();
        public int SonGunlerToplamSayfa { get; init; }
        public double AktifGunBasinaSayfa { get; init; }
        public double GunlukTempo { get; init; }   // son 14 günün günlük ortalaması (sayfa/gün)
        public int BuHaftaDakika { get; init; }
        public int ToplamDakika { get; init; }
        public string? EnVerimliZaman { get; init; }
        public List<TahminliBitis> Tahminler { get; init; } = new();

        public bool VeriVar => SonGunlerToplamSayfa > 0 || ToplamDakika > 0 || GuncelSeri > 0;

        // gunlukler: kitaplık kaydına göre günlük girdileri (sayfa her zaman "şu an bulunulan sayfa")
        public static OkumaRitmi Hesapla(
            DateTime simdi,
            IEnumerable<GunlukGirdi> gunlukler,
            IEnumerable<OturumGirdi> oturumlar,
            IEnumerable<DevamEdenKitap> devamEdenler,
            IEnumerable<DateTime>? bitisTarihleri = null)
        {
            var bugun = simdi.Date;
            var gunlukListe = gunlukler.ToList();
            var oturumListe = oturumlar.Where(o => o.Dakika >= OkumaOturumu.EnAzDakika).ToList();

            // 1) Günlük sayfa: aynı kitapta ardışık girdiler arasındaki artış (ilk girdi baştan itibaren sayılır)
            var gunSayfa = new Dictionary<DateTime, int>();
            foreach (var kitap in gunlukListe.Where(g => g.Sayfa.HasValue).GroupBy(g => g.KitaplikId))
            {
                var onceki = 0;
                foreach (var g in kitap.OrderBy(x => x.Tarih))
                {
                    var fark = Math.Max(0, g.Sayfa!.Value - onceki);
                    onceki = Math.Max(onceki, g.Sayfa.Value);
                    if (fark > 0)
                        gunSayfa[g.Tarih.Date] = gunSayfa.GetValueOrDefault(g.Tarih.Date) + fark;
                }
            }

            // 2) Etkinlik günleri: günlük girdisi, en az bir dakikalık oturum ya da kitap bitirme
            var etkinlik = new HashSet<DateTime>(gunlukListe.Select(g => g.Tarih.Date));
            foreach (var o in oturumListe) etkinlik.Add(o.Baslangic.Date);
            if (bitisTarihleri != null)
                foreach (var t in bitisTarihleri) etkinlik.Add(t.Date);

            // 3) Seriler
            var bugunOkundu = etkinlik.Contains(bugun);
            var guncel = 0;
            // Bugün henüz okumadıysan seri dünden sayılır (gün bitmeden bozulmaz)
            var gun = bugunOkundu ? bugun : bugun.AddDays(-1);
            while (etkinlik.Contains(gun)) { guncel++; gun = gun.AddDays(-1); }

            var enUzun = 0; var gecici = 0; DateTime? onceki2 = null;
            foreach (var g in etkinlik.OrderBy(x => x))
            {
                gecici = onceki2.HasValue && (g - onceki2.Value).Days == 1 ? gecici + 1 : 1;
                enUzun = Math.Max(enUzun, gecici);
                onceki2 = g;
            }

            // 4) Son 30 gün
            var oturumDakika = oturumListe.GroupBy(o => o.Baslangic.Date).ToDictionary(g => g.Key, g => (int)Math.Round(g.Sum(x => x.Dakika)));
            var sonGunler = new List<GunlukOkuma>();
            for (var i = GrafikGunSayisi - 1; i >= 0; i--)
            {
                var d = bugun.AddDays(-i);
                sonGunler.Add(new GunlukOkuma(d, gunSayfa.GetValueOrDefault(d), oturumDakika.GetValueOrDefault(d)));
            }
            var sonToplam = sonGunler.Sum(g => g.Sayfa);
            var aktifGun = sonGunler.Count(g => g.Sayfa > 0);

            // 5) Tempo ve tahmini bitiş (son 14 günün ortalamasına göre)
            var tempoToplam = sonGunler.Skip(GrafikGunSayisi - TempoGunSayisi).Sum(g => g.Sayfa);
            var tempo = tempoToplam / (double)TempoGunSayisi;
            var tahminler = new List<TahminliBitis>();
            if (tempo > 0)
            {
                foreach (var k in devamEdenler)
                {
                    if (k.ToplamSayfa is not > 0) continue;
                    var kalan = k.ToplamSayfa.Value - k.MevcutSayfa;
                    if (kalan <= 0) continue;
                    var gunSayisi = (int)Math.Ceiling(kalan / tempo);
                    tahminler.Add(new TahminliBitis(k.KitaplikId, k.KitapAdi, kalan, bugun.AddDays(Math.Min(gunSayisi, 3650)), gunSayisi));
                }
            }

            // 6) Süre: bu hafta (pazartesiden) ve toplam
            var haftaBasi = bugun.AddDays(-(((int)bugun.DayOfWeek + 6) % 7));
            var buHafta = (int)Math.Round(oturumListe.Where(o => o.Baslangic.Date >= haftaBasi).Sum(o => o.Dakika));
            var toplamDk = (int)Math.Round(oturumListe.Sum(o => o.Dakika));

            return new OkumaRitmi
            {
                GuncelSeri = guncel,
                EnUzunSeri = enUzun,
                BugunOkundu = bugunOkundu,
                SonGunler = sonGunler,
                SonGunlerToplamSayfa = sonToplam,
                AktifGunBasinaSayfa = aktifGun > 0 ? Math.Round(sonToplam / (double)aktifGun, 1) : 0,
                GunlukTempo = Math.Round(tempo, 1),
                BuHaftaDakika = buHafta,
                ToplamDakika = toplamDk,
                EnVerimliZaman = EnVerimliZamanDilimi(oturumListe),
                Tahminler = tahminler.OrderBy(t => t.GunSayisi).ToList()
            };
        }

        // Oturumların süre ağırlıklı olarak en çok denk geldiği gün dilimi (en az 3 oturum varsa)
        public static string? EnVerimliZamanDilimi(IReadOnlyCollection<OturumGirdi> oturumlar)
        {
            if (oturumlar.Count < 3) return null;
            string Dilim(int saat) => saat switch
            {
                >= 5 and < 11 => "sabah",
                >= 11 and < 17 => "öğleden sonra",
                >= 17 and < 23 => "akşam",
                _ => "gece"
            };
            return oturumlar
                .GroupBy(o => Dilim(o.Baslangic.Hour))
                .OrderByDescending(g => g.Sum(x => x.Dakika))
                .First().Key;
        }
    }

    public record GunlukGirdi(int KitaplikId, DateTime Tarih, int? Sayfa);
    public record OturumGirdi(DateTime Baslangic, double Dakika);
    public record DevamEdenKitap(int KitaplikId, string KitapAdi, int MevcutSayfa, int? ToplamSayfa);
    public record GunlukOkuma(DateTime Gun, int Sayfa, int Dakika);
    public record TahminliBitis(int KitaplikId, string KitapAdi, int KalanSayfa, DateTime Tarih, int GunSayisi);
}
