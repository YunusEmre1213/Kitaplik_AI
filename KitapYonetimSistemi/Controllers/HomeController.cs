using System.Diagnostics;
using KitapYonetimSistemi.Models;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitapYonetimSistemi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly BugunServisi _bugun;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, BugunServisi bugun, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _bugun = bugun;
            _userManager = userManager;
        }

        // Okur için "Bugün" ekranı; ziyaretçi ve yönetici için tanıtım sayfası (son eklenen kitaplar)
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("User"))
            {
                var kullaniciId = _userManager.GetUserId(User)!;
                var vm = await _bugun.HazirlaAsync(kullaniciId, User.Identity.Name, DateTime.Now);
                return View("Bugun", vm);
            }

            var yeniler = await _context.Kitaplar
                .AsNoTracking()
                .Where(k => k.KapakResmiUrl != null && k.KapakResmiUrl != "")
                .OrderByDescending(k => k.Aciklama != "")
                .ThenByDescending(k => k.Id)
                .Take(8)
                .ToListAsync();
            return View(yeniler);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
