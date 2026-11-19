using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Controllers
{
    // Ortak okuma odaları (buddy read): üyeler birbirinin ilerlemesini görür, mesajlar sayfaya kapılıdır.
    [Authorize(Roles = "User")]
    public class OdaController : Controller
    {
        private const string KodAlfabesi = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // karışan harfler (0/O, 1/I) yok

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OdaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var kullaniciId = _userManager.GetUserId(User)!;
            var odalar = await _context.OkumaOdalari.AsNoTracking()
                .Where(o => o.Uyeler.Any(u => u.KullaniciId == kullaniciId))
                .Include(o => o.Kitap)
                .Include(o => o.Uyeler)
                .OrderByDescending(o => o.Tarih)
                .ToListAsync();
            return View(odalar);
        }

        // Kitap detayından oda aç
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Olustur(int kitapId, string? ad)
        {
            var kitap = await _context.Kitaplar.AsNoTracking().Where(k => k.Id == kitapId)
                .Select(k => new { k.Id, k.KitapAdi }).FirstOrDefaultAsync();
            if (kitap == null)
                return NotFound();

            ad = (ad ?? string.Empty).Trim();
            if (ad.Length == 0) ad = $"{kitap.KitapAdi} okuma odası";
            if (ad.Length > OkumaOdasi.AdUzunlugu) ad = ad[..OkumaOdasi.AdUzunlugu];

            var kullaniciId = _userManager.GetUserId(User)!;
            var oda = new OkumaOdasi
            {
                Ad = ad,
                Kod = await YeniKodUret(),
                KitapId = kitap.Id,
                OlusturanId = kullaniciId,
                Tarih = DateTime.Now
            };
            oda.Uyeler.Add(new OdaUyesi { KullaniciId = kullaniciId, KatilmaTarihi = DateTime.Now });
            _context.OkumaOdalari.Add(oda);
            await KitaplikGarantiEt(kullaniciId, kitap.Id);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = $"Oda hazır! Arkadaşlarına davet kodunu ver: {oda.Kod}";
            return RedirectToAction(nameof(Detay), new { id = oda.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Katil(string? kod)
        {
            kod = (kod ?? string.Empty).Trim().ToUpperInvariant();
            var oda = kod.Length == 0 ? null : await _context.OkumaOdalari.Include(o => o.Uyeler).FirstOrDefaultAsync(o => o.Kod == kod);
            if (oda == null)
            {
                TempData["Hata"] = "Bu koda ait bir oda bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var kullaniciId = _userManager.GetUserId(User)!;
            if (oda.Uyeler.Any(u => u.KullaniciId == kullaniciId))
                return RedirectToAction(nameof(Detay), new { id = oda.Id });

            if (oda.Uyeler.Count >= OkumaOdasi.EnFazlaUye)
            {
                TempData["Hata"] = $"Oda dolu (en fazla {OkumaOdasi.EnFazlaUye} kişi).";
                return RedirectToAction(nameof(Index));
            }

            oda.Uyeler.Add(new OdaUyesi { KullaniciId = kullaniciId, KatilmaTarihi = DateTime.Now });
            await KitaplikGarantiEt(kullaniciId, oda.KitapId);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Odaya katıldın!";
            return RedirectToAction(nameof(Detay), new { id = oda.Id });
        }

        public async Task<IActionResult> Detay(int id)
        {
            var kullaniciId = _userManager.GetUserId(User)!;
            var oda = await _context.OkumaOdalari.AsNoTracking()
                .Include(o => o.Kitap)
                .Include(o => o.Uyeler).ThenInclude(u => u.Kullanici)
                .FirstOrDefaultAsync(o => o.Id == id);
            // Üye olmayan için oda yokmuş gibi davranılır (kod dışında içerik sızmaz)
            if (oda == null || oda.Uyeler.All(u => u.KullaniciId != kullaniciId))
                return NotFound();

            var uyeIdleri = oda.Uyeler.Select(u => u.KullaniciId).ToList();
            var ilerlemeler = await _context.KullaniciKitapliklari.AsNoTracking()
                .Where(kk => kk.KitapId == oda.KitapId && uyeIdleri.Contains(kk.KullaniciId))
                .Select(kk => new { kk.Id, kk.KullaniciId, kk.MevcutSayfa, kk.OkumaDurumu })
                .ToListAsync();

            var benimki = ilerlemeler.FirstOrDefault(i => i.KullaniciId == kullaniciId);
            var bitirdim = benimki?.OkumaDurumu == OkumaDurumu.Okundu;
            var benimSayfam = benimki?.MevcutSayfa ?? 0;

            var vm = new OdaViewModel
            {
                Oda = oda,
                BenimSayfam = benimSayfam,
                Bitirdim = bitirdim,
                KitapSayfaSayisi = oda.Kitap?.SayfaSayisi,
                KitaplikId = benimki?.Id,
                Olusturan = oda.OlusturanId == kullaniciId,
                Uyeler = oda.Uyeler.Select(u =>
                {
                    var ilerleme = ilerlemeler.FirstOrDefault(i => i.KullaniciId == u.KullaniciId);
                    return new OdaUyeGorunumu
                    {
                        KullaniciId = u.KullaniciId,
                        Ad = Ad(u.Kullanici?.UserName),
                        Sayfa = ilerleme?.MevcutSayfa ?? 0,
                        Bitirdi = ilerleme?.OkumaDurumu == OkumaDurumu.Okundu,
                        Ben = u.KullaniciId == kullaniciId
                    };
                }).OrderByDescending(u => u.Sayfa).ToList()
            };

            var mesajlar = await _context.OdaMesajlari.AsNoTracking()
                .Include(m => m.Kullanici)
                .Where(m => m.OdaId == id)
                .OrderBy(m => m.Tarih)
                .Take(300)
                .ToListAsync();

            // Geride kalan üye için ilerideki mesajın metni sunucuda silinir, sayfaya kadar okumadan açılmaz
            vm.Mesajlar = mesajlar.Select(m =>
            {
                var benim = m.KullaniciId == kullaniciId;
                var kilitli = m.SayfaNo > 0 && !YorumGorunurluk.Acik(m.SayfaNo, benim, bitirdim, benimSayfam);
                return new OdaMesajGorunumu
                {
                    Id = m.Id,
                    Ad = Ad(m.Kullanici?.UserName),
                    SayfaNo = m.SayfaNo,
                    Metin = kilitli ? string.Empty : m.Metin,
                    Tarih = m.Tarih,
                    Kilitli = kilitli,
                    Benim = benim
                };
            }).ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MesajGonder(int odaId, string? metin)
        {
            var kullaniciId = _userManager.GetUserId(User)!;
            var oda = await _context.OkumaOdalari.AsNoTracking()
                .Where(o => o.Id == odaId && o.Uyeler.Any(u => u.KullaniciId == kullaniciId))
                .Select(o => new { o.Id, o.KitapId, o.Kitap!.SayfaSayisi })
                .FirstOrDefaultAsync();
            if (oda == null)
                return NotFound();

            metin = (metin ?? string.Empty).Trim();
            if (metin.Length == 0 || metin.Length > OdaMesaji.EnFazlaUzunluk)
            {
                TempData["Hata"] = $"Mesaj 1 ile {OdaMesaji.EnFazlaUzunluk} karakter arasında olmalıdır!";
                return RedirectToAction(nameof(Detay), new { id = odaId });
            }

            // Mesaj yazarın o anki sayfasına bağlanır; bitirenler için kitabın son sayfası
            var ilerleme = await _context.KullaniciKitapliklari.AsNoTracking()
                .Where(kk => kk.KullaniciId == kullaniciId && kk.KitapId == oda.KitapId)
                .Select(kk => new { kk.MevcutSayfa, kk.OkumaDurumu })
                .FirstOrDefaultAsync();
            var sayfa = ilerleme?.OkumaDurumu == OkumaDurumu.Okundu
                ? Math.Max(1, oda.SayfaSayisi ?? ilerleme.MevcutSayfa ?? 1)
                : ilerleme?.MevcutSayfa ?? 0;

            _context.OdaMesajlari.Add(new OdaMesaji
            {
                OdaId = odaId,
                KullaniciId = kullaniciId,
                SayfaNo = sayfa,
                Metin = metin,
                Tarih = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Redirect(Url.Action(nameof(Detay), new { id = odaId }) + "#mesajlar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MesajSil(int mesajId)
        {
            var kullaniciId = _userManager.GetUserId(User);
            var mesaj = await _context.OdaMesajlari.FirstOrDefaultAsync(m => m.Id == mesajId && m.KullaniciId == kullaniciId);
            if (mesaj == null)
                return NotFound();

            _context.OdaMesajlari.Remove(mesaj);
            await _context.SaveChangesAsync();
            return Redirect(Url.Action(nameof(Detay), new { id = mesaj.OdaId }) + "#mesajlar");
        }

        // Odadan ayrıl; son üye çıkarsa oda silinir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ayril(int odaId)
        {
            var kullaniciId = _userManager.GetUserId(User)!;
            var uye = await _context.OdaUyeleri.Include(u => u.Oda).FirstOrDefaultAsync(u => u.OdaId == odaId && u.KullaniciId == kullaniciId);
            if (uye == null)
                return NotFound();

            _context.OdaUyeleri.Remove(uye);
            var kalan = await _context.OdaUyeleri.CountAsync(u => u.OdaId == odaId && u.KullaniciId != kullaniciId);
            if (kalan == 0 && uye.Oda != null)
                _context.OkumaOdalari.Remove(uye.Oda);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Odadan ayrıldın.";
            return RedirectToAction(nameof(Index));
        }

        private static string Ad(string? kullaniciAdi) => string.IsNullOrEmpty(kullaniciAdi) ? "okur" : kullaniciAdi.Split('@')[0];

        // Odaya giren kişinin kitaplığında kitap yoksa "okuyorum" olarak eklenir; böylece ilerlemesi takip edilir
        private async Task KitaplikGarantiEt(string kullaniciId, int kitapId)
        {
            if (await _context.KullaniciKitapliklari.AnyAsync(kk => kk.KullaniciId == kullaniciId && kk.KitapId == kitapId))
                return;

            _context.KullaniciKitapliklari.Add(new KullaniciKitaplik
            {
                KullaniciId = kullaniciId,
                KitapId = kitapId,
                HerkesGorebilir = false,
                EklenmeTarihi = DateTime.Now,
                OkumaDurumu = OkumaDurumu.Okuyorum,
                OkumayaBaslamaTarihi = DateTime.Now,
                MevcutSayfa = 0
            });
        }

        private async Task<string> YeniKodUret()
        {
            while (true)
            {
                var kod = string.Create(OkumaOdasi.KodUzunlugu, 0, (span, _) =>
                {
                    for (var i = 0; i < span.Length; i++)
                        span[i] = KodAlfabesi[RandomNumberGenerator.GetInt32(KodAlfabesi.Length)];
                });
                if (!await _context.OkumaOdalari.AnyAsync(o => o.Kod == kod))
                    return kod;
            }
        }
    }
}
