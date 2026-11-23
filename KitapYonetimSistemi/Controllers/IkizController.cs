using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Controllers
{
    // Okuma ikizi: yıldıza değil, aynı sayfalarda aynı şeyi hissetmeye göre okur eşleştirir. Yalnızca katılanlar eşleşir.
    [Authorize(Roles = "User")]
    public class IkizController : Controller
    {
        private const int EnCokIkiz = 10;
        private const int EnCokOnerilenKitap = 8;
        // Kitap önermek için bu uyumun üstündeki ikizlere bakılır
        private const int OneriIcinEnAzUyum = 60;
        private const int OneriIcinEnCokIkiz = 5;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IkizController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var kullanici = await _userManager.GetUserAsync(User);
            var ben = kullanici!.Id;
            var vm = new IkizViewModel { Katiliyor = kullanici.IkizOlabilir };

            var benim = await _context.SayfaTepkileri.AsNoTracking()
                .Where(t => t.KullaniciId == ben)
                .Select(t => new IkizTepkisi(t.KitapId, t.SayfaNo, t.Tur))
                .ToListAsync();
            vm.BenimTepkiSayim = benim.Count;
            vm.BenimKitapSayim = benim.Select(t => t.KitapId).Distinct().Count();

            if (!vm.Katiliyor || benim.Count == 0)
                return View(vm);

            var kitapIdleri = benim.Select(t => t.KitapId).Distinct().ToList();
            var digerleri = (await _context.SayfaTepkileri.AsNoTracking()
                .Where(t => t.KullaniciId != ben && kitapIdleri.Contains(t.KitapId))
                .Join(_context.Users.Where(u => u.IkizOlabilir), t => t.KullaniciId, u => u.Id,
                    (t, u) => new { t.KullaniciId, t.KitapId, t.SayfaNo, t.Tur })
                .ToListAsync())
                .GroupBy(t => t.KullaniciId)
                .ToDictionary(g => g.Key, g => g.Select(t => new IkizTepkisi(t.KitapId, t.SayfaNo, t.Tur)).ToList());

            vm.Ikizler = OkumaIkizi.Hesapla(benim, digerleri).Take(EnCokIkiz).ToList();
            if (vm.Ikizler.Count == 0)
                return View(vm);

            var ikizIdleri = vm.Ikizler.Select(i => i.KullaniciId).ToList();
            var ornekKitapIdleri = vm.Ikizler.SelectMany(i => i.Ornekler.Select(o => o.KitapId)).Distinct().ToList();
            vm.KitapAdlari = await _context.Kitaplar.AsNoTracking()
                .Where(k => ornekKitapIdleri.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id, k => k.KitapAdi);
            vm.Adlar = (await _context.Users.AsNoTracking()
                .Where(u => ikizIdleri.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName }).ToListAsync())
                .ToDictionary(u => u.Id, u => string.IsNullOrEmpty(u.UserName) ? "okur" : u.UserName.Split('@')[0]);

            // Önerilen kitaplar: güçlü ikizlerin herkese açık kitaplığında bitirdikleri, sende olmayanlar
            var guclu = vm.Ikizler.Where(i => i.Uyum >= OneriIcinEnAzUyum).Take(OneriIcinEnCokIkiz).Select(i => i.KullaniciId).ToList();
            if (guclu.Count > 0)
            {
                var sendekiler = (await _context.KullaniciKitapliklari.AsNoTracking()
                    .Where(kk => kk.KullaniciId == ben).Select(kk => kk.KitapId).ToListAsync()).ToHashSet();
                var onerilen = (await _context.KullaniciKitapliklari.AsNoTracking()
                    .Include(kk => kk.Kitap)
                    .Where(kk => guclu.Contains(kk.KullaniciId) && kk.HerkesGorebilir && kk.OkumaDurumu == OkumaDurumu.Okundu)
                    .ToListAsync())
                    .Where(kk => !sendekiler.Contains(kk.KitapId))
                    .GroupBy(kk => kk.Kitap!)
                    .Select(g => new IkizKitabi { Kitap = g.Key, Okuyanlar = g.Select(kk => kk.KullaniciId).Distinct().ToList() })
                    .OrderByDescending(x => x.Okuyanlar.Count).ThenBy(x => x.Kitap.KitapAdi)
                    .Take(EnCokOnerilenKitap)
                    .ToList();
                vm.OnerilenKitaplar = onerilen;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Katilimi(bool katil)
        {
            var kullanici = await _userManager.GetUserAsync(User);
            kullanici!.IkizOlabilir = katil;
            await _userManager.UpdateAsync(kullanici);
            TempData["Mesaj"] = katil ? "Okuma ikizi eşleşmesi açıldı." : "Okuma ikizi eşleşmesi kapatıldı. Artık kimse seni göremez.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class IkizKitabi
    {
        public Kitap Kitap { get; set; } = null!;
        public List<string> Okuyanlar { get; set; } = new();
    }

    public class IkizViewModel
    {
        public bool Katiliyor { get; set; }
        public int BenimTepkiSayim { get; set; }
        public int BenimKitapSayim { get; set; }
        public List<IkizAdayi> Ikizler { get; set; } = new();
        public Dictionary<string, string> Adlar { get; set; } = new();
        public Dictionary<int, string> KitapAdlari { get; set; } = new();
        public List<IkizKitabi> OnerilenKitaplar { get; set; } = new();
    }
}
