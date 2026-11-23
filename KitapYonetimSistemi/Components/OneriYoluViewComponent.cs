using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitapYonetimSistemi.Components
{
    // Kitap sayfasında: kitap okura hangi zincirle ulaştı ve "birine öner" formu
    public class OneriYoluViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OneriYoluViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(int kitapId)
        {
            if (UserClaimsPrincipal.Identity?.IsAuthenticated != true || !UserClaimsPrincipal.IsInRole("User"))
                return Content(string.Empty);

            var ben = _userManager.GetUserId(UserClaimsPrincipal)!;
            var oneriler = await _context.Oneriler.AsNoTracking()
                .Where(o => o.KitapId == kitapId)
                .Select(o => new OneriKaydi(o.Id, o.GonderenId, o.OncekiKabulId))
                .ToListAsync();
            var kabuller = await _context.OneriKabulleri.AsNoTracking()
                .Where(k => k.KitapId == kitapId)
                .Select(k => new KabulKaydi(k.Id, k.OneriId, k.KullaniciId, false))
                .ToListAsync();

            var benimKabulum = kabuller.FirstOrDefault(k => k.KullaniciId == ben);
            var yol = benimKabulum == null ? new List<string?>() : OneriZinciri.Yol(benimKabulum.OneriId, oneriler, kabuller);
            var idler = yol.Where(i => i != null).Select(i => i!).Distinct().ToList();
            var adlar = (await _context.Users.AsNoTracking().Where(u => idler.Contains(u.Id)).Select(u => new { u.Id, u.UserName }).ToListAsync())
                .ToDictionary(u => u.Id, u => string.IsNullOrEmpty(u.UserName) ? "okur" : u.UserName.Split('@')[0]);

            return View(new OneriYoluModel
            {
                KitapId = kitapId,
                Yol = yol.Select(i => i != null && adlar.TryGetValue(i, out var a) ? a : "silinmiş okur").ToList()
            });
        }
    }

    public class OneriYoluModel
    {
        public int KitapId { get; set; }
        public List<string> Yol { get; set; } = new();
    }
}
