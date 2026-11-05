using System.ComponentModel.DataAnnotations;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitapYonetimSistemi.Controllers
{
    // Kitap kataloğunu JSON olarak sunan salt okunur API.
    [ApiController]
    [AllowAnonymous]
    [Route("api/books")]
    public class KitapApiController : ControllerBase
    {
        private const int EnBuyukSayfaBoyutu = 50;
        private const string KapakYokUrl = "/img/kapak-yok.svg";

        private readonly ApplicationDbContext _context;

        public KitapApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/books?sayfa=1&boyut=20&dil=tr&tur=Roman
        [HttpGet]
        public async Task<ActionResult<SayfaliSonuc<KitapOzetDto>>> Listele(
            [FromQuery, Range(1, int.MaxValue)] int sayfa = 1,
            [FromQuery, Range(1, EnBuyukSayfaBoyutu)] int boyut = 20,
            [FromQuery, StringLength(10)] string? dil = null,
            [FromQuery, StringLength(100)] string? tur = null,
            CancellationToken iptal = default)
        {
            var sorgu = _context.Kitaplar.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(dil))
                sorgu = sorgu.Where(k => k.Dil == dil);
            if (!string.IsNullOrWhiteSpace(tur))
                sorgu = sorgu.Where(k => k.Tur == tur);

            return Ok(await Sayfala(sorgu.OrderByDescending(k => k.EklenmeTarihi).ThenBy(k => k.Id), sayfa, boyut, iptal));
        }

        // GET api/books/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<KitapDetayDto>> Detay(int id, CancellationToken iptal)
        {
            var kitap = await _context.Kitaplar.AsNoTracking()
                .Where(k => k.Id == id)
                .Select(k => new KitapDetayDto
                {
                    Id = k.Id,
                    Baslik = k.KitapAdi,
                    Yazar = k.YazarAdi,
                    KapakUrl = k.KapakResmiUrl ?? KapakYokUrl,
                    YayinYili = k.YayinYili,
                    Dil = k.Dil,
                    Tur = k.Tur,
                    SayfaSayisi = k.SayfaSayisi,
                    Isbn13 = k.Isbn13 ?? k.Isbn,
                    Aciklama = k.Aciklama,
                    Isbn10 = k.Isbn10,
                    YayinTarihi = k.YayinTarihi,
                    Yayinevi = k.Yayinevi != null ? k.Yayinevi.Ad : null,
                    OpenLibraryEditionKey = k.OpenLibraryEditionKey,
                    OpenLibraryWorkKey = k.OpenLibraryWorkKey,
                    Yazarlar = k.Yazarlar.OrderBy(ky => ky.Sira)
                        .Select(ky => new YazarDto { Id = ky.YazarId, Ad = ky.Yazar!.Ad }).ToList(),
                    Kategoriler = k.Kategoriler.OrderBy(kk => kk.Kategori!.Ad)
                        .Select(kk => new KategoriDto { Id = kk.KategoriId, Ad = kk.Kategori!.Ad }).ToList()
                })
                .AsSplitQuery()
                .FirstOrDefaultAsync(iptal);

            if (kitap == null)
                return NotFound(new ProblemDetails { Title = "Kitap bulunamadı.", Status = StatusCodes.Status404NotFound });

            // Aynı eserin diğer baskıları (farklı baskılar tek kayıtta birleştirilmez, burada yan yana gösterilir)
            if (!string.IsNullOrEmpty(kitap.OpenLibraryWorkKey))
            {
                kitap.DigerBaskilar = await _context.Kitaplar.AsNoTracking()
                    .Where(k => k.OpenLibraryWorkKey == kitap.OpenLibraryWorkKey && k.Id != id)
                    .OrderBy(k => k.YayinYili)
                    .Take(10)
                    .Select(k => new DigerBaskiDto
                    {
                        Id = k.Id,
                        Baslik = k.KitapAdi,
                        Dil = k.Dil,
                        YayinYili = k.YayinYili,
                        Yayinevi = k.Yayinevi != null ? k.Yayinevi.Ad : null
                    })
                    .ToListAsync(iptal);
            }

            return Ok(kitap);
        }

        // GET api/books/search?query=suç
        // Önce başlık/yazar adının başından eşleşme aranır (indeks kullanır). Sonuç yoksa içinde geçenlere bakılır.
        [HttpGet("search")]
        public async Task<ActionResult<SayfaliSonuc<KitapOzetDto>>> Ara(
            [FromQuery, Required, StringLength(100, MinimumLength = 2)] string query,
            [FromQuery, Range(1, int.MaxValue)] int sayfa = 1,
            [FromQuery, Range(1, EnBuyukSayfaBoyutu)] int boyut = 20,
            CancellationToken iptal = default)
        {
            var q = query.Trim();
            if (q.Length < 2)
                return BadRequest(new ProblemDetails { Title = "Arama en az 2 karakter olmalıdır.", Status = StatusCodes.Status400BadRequest });

            var baslayan = _context.Kitaplar.AsNoTracking()
                .Where(k => EF.Functions.Like(k.KitapAdi, EscapeLike(q) + "%", "\\")
                         || EF.Functions.Like(k.YazarAdi, EscapeLike(q) + "%", "\\"));

            var sonuc = await Sayfala(baslayan.OrderBy(k => k.KitapAdi).ThenBy(k => k.Id), sayfa, boyut, iptal);

            if (sonuc.Toplam == 0)
            {
                var icinde = _context.Kitaplar.AsNoTracking()
                    .Where(k => EF.Functions.Like(k.KitapAdi, "%" + EscapeLike(q) + "%", "\\")
                             || EF.Functions.Like(k.YazarAdi, "%" + EscapeLike(q) + "%", "\\"));
                sonuc = await Sayfala(icinde.OrderBy(k => k.KitapAdi).ThenBy(k => k.Id), sayfa, boyut, iptal);
            }

            return Ok(sonuc);
        }

        // GET api/books/author/12
        [HttpGet("author/{yazarId:int}")]
        public async Task<ActionResult<SayfaliSonuc<KitapOzetDto>>> YazaraGore(
            int yazarId,
            [FromQuery, Range(1, int.MaxValue)] int sayfa = 1,
            [FromQuery, Range(1, EnBuyukSayfaBoyutu)] int boyut = 20,
            CancellationToken iptal = default)
        {
            if (!await _context.Yazarlar.AsNoTracking().AnyAsync(y => y.Id == yazarId, iptal))
                return NotFound(new ProblemDetails { Title = "Yazar bulunamadı.", Status = StatusCodes.Status404NotFound });

            var sorgu = _context.Kitaplar.AsNoTracking()
                .Where(k => k.Yazarlar.Any(ky => ky.YazarId == yazarId))
                .OrderBy(k => k.YayinYili).ThenBy(k => k.Id);

            return Ok(await Sayfala(sorgu, sayfa, boyut, iptal));
        }

        // GET api/books/category/3
        [HttpGet("category/{kategoriId:int}")]
        public async Task<ActionResult<SayfaliSonuc<KitapOzetDto>>> KategoriyeGore(
            int kategoriId,
            [FromQuery, Range(1, int.MaxValue)] int sayfa = 1,
            [FromQuery, Range(1, EnBuyukSayfaBoyutu)] int boyut = 20,
            CancellationToken iptal = default)
        {
            if (!await _context.Kategoriler.AsNoTracking().AnyAsync(k => k.Id == kategoriId, iptal))
                return NotFound(new ProblemDetails { Title = "Kategori bulunamadı.", Status = StatusCodes.Status404NotFound });

            var sorgu = _context.Kitaplar.AsNoTracking()
                .Where(k => k.Kategoriler.Any(kk => kk.KategoriId == kategoriId))
                .OrderBy(k => k.KitapAdi).ThenBy(k => k.Id);

            return Ok(await Sayfala(sorgu, sayfa, boyut, iptal));
        }

        // GET api/books/categories
        [HttpGet("categories")]
        public async Task<ActionResult<List<KategoriDto>>> Kategoriler(CancellationToken iptal)
        {
            var liste = await _context.Kategoriler.AsNoTracking()
                .OrderBy(k => k.Ad)
                .Select(k => new KategoriDto { Id = k.Id, Ad = k.Ad })
                .ToListAsync(iptal);

            return Ok(liste);
        }

        // ---------- yardımcılar ----------

        // Tek SQL ile sayfa içeriği (yazar adları ve kapak dahil) çekilir; kitap başına ek sorgu yapılmaz.
        private static async Task<SayfaliSonuc<KitapOzetDto>> Sayfala(IOrderedQueryable<Kitap> sorgu, int sayfa, int boyut, CancellationToken iptal)
        {
            var toplam = await sorgu.CountAsync(iptal);

            var ogeler = await sorgu
                .Skip((sayfa - 1) * boyut)
                .Take(boyut)
                .Select(k => new KitapOzetDto
                {
                    Id = k.Id,
                    Baslik = k.KitapAdi,
                    Yazar = k.YazarAdi,
                    KapakUrl = k.KapakResmiUrl ?? KapakYokUrl,
                    YayinYili = k.YayinYili,
                    Dil = k.Dil,
                    Tur = k.Tur,
                    SayfaSayisi = k.SayfaSayisi,
                    Isbn13 = k.Isbn13 ?? k.Isbn
                })
                .ToListAsync(iptal);

            return new SayfaliSonuc<KitapOzetDto>
            {
                Sayfa = sayfa,
                Boyut = boyut,
                Toplam = toplam,
                ToplamSayfa = (int)Math.Ceiling(toplam / (double)boyut),
                Ogeler = ogeler
            };
        }

        // LIKE joker karakterlerini kaçırır
        private static string EscapeLike(string metin) =>
            metin.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
    }
}
