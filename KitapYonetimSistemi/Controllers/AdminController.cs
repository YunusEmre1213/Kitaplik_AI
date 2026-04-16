using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitapYonetimSistemi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //  DASHBOARD İŞLEMLERİ

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                ToplamKitapSayisi = await _context.Kitaplar.CountAsync(),
                ToplamKullaniciSayisi = await _context.Users.CountAsync(),
                ToplamYorumSayisi = await _context.Yorumlar.CountAsync(),
                ToplamKitaplikSayisi = await _context.KullaniciKitapliklari.CountAsync(),

                BuAyEklenenKitapSayisi = await _context.Kitaplar
                    .Where(k => k.EklenmeTarihi.Month == DateTime.Now.Month &&
                               k.EklenmeTarihi.Year == DateTime.Now.Year)
                    .CountAsync(),

                EnCokYorumlananKitaplar = await _context.Kitaplar
                    .Select(k => new KitapYorumIstatistik
                    {
                        KitapAdi = k.KitapAdi,
                        YorumSayisi = k.Yorumlar.Count()
                    })
                    .OrderByDescending(k => k.YorumSayisi)
                    .Take(5)
                    .ToListAsync(),

                SonEklenenKitaplar = await _context.Kitaplar
                    .OrderByDescending(k => k.EklenmeTarihi)
                    .Take(5)
                    .ToListAsync(),

                EnYuksekPuanliKitaplar = await _context.Kitaplar
                    .Include(k => k.Puanlar)
                    .Where(k => k.Puanlar.Any())
                    .OrderByDescending(k => k.Puanlar.Average(p => p.Puan))
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }
        // EKLE/DÜZENLE/SİL/LİSTELE

        public async Task<IActionResult> KitapListesi()
        {
            var kitaplar = await _context.Kitaplar
                .OrderByDescending(k => k.EklenmeTarihi)
                .ToListAsync();
            return View(kitaplar);
        }

        public IActionResult KitapEkle()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KitapEkle(Kitap kitap, IFormFile kapakResmi)
        {
            if (ModelState.IsValid)
            {
                kitap.EklenmeTarihi = DateTime.Now;

                // Kapak resmi yükleme işlemi
                if (kapakResmi != null && kapakResmi.Length > 0)
                {
                    var uzanti = Path.GetExtension(kapakResmi.FileName).ToLower();
                    var izinliUzantilar = new[] { ".jpg", ".jpeg", ".png", ".gif" };

                    if (izinliUzantilar.Contains(uzanti))
                    {
                        var dosyaAdi = Guid.NewGuid().ToString() + uzanti;
                        var klasorYolu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "kapaklar");
                        if (!Directory.Exists(klasorYolu))
                        {
                            Directory.CreateDirectory(klasorYolu);
                        }
                        var dosyaYolu = Path.Combine(klasorYolu, dosyaAdi);

                        using (var stream = new FileStream(dosyaYolu, FileMode.Create))
                        {
                            await kapakResmi.CopyToAsync(stream);
                        }
                        kitap.KapakResmiUrl = "/uploads/kapaklar/" + dosyaAdi;
                    }
                    else
                    {
                        TempData["Hata"] = "Sadece JPG, PNG veya GIF formatında resim yükleyebilirsiniz!";
                        return View(kitap);
                    }
                }

                _context.Kitaplar.Add(kitap);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Kitap başarıyla eklendi!";
                return RedirectToAction(nameof(KitapListesi));
            }
            return View(kitap);
        }

        public async Task<IActionResult> KitapDuzenle(int? id)
        {
            if (id == null) return NotFound();
            var kitap = await _context.Kitaplar.FindAsync(id);
            if (kitap == null) return NotFound();
            return View(kitap);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KitapDuzenle(int id, Kitap kitap, IFormFile kapakResmi)
        {
            if (id != kitap.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (kapakResmi != null && kapakResmi.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(kitap.KapakResmiUrl))
                        {
                            var eskiDosyaYolu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", kitap.KapakResmiUrl.TrimStart('/'));
                            if (System.IO.File.Exists(eskiDosyaYolu))
                            {
                                System.IO.File.Delete(eskiDosyaYolu);
                            }
                        }

                        var uzanti = Path.GetExtension(kapakResmi.FileName).ToLower();
                        var izinliUzantilar = new[] { ".jpg", ".jpeg", ".png", ".gif" };

                        if (izinliUzantilar.Contains(uzanti))
                        {
                            var dosyaAdi = Guid.NewGuid().ToString() + uzanti;
                            var klasorYolu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "kapaklar");

                            if (!Directory.Exists(klasorYolu))
                            {
                                Directory.CreateDirectory(klasorYolu);
                            }

                            var dosyaYolu = Path.Combine(klasorYolu, dosyaAdi);

                            using (var stream = new FileStream(dosyaYolu, FileMode.Create))
                            {
                                await kapakResmi.CopyToAsync(stream);
                            }

                            kitap.KapakResmiUrl = "/uploads/kapaklar/" + dosyaAdi;
                        }
                    }

                    _context.Update(kitap);
                    await _context.SaveChangesAsync();
                    TempData["Mesaj"] = "Kitap başarıyla güncellendi!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KitapVarMi(kitap.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(KitapListesi));
            }
            return View(kitap);
        }

        public async Task<IActionResult> KitapSil(int? id)
        {
            if (id == null) return NotFound();
            var kitap = await _context.Kitaplar.FindAsync(id);
            if (kitap == null) return NotFound();
            return View(kitap);
        }

        [HttpPost, ActionName("KitapSil")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KitapSilOnay(int id)
        {
            var kitap = await _context.Kitaplar.FindAsync(id);
            if (kitap != null)
            {
                _context.Kitaplar.Remove(kitap);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Kitap başarıyla silindi!";
            }
            return RedirectToAction(nameof(KitapListesi));
        }

        private bool KitapVarMi(int id)
        {
            return _context.Kitaplar.Any(e => e.Id == id);
        }

        //  YORUM LİSTELE/SİL

        public async Task<IActionResult> YorumYonetimi()
        {
            var yorumlar = await _context.Yorumlar
                .Include(y => y.Kitap)
                .Include(y => y.Kullanici)
                .OrderByDescending(y => y.YorumTarihi)
                .Select(y => new YorumYonetimViewModel
                {
                    YorumId = y.Id,
                    YorumMetni = y.YorumMetni,
                    YorumTarihi = y.YorumTarihi,
                    KitapId = y.KitapId,
                    KitapAdi = y.Kitap.KitapAdi,
                    KullaniciId = y.KullaniciId,
                    KullaniciEmail = y.Kullanici.Email
                })
                .ToListAsync();

            return View(yorumlar);
        }

        public async Task<IActionResult> KitapYorumlari(int kitapId)
        {
            var kitap = await _context.Kitaplar.FindAsync(kitapId);
            if (kitap == null)
                return NotFound();

            ViewBag.KitapAdi = kitap.KitapAdi;

            var yorumlar = await _context.Yorumlar
                .Include(y => y.Kullanici)
                .Where(y => y.KitapId == kitapId)
                .OrderByDescending(y => y.YorumTarihi)
                .Select(y => new YorumYonetimViewModel
                {
                    YorumId = y.Id,
                    YorumMetni = y.YorumMetni,
                    YorumTarihi = y.YorumTarihi,
                    KitapId = y.KitapId,
                    KitapAdi = kitap.KitapAdi,
                    KullaniciId = y.KullaniciId,
                    KullaniciEmail = y.Kullanici.Email
                })
                .ToListAsync();

            return View(yorumlar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YorumSil(int yorumId, string returnUrl = null)
        {
            var yorum = await _context.Yorumlar.FindAsync(yorumId);

            if (yorum == null)
            {
                TempData["Hata"] = "Yorum bulunamadı!";
                return RedirectToAction(nameof(YorumYonetimi));
            }

            _context.Yorumlar.Remove(yorum);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Yorum başarıyla silindi!";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(YorumYonetimi));
        }

        public async Task<IActionResult> KullaniciYorumlari(string kullaniciId)
        {
            if (string.IsNullOrEmpty(kullaniciId))
                return NotFound();

            var kullanici = await _userManager.FindByIdAsync(kullaniciId);
            if (kullanici == null)
                return NotFound();

            ViewBag.KullaniciEmail = kullanici.Email;

            var yorumlar = await _context.Yorumlar
                .Include(y => y.Kitap)
                .Where(y => y.KullaniciId == kullaniciId)
                .OrderByDescending(y => y.YorumTarihi)
                .Select(y => new YorumYonetimViewModel
                {
                    YorumId = y.Id,
                    YorumMetni = y.YorumMetni,
                    YorumTarihi = y.YorumTarihi,
                    KitapId = y.KitapId,
                    KitapAdi = y.Kitap.KitapAdi,
                    KullaniciId = y.KullaniciId,
                    KullaniciEmail = kullanici.Email
                })
                .ToListAsync();

            return View(yorumlar);
        }

        //  KULLANICI YÖNETİMİ İŞLEMLERİ
        public async Task<IActionResult> KullaniciYonetimi()
        {
            var kullanicilar = await _context.Users.ToListAsync();
            var model = new List<KullaniciYonetimViewModel>();

            foreach (var kullanici in kullanicilar)
            {
                var roller = await _userManager.GetRolesAsync(kullanici);
                var rol = roller.FirstOrDefault() ?? "Rol Yok";

                var kitaplikSayisi = await _context.KullaniciKitapliklari
                    .Where(k => k.KullaniciId == kullanici.Id)
                    .CountAsync();

                var yorumSayisi = await _context.Yorumlar
                    .Where(y => y.KullaniciId == kullanici.Id)
                    .CountAsync();

                model.Add(new KullaniciYonetimViewModel
                {
                    Id = kullanici.Id,
                    Email = kullanici.Email,
                    Rol = rol,
                    KayitTarihi = DateTime.Now, 
                    KitaplikSayisi = kitaplikSayisi,
                    YorumSayisi = yorumSayisi
                });
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RolDegistir(string kullaniciId, string yeniRol)
        {
            var kullanici = await _userManager.FindByIdAsync(kullaniciId);
            if (kullanici == null)
            {
                TempData["Hata"] = "Kullanıcı bulunamadı!";
                return RedirectToAction(nameof(KullaniciYonetimi));
            }

            var mevcutRoller = await _userManager.GetRolesAsync(kullanici);
            if (mevcutRoller.Any())
            {
                await _userManager.RemoveFromRolesAsync(kullanici, mevcutRoller);
            }

            if (!string.IsNullOrEmpty(yeniRol))
            {
                await _userManager.AddToRoleAsync(kullanici, yeniRol);
                TempData["Mesaj"] = $"Kullanıcının rolü '{yeniRol}' olarak değiştirildi!";
            }

            return RedirectToAction(nameof(KullaniciYonetimi));
        }

        [HttpPost]
        public async Task<IActionResult> KullaniciSil(string kullaniciId)
        {
            var kullanici = await _userManager.FindByIdAsync(kullaniciId);
            if (kullanici == null)
            {
                TempData["Hata"] = "Kullanıcı bulunamadı!";
                return RedirectToAction(nameof(KullaniciYonetimi));
            }

            var yorumlar = await _context.Yorumlar.Where(y => y.KullaniciId == kullaniciId).ToListAsync();
            _context.Yorumlar.RemoveRange(yorumlar);

            var kitapliklar = await _context.KullaniciKitapliklari.Where(k => k.KullaniciId == kullaniciId).ToListAsync();
            _context.KullaniciKitapliklari.RemoveRange(kitapliklar);

            await _context.SaveChangesAsync();

            await _userManager.DeleteAsync(kullanici);

            TempData["Mesaj"] = "Kullanıcı başarıyla silindi!";
            return RedirectToAction(nameof(KullaniciYonetimi));
        }
    }
}