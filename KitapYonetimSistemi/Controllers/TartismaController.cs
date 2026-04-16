using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class TartismaController : Controller
{
    private readonly ApplicationDbContext _context;

    public TartismaController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()//bütün tartışmaları getir
    {
        var tartismalar = _context.Tartismalar
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        return View(tartismalar);
    }
    [Authorize]
    public IActionResult Create()
    {
        return View();
    }


    [HttpPost]
    [Authorize]
    public IActionResult Create(Tartisma tartisma)
    {
        tartisma.CreatedAt = DateTime.Now;
        tartisma.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _context.Tartismalar.Add(tartisma);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }
    public IActionResult Details(int id)
    {
        var tartisma = _context.Tartismalar
            .Include(t => t.User)
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefault(t => t.Id == id);

        return View(tartisma);
    }
    [HttpPost]
    [Authorize]
    public IActionResult AddComment(Comment comment)
    {
        comment.CreatedAt = DateTime.Now;
        comment.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _context.Comments.Add(comment);
        _context.SaveChanges();

        return RedirectToAction("Details", new { id = comment.TartismaId });
    }
  

}