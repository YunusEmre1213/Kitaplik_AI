using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitapYonetimSistemi.Components
{
    // Üst çubukta, kullanıcının açık bir okuma oturumu varsa canlı sayaç gösterir
    public class AktifOturumViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AktifOturumViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (UserClaimsPrincipal.Identity?.IsAuthenticated != true || !UserClaimsPrincipal.IsInRole("User"))
                return Content(string.Empty);

            var kullaniciId = _userManager.GetUserId(UserClaimsPrincipal);
            var oturum = await _context.OkumaOturumlari
                .AsNoTracking()
                .Include(o => o.KullaniciKitaplik!).ThenInclude(kk => kk.Kitap)
                .FirstOrDefaultAsync(o => o.Bitis == null && o.KullaniciKitaplik!.KullaniciId == kullaniciId);

            return oturum == null ? Content(string.Empty) : View(oturum);
        }
    }
}
