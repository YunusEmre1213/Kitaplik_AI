using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;
using Microsoft.EntityFrameworkCore;

namespace KitapYonetimSistemi.Services
{
    // Giriş yapmış okurun "Bugün" ekranı: okuduğu kitaplar, sezon görevleri, odalar, öneri zincirindeki gelişmeler ve açılan zaman kapsülleri
    public class BugunServisi
    {
        private const int EnCokOkuyan = 6;
        private const int EnCokOda = 4;
        private const int EnCokOneri = 5;
        private const int EnCokKapsul = 2;
        private const int YenilerSayisi = 8;

        private readonly ApplicationDbContext _context;
        private readonly SozServisi _sozServisi;

        public BugunServisi(ApplicationDbContext context, SozServisi sozServisi)
        {
            _context = context;
            _sozServisi = sozServisi;
        }

        public async Task<BugunViewModel> HazirlaAsync(string kullaniciId, string? kullaniciAdi, DateTime simdi)
        {
            var vm = new BugunViewModel
            {
                Selam = BugunKurallari.Selam(simdi.Hour),
                Ad = string.IsNullOrEmpty(kullaniciAdi) ? "okur" : kullaniciAdi.Split('@')[0]
            };

            await RitimiDoldur(vm, kullaniciId, simdi);

            vm.Okuyorlar = await _context.KullaniciKitapliklari.AsNoTracking()
                .Include(kk => kk.Kitap)
                .Where(kk => kk.KullaniciId == kullaniciId && kk.OkumaDurumu == OkumaDurumu.Okuyorum && kk.BirakmaTarihi == null)
                .OrderByDescending(kk => kk.OkumayaBaslamaTarihi)
                .Take(EnCokOkuyan)
                .ToListAsync();

            vm.AktifOturumKitap = await _context.OkumaOturumlari.AsNoTracking()
                .Where(o => o.Bitis == null && o.KullaniciKitaplik!.KullaniciId == kullaniciId)
                .Select(o => o.KullaniciKitaplik!.Kitap!.KitapAdi)
                .FirstOrDefaultAsync();

            await SezonlariDoldur(vm, kullaniciId, simdi);
            var adIdleri = new HashSet<string>();
            await OdalariDoldur(vm, kullaniciId, adIdleri);
            await OnerileriDoldur(vm, kullaniciId, simdi, adIdleri);
            await KapsulleriDoldur(vm, kullaniciId, simdi);
            await SozleriDoldur(vm, kullaniciId, simdi);
            await AdlariCoz(vm, adIdleri);

            vm.Yeniler = await _context.Kitaplar.AsNoTracking()
                .Where(k => k.KapakResmiUrl != null && k.KapakResmiUrl != "")
                .OrderByDescending(k => k.Aciklama != "")
                .ThenByDescending(k => k.Id)
                .Take(YenilerSayisi)
                .ToListAsync();

            return vm;
        }

        private async Task RitimiDoldur(BugunViewModel vm, string kullaniciId, DateTime simdi)
        {
            var gunlukler = await _context.OkumaGunlukleri.AsNoTracking()
                .Where(g => g.KullaniciKitaplik!.KullaniciId == kullaniciId)
                .Select(g => new GunlukGirdi(g.KullaniciKitaplikId, g.Tarih, g.Sayfa))
                .ToListAsync();
            var oturumlar = (await _context.OkumaOturumlari.AsNoTracking()
                .Where(o => o.Bitis != null && o.KullaniciKitaplik!.KullaniciId == kullaniciId)
                .Select(o => new { o.Baslangic, Bitis = o.Bitis!.Value })
                .ToListAsync())
                .Select(o => new OturumGirdi(o.Baslangic, (o.Bitis - o.Baslangic).TotalMinutes))
                .ToList();
            var bitisTarihleri = await _context.KullaniciKitapliklari.AsNoTracking()
                .Where(kk => kk.KullaniciId == kullaniciId && kk.OkumaDurumu == OkumaDurumu.Okundu && kk.OkumaBitisTarihi != null)
                .Select(kk => kk.OkumaBitisTarihi!.Value)
                .ToListAsync();

            var ritim = OkumaRitmi.Hesapla(simdi, gunlukler, oturumlar, Array.Empty<DevamEdenKitap>(), bitisTarihleri);
            vm.Seri = ritim.GuncelSeri;
            vm.BugunOkundu = ritim.BugunOkundu;
            var bugun = ritim.SonGunler.LastOrDefault();
            vm.BugunSayfa = bugun?.Sayfa ?? 0;
            vm.BugunDakika = bugun?.Dakika ?? 0;
        }

        private async Task SezonlariDoldur(BugunViewModel vm, string kullaniciId, DateTime simdi)
        {
            var uyelikler = await _context.SezonUyeleri.AsNoTracking()
                .Include(u => u.Sezon!).ThenInclude(s => s.Kitap)
                .Where(u => u.KullaniciId == kullaniciId)
                .ToListAsync();

            var liste = new List<BugunSezon>();
            foreach (var u in uyelikler)
            {
                var s = u.Sezon!;
                var takvim = new SezonTakvimi(s.Baslangic, s.GunSayisi, s.Kitap?.SayfaSayisi ?? 1);
                var gorev = BugunKurallari.SezonGorevi(takvim, u.SonBolum, simdi);
                // Bitirilmiş sezonlar ve süresi dolmuş sezonlar ana ekranı doldurmaz
                if (gorev.Durum == SezonGorevDurumu.Bitirdi || takvim.Durum(simdi) == SezonDurumu.Bitti)
                    continue;

                var bolum = gorev.Durum == SezonGorevDurumu.Gorev ? gorev.Bolum : Math.Max(1, takvim.BugunBolum(simdi));
                liste.Add(new BugunSezon
                {
                    SezonId = s.Id,
                    Ad = s.Ad,
                    KitapAdi = s.Kitap?.KitapAdi ?? string.Empty,
                    Gorev = gorev,
                    SayfaBasi = takvim.SayfaBasi(bolum),
                    SayfaSonu = takvim.SayfaSonu(bolum),
                    BolumSayisi = takvim.BolumSayisi,
                    Baslangic = s.Baslangic
                });
            }

            // Önce yapılacak görevi olanlar, sonra tamamlananlar, en sonda yaklaşanlar
            vm.Sezonlar = liste.OrderBy(x => (int)x.Gorev.Durum switch { 1 => 0, 2 => 1, _ => 2 }).ThenBy(x => x.Baslangic).ToList();
        }

        private async Task OdalariDoldur(BugunViewModel vm, string kullaniciId, HashSet<string> adIdleri)
        {
            var odaIdleri = await _context.OdaUyeleri.AsNoTracking()
                .Where(u => u.KullaniciId == kullaniciId).Select(u => u.OdaId).ToListAsync();
            if (odaIdleri.Count == 0) return;

            var odalar = await _context.OkumaOdalari.AsNoTracking()
                .Include(o => o.Kitap).Include(o => o.Uyeler)
                .Where(o => odaIdleri.Contains(o.Id))
                .OrderByDescending(o => o.Tarih)
                .Take(EnCokOda)
                .ToListAsync();
            var gosterilenIdler = odalar.Select(o => o.Id).ToList();
            var kitapIdleri = odalar.Select(o => o.KitapId).Distinct().ToList();
            var uyeIdleri = odalar.SelectMany(o => o.Uyeler.Select(u => u.KullaniciId)).Distinct().ToList();

            var ilerlemeler = await _context.KullaniciKitapliklari.AsNoTracking()
                .Where(kk => kitapIdleri.Contains(kk.KitapId) && uyeIdleri.Contains(kk.KullaniciId))
                .Select(kk => new { kk.KitapId, kk.KullaniciId, Sayfa = kk.MevcutSayfa ?? 0, Bitti = kk.OkumaDurumu == OkumaDurumu.Okundu })
                .ToListAsync();

            // Son mesajlar: odaların en yeni mesajlarından her oda için biri (metin gösterilmez, spoiler olmasın)
            var sonMesajlar = (await _context.OdaMesajlari.AsNoTracking()
                .Where(m => gosterilenIdler.Contains(m.OdaId))
                .OrderByDescending(m => m.Tarih)
                .Take(200)
                .Select(m => new { m.OdaId, m.KullaniciId, m.Tarih })
                .ToListAsync())
                .GroupBy(m => m.OdaId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var o in odalar)
            {
                int Sayfa(string kid)
                {
                    var i = ilerlemeler.FirstOrDefault(x => x.KitapId == o.KitapId && x.KullaniciId == kid);
                    if (i == null) return 0;
                    return i.Bitti ? Math.Max(i.Sayfa, o.Kitap?.SayfaSayisi ?? i.Sayfa) : i.Sayfa;
                }

                var benim = Sayfa(kullaniciId);
                var ilerideki = o.Uyeler.Where(u => u.KullaniciId != kullaniciId).Select(u => Sayfa(u.KullaniciId)).Where(s => s > benim).ToList();
                var oda = new BugunOda
                {
                    OdaId = o.Id,
                    Ad = o.Ad,
                    KitapAdi = o.Kitap?.KitapAdi ?? string.Empty,
                    BenimSayfam = benim,
                    IlerideKisi = ilerideki.Count,
                    EnIlerideSayfa = ilerideki.Count == 0 ? 0 : ilerideki.Max()
                };
                if (sonMesajlar.TryGetValue(o.Id, out var m))
                {
                    oda.SonMesajTarihi = m.Tarih;
                    oda.SonMesajYazari = m.KullaniciId == kullaniciId ? "sen" : m.KullaniciId; // kimlik, sonra ada çevrilir
                    if (m.KullaniciId != kullaniciId) adIdleri.Add(m.KullaniciId);
                }
                vm.Odalar.Add(oda);
            }
        }

        private async Task OnerileriDoldur(BugunViewModel vm, string kullaniciId, DateTime simdi, HashSet<string> adIdleri)
        {
            var since = simdi.AddDays(-BugunKurallari.OneriPenceresiGun);
            var kabuller = await _context.OneriKabulleri.AsNoTracking()
                .Include(k => k.Oneri!).ThenInclude(o => o.Kitap)
                .Where(k => k.Oneri!.GonderenId == kullaniciId && k.KabulTarihi >= since)
                .OrderByDescending(k => k.KabulTarihi)
                .Take(EnCokOneri)
                .ToListAsync();

            foreach (var k in kabuller)
            {
                adIdleri.Add(k.KullaniciId);
                vm.Oneriler.Add(new BugunOneri
                {
                    OneriId = k.OneriId,
                    KitapAdi = k.Oneri?.Kitap?.KitapAdi ?? string.Empty,
                    KabulEden = k.KullaniciId, // kimlik, sonra ada çevrilir
                    Tarih = k.KabulTarihi
                });
            }
        }

        private async Task KapsulleriDoldur(BugunViewModel vm, string kullaniciId, DateTime simdi)
        {
            var notlar = await _context.KenarNotlari.AsNoTracking()
                .Include(n => n.Kitap)
                .Where(n => n.KullaniciId == kullaniciId && (n.KapsulBitinceAc || n.KapsulAcilisTarihi != null))
                .ToListAsync();
            if (notlar.Count == 0) return;

            var kitapIdleri = notlar.Select(n => n.KitapId).Distinct().ToList();
            var bitirmeler = (await _context.KullaniciKitapliklari.AsNoTracking()
                .Where(kk => kk.KullaniciId == kullaniciId && kitapIdleri.Contains(kk.KitapId) && kk.OkumaDurumu == OkumaDurumu.Okundu)
                .Select(kk => new { kk.KitapId, kk.OkumaBitisTarihi })
                .ToListAsync())
                .ToDictionary(x => x.KitapId, x => x.OkumaBitisTarihi);

            vm.Kapsuller = notlar
                .Where(n => BugunKurallari.KapsulYeniAcildi(n.KapsulBitinceAc, n.KapsulAcilisTarihi,
                    bitirmeler.ContainsKey(n.KitapId), bitirmeler.GetValueOrDefault(n.KitapId), simdi))
                .OrderByDescending(n => n.Tarih)
                .Take(EnCokKapsul)
                .Select(n => new BugunKapsul
                {
                    KitapId = n.KitapId,
                    KitapAdi = n.Kitap?.KitapAdi ?? string.Empty,
                    SayfaNo = n.SayfaNo,
                    Metin = n.Metin,
                    Yazildi = n.Tarih
                })
                .ToList();
        }

        // Verdiğim ve tanık olduğum açık sözler (yakın zamanda tutulanlar da kutlanır)
        private async Task SozleriDoldur(BugunViewModel vm, string kullaniciId, DateTime simdi)
        {
            var taniklikIdleri = await _context.SozTanikleri.AsNoTracking()
                .Where(t => t.KullaniciId == kullaniciId).Select(t => t.SozId).ToListAsync();
            var sozler = await _context.OkumaSozleri.AsNoTracking()
                .Include(s => s.Kitap).Include(s => s.Tanikler)
                .Where(s => s.SahipId == kullaniciId || taniklikIdleri.Contains(s.Id))
                .ToListAsync();
            if (sozler.Count == 0) return;

            var kartlar = await _sozServisi.KartlariOlusturAsync(sozler, simdi);
            var pencere = simdi.AddDays(-SozKurali.DurtmeGostermeGunu);
            var benimIdler = sozler.Where(s => s.SahipId == kullaniciId).Select(s => s.Id).ToList();
            var destekler = (await _context.SozDurtmeleri.AsNoTracking()
                .Where(d => benimIdler.Contains(d.SozId) && d.Tarih >= pencere)
                .Select(d => d.SozId).ToListAsync())
                .GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

            vm.Sozler = sozler
                .Select(s => kartlar[s.Id])
                .Where(k => k.Durum is SozDurumu.Surmekte or SozDurumu.SuresiDoldu
                         || (k.Soz.Bitis >= simdi.Date.AddDays(-SozKurali.DurtmeGostermeGunu)))
                .OrderBy(k => k.Durum is SozDurumu.Surmekte ? 0 : k.Durum is SozDurumu.SuresiDoldu ? 1 : 2)
                .ThenBy(k => k.Soz.Bitis)
                .Select(k => new BugunSoz
                {
                    SozId = k.Soz.Id,
                    KitapAdi = k.Soz.Kitap?.KitapAdi ?? string.Empty,
                    Benim = k.Soz.SahipId == kullaniciId,
                    SahipAdi = k.Soz.SahipId == kullaniciId ? null : k.SahipAdi,
                    Durum = k.Durum,
                    KalanGun = k.KalanGun,
                    Yuzde = k.Yuzde,
                    GunlukGereken = k.GunlukGereken,
                    YeniDestek = destekler.GetValueOrDefault(k.Soz.Id)
                })
                .ToList();
        }

        // Odalardaki son mesaj yazarları ve öneri kabul edenler için kimlikleri kullanıcı adlarına çevirir
        private async Task AdlariCoz(BugunViewModel vm, HashSet<string> idler)
        {
            if (idler.Count == 0) return;
            var liste = idler.ToList();
            var adlar = (await _context.Users.AsNoTracking()
                .Where(u => liste.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync())
                .ToDictionary(u => u.Id, u => string.IsNullOrEmpty(u.UserName) ? "okur" : u.UserName.Split('@')[0]);

            string Ad(string id) => adlar.TryGetValue(id, out var a) ? a : "silinmiş okur";
            foreach (var o in vm.Odalar.Where(o => o.SonMesajYazari != null && o.SonMesajYazari != "sen"))
                o.SonMesajYazari = Ad(o.SonMesajYazari!);
            foreach (var o in vm.Oneriler)
                o.KabulEden = Ad(o.KabulEden);
        }
    }
}
