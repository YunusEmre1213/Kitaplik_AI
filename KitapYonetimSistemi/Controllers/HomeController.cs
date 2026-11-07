using System.Diagnostics;
using KitapYonetimSistemi.Models;
using KitapYonetimSistemi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitapYonetimSistemi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Ana sayfada gösterilecek son eklenen kitaplar (kapaklı ve açıklamalı olanlar öne alınır)
        public async Task<IActionResult> Index()
        {
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
