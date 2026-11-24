using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;
using KitapYonetimSistemi.Services;

namespace KitapYonetimSistemi.Controllers
{
    // Dizi gibi okuma: topluluk bir kitabı günlük bölümlerle birlikte okur, sohbet bölüm bölüm açılır.
    [Authorize(Roles = "User")]
    public class SezonController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SezonController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Oda ve sezon listeleri "Birlikte okuma" merkezinde birleşti
        public IActionResult Index() => RedirectToAction("Index", "Birlikte");

        // Kitap detayından sezon başlat
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Olustur(int kitapId, string? ad, DateTime? baslangic, int gunSayisi)
        {
            var kitap = await _context.Kitaplar.AsNoTracking().Where(k => k.Id == kitapId)
                .Select(k => new { k.Id, k.KitapAdi, k.SayfaSayisi }).FirstOrDefaultAsync();
            if (kitap == null)
                return NotFound();

            var bugun = DateTime.Now.Date;
            var gun = baslangic?.Date ?? bugun;
            string? hata = null;
            if (kitap.SayfaSayisi is not > 0)
                hata = "Sayfa sayısı bilinmeyen bir kitap için sezon açılamaz.";
            else if (gunSayisi < OkumaSezonu.EnAzGun || gunSayisi > OkumaSezonu.EnCokGun)
                hata = $"Sezon {OkumaSezonu.EnAzGun} ile {OkumaSezonu.EnCokGun} gün arasında olmalıdır.";
            else if (gun < bugun || gun > bugun.AddDays(OkumaSezonu.EnCokBaslangicGunu))
                hata = $"Başlangıç bugünden itibaren en fazla {OkumaSezonu.EnCokBaslangicGunu} gün sonrası olabilir.";

            var kullaniciId = _userManager.GetUserId(User)!;
            if (hata == null)
            {
                // Sezon yağmasını önlemek için aynı anda sınırlı sayıda açık sezon
                var acik = await _context.OkumaSezonlari.AsNoTracking()
                    .Where(s => s.OlusturanId == kullaniciId)
                    .Select(s => new { s.Baslangic, s.GunSayisi, Sayfa = s.Kitap!.SayfaSayisi })
                    .ToListAsync();
                var acikSayisi = acik.Count(s => new SezonTakvimi(s.Baslangic, s.GunSayisi, s.Sayfa ?? 1).Durum(DateTime.Now) != SezonDurumu.Bitti);
                if (acikSayisi >= OkumaSezonu.KullaniciBasinaEnCokAcik)
                    hata = $"Aynı anda en fazla {OkumaSezonu.KullaniciBasinaEnCokAcik} açık sezonun olabilir.";
            }

            if (hata != null)
            {
                TempData["Hata"] = hata;
                return RedirectToAction("Detay", "Kitap", new { id = kitapId });
            }

            ad = (ad ?? string.Empty).Trim();
            if (ad.Length == 0) ad = $"{kitap.KitapAdi} sezonu";
            if (ad.Length > OkumaSezonu.AdUzunlugu) ad = ad[..OkumaSezonu.AdUzunlugu];

            var sezon = new OkumaSezonu
            {
                Ad = ad,
                KitapId = kitap.Id,
                Baslangic = gun,
                GunSayisi = gunSayisi,
                OlusturanId = kullaniciId,
                Tarih = DateTime.Now
            };
            sezon.Uyeler.Add(new SezonUyesi { KullaniciId = kullaniciId, KatilmaTarihi = DateTime.Now });
            _context.OkumaSezonlari.Add(sezon);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Sezon açıldı! Arkadaşlarını davet etmek için sayfanın bağlantısını paylaşabilirsin.";
            return RedirectToAction(nameof(Detay), new { id = sezon.Id });
        }

        public async Task<IActionResult> Detay(int id)
        {
            var kullaniciId = _userManager.GetUserId(User)!;
            var sezon = await _context.OkumaSezonlari.AsNoTracking()
                .Include(s => s.Kitap)
                .Include(s => s.Uyeler).ThenInclude(u => u.Kullanici)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sezon == null)
                return NotFound();

            var simdi = DateTime.Now;
            var takvim = new SezonTakvimi(sezon.Baslangic, sezon.GunSayisi, sezon.Kitap?.SayfaSayisi ?? 1);
            var ben = sezon.Uyeler.FirstOrDefault(u => u.KullaniciId == kullaniciId);
            var benimSon = ben?.SonBolum ?? 0;

            var mesajlar = await _context.SezonMesajlari.AsNoTracking()
                .Include(m => m.Kullanici)
                .Where(m => m.SezonId == id)
                .OrderBy(m => m.Tarih)
                .ToListAsync();

            var bugunBolum = takvim.BugunBolum(simdi);
            var vm = new SezonViewModel
            {
                Sezon = sezon,
                Takvim = takvim,
                Durum = takvim.Durum(simdi),
                BugunBolum = bugunBolum,
                UyeSayisi = sezon.Uyeler.Count,
                Uye = ben != null,
                BenimSonBolum = benimSon,
                Onaylanabilir = ben != null && benimSon < takvim.BolumSayisi && takvim.OnaylanabilirMi(benimSon, simdi),
                TamamlayanSayisi = sezon.Uyeler.Count(u => u.SonBolum >= takvim.BolumSayisi)
            };

            for (var n = 1; n <= takvim.BolumSayisi; n++)
            {
                var bolumMesajlari = mesajlar.Where(m => m.Bolum == n).ToList();
                var b = new SezonBolumGorunumu
                {
                    No = n,
                    SayfaBasi = takvim.SayfaBasi(n),
                    SayfaSonu = takvim.SayfaSonu(n),
                    Gun = takvim.BolumGunu(n),
                    Acildi = n <= bugunBolum,
                    Bugun = n == bugunBolum && vm.Durum == SezonDurumu.Surmekte,
                    OkuyanSayisi = sezon.Uyeler.Count(u => u.SonBolum >= n),
                    MesajSayisi = bolumMesajlari.Count,
                    SohbetAcik = ben != null && SezonTakvimi.SohbetAcik(n, benimSon, false)
                };
                // Okunmamış bölümün sohbet metni sunucuda hiç gönderilmez
                if (b.SohbetAcik)
                {
                    b.Mesajlar = bolumMesajlari.Select(m => new SezonMesajGorunumu
                    {
                        Id = m.Id,
                        Ad = Ad(m.Kullanici?.UserName),
                        Metin = m.Metin,
                        Tarih = m.Tarih,
                        Benim = m.KullaniciId == kullaniciId
                    }).ToList();
                }
                vm.Bolumler.Add(b);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Katil(int sezonId)
        {
            var kullaniciId = _userManager.GetUserId(User)!;
            var sezon = await _context.OkumaSezonlari.Include(s => s.Kitap).Include(s => s.Uyeler)
                .FirstOrDefaultAsync(s => s.Id == sezonId);
            if (sezon == null)
                return NotFound();

            if (sezon.Uyeler.Any(u => u.KullaniciId == kullaniciId))
                return RedirectToAction(nameof(Detay), new { id = sezonId });

            sezon.Uyeler.Add(new SezonUyesi { KullaniciId = kullaniciId, KatilmaTarihi = DateTime.Now });
            await KitaplikGarantiEt(kullaniciId, sezon.KitapId);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Sezona katıldın! Bölümleri okudukça sohbetleri açılır.";
            return RedirectToAction(nameof(Detay), new { id = sezonId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ayril(int sezonId)
        {
            var kullaniciId = _userManager.GetUserId(User)!;
            var uye = await _context.SezonUyeleri.FirstOrDefaultAsync(u => u.SezonId == sezonId && u.KullaniciId == kullaniciId);
            if (uye == null)
                return NotFound();

            _context.SezonUyeleri.Remove(uye);
            await _context.SaveChangesAsync();
            TempData["Mesaj"] = "Sezondan ayrıldın.";
            return RedirectToAction(nameof(Index));
        }

        // "Bölümü okudum": sıradaki açık bölümü onaylar (hepsi: bugüne kadarkileri toplu onaylar). Kitaplık ilerlemesi de güncellenir.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BolumuOkudum(int sezonId, bool hepsi = false)
        {
            var kullaniciId = _userManager.GetUserId(User)!;
            var sezon = await _context.OkumaSezonlari.AsNoTracking().Include(s => s.Kitap)
                .FirstOrDefaultAsync(s => s.Id == sezonId);
            var uye = await _context.SezonUyeleri.FirstOrDefaultAsync(u => u.SezonId == sezonId && u.KullaniciId == kullaniciId);
            if (sezon == null || uye == null)
                return NotFound();

            var simdi = DateTime.Now;
            var takvim = new SezonTakvimi(sezon.Baslangic, sezon.GunSayisi, sezon.Kitap?.SayfaSayisi ?? 1);
            if (!takvim.OnaylanabilirMi(uye.SonBolum, simdi))
            {
                TempData["Hata"] = takvim.BugunBolum(simdi) == 0
                    ? "Sezon henüz başlamadı."
                    : "Şimdilik okuyabileceğin yeni bir bölüm yok. Sıradaki bölüm kendi gününde açılır.";
                return RedirectToAction(nameof(Detay), new { id = sezonId });
            }

            var hedef = hepsi ? takvim.BugunBolum(simdi) : uye.SonBolum + 1;
            uye.SonBolum = hedef;

            // Kitaplık ilerlemesi sadece ileri gider
            var kayit = await _context.KullaniciKitapliklari.Include(kk => kk.Kitap)
                .FirstOrDefaultAsync(kk => kk.KullaniciId == kullaniciId && kk.KitapId == sezon.KitapId);
            var sayfa = takvim.SayfaSonu(hedef);
            if (kayit == null)
            {
                kayit = new KullaniciKitaplik
                {
                    KullaniciId = kullaniciId,
                    KitapId = sezon.KitapId,
                    HerkesGorebilir = false,
                    EklenmeTarihi = simdi,
                    OkumaDurumu = OkumaDurumu.Okuyorum,
                    OkumayaBaslamaTarihi = simdi,
                    MevcutSayfa = 0,
                    Kitap = sezon.Kitap
                };
                _context.KullaniciKitapliklari.Add(kayit);
                await _context.SaveChangesAsync();
            }
            if (sayfa > (kayit.MevcutSayfa ?? 0))
                IlerlemeIsleyici.Uygula(_context, kayit, sayfa, $"Sezon: {hedef}. bölüm");

            await _context.SaveChangesAsync();
            TempData["Mesaj"] = hedef >= takvim.BolumSayisi
                ? "Tebrikler, sezonu tamamladın!"
                : $"{hedef}. bölüm tamam! Bölümün sohbeti açıldı.";
            return Redirect(Url.Action(nameof(Detay), new { id = sezonId }) + $"#bolum-{hedef}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MesajGonder(int sezonId, int bolum, string? metin)
        {
            var kullaniciId = _userManager.GetUserId(User)!;
            var sezon = await _context.OkumaSezonlari.AsNoTracking().Include(s => s.Kitap)
                .FirstOrDefaultAsync(s => s.Id == sezonId);
            var uye = await _context.SezonUyeleri.AsNoTracking().FirstOrDefaultAsync(u => u.SezonId == sezonId && u.KullaniciId == kullaniciId);
            if (sezon == null || uye == null)
                return NotFound();

            var takvim = new SezonTakvimi(sezon.Baslangic, sezon.GunSayisi, sezon.Kitap?.SayfaSayisi ?? 1);
            metin = (metin ?? string.Empty).Trim();
            if (metin.Length == 0 || metin.Length > SezonMesaji.EnFazlaUzunluk)
                TempData["Hata"] = $"Mesaj 1 ile {SezonMesaji.EnFazlaUzunluk} karakter arasında olmalıdır!";
            else if (!takvim.YazabilirMi(bolum, uye.SonBolum))
                TempData["Hata"] = "Yalnızca okuduğun bölümlerin sohbetine yazabilirsin.";
            else
            {
                _context.SezonMesajlari.Add(new SezonMesaji
                {
                    SezonId = sezonId,
                    KullaniciId = kullaniciId,
                    Bolum = bolum,
                    Metin = metin,
                    Tarih = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            return Redirect(Url.Action(nameof(Detay), new { id = sezonId }) + $"#bolum-{bolum}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MesajSil(int mesajId)
        {
            var kullaniciId = _userManager.GetUserId(User);
            var mesaj = await _context.SezonMesajlari.FirstOrDefaultAsync(m => m.Id == mesajId && m.KullaniciId == kullaniciId);
            if (mesaj == null)
                return NotFound();

            _context.SezonMesajlari.Remove(mesaj);
            await _context.SaveChangesAsync();
            return Redirect(Url.Action(nameof(Detay), new { id = mesaj.SezonId }) + $"#bolum-{mesaj.Bolum}");
        }

        private static string Ad(string? kullaniciAdi) => string.IsNullOrEmpty(kullaniciAdi) ? "okur" : kullaniciAdi.Split('@')[0];

        // Sezona katılanın kitaplığında kitap yoksa "okuyorum" olarak eklenir
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
    }
}
