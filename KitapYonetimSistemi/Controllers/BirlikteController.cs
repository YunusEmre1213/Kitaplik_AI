using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Controllers
{
    // Birlikte okuma merkezi: okuma odaları (serbest tempo) ve sezonlar (takvimli) tek listede, tek giriş noktasıyla.
    [Authorize(Roles = "User")]
    public class BirlikteController : Controller
    {
        private const int EnCokAcikSezon = 20;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BirlikteController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var ben = _userManager.GetUserId(User)!;
            var simdi = DateTime.Now;
            var vm = new BirlikteViewModel();

            var odalar = await _context.OkumaOdalari.AsNoTracking()
                .Include(o => o.Kitap).Include(o => o.Uyeler)
                .Where(o => o.Uyeler.Any(u => u.KullaniciId == ben))
                .ToListAsync();
            foreach (var o in odalar)
            {
                vm.Benimkiler.Add(new BirlikteOgesi
                {
                    Tur = BirlikteTuru.Oda,
                    Id = o.Id,
                    Ad = o.Ad,
                    KitapAdi = o.Kitap?.KitapAdi ?? string.Empty,
                    Ozet = $"{o.Uyeler.Count} kişi · serbest tempo",
                    Sira = 1,
                    Tarih = o.Tarih
                });
            }

            var sezonlar = await _context.OkumaSezonlari.AsNoTracking()
                .Include(s => s.Kitap).Include(s => s.Uyeler)
                .ToListAsync();
            foreach (var s in sezonlar)
            {
                var takvim = new SezonTakvimi(s.Baslangic, s.GunSayisi, s.Kitap?.SayfaSayisi ?? 1);
                var durum = takvim.Durum(simdi);
                var uye = s.Uyeler.FirstOrDefault(u => u.KullaniciId == ben);
                var ozet = durum switch
                {
                    SezonDurumu.Yaklasan => $"{s.Baslangic:dd.MM.yyyy} tarihinde başlıyor",
                    SezonDurumu.Surmekte => $"bugün {takvim.BugunBolum(simdi)}. bölüm / {takvim.BolumSayisi}",
                    _ => "sezon bitti, sohbetler açık"
                };

                if (uye != null)
                {
                    vm.Benimkiler.Add(new BirlikteOgesi
                    {
                        Tur = BirlikteTuru.Sezon,
                        Id = s.Id,
                        Ad = s.Ad,
                        KitapAdi = s.Kitap?.KitapAdi ?? string.Empty,
                        Ozet = $"{s.Uyeler.Count} okur · {ozet}",
                        // Sürenler ve yaklaşanlar önde, biten sezonlar en sonda
                        Sira = durum == SezonDurumu.Surmekte ? 0 : durum == SezonDurumu.Yaklasan ? 0 : 2,
                        Tarih = s.Baslangic
                    });
                }
                else if (durum != SezonDurumu.Bitti)
                {
                    vm.Acik.Add(new BirlikteOgesi
                    {
                        Tur = BirlikteTuru.Sezon,
                        Id = s.Id,
                        Ad = s.Ad,
                        KitapAdi = s.Kitap?.KitapAdi ?? string.Empty,
                        Ozet = $"{s.Uyeler.Count} okur · {ozet}",
                        Sira = 0,
                        Tarih = s.Baslangic
                    });
                }
            }

            vm.Benimkiler = vm.Benimkiler.OrderBy(x => x.Sira).ThenByDescending(x => x.Tarih).ToList();
            vm.Acik = vm.Acik.OrderBy(x => x.Tarih).Take(EnCokAcikSezon).ToList();
            return View(vm);
        }
    }

    public enum BirlikteTuru
    {
        Oda,
        Sezon
    }

    public class BirlikteOgesi
    {
        public BirlikteTuru Tur { get; set; }
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string KitapAdi { get; set; } = string.Empty;
        public string Ozet { get; set; } = string.Empty;
        public int Sira { get; set; }
        public DateTime Tarih { get; set; }
    }

    public class BirlikteViewModel
    {
        public List<BirlikteOgesi> Benimkiler { get; set; } = new();
        public List<BirlikteOgesi> Acik { get; set; } = new();
    }
}
