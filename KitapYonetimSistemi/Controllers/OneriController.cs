using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Controllers
{
    // Öneri zinciri: bir kitabı kişisel notla öner, bağlantıyı açan okur zincire katılır, kitabın yolculuğu görünür.
    [Authorize(Roles = "User")]
    public class OneriController : Controller
    {
        private const string KodAlfabesi = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OneriController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var ben = _userManager.GetUserId(User)!;
            var vm = new OneriIndexViewModel();

            var gonderdiklerim = await _context.Oneriler.AsNoTracking()
                .Include(o => o.Kitap)
                .Where(o => o.GonderenId == ben)
                .OrderByDescending(o => o.Tarih)
                .ToListAsync();

            var onbellek = new Dictionary<int, (List<OneriKaydi> Oneriler, List<KabulKaydi> Kabuller)>();
            foreach (var o in gonderdiklerim)
            {
                if (!onbellek.TryGetValue(o.KitapId, out var veri))
                    onbellek[o.KitapId] = veri = await ZincirVerisi(o.KitapId);
                var agac = OneriZinciri.Agac(o.Id, veri.Oneriler, veri.Kabuller);
                vm.Gonderdiklerim.Add(new OneriSatiri
                {
                    Oneri = o,
                    Ozet = OneriZinciri.Ozet(agac),
                    DogrudanKabul = agac.Count,
                    Baglanti = Baglanti(o.Kod)
                });
            }

            var kabullerim = await _context.OneriKabulleri.AsNoTracking()
                .Include(k => k.Oneri!).ThenInclude(o => o.Kitap)
                .Where(k => k.KullaniciId == ben)
                .OrderByDescending(k => k.KabulTarihi)
                .ToListAsync();
            foreach (var k in kabullerim)
            {
                if (!onbellek.TryGetValue(k.KitapId, out var veri))
                    onbellek[k.KitapId] = veri = await ZincirVerisi(k.KitapId);
                var yol = OneriZinciri.Yol(k.OneriId, veri.Oneriler, veri.Kabuller);
                vm.Alinanlar.Add(new AlinanOneri
                {
                    Kitap = k.Oneri!.Kitap!,
                    Not = k.Oneri.Not,
                    Tarih = k.KabulTarihi,
                    Yol = yol
                });
            }

            var adlar = await Adlar(vm.Alinanlar.SelectMany(a => a.Yol));
            vm.Adlar = adlar;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Olustur(int kitapId, string? not)
        {
            var kitap = await _context.Kitaplar.AsNoTracking().AnyAsync(k => k.Id == kitapId);
            if (!kitap)
                return NotFound();

            not = (not ?? string.Empty).Trim();
            if (not.Length > Oneri.NotUzunlugu)
            {
                TempData["Hata"] = $"Not en fazla {Oneri.NotUzunlugu} karakter olabilir!";
                return RedirectToAction("Detay", "Kitap", new { id = kitapId });
            }

            var ben = _userManager.GetUserId(User)!;
            var bugun = DateTime.Now.Date;
            if (await _context.Oneriler.CountAsync(o => o.GonderenId == ben && o.Tarih >= bugun) >= Oneri.GunlukEnCok)
            {
                TempData["Hata"] = "Bugün için öneri sınırına ulaştın. Yarın tekrar dene.";
                return RedirectToAction("Detay", "Kitap", new { id = kitapId });
            }

            // Bu kitap sana bir zincirle ulaştıysa, yeni öneri o zincirin devamı olur
            var oncekiKabul = await _context.OneriKabulleri.AsNoTracking()
                .Where(k => k.KullaniciId == ben && k.KitapId == kitapId)
                .Select(k => (int?)k.Id).FirstOrDefaultAsync();

            var oneri = new Oneri
            {
                Kod = await YeniKodUret(),
                Not = not,
                Tarih = DateTime.Now,
                KitapId = kitapId,
                GonderenId = ben,
                OncekiKabulId = oncekiKabul
            };
            _context.Oneriler.Add(oneri);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Öneri bağlantın hazır. Bağlantıyı arkadaşlarına gönder; açıp kabul ettikçe zincir uzar.";
            return RedirectToAction(nameof(Detay), new { id = oneri.Id });
        }

        // Kendi önerinin zinciri: kimler aldı, kim bitirdi, kim kime önerdi
        public async Task<IActionResult> Detay(int id)
        {
            var ben = _userManager.GetUserId(User)!;
            var oneri = await _context.Oneriler.AsNoTracking().Include(o => o.Kitap)
                .FirstOrDefaultAsync(o => o.Id == id && o.GonderenId == ben);
            if (oneri == null)
                return NotFound();

            var veri = await ZincirVerisi(oneri.KitapId);
            var agac = OneriZinciri.Agac(oneri.Id, veri.Oneriler, veri.Kabuller);

            string?[] KullaniciIdleri(IEnumerable<OneriDugumu> d) =>
                d.SelectMany(x => new[] { x.KullaniciId }.Concat(KullaniciIdleri(x.Cocuklar))).ToArray();

            return View(new OneriDetayViewModel
            {
                Oneri = oneri,
                Baglanti = Baglanti(oneri.Kod),
                Agac = agac,
                Ozet = OneriZinciri.Ozet(agac),
                Adlar = await Adlar(KullaniciIdleri(agac)),
                Yol = OneriZinciri.Yol(oneri.Id, veri.Oneriler, veri.Kabuller)
            });
        }

        // Bağlantıyı açan okurun karşılaştığı sayfa
        [HttpGet]
        public async Task<IActionResult> Ac(string? kod)
        {
            var vm = await AcVerisi(kod);
            if (vm == null)
                return NotFound();
            if (vm.Zaten)
            {
                TempData["Mesaj"] = "Bu kitap zaten bir öneriyle sana ulaşmış.";
                return RedirectToAction("Detay", "Kitap", new { id = vm.Oneri.KitapId });
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kabul(string? kod)
        {
            var vm = await AcVerisi(kod);
            if (vm == null)
                return NotFound();

            if (vm.Kendi || vm.Dongu || vm.Zaten)
                return RedirectToAction(nameof(Ac), new { kod });

            var ben = _userManager.GetUserId(User)!;
            _context.OneriKabulleri.Add(new OneriKabul
            {
                OneriId = vm.Oneri.Id,
                KitapId = vm.Oneri.KitapId,
                KullaniciId = ben,
                KabulTarihi = DateTime.Now
            });

            // Kitap kitaplığında yoksa "okunacak" olarak eklenir
            if (!await _context.KullaniciKitapliklari.AnyAsync(kk => kk.KullaniciId == ben && kk.KitapId == vm.Oneri.KitapId))
            {
                _context.KullaniciKitapliklari.Add(new KullaniciKitaplik
                {
                    KullaniciId = ben,
                    KitapId = vm.Oneri.KitapId,
                    HerkesGorebilir = false,
                    EklenmeTarihi = DateTime.Now,
                    OkumaDurumu = OkumaDurumu.Okunacak
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Aynı kitap için çift tıklama / eşzamanlı kabul: benzersiz indeks korur
                TempData["Mesaj"] = "Bu kitap zaten bir öneriyle sana ulaşmış.";
                return RedirectToAction("Detay", "Kitap", new { id = vm.Oneri.KitapId });
            }

            TempData["Mesaj"] = "Zincire katıldın! Kitap kitaplığına eklendi. Sevdiysen sen de birine önerebilirsin.";
            return RedirectToAction("Detay", "Kitap", new { id = vm.Oneri.KitapId });
        }

        private async Task<OneriAcViewModel?> AcVerisi(string? kod)
        {
            kod = (kod ?? string.Empty).Trim().ToUpperInvariant();
            var oneri = kod.Length == 0 ? null : await _context.Oneriler.AsNoTracking().Include(o => o.Kitap)
                .FirstOrDefaultAsync(o => o.Kod == kod);
            if (oneri == null)
                return null;

            var ben = _userManager.GetUserId(User)!;
            var veri = await ZincirVerisi(oneri.KitapId);
            var yol = OneriZinciri.Yol(oneri.Id, veri.Oneriler, veri.Kabuller);

            return new OneriAcViewModel
            {
                Oneri = oneri,
                Kod = oneri.Kod,
                Yol = yol,
                Adlar = await Adlar(yol),
                Kendi = oneri.GonderenId == ben,
                // Zincirde zaten halka olan biri kendi zincirine geri katılamaz
                Dongu = yol.Contains(ben),
                Zaten = await _context.OneriKabulleri.AnyAsync(k => k.KullaniciId == ben && k.KitapId == oneri.KitapId)
            };
        }

        // Bir kitabın tüm zincir verisi: öneriler ve kabuller (bitirenler kitaplık durumundan bulunur)
        private async Task<(List<OneriKaydi> Oneriler, List<KabulKaydi> Kabuller)> ZincirVerisi(int kitapId)
        {
            var oneriler = await _context.Oneriler.AsNoTracking()
                .Where(o => o.KitapId == kitapId)
                .Select(o => new OneriKaydi(o.Id, o.GonderenId, o.OncekiKabulId))
                .ToListAsync();
            var kabuller = await _context.OneriKabulleri.AsNoTracking()
                .Where(k => k.KitapId == kitapId)
                .Select(k => new { k.Id, k.OneriId, k.KullaniciId })
                .ToListAsync();
            var kullaniciIdleri = kabuller.Select(k => k.KullaniciId).ToList();
            var bitirenler = (await _context.KullaniciKitapliklari.AsNoTracking()
                .Where(kk => kk.KitapId == kitapId && kk.OkumaDurumu == OkumaDurumu.Okundu && kullaniciIdleri.Contains(kk.KullaniciId))
                .Select(kk => kk.KullaniciId).ToListAsync()).ToHashSet();

            return (oneriler, kabuller.Select(k => new KabulKaydi(k.Id, k.OneriId, k.KullaniciId, bitirenler.Contains(k.KullaniciId))).ToList());
        }

        private async Task<Dictionary<string, string>> Adlar(IEnumerable<string?> idler)
        {
            var liste = idler.Where(i => i != null).Select(i => i!).Distinct().ToList();
            var kullanicilar = await _context.Users.AsNoTracking()
                .Where(u => liste.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();
            return kullanicilar.ToDictionary(u => u.Id, u => string.IsNullOrEmpty(u.UserName) ? "okur" : u.UserName.Split('@')[0]);
        }

        private string Baglanti(string kod) => Url.Action(nameof(Ac), "Oneri", new { kod }, Request.Scheme)!;

        private async Task<string> YeniKodUret()
        {
            while (true)
            {
                var kod = string.Create(Oneri.KodUzunlugu, 0, (span, _) =>
                {
                    for (var i = 0; i < span.Length; i++)
                        span[i] = KodAlfabesi[RandomNumberGenerator.GetInt32(KodAlfabesi.Length)];
                });
                if (!await _context.Oneriler.AnyAsync(o => o.Kod == kod))
                    return kod;
            }
        }
    }

    public class OneriSatiri
    {
        public Oneri Oneri { get; set; } = null!;
        public OneriOzeti Ozet { get; set; } = new(0, 0, 0);
        public int DogrudanKabul { get; set; }
        public string Baglanti { get; set; } = string.Empty;
    }

    public class AlinanOneri
    {
        public Kitap Kitap { get; set; } = null!;
        public string Not { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }
        public List<string?> Yol { get; set; } = new();
    }

    public class OneriIndexViewModel
    {
        public List<OneriSatiri> Gonderdiklerim { get; set; } = new();
        public List<AlinanOneri> Alinanlar { get; set; } = new();
        public Dictionary<string, string> Adlar { get; set; } = new();
    }

    public class OneriDetayViewModel
    {
        public Oneri Oneri { get; set; } = null!;
        public string Baglanti { get; set; } = string.Empty;
        public List<OneriDugumu> Agac { get; set; } = new();
        public OneriOzeti Ozet { get; set; } = new(0, 0, 0);
        public Dictionary<string, string> Adlar { get; set; } = new();
        public List<string?> Yol { get; set; } = new();
    }

    public class OneriAgacModel
    {
        public List<OneriDugumu> Dugumler { get; set; } = new();
        public Dictionary<string, string> Adlar { get; set; } = new();
    }

    public class OneriAcViewModel
    {
        public Oneri Oneri { get; set; } = null!;
        public string Kod { get; set; } = string.Empty;
        public List<string?> Yol { get; set; } = new();
        public Dictionary<string, string> Adlar { get; set; } = new();
        public bool Kendi { get; set; }
        public bool Dongu { get; set; }
        public bool Zaten { get; set; }
    }
}
