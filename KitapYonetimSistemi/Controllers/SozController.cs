using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Controllers
{
    // Okuma sözü: bir kitabı belirli tarihe kadar bitireceğine arkadaşlarına söz ver. Tanıklar ilerlemeyi görür ve nazikçe dürter.
    [Authorize(Roles = "User")]
    public class SozController : Controller
    {
        private const string KodAlfabesi = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Services.SozServisi _servis;

        public SozController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, Services.SozServisi servis)
        {
            _context = context;
            _userManager = userManager;
            _servis = servis;
        }

        public async Task<IActionResult> Index()
        {
            var ben = _userManager.GetUserId(User)!;
            var simdi = DateTime.Now;

            var benim = await _context.OkumaSozleri.AsNoTracking()
                .Include(s => s.Kitap).Include(s => s.Tanikler)
                .Where(s => s.SahipId == ben)
                .OrderByDescending(s => s.Bitis)
                .ToListAsync();
            var taniklarimIdleri = await _context.SozTanikleri.AsNoTracking()
                .Where(t => t.KullaniciId == ben).Select(t => t.SozId).ToListAsync();
            var taniklarim = await _context.OkumaSozleri.AsNoTracking()
                .Include(s => s.Kitap).Include(s => s.Tanikler)
                .Where(s => taniklarimIdleri.Contains(s.Id))
                .OrderByDescending(s => s.Bitis)
                .ToListAsync();

            var hepsi = benim.Concat(taniklarim).ToList();
            var kartlar = await KartlariOlustur(hepsi, simdi);
            return View(new SozIndexViewModel
            {
                Benim = benim.Select(s => kartlar[s.Id]).ToList(),
                Taniklarim = taniklarim.Select(s => kartlar[s.Id]).ToList()
            });
        }

        // Kitap sayfasından söz ver
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Olustur(int kitapId, string? not, DateTime? bitis)
        {
            var kitap = await _context.Kitaplar.AsNoTracking().Where(k => k.Id == kitapId)
                .Select(k => new { k.Id }).FirstOrDefaultAsync();
            if (kitap == null)
                return NotFound();

            var simdi = DateTime.Now;
            var ben = _userManager.GetUserId(User)!;
            not = (not ?? string.Empty).Trim();

            string? hata = null;
            if (not.Length > SozKurali.NotUzunlugu)
                hata = $"Not en fazla {SozKurali.NotUzunlugu} karakter olabilir!";
            else if (bitis == null || !SozKurali.TarihGecerliMi(bitis.Value, simdi))
                hata = $"Söz tarihi yarından başlayıp en fazla {SozKurali.EnFazlaGun} gün sonrası olabilir.";

            var kayit = await _context.KullaniciKitapliklari.FirstOrDefaultAsync(kk => kk.KullaniciId == ben && kk.KitapId == kitapId);
            if (hata == null && kayit?.OkumaDurumu == OkumaDurumu.Okundu)
                hata = "Bu kitabı zaten bitirmişsin, söz vermene gerek yok.";

            if (hata == null)
            {
                // Açık sözler: süresi dolmamış ve kitabı henüz bitmemiş olanlar
                var sozler = await _context.OkumaSozleri.AsNoTracking()
                    .Where(s => s.SahipId == ben && s.Bitis >= simdi.Date).ToListAsync();
                var bitenKitaplar = (await _context.KullaniciKitapliklari.AsNoTracking()
                    .Where(kk => kk.KullaniciId == ben && kk.OkumaDurumu == OkumaDurumu.Okundu)
                    .Select(kk => kk.KitapId).ToListAsync()).ToHashSet();
                var acik = sozler.Where(s => !bitenKitaplar.Contains(s.KitapId)).ToList();
                if (acik.Any(s => s.KitapId == kitapId))
                    hata = "Bu kitap için zaten açık bir sözün var.";
                else if (acik.Count >= SozKurali.SahipBasinaEnFazlaAcik)
                    hata = $"Aynı anda en fazla {SozKurali.SahipBasinaEnFazlaAcik} açık sözün olabilir.";
            }

            if (hata != null)
            {
                TempData["Hata"] = hata;
                return RedirectToAction("Detay", "Kitap", new { id = kitapId });
            }

            // Söz veren okur kitabı okumaya başlamış sayılır
            if (kayit == null)
            {
                kayit = new KullaniciKitaplik
                {
                    KullaniciId = ben,
                    KitapId = kitapId,
                    HerkesGorebilir = false,
                    EklenmeTarihi = simdi,
                    OkumaDurumu = OkumaDurumu.Okuyorum,
                    OkumayaBaslamaTarihi = simdi,
                    MevcutSayfa = 0
                };
                _context.KullaniciKitapliklari.Add(kayit);
            }
            else if (kayit.OkumaDurumu == OkumaDurumu.Okunacak)
            {
                kayit.OkumaDurumu = OkumaDurumu.Okuyorum;
                kayit.OkumayaBaslamaTarihi ??= simdi;
            }

            var soz = new OkumaSozu
            {
                Kod = await YeniKodUret(),
                Not = not,
                Bitis = bitis!.Value.Date,
                Tarih = simdi,
                BaslangicSayfa = kayit.MevcutSayfa ?? 0,
                KitapId = kitapId,
                SahipId = ben
            };
            _context.OkumaSozleri.Add(soz);
            await _context.SaveChangesAsync();

            TempData["Mesaj"] = "Söz verildi! Bağlantıyı tanık olmasını istediğin arkadaşlarınla paylaş.";
            return RedirectToAction(nameof(Detay), new { id = soz.Id });
        }

        public async Task<IActionResult> Detay(int id)
        {
            var ben = _userManager.GetUserId(User)!;
            var simdi = DateTime.Now;
            var soz = await _context.OkumaSozleri.AsNoTracking()
                .Include(s => s.Kitap).Include(s => s.Tanikler).Include(s => s.Durtmeler)
                .FirstOrDefaultAsync(s => s.Id == id);
            var sahip = soz?.SahipId == ben;
            var tanik = soz?.Tanikler.Any(t => t.KullaniciId == ben) == true;
            if (soz == null || (!sahip && !tanik))
                return NotFound();

            var kart = (await KartlariOlustur(new List<OkumaSozu> { soz }, simdi))[soz.Id];

            var idler = soz.Tanikler.Select(t => t.KullaniciId).Concat(soz.Durtmeler.Select(d => d.GonderenId)).Distinct().ToList();
            var adlar = await Adlar(idler);

            var sonDurtmem = soz.Durtmeler.Where(d => d.GonderenId == ben && d.Tur != DurtmeTuru.Tebrikler).Select(d => (DateTime?)d.Tarih).DefaultIfEmpty().Max();
            var pencere = simdi.AddDays(-SozKurali.DurtmeGostermeGunu);
            return View(new SozDetayViewModel
            {
                Kart = kart,
                Sahip = sahip,
                Baglanti = Url.Action(nameof(Ac), "Soz", new { kod = soz.Kod }, Request.Scheme)!,
                Tanikler = soz.Tanikler.OrderBy(t => t.Tarih).Select(t => adlar.GetValueOrDefault(t.KullaniciId, "okur")).ToList(),
                Durtmeler = soz.Durtmeler.Where(d => d.Tarih >= pencere).OrderByDescending(d => d.Tarih)
                    .Select(d => new SozDurtmeGorunumu(adlar.GetValueOrDefault(d.GonderenId, "okur"), SozKurali.DurtmeMetni(d.Tur), d.Tarih)).ToList(),
                Durtebilir = !sahip && SozKurali.DurtebilirMi(sonDurtmem, simdi),
                TebrikGonderdi = soz.Durtmeler.Any(d => d.GonderenId == ben && d.Tur == DurtmeTuru.Tebrikler),
                UzatilabilirMi = sahip && kart.Durum is SozDurumu.Surmekte or SozDurumu.SuresiDoldu && soz.UzatmaSayisi < SozKurali.EnFazlaUzatma
            });
        }

        // Bağlantıyı açan okurun karşılaştığı sayfa
        [HttpGet]
        public async Task<IActionResult> Ac(string? kod)
        {
            var vm = await AcVerisi(kod);
            if (vm == null)
                return NotFound();
            if (vm.ZatenTanik)
                return RedirectToAction(nameof(Detay), new { id = vm.Kart.Soz.Id });
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Taniklik(string? kod)
        {
            var vm = await AcVerisi(kod);
            if (vm == null)
                return NotFound();
            if (vm.Kendi || vm.ZatenTanik || vm.Dolu || vm.Kart.Durum != SozDurumu.Surmekte)
                return RedirectToAction(nameof(Ac), new { kod });

            _context.SozTanikleri.Add(new SozTanigi { SozId = vm.Kart.Soz.Id, KullaniciId = _userManager.GetUserId(User)!, Tarih = DateTime.Now });
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Çift tıklama: benzersiz indeks korur
            }

            TempData["Mesaj"] = "Tanık oldun! Artık ilerlemeyi görebilir ve nazikçe dürtebilirsin.";
            return RedirectToAction(nameof(Detay), new { id = vm.Kart.Soz.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Durt(int sozId, DurtmeTuru tur)
        {
            var ben = _userManager.GetUserId(User)!;
            var simdi = DateTime.Now;
            var soz = await _context.OkumaSozleri.Include(s => s.Tanikler).Include(s => s.Durtmeler).Include(s => s.Kitap)
                .FirstOrDefaultAsync(s => s.Id == sozId);
            if (soz == null || soz.SahipId == ben || soz.Tanikler.All(t => t.KullaniciId != ben))
                return NotFound();

            var kart = (await KartlariOlustur(new List<OkumaSozu> { soz }, simdi))[soz.Id];
            var son = soz.Durtmeler.Where(d => d.GonderenId == ben && d.Tur != DurtmeTuru.Tebrikler).Select(d => (DateTime?)d.Tarih).DefaultIfEmpty().Max();

            if (!Enum.IsDefined(tur) || !SozKurali.DurtmeUygunMu(tur, kart.Durum))
                TempData["Hata"] = "Bu mesaj şu an gönderilemez.";
            else if (tur == DurtmeTuru.Tebrikler && soz.Durtmeler.Any(d => d.GonderenId == ben && d.Tur == DurtmeTuru.Tebrikler))
                TempData["Hata"] = "Tebrik mesajını zaten gönderdin.";
            // Günlük sınır yalnızca dürtmeler için; tebrik söz başına bir kez gönderilir
            else if (tur != DurtmeTuru.Tebrikler && !SozKurali.DurtebilirMi(son, simdi))
                TempData["Hata"] = "Günde bir kez dürtebilirsin, sabırlı ol.";
            else
            {
                _context.SozDurtmeleri.Add(new SozDurtmesi { SozId = sozId, GonderenId = ben, Tur = tur, Tarih = simdi });
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Mesajın gönderildi.";
            }
            return RedirectToAction(nameof(Detay), new { id = sozId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Uzat(int sozId, DateTime? yeniBitis)
        {
            var ben = _userManager.GetUserId(User)!;
            var simdi = DateTime.Now;
            var soz = await _context.OkumaSozleri.Include(s => s.Kitap).FirstOrDefaultAsync(s => s.Id == sozId && s.SahipId == ben);
            if (soz == null)
                return NotFound();

            var kart = (await KartlariOlustur(new List<OkumaSozu> { soz }, simdi))[soz.Id];
            if (kart.Durum is not (SozDurumu.Surmekte or SozDurumu.SuresiDoldu))
                TempData["Hata"] = "Tamamlanmış bir söz uzatılamaz.";
            else if (yeniBitis == null || !SozKurali.UzatmaGecerliMi(soz.Bitis, yeniBitis.Value, soz.UzatmaSayisi, simdi))
                TempData["Hata"] = $"Sözü yalnızca bir kez ve en fazla {SozKurali.UzatmaEnFazlaGun} gün uzatabilirsin.";
            else
            {
                soz.Bitis = yeniBitis.Value.Date;
                soz.UzatmaSayisi++;
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Söz uzatıldı. Bu kadar süre yeter, sana güveniyoruz!";
            }
            return RedirectToAction(nameof(Detay), new { id = sozId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vazgec(int sozId)
        {
            var ben = _userManager.GetUserId(User)!;
            var soz = await _context.OkumaSozleri.FirstOrDefaultAsync(s => s.Id == sozId && s.SahipId == ben);
            if (soz == null)
                return NotFound();

            _context.OkumaSozleri.Remove(soz);
            await _context.SaveChangesAsync();
            TempData["Mesaj"] = "Söz geri çekildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaniklikBirak(int sozId)
        {
            var ben = _userManager.GetUserId(User)!;
            var tanik = await _context.SozTanikleri.FirstOrDefaultAsync(t => t.SozId == sozId && t.KullaniciId == ben);
            if (tanik == null)
                return NotFound();

            _context.SozTanikleri.Remove(tanik);
            await _context.SaveChangesAsync();
            TempData["Mesaj"] = "Tanıklıktan çıktın.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<SozAcViewModel?> AcVerisi(string? kod)
        {
            kod = (kod ?? string.Empty).Trim().ToUpperInvariant();
            var soz = kod.Length == 0 ? null : await _context.OkumaSozleri.AsNoTracking()
                .Include(s => s.Kitap).Include(s => s.Tanikler)
                .FirstOrDefaultAsync(s => s.Kod == kod);
            if (soz == null)
                return null;

            var ben = _userManager.GetUserId(User)!;
            var kart = (await KartlariOlustur(new List<OkumaSozu> { soz }, DateTime.Now))[soz.Id];
            return new SozAcViewModel
            {
                Kart = kart,
                Kod = soz.Kod,
                Kendi = soz.SahipId == ben,
                ZatenTanik = soz.Tanikler.Any(t => t.KullaniciId == ben),
                Dolu = soz.Tanikler.Count >= SozKurali.EnFazlaTanik
            };
        }

        private Task<Dictionary<int, SozKarti>> KartlariOlustur(List<OkumaSozu> sozler, DateTime simdi) =>
            _servis.KartlariOlusturAsync(sozler, simdi);

        private Task<Dictionary<string, string>> Adlar(IEnumerable<string> idler) => _servis.AdlarAsync(idler);

        private async Task<string> YeniKodUret()
        {
            while (true)
            {
                var kod = string.Create(OkumaSozu.KodUzunlugu, 0, (span, _) =>
                {
                    for (var i = 0; i < span.Length; i++)
                        span[i] = KodAlfabesi[RandomNumberGenerator.GetInt32(KodAlfabesi.Length)];
                });
                if (!await _context.OkumaSozleri.AnyAsync(s => s.Kod == kod))
                    return kod;
            }
        }
    }

    public record SozDurtmeGorunumu(string Gonderen, string Mesaj, DateTime Tarih);

    public class SozIndexViewModel
    {
        public List<SozKarti> Benim { get; set; } = new();
        public List<SozKarti> Taniklarim { get; set; } = new();
    }

    public class SozDetayViewModel
    {
        public SozKarti Kart { get; set; } = null!;
        public bool Sahip { get; set; }
        public string Baglanti { get; set; } = string.Empty;
        public List<string> Tanikler { get; set; } = new();
        public List<SozDurtmeGorunumu> Durtmeler { get; set; } = new();
        public bool Durtebilir { get; set; }
        public bool TebrikGonderdi { get; set; }
        public bool UzatilabilirMi { get; set; }
    }

    public class SozAcViewModel
    {
        public SozKarti Kart { get; set; } = null!;
        public string Kod { get; set; } = string.Empty;
        public bool Kendi { get; set; }
        public bool ZatenTanik { get; set; }
        public bool Dolu { get; set; }
    }
}
