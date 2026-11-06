using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Controllers
{
    [Authorize(Roles = "User")]
    public class KitapController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Services.DisKitapServisi _disKitapServisi;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public KitapController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            Services.DisKitapServisi disKitapServisi,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _disKitapServisi = disKitapServisi;
            _cache = cache;
        }

        // Tüm kitapları listele (arama, tür filtresi, sıralama ve sayfalama)
        public async Task<IActionResult> Index(string? arama, string? tur, string? siralama, bool kapakli = false, int sayfa = 1)
        {
            var sorgu = _context.Kitaplar.Include(k => k.Puanlar).AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                arama = arama.Trim();
                sorgu = sorgu.Where(k => k.KitapAdi.Contains(arama) || k.YazarAdi.Contains(arama));
            }

            if (!string.IsNullOrWhiteSpace(tur))
            {
                // Toplu aktarılan kitaplarda tür, kategori tablosunda tutulur
                sorgu = sorgu.Where(k => k.Tur == tur || k.Kategoriler.Any(x => x.Kategori!.Ad == tur));
            }

            if (kapakli)
            {
                sorgu = sorgu.Where(k => k.KapakResmiUrl != null && k.KapakResmiUrl != "");
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
                Kapakli = kapakli,
                Siralama = siralama ?? "yeni",
                Sayfa = sayfa,
                ToplamSayfa = toplamSayfa,
                ToplamKayit = toplamKayit
            };

            // Arama yapıldıysa kataloğumuzda olmayan kitaplar dış kaynaktan da önerilir
            if (!string.IsNullOrWhiteSpace(arama) && sayfa == 1 && string.IsNullOrWhiteSpace(tur))
            {
                var disSonuclar = await _disKitapServisi.AraAsync(arama, 8);
                model.DisSonuclar = disSonuclar.Where(d => d.KatalogId == null).Take(6).ToList();
            }

            return View(model);
        }

        // Dış kaynaktaki kitabı kataloğa ekleyip kullanıcının kitaplığına koyar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisKaynaktanKitapligaEkle(string disKaynakId)
        {
            var kullanici = await _userManager.GetUserAsync(User);
            if (kullanici == null)
                return Challenge();

            // Katalog kirliliğini önlemek için kullanıcı başına saatte en fazla 10 yeni kitap
            var sinirKey = $"DisEkle_{kullanici.Id}";
            var sayi = _cache.TryGetValue(sinirKey, out int mevcutSayi) ? mevcutSayi : 0;
            if (sayi >= 10)
            {
                TempData["Hata"] = "Bir saat içinde en fazla 10 yeni kitap ekleyebilirsiniz. Lütfen daha sonra tekrar deneyin.";
                return RedirectToAction(nameof(Index));
            }

            var kitap = await _disKitapServisi.IceAktarAsync(disKaynakId);
            if (kitap == null)
            {
                TempData["Hata"] = "Kitap dış kaynaktan alınamadı!";
                return RedirectToAction(nameof(Index));
            }

            _cache.Set(sinirKey, sayi + 1, TimeSpan.FromHours(1));

            var zatenVar = await _context.KullaniciKitapliklari
                .AnyAsync(kk => kk.KullaniciId == kullanici.Id && kk.KitapId == kitap.Id);

            if (!zatenVar)
            {
                _context.KullaniciKitapliklari.Add(new KullaniciKitaplik
                {
                    KullaniciId = kullanici.Id,
                    KitapId = kitap.Id,
                    EklenmeTarihi = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            TempData["Mesaj"] = $"'{kitap.KitapAdi}' kitaplığınıza eklendi!";
            return RedirectToAction(nameof(Kitapligim));
        }

        public async Task<IActionResult> Detay(int? id)
        {
            if (id == null)
                return NotFound();

            var kitap = await _context.Kitaplar
                .Include(k => k.Yorumlar)
                .ThenInclude(y => y.Kullanici)
                .Include(k => k.Puanlar) // YENİ: Puanları da çek
                .Include(k => k.Alintilar!).ThenInclude(a => a.Kullanici)
                .Include(k => k.Alintilar!).ThenInclude(a => a.Begeniler)
                .AsSplitQuery()
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
        public async Task<IActionResult> YorumEkle(int kitapId, string yorumMetni, bool spoiler = false)
        {
            if (string.IsNullOrWhiteSpace(yorumMetni))
            {
                TempData["Hata"] = "Yorum boş olamaz!";
                return RedirectToAction("Detay", new { id = kitapId });
            }

            if (yorumMetni.Length > 2000 || !await _context.Kitaplar.AnyAsync(k => k.Id == kitapId))
            {
                TempData["Hata"] = "Yorum eklenemedi!";
                return RedirectToAction("Detay", new { id = kitapId });
            }

            var kullanici = await _userManager.GetUserAsync(User);

            var yorum = new Yorum
            {
                KitapId = kitapId,
                KullaniciId = kullanici.Id,
                YorumMetni = yorumMetni.Trim(),
                YorumTarihi = DateTime.Now,
                Spoiler = spoiler
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

        // Yıllık okuma hedefi belirle ya da güncelle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HedefBelirle(int hedef)
        {
            if (hedef < 1 || hedef > 1000)
            {
                TempData["Hata"] = "Hedef 1 ile 1000 arasında olmalıdır!";
                return RedirectToAction(nameof(OkumaIstatistiklerim));
            }

            var kullaniciId = _userManager.GetUserId(User)!;
            var yil = DateTime.Now.Year;

            var mevcut = await _context.OkumaHedefleri.FirstOrDefaultAsync(h => h.KullaniciId == kullaniciId && h.Yil == yil);
            if (mevcut == null)
                _context.OkumaHedefleri.Add(new OkumaHedefi { KullaniciId = kullaniciId, Yil = yil, KitapSayisi = hedef });
            else
                mevcut.KitapSayisi = hedef;

            await _context.SaveChangesAsync();
            TempData["Mesaj"] = $"{yil} okuma hedefiniz {hedef} kitap olarak kaydedildi!";
            return RedirectToAction(nameof(OkumaIstatistiklerim));
        }

        // Okuma ilerlemesini (sayfa) güncelle ve isteğe bağlı günlük notu ekle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IlerlemeGuncelle(int kitaplikId, int sayfa, string? not)
        {
            var kullaniciId = _userManager.GetUserId(User)!;

            var kayit = await _context.KullaniciKitapliklari
                .Include(kk => kk.Kitap)
                .FirstOrDefaultAsync(kk => kk.Id == kitaplikId && kk.KullaniciId == kullaniciId);

            if (kayit == null)
            {
                TempData["Hata"] = "Kitaplık kaydı bulunamadı!";
                return RedirectToAction(nameof(Kitapligim));
            }

            var toplamSayfa = kayit.Kitap?.SayfaSayisi;
            if (sayfa < 0 || (toplamSayfa.HasValue && sayfa > toplamSayfa.Value) || sayfa > 20000)
            {
                TempData["Hata"] = toplamSayfa.HasValue
                    ? $"Sayfa 0 ile {toplamSayfa} arasında olmalıdır!"
                    : "Geçersiz sayfa numarası!";
                return RedirectToAction(nameof(Kitapligim));
            }

            not = string.IsNullOrWhiteSpace(not) ? null : not.Trim();
            if (not != null && not.Length > 500)
            {
                TempData["Hata"] = "Not en fazla 500 karakter olabilir!";
                return RedirectToAction(nameof(Kitapligim));
            }

            kayit.MevcutSayfa = sayfa;

            // İlk sayfa girilince okunacak kitap otomatik "Okuyorum" olur
            if (kayit.OkumaDurumu == OkumaDurumu.Okunacak && sayfa > 0)
            {
                kayit.OkumaDurumu = OkumaDurumu.Okuyorum;
                kayit.OkumayaBaslamaTarihi ??= DateTime.Now;
            }

            // Son sayfaya gelince kitap otomatik "Okundu" olur
            if (toplamSayfa.HasValue && sayfa == toplamSayfa.Value && kayit.OkumaDurumu != OkumaDurumu.Okundu)
            {
                kayit.OkumaDurumu = OkumaDurumu.Okundu;
                kayit.OkumayaBaslamaTarihi ??= DateTime.Now;
                kayit.OkumaBitisTarihi = DateTime.Now;
                TempData["Mesaj"] = "Tebrikler, kitabı bitirdiniz! 🎉";
            }
            else
            {
                TempData["Mesaj"] = "İlerlemeniz kaydedildi!";
            }

            _context.OkumaGunlukleri.Add(new OkumaGunlugu
            {
                KullaniciKitaplikId = kayit.Id,
                Sayfa = sayfa,
                Not = not,
                Tarih = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Kitapligim));
        }

        // Alıntı ekle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlintiEkle(int kitapId, string metin, int? sayfaNo)
        {
            metin = (metin ?? string.Empty).Trim();

            if (metin.Length == 0 || metin.Length > Alinti.EnFazlaUzunluk)
            {
                TempData["Hata"] = $"Alıntı 1 ile {Alinti.EnFazlaUzunluk} karakter arasında olmalıdır!";
                return RedirectToAction(nameof(Detay), new { id = kitapId });
            }

            if (sayfaNo is < 1 or > 20000 || !await _context.Kitaplar.AnyAsync(k => k.Id == kitapId))
            {
                TempData["Hata"] = "Alıntı eklenemedi!";
                return RedirectToAction(nameof(Detay), new { id = kitapId });
            }

            _context.Alintilar.Add(new Alinti
            {
                KitapId = kitapId,
                KullaniciId = _userManager.GetUserId(User)!,
                Metin = metin,
                SayfaNo = sayfaNo,
                Tarih = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Alıntınız eklendi!";
            return RedirectToAction(nameof(Detay), new { id = kitapId });
        }

        // Kendi alıntısını sil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlintiSil(int alintiId)
        {
            var kullaniciId = _userManager.GetUserId(User);
            var alinti = await _context.Alintilar.FirstOrDefaultAsync(a => a.Id == alintiId && a.KullaniciId == kullaniciId);

            if (alinti == null)
                return NotFound();

            _context.Alintilar.Remove(alinti);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Alıntı silindi!";
            return RedirectToAction(nameof(Detay), new { id = alinti.KitapId });
        }

        // Alıntıyı beğen / beğenmekten vazgeç
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlintiBegen(int alintiId)
        {
            var kullaniciId = _userManager.GetUserId(User)!;
            var alinti = await _context.Alintilar.FindAsync(alintiId);

            if (alinti == null)
                return NotFound();

            var begeni = await _context.AlintiBegenileri
                .FirstOrDefaultAsync(b => b.AlintiId == alintiId && b.KullaniciId == kullaniciId);

            if (begeni == null)
                _context.AlintiBegenileri.Add(new AlintiBegeni { AlintiId = alintiId, KullaniciId = kullaniciId });
            else
                _context.AlintiBegenileri.Remove(begeni);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Detay), new { id = alinti.KitapId });
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

            // Yıllık hedef ve sayfa istatistikleri
            var buYil = DateTime.Now.Year;
            istatistikler.Yil = buYil;
            istatistikler.HedefKitapSayisi = await _context.OkumaHedefleri
                .Where(h => h.KullaniciId == kullanici.Id && h.Yil == buYil)
                .Select(h => (int?)h.KitapSayisi)
                .FirstOrDefaultAsync();

            istatistikler.BuYilOkunanSayisi = await _context.KullaniciKitapliklari
                .CountAsync(kk => kk.KullaniciId == kullanici.Id &&
                                  kk.OkumaDurumu == OkumaDurumu.Okundu &&
                                  kk.OkumaBitisTarihi != null && kk.OkumaBitisTarihi.Value.Year == buYil);

            istatistikler.BuYilOkunanSayfa = await _context.KullaniciKitapliklari
                .Where(kk => kk.KullaniciId == kullanici.Id &&
                             kk.OkumaDurumu == OkumaDurumu.Okundu &&
                             kk.OkumaBitisTarihi != null && kk.OkumaBitisTarihi.Value.Year == buYil)
                .SumAsync(kk => kk.Kitap!.SayfaSayisi ?? 0);

            istatistikler.SonGunlukler = await _context.OkumaGunlukleri
                .Include(g => g.KullaniciKitaplik!).ThenInclude(kk => kk.Kitap)
                .Where(g => g.KullaniciKitaplik!.KullaniciId == kullanici.Id)
                .OrderByDescending(g => g.Tarih)
                .Take(8)
                .ToListAsync();

            return View(istatistikler);
        }
    }
}