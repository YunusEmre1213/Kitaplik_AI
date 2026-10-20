using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Controllers
{
    [Authorize(Roles = "User")]
    public class KitapController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public KitapController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Tüm kitapları listele (arama, tür filtresi, sıralama ve sayfalama)
        public async Task<IActionResult> Index(string? arama, string? tur, string? siralama, int sayfa = 1)
        {
            var sorgu = _context.Kitaplar.Include(k => k.Puanlar).AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama.Trim();
                sorgu = sorgu.Where(k => k.KitapAdi.Contains(arama) || k.YazarAdi.Contains(arama));
            }

            if (!string.IsNullOrWhiteSpace(tur))
            {
                sorgu = sorgu.Where(k => k.Tur == tur);
            }

            sorgu = siralama switch
            {
                "ad" => sorgu.OrderBy(k => k.KitapAdi),
                "yazar" => sorgu.OrderBy(k => k.YazarAdi).ThenBy(k => k.KitapAdi),
                "puan" => sorgu.OrderByDescending(k => k.Puanlar!.Average(p => (double?)p.Puan)).ThenBy(k => k.KitapAdi),
                _ => sorgu.OrderByDescending(k => k.EklenmeTarihi)
            };

            var toplamKayit = await sorgu.CountAsync();
            var toplamSayfa = Math.Max(1, (int)Math.Ceiling(toplamKayit / (double)KitapListeViewModel.SayfaBoyutu));
            sayfa = Math.Clamp(sayfa, 1, toplamSayfa);

            var model = new KitapListeViewModel
            {
                Kitaplar = await sorgu
                    .Skip((sayfa - 1) * KitapListeViewModel.SayfaBoyutu)
                    .Take(KitapListeViewModel.SayfaBoyutu)
                    .ToListAsync(),
                Arama = arama,
                Tur = tur,
                Siralama = siralama ?? "yeni",
                Sayfa = sayfa,
                ToplamSayfa = toplamSayfa,
                ToplamKayit = toplamKayit
            };

            return View(model);
        }

        public async Task<IActionResult> Detay(int? id)
        {
            if (id == null)
                return NotFound();

            var kitap = await _context.Kitaplar
                .Include(k => k.Yorumlar)
                .ThenInclude(y => y.Kullanici)
                .Include(k => k.Puanlar) // YENİ: Puanları da çek
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kitap == null)
                return NotFound();
            var kullanici = await _userManager.GetUserAsync(User);
            if (kullanici != null)
            {
                ViewBag.KullaniciPuani = await KullaniciPuaniniGetir(kitap.Id, kullanici.Id);
            }
            return View(kitap);
        }

        // Yorum ekleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YorumEkle(int kitapId, string yorumMetni)
        {
            if (string.IsNullOrWhiteSpace(yorumMetni))
            {
                TempData["Hata"] = "Yorum boş olamaz!";
                return RedirectToAction("Detay", new { id = kitapId });
            }

            var kullanici = await _userManager.GetUserAsync(User);

            var yorum = new Yorum
            {
                KitapId = kitapId,
                KullaniciId = kullanici.Id,
                YorumMetni = yorumMetni,
                YorumTarihi = DateTime.Now
            };

            _context.Yorumlar.Add(yorum);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Yorumunuz başarıyla eklendi!";
            return RedirectToAction("Detay", new { id = kitapId });
        }

        // Kitaplığıma ekle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KitapligaEkle(int kitapId, bool herkesGorebilir = false)
        {
            var kullanici = await _userManager.GetUserAsync(User);

            // Zaten eklenmişse tekrar ekleme
            var mevcutMu = await _context.KullaniciKitapliklari
                .AnyAsync(kk => kk.KullaniciId == kullanici.Id && kk.KitapId == kitapId);

            if (mevcutMu)
            {
                TempData["Hata"] = "Bu kitap zaten kitaplığınızda!";
                return RedirectToAction("Index");
            }

            var kitaplikKaydi = new KullaniciKitaplik
            {
                KullaniciId = kullanici.Id,
                KitapId = kitapId,
                HerkesGorebilir = herkesGorebilir,
                EklenmeTarihi = DateTime.Now
            };

            _context.KullaniciKitapliklari.Add(kitaplikKaydi);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Kitap kitaplığınıza eklendi!";
            return RedirectToAction("Index");
        }

        // Kitaplıktan çıkar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KitapliktanCikar(int kitapId)
        {
            var kullanici = await _userManager.GetUserAsync(User);

            var kayit = await _context.KullaniciKitapliklari
                .FirstOrDefaultAsync(kk => kk.KullaniciId == kullanici.Id && kk.KitapId == kitapId);

            if (kayit != null)
            {
                _context.KullaniciKitapliklari.Remove(kayit);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Kitap kitaplığınızdan çıkarıldı!";
            }

            return RedirectToAction("Kitapligim");
        }

        // Kendi kitaplığım
        public async Task<IActionResult> Kitapligim()
        {
            var kullanici = await _userManager.GetUserAsync(User);

            var kitaplar = await _context.KullaniciKitapliklari
                .Include(kk => kk.Kitap)
                .Where(kk => kk.KullaniciId == kullanici.Id)
                .OrderByDescending(kk => kk.EklenmeTarihi)
                .ToListAsync();

            return View(kitaplar);
        }

        // Diğer kullanıcıların kitaplıkları
        public async Task<IActionResult> KullaniciKitapliklari()
        {
            var mevcutKullanici = await _userManager.GetUserAsync(User);

            var herkesAcikKitapliklar = await _context.KullaniciKitapliklari
                .Include(kk => kk.Kullanici)
                .Include(kk => kk.Kitap)
                .Where(kk => kk.HerkesGorebilir && kk.KullaniciId != mevcutKullanici.Id)
                .OrderByDescending(kk => kk.EklenmeTarihi)
                .ToListAsync();

            var gruplar = herkesAcikKitapliklar
                .GroupBy(kk => kk.Kullanici)
                .ToList();

            return View(gruplar);
        }

        // Belirli bir kullanıcının kitaplığını görüntüle
        public async Task<IActionResult> KullaniciKitapligi(string kullaniciId)
        {
            if (string.IsNullOrEmpty(kullaniciId))
                return NotFound();

            var kullanici = await _userManager.FindByIdAsync(kullaniciId);
            if (kullanici == null)
                return NotFound();

            var kitaplar = await _context.KullaniciKitapliklari
                .Include(kk => kk.Kitap)
                .Where(kk => kk.KullaniciId == kullaniciId && kk.HerkesGorebilir)
                .OrderByDescending(kk => kk.EklenmeTarihi)
                .ToListAsync();

            ViewBag.KullaniciAdi = kullanici.UserName;
            return View(kitaplar);
        }
        // Puan verme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PuanVer(int kitapId, int puan)
        {
            if (puan < 1 || puan > 5)
            {
                TempData["Hata"] = "Puan 1-5 arasında olmalıdır!";
                return RedirectToAction("Detay", new { id = kitapId });
            }

            var kullanici = await _userManager.GetUserAsync(User);

            // Kullanıcı daha önce puan vermiş mi kontrol et
            var mevcutPuan = await _context.KitapPuanlari
                .FirstOrDefaultAsync(kp => kp.KitapId == kitapId && kp.KullaniciId == kullanici.Id);

            if (mevcutPuan != null)
            {
                // Mevcut puanı güncelle
                mevcutPuan.Puan = puan;
                mevcutPuan.PuanTarihi = DateTime.Now;
                _context.KitapPuanlari.Update(mevcutPuan);
                TempData["Mesaj"] = "Puanınız güncellendi!";
            }
            else
            {
                // Yeni puan ekle
                var yeniPuan = new KitapPuan
                {
                    KitapId = kitapId,
                    KullaniciId = kullanici.Id,
                    Puan = puan,
                    PuanTarihi = DateTime.Now
                };

                _context.KitapPuanlari.Add(yeniPuan);
                TempData["Mesaj"] = "Puanınız kaydedildi!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Detay", new { id = kitapId });
        }

        // Kullanıcının verdiği puanı getir
        private async Task<int?> KullaniciPuaniniGetir(int kitapId, string kullaniciId)
        {
            var puan = await _context.KitapPuanlari
                .Where(kp => kp.KitapId == kitapId && kp.KullaniciId == kullaniciId)
                .Select(kp => (int?)kp.Puan)
                .FirstOrDefaultAsync();

            return puan;
        }
        // Okuma durumunu değiştir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OkumaDurumuDegistir(int kitaplikId, OkumaDurumu yeniDurum)
        {
            var kullanici = await _userManager.GetUserAsync(User);

            var kayit = await _context.KullaniciKitapliklari
                .FirstOrDefaultAsync(kk => kk.Id == kitaplikId && kk.KullaniciId == kullanici.Id);

            if (kayit == null)
            {
                TempData["Hata"] = "Kitaplık kaydı bulunamadı!";
                return RedirectToAction("Kitapligim");
            }

            var eskiDurum = kayit.OkumaDurumu;
            kayit.OkumaDurumu = yeniDurum;

            // Durum değişikliklerine göre tarihleri güncelle
            if (yeniDurum == OkumaDurumu.Okuyorum && eskiDurum != OkumaDurumu.Okuyorum)
            {
                kayit.OkumayaBaslamaTarihi = DateTime.Now;
                kayit.OkumaBitisTarihi = null;
            }
            else if (yeniDurum == OkumaDurumu.Okundu && eskiDurum != OkumaDurumu.Okundu)
            {
                kayit.OkumaBitisTarihi = DateTime.Now;
                if (kayit.OkumayaBaslamaTarihi == null)
                {
                    kayit.OkumayaBaslamaTarihi = DateTime.Now;
                }
            }
            else if (yeniDurum == OkumaDurumu.Okunacak)
            {
                kayit.OkumayaBaslamaTarihi = null;
                kayit.OkumaBitisTarihi = null;
            }

            _context.KullaniciKitapliklari.Update(kayit);
            await _context.SaveChangesAsync();

            string durumText = yeniDurum switch
            {
                OkumaDurumu.Okunacak => "Okunacak",
                OkumaDurumu.Okuyorum => "Okuyorum",
                OkumaDurumu.Okundu => "Okundu",
                _ => ""
            };

            TempData["Mesaj"] = $"Okuma durumu '{durumText}' olarak güncellendi!";
            return RedirectToAction("Kitapligim");
        }

        // Kitaplığa eklerken okuma durumu seç
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KitapligaEkleYeni(int kitapId, bool herkesGorebilir, OkumaDurumu okumaDurumu)
        {
            var kullanici = await _userManager.GetUserAsync(User);

            // Zaten eklenmişse tekrar ekleme
            var mevcutMu = await _context.KullaniciKitapliklari
                .AnyAsync(kk => kk.KullaniciId == kullanici.Id && kk.KitapId == kitapId);

            if (mevcutMu)
            {
                TempData["Hata"] = "Bu kitap zaten kitaplığınızda!";
                return RedirectToAction("Index");
            }

            var kitaplikKaydi = new KullaniciKitaplik
            {
                KullaniciId = kullanici.Id,
                KitapId = kitapId,
                HerkesGorebilir = herkesGorebilir,
                EklenmeTarihi = DateTime.Now,
                OkumaDurumu = okumaDurumu
            };

            // Durum "Okuyorum" ise başlama tarihini ekle
            if (okumaDurumu == OkumaDurumu.Okuyorum)
            {
                kitaplikKaydi.OkumayaBaslamaTarihi = DateTime.Now;
            }
            // Durum "Okundu" ise hem başlama hem bitiş tarihini ekle
            else if (okumaDurumu == OkumaDurumu.Okundu)
            {
                kitaplikKaydi.OkumayaBaslamaTarihi = DateTime.Now;
                kitaplikKaydi.OkumaBitisTarihi = DateTime.Now;
            }

            _context.KullaniciKitapliklari.Add(kitaplikKaydi);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Kitap kitaplığınıza eklendi!";
            return RedirectToAction("Index");
        }

        // Okuma istatistikleri sayfası
        public async Task<IActionResult> OkumaIstatistiklerim()
        {
            var kullanici = await _userManager.GetUserAsync(User);

            var istatistikler = new OkumaIstatistikViewModel
            {
                OkunacakSayisi = await _context.KullaniciKitapliklari
                    .Where(kk => kk.KullaniciId == kullanici.Id && kk.OkumaDurumu == OkumaDurumu.Okunacak)
                    .CountAsync(),

                OkuyorumSayisi = await _context.KullaniciKitapliklari
                    .Where(kk => kk.KullaniciId == kullanici.Id && kk.OkumaDurumu == OkumaDurumu.Okuyorum)
                    .CountAsync(),

                OkunduSayisi = await _context.KullaniciKitapliklari
                    .Where(kk => kk.KullaniciId == kullanici.Id && kk.OkumaDurumu == OkumaDurumu.Okundu)
                    .CountAsync(),

                ToplamKitapSayisi = await _context.KullaniciKitapliklari
                    .Where(kk => kk.KullaniciId == kullanici.Id)
                    .CountAsync(),

                SonOkunanKitaplar = await _context.KullaniciKitapliklari
                    .Include(kk => kk.Kitap)
                    .Where(kk => kk.KullaniciId == kullanici.Id && kk.OkumaDurumu == OkumaDurumu.Okundu)
                    .OrderByDescending(kk => kk.OkumaBitisTarihi)
                    .Take(5)
                    .ToListAsync(),

                SimdiOkuduklarim = await _context.KullaniciKitapliklari
                    .Include(kk => kk.Kitap)
                    .Where(kk => kk.KullaniciId == kullanici.Id && kk.OkumaDurumu == OkumaDurumu.Okuyorum)
                    .OrderByDescending(kk => kk.OkumayaBaslamaTarihi)
                    .ToListAsync()
            };

            // Okunan kitapların aylık dağılımı
            istatistikler.AylikOkumaSayilari = await _context.KullaniciKitapliklari
                .Where(kk => kk.KullaniciId == kullanici.Id &&
                            kk.OkumaDurumu == OkumaDurumu.Okundu &&
                            kk.OkumaBitisTarihi.HasValue)
                .GroupBy(kk => new { kk.OkumaBitisTarihi.Value.Year, kk.OkumaBitisTarihi.Value.Month })
                .Select(g => new AylikOkumaIstatistik
                {
                    Yil = g.Key.Year,
                    Ay = g.Key.Month,
                    KitapSayisi = g.Count()
                })
                .OrderByDescending(a => a.Yil)
                .ThenByDescending(a => a.Ay)
                .Take(6)
                .ToListAsync();

            return View(istatistikler);
        }
    }
}