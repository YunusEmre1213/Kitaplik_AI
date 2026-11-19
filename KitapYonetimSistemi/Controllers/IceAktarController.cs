using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Controllers
{
    // Goodreads dışa aktarımı ya da Türkçe/İngilizce başlıklı CSV ile kitaplığı toplu ekler.
    // Yalnızca katalogda bulunan kitaplar eklenir; var olan kitaplık kayıtlarına ve puanlara dokunulmaz.
    [Authorize(Roles = "User")]
    public class IceAktarController : Controller
    {
        private const long EnFazlaBayt = 2 * 1024 * 1024;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IceAktarController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index() => View(new IceAktarSonucu());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(EnFazlaBayt + 4096)]
        public async Task<IActionResult> Index(IFormFile? dosya)
        {
            var sonuc = new IceAktarSonucu { Denendi = true };
            if (dosya == null || dosya.Length == 0)
            {
                sonuc.Hata = "Bir CSV dosyası seçmelisin.";
                return View(sonuc);
            }
            if (dosya.Length > EnFazlaBayt)
            {
                sonuc.Hata = "Dosya en fazla 2 MB olabilir.";
                return View(sonuc);
            }

            string metin;
            await using (var akis = dosya.OpenReadStream())
            using (var ms = new MemoryStream())
            {
                await akis.CopyToAsync(ms);
                metin = Coz(ms.ToArray());
            }

            var okunan = KitaplikCsv.Oku(metin);
            if (!okunan.BaslikBulundu)
            {
                sonuc.Hata = "Dosyada kitap adı sütunu bulunamadı. İlk satırda \"Title\" ya da \"Kitap Adı\" gibi bir başlık olmalı.";
                return View(sonuc);
            }

            var kullaniciId = _userManager.GetUserId(User)!;
            var mevcutKitaplar = (await _context.KullaniciKitapliklari.AsNoTracking()
                .Where(kk => kk.KullaniciId == kullaniciId).Select(kk => kk.KitapId).ToListAsync()).ToHashSet();
            var puanlilar = (await _context.KitapPuanlari.AsNoTracking()
                .Where(p => p.KullaniciId == kullaniciId).Select(p => p.KitapId).ToListAsync()).ToHashSet();

            var simdi = DateTime.Now;
            sonuc.Toplam = okunan.Satirlar.Count;
            sonuc.BosAtlanan = okunan.Atlanan;
            sonuc.Kirpildi = okunan.Kirpildi;

            foreach (var s in okunan.Satirlar)
            {
                var kitap = await Eslestir(s);
                if (kitap == null)
                {
                    sonuc.Bulunamayan.Add($"{s.Baslik}{(string.IsNullOrEmpty(s.Yazar) ? "" : " – " + s.Yazar)}");
                    continue;
                }

                if (!mevcutKitaplar.Add(kitap.Id))
                {
                    sonuc.ZatenVar++;
                    continue;
                }

                var kayit = new KullaniciKitaplik
                {
                    KullaniciId = kullaniciId,
                    KitapId = kitap.Id,
                    HerkesGorebilir = false,
                    EklenmeTarihi = simdi,
                    OkumaDurumu = s.Durum
                };
                if (s.Durum == OkumaDurumu.Okundu)
                {
                    var tarih = s.OkunmaTarihi is { } t && t <= simdi ? t : simdi;
                    kayit.OkumayaBaslamaTarihi = tarih;
                    kayit.OkumaBitisTarihi = tarih;
                    kayit.MevcutSayfa = kitap.SayfaSayisi;
                }
                else if (s.Durum == OkumaDurumu.Okuyorum)
                {
                    kayit.OkumayaBaslamaTarihi = simdi;
                    kayit.MevcutSayfa = 0;
                }
                _context.KullaniciKitapliklari.Add(kayit);
                sonuc.Eklenen++;

                if (s.Puan.HasValue && puanlilar.Add(kitap.Id))
                {
                    _context.KitapPuanlari.Add(new KitapPuan { KitapId = kitap.Id, KullaniciId = kullaniciId, Puan = s.Puan.Value, PuanTarihi = simdi });
                    sonuc.PuanlananSayisi++;
                }
            }

            await _context.SaveChangesAsync();
            return View(sonuc);
        }

        // Önce ISBN, sonra aynı ad + uyuşan yazar soyadı ile katalogda arar
        private async Task<Kitap?> Eslestir(IceAktarilanSatir s)
        {
            if (s.Isbn != null)
            {
                var isbnli = await _context.Kitaplar.AsNoTracking()
                    .Where(k => k.Isbn13 == s.Isbn || k.Isbn10 == s.Isbn || k.Isbn == s.Isbn)
                    .OrderBy(k => k.Id).FirstOrDefaultAsync();
                if (isbnli != null) return isbnli;
            }

            var adaylar = await _context.Kitaplar.AsNoTracking()
                .Where(k => k.KitapAdi == s.Baslik).OrderBy(k => k.Id).Take(20).ToListAsync();
            if (adaylar.Count == 0) return null;

            var yazar = KitaplikCsv.Katla(s.Yazar);
            if (yazar.Length == 0)
                return adaylar.Count == 1 ? adaylar[0] : null;

            // Yazarın en az bir sözcüğü (ad ya da soyad) katalogdaki yazarla örtüşmeli
            var yazarSozcukleri = yazar.Split(' ').Where(w => w.Length > 2).ToArray();
            return adaylar.FirstOrDefault(k =>
            {
                var katalogYazar = KitaplikCsv.Katla(k.YazarAdi);
                return yazarSozcukleri.Length > 0 && yazarSozcukleri.Any(w => katalogYazar.Contains(w));
            });
        }

        // UTF-8 olarak geçerliyse onu, değilse Excel'in Türkçe kodlamasını (Windows-1254) kullanır
        private static string Coz(byte[] veri)
        {
            try
            {
                return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(veri);
            }
            catch (DecoderFallbackException)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(1254).GetString(veri);
            }
        }
    }

    public class IceAktarSonucu
    {
        public bool Denendi { get; set; }
        public string? Hata { get; set; }
        public int Toplam { get; set; }
        public int Eklenen { get; set; }
        public int ZatenVar { get; set; }
        public int PuanlananSayisi { get; set; }
        public int BosAtlanan { get; set; }
        public bool Kirpildi { get; set; }
        public List<string> Bulunamayan { get; set; } = new();
    }
}
