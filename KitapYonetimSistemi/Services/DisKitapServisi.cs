using System.Text.Json;
using System.Text.RegularExpressions;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KitapYonetimSistemi.Services
{
    public class DisKitapSonuc
    {
        public string DisKaynakId { get; set; } = string.Empty;
        public string Baslik { get; set; } = string.Empty;
        public string Yazar { get; set; } = string.Empty;
        public int Yil { get; set; }
        public int? SayfaSayisi { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public string? Tur { get; set; }
        public string? KapakUrl { get; set; }
        public string? Isbn { get; set; }

        // Kitap zaten kataloğumuzdaysa id'si
        public int? KatalogId { get; set; }

        // Kitabın bulunduğu kaynak, kimliğin önekinden anlaşılır
        public string Kaynak => DisKaynakId.StartsWith("gb:") ? "Google Books" : "Open Library";
    }

    // Google Books üzerinden kitap arar ve seçilen kitabı kataloğa aktarır.
    public class DisKitapServisi
    {
        public const string GoogleIstemciAdi = "GoogleBooks";
        public const string OpenLibraryIstemciAdi = "OpenLibrary";
        private const string GoogleAdres = "https://www.googleapis.com/books/v1/volumes";
        private const string OpenLibraryAdres = "https://openlibrary.org";
        private const string GooglePrefix = "gb:";
        private const string OpenLibraryPrefix = "ol:";

        private static readonly (string Anahtar, string Tur)[] TurEslemeleri =
        {
            ("science fiction", "Bilim Kurgu"), ("bilim kurgu", "Bilim Kurgu"),
            ("fantasy", "Fantastik"), ("fantastik", "Fantastik"),
            ("mystery", "Polisiye"), ("crime", "Polisiye"), ("detective", "Polisiye"), ("thriller", "Polisiye"), ("polisiye", "Polisiye"),
            ("biograph", "Biyografi"), ("biyografi", "Biyografi"), ("autobiograph", "Biyografi"),
            ("self-help", "Kişisel Gelişim"), ("kişisel gelişim", "Kişisel Gelişim"),
            ("psycholog", "Psikoloji"), ("psikoloji", "Psikoloji"),
            ("philosoph", "Felsefe"), ("felsefe", "Felsefe"),
            ("histor", "Tarih"), ("tarih", "Tarih"),
            ("poetry", "Şiir"), ("şiir", "Şiir"),
            ("juvenile", "Çocuk"), ("children", "Çocuk"), ("çocuk", "Çocuk"),
            ("short stories", "Hikâye"), ("hikaye", "Hikâye"), ("hikâye", "Hikâye"),
            ("science", "Bilim"), ("nature", "Bilim"), ("bilim", "Bilim"),
            ("fiction", "Roman"), ("novel", "Roman"), ("literary", "Roman"), ("roman", "Roman")
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DisKitapServisi> _logger;

        public DisKitapServisi(
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext context,
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<DisKitapServisi> logger)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        // Başlık, yazar ya da ISBN ile arar. Dış servis hata verirse boş liste döner.
        public async Task<List<DisKitapSonuc>> AraAsync(string arama, int adet = 8)
        {
            arama = (arama ?? string.Empty).Trim();
            if (arama.Length < 2 || arama.Length > 100)
                return new List<DisKitapSonuc>();

            var cacheKey = $"DisArama_{adet}_{arama.ToLowerInvariant()}";
            if (!_cache.TryGetValue(cacheKey, out List<DisKitapSonuc>? sonuclar) || sonuclar == null)
            {
                sonuclar = await SaglayicilardaAraAsync(arama, adet);

                // Hata durumunda boş sonuç önbelleğe alınmaz
                if (sonuclar.Count > 0)
                    _cache.Set(cacheKey, sonuclar, TimeSpan.FromMinutes(10));
            }

            // Önbellekteki liste paylaşıldığı için kopya üzerinde katalog eşleşmesi yapılır
            var kopya = sonuclar.Select(s => new DisKitapSonuc
            {
                DisKaynakId = s.DisKaynakId, Baslik = s.Baslik, Yazar = s.Yazar, Yil = s.Yil,
                SayfaSayisi = s.SayfaSayisi, Aciklama = s.Aciklama, Tur = s.Tur, KapakUrl = s.KapakUrl, Isbn = s.Isbn
            }).ToList();

            foreach (var sonuc in kopya)
                sonuc.KatalogId = await KatalogdaBulAsync(sonuc);

            return kopya;
        }

        // Seçilen kitabı kimliğinden dış servisten tekrar çeker (istemciden gelen alanlara güvenilmez)
        // ve kataloğa ekler. Kitap zaten varsa mevcut kaydı döner.
        public async Task<Kitap?> IceAktarAsync(string disKaynakId)
        {
            if (string.IsNullOrWhiteSpace(disKaynakId) || disKaynakId.Length > 40 || !Regex.IsMatch(disKaynakId, "^(gb|ol):[A-Za-z0-9_-]+$"))
                return null;

            var mevcut = await _context.Kitaplar.FirstOrDefaultAsync(k => k.DisKaynakId == disKaynakId);
            if (mevcut != null)
                return mevcut;

            var sonuc = disKaynakId.StartsWith(GooglePrefix)
                ? (await GoogleGetirAsync($"/{disKaynakId[GooglePrefix.Length..]}", tekKitap: true)).FirstOrDefault()
                : (await OpenLibraryAraAsync($"key:/works/{disKaynakId[OpenLibraryPrefix.Length..]}", 1, aciklamaGetir: true)).FirstOrDefault();

            if (sonuc == null)
                return null;

            var katalogId = await KatalogdaBulAsync(sonuc);
            if (katalogId.HasValue)
                return await _context.Kitaplar.FindAsync(katalogId.Value);

            var kitap = new Kitap
            {
                KitapAdi = Kisalt(sonuc.Baslik, 300),
                YazarAdi = Kisalt(sonuc.Yazar, 300),
                YayinYili = sonuc.Yil,
                Aciklama = Kisalt(sonuc.Aciklama, 2000),
                SayfaSayisi = sonuc.SayfaSayisi is > 0 and <= 20000 ? sonuc.SayfaSayisi : null,
                Tur = sonuc.Tur,
                KapakResmiUrl = sonuc.KapakUrl,
                Isbn = sonuc.Isbn,
                DisKaynakId = sonuc.DisKaynakId,
                EklenmeTarihi = DateTime.Now
            };

            _context.Kitaplar.Add(kitap);
            await _context.SaveChangesAsync();
            return kitap;
        }

        private async Task<int?> KatalogdaBulAsync(DisKitapSonuc sonuc)
        {
            var isbn = sonuc.Isbn;
            var kaynakId = sonuc.DisKaynakId;
            var baslik = sonuc.Baslik.ToLower();
            var yazar = sonuc.Yazar.ToLower();

            return await _context.Kitaplar
                .Where(k => k.DisKaynakId == kaynakId
                            || (isbn != null && k.Isbn == isbn)
                            || (k.KitapAdi.ToLower() == baslik && k.YazarAdi.ToLower() == yazar))
                .Select(k => (int?)k.Id)
                .FirstOrDefaultAsync();
        }

        // Google Books anahtarı tanımlıysa önce o denenir. Anahtarsız istekler ortak kotaya takıldığı
        // için güvenilir değildir. Sonuç gelmezse ya da anahtar yoksa Open Library kullanılır.
        private async Task<List<DisKitapSonuc>> SaglayicilardaAraAsync(string arama, int adet)
        {
            if (!string.IsNullOrEmpty(_configuration["GoogleBooks:ApiKey"]))
            {
                var google = await GoogleGetirAsync(IsbnMi(arama)
                    ? $"?q=isbn:{Uri.EscapeDataString(IsbnTemizle(arama))}&maxResults={adet}"
                    : $"?q={Uri.EscapeDataString(arama)}&maxResults={adet}&printType=books");

                if (google.Count > 0)
                    return google;
            }

            return await OpenLibraryAraAsync(IsbnMi(arama) ? $"isbn:{IsbnTemizle(arama)}" : arama, adet);
        }

        private async Task<List<DisKitapSonuc>> OpenLibraryAraAsync(string sorgu, int adet, bool aciklamaGetir = false)
        {
            var sonuclar = new List<DisKitapSonuc>();

            try
            {
                var istemci = _httpClientFactory.CreateClient(OpenLibraryIstemciAdi);
                var adres = $"{OpenLibraryAdres}/search.json?q={Uri.EscapeDataString(sorgu)}&limit={adet}" +
                            "&fields=key,title,author_name,first_publish_year,number_of_pages_median,isbn,cover_i,subject";

                using var yanit = await istemci.GetAsync(adres);
                if (!yanit.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Open Library isteği başarısız: {Kod}", (int)yanit.StatusCode);
                    return sonuclar;
                }

                using var doc = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());
                if (!doc.RootElement.TryGetProperty("docs", out var belgeler))
                    return sonuclar;

                foreach (var belge in belgeler.EnumerateArray())
                {
                    var sonuc = OpenLibraryEsle(belge);
                    if (sonuc != null) sonuclar.Add(sonuc);
                }

                if (aciklamaGetir && sonuclar.Count == 1)
                    sonuclar[0].Aciklama = Kisalt(await OpenLibraryAciklamaAsync(istemci, sonuclar[0].DisKaynakId), 2000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Open Library'ye ulaşılamadı.");
            }

            return sonuclar;
        }

        private static async Task<string> OpenLibraryAciklamaAsync(HttpClient istemci, string disKaynakId)
        {
            try
            {
                var calismaId = disKaynakId[OpenLibraryPrefix.Length..];
                using var yanit = await istemci.GetAsync($"{OpenLibraryAdres}/works/{calismaId}.json");
                if (!yanit.IsSuccessStatusCode) return string.Empty;

                using var doc = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());
                if (!doc.RootElement.TryGetProperty("description", out var aciklama))
                    return string.Empty;

                var ham = aciklama.ValueKind switch
                {
                    JsonValueKind.String => aciklama.GetString() ?? string.Empty,
                    JsonValueKind.Object => Metin(aciklama, "value"),
                    _ => string.Empty
                };
                return HtmlTemizle(ham);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static DisKitapSonuc? OpenLibraryEsle(JsonElement belge)
        {
            var anahtar = Metin(belge, "key");              // /works/OL123W
            var baslik = Metin(belge, "title");
            if (string.IsNullOrWhiteSpace(baslik) || !anahtar.StartsWith("/works/"))
                return null;

            var yazar = belge.TryGetProperty("author_name", out var yz) && yz.ValueKind == JsonValueKind.Array
                ? string.Join(", ", yz.EnumerateArray().Select(a => a.GetString()).Where(a => !string.IsNullOrWhiteSpace(a)).Take(3))
                : string.Empty;

            var yil = belge.TryGetProperty("first_publish_year", out var yp) && yp.TryGetInt32(out var y) ? y : 0;
            int? sayfa = belge.TryGetProperty("number_of_pages_median", out var sp) && sp.TryGetInt32(out var sayi) && sayi > 0 ? sayi : null;

            string? isbn = null;
            if (belge.TryGetProperty("isbn", out var isbnler) && isbnler.ValueKind == JsonValueKind.Array)
            {
                var liste = isbnler.EnumerateArray().Select(i => i.GetString()).Where(i => !string.IsNullOrEmpty(i)).ToList();
                isbn = liste.FirstOrDefault(i => i!.Length == 13) ?? liste.FirstOrDefault(i => i!.Length == 10);
            }

            string? tur = null;
            if (belge.TryGetProperty("subject", out var konular) && konular.ValueKind == JsonValueKind.Array)
            {
                var birlesik = string.Join(" ", konular.EnumerateArray().Take(30).Select(k => k.GetString())).ToLower();
                tur = TurEslemeleri.FirstOrDefault(e => birlesik.Contains(e.Anahtar)).Tur;
            }

            string? kapak = belge.TryGetProperty("cover_i", out var ci) && ci.TryGetInt64(out var kapakId) && kapakId > 0
                ? $"https://covers.openlibrary.org/b/id/{kapakId}-M.jpg"
                : null;

            return new DisKitapSonuc
            {
                DisKaynakId = OpenLibraryPrefix + anahtar["/works/".Length..],
                Baslik = baslik.Trim(),
                Yazar = string.IsNullOrWhiteSpace(yazar) ? "Bilinmiyor" : yazar,
                Yil = yil,
                SayfaSayisi = sayfa,
                Tur = tur,
                KapakUrl = kapak,
                Isbn = isbn
            };
        }

        private async Task<List<DisKitapSonuc>> GoogleGetirAsync(string yolVeSorgu, bool tekKitap = false)
        {
            var sonuclar = new List<DisKitapSonuc>();

            try
            {
                var istemci = _httpClientFactory.CreateClient(GoogleIstemciAdi);
                var anahtar = _configuration["GoogleBooks:ApiKey"];
                var adres = GoogleAdres + yolVeSorgu;
                if (!string.IsNullOrEmpty(anahtar))
                    adres += (adres.Contains('?') ? "&" : "?") + "key=" + Uri.EscapeDataString(anahtar);

                using var yanit = await istemci.GetAsync(adres);
                if (!yanit.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Google Books isteği başarısız: {Kod}", (int)yanit.StatusCode);
                    return sonuclar;
                }

                using var doc = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());
                var kok = doc.RootElement;

                if (tekKitap)
                {
                    var tek = Esle(kok);
                    if (tek != null) sonuclar.Add(tek);
                }
                else if (kok.TryGetProperty("items", out var ogeler))
                {
                    foreach (var oge in ogeler.EnumerateArray())
                    {
                        var sonuc = Esle(oge);
                        if (sonuc != null) sonuclar.Add(sonuc);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google Books'a ulaşılamadı.");
            }

            return sonuclar;
        }

        private static DisKitapSonuc? Esle(JsonElement oge)
        {
            if (!oge.TryGetProperty("id", out var idProp) || !oge.TryGetProperty("volumeInfo", out var bilgi))
                return null;

            var baslik = Metin(bilgi, "title");
            if (string.IsNullOrWhiteSpace(baslik))
                return null;

            var yazarlar = bilgi.TryGetProperty("authors", out var yz) && yz.ValueKind == JsonValueKind.Array
                ? string.Join(", ", yz.EnumerateArray().Select(a => a.GetString()).Where(a => !string.IsNullOrWhiteSpace(a)))
                : string.Empty;

            var yil = 0;
            var tarih = Metin(bilgi, "publishedDate");
            if (tarih.Length >= 4)
                int.TryParse(tarih.AsSpan(0, 4), out yil);

            int? sayfa = bilgi.TryGetProperty("pageCount", out var sp) && sp.TryGetInt32(out var sayi) && sayi > 0 ? sayi : null;

            string? isbn = null;
            if (bilgi.TryGetProperty("industryIdentifiers", out var kimlikler) && kimlikler.ValueKind == JsonValueKind.Array)
            {
                var kimlikListesi = kimlikler.EnumerateArray()
                    .Select(k => (Tur: Metin(k, "type"), Deger: Metin(k, "identifier")))
                    .ToList();

                // Önce ISBN-13, yoksa ISBN-10 tercih edilir
                isbn = kimlikListesi.FirstOrDefault(k => k.Tur == "ISBN_13").Deger
                       ?? kimlikListesi.FirstOrDefault(k => k.Tur == "ISBN_10").Deger;
                if (string.IsNullOrWhiteSpace(isbn)) isbn = null;
            }

            string? tur = null;
            if (bilgi.TryGetProperty("categories", out var kategoriler) && kategoriler.ValueKind == JsonValueKind.Array)
            {
                var birlesik = string.Join(" ", kategoriler.EnumerateArray().Select(k => k.GetString())).ToLower();
                tur = TurEslemeleri.FirstOrDefault(e => birlesik.Contains(e.Anahtar)).Tur;
            }

            string? kapak = null;
            if (bilgi.TryGetProperty("imageLinks", out var resimler))
            {
                var ham = Metin(resimler, "thumbnail");
                if (string.IsNullOrEmpty(ham)) ham = Metin(resimler, "smallThumbnail");
                kapak = KapakUrlTemizle(ham);
            }

            return new DisKitapSonuc
            {
                DisKaynakId = GooglePrefix + (idProp.GetString() ?? string.Empty),
                Baslik = baslik.Trim(),
                Yazar = string.IsNullOrWhiteSpace(yazarlar) ? "Bilinmiyor" : yazarlar,
                Yil = yil,
                SayfaSayisi = sayfa,
                Aciklama = HtmlTemizle(Metin(bilgi, "description")),
                Tur = tur,
                KapakUrl = kapak,
                Isbn = isbn
            };
        }

        // Yalnızca Google'a ait https adresleri kapak olarak kabul edilir
        private static string? KapakUrlTemizle(string? ham)
        {
            if (string.IsNullOrWhiteSpace(ham)) return null;

            ham = ham.Replace("http://", "https://").Replace("&edge=curl", "");

            if (!Uri.TryCreate(ham, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                return null;

            var host = uri.Host.ToLowerInvariant();
            return host.EndsWith(".google.com") || host.EndsWith(".googleusercontent.com") || host.EndsWith(".gstatic.com")
                ? ham
                : null;
        }

        private static string Metin(JsonElement e, string ad) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(ad, out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString() ?? string.Empty
                : string.Empty;

        private static string HtmlTemizle(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return string.Empty;
            metin = Regex.Replace(metin, "<br\\s*/?>|</p>", "\n", RegexOptions.IgnoreCase);
            metin = Regex.Replace(metin, "<[^>]+>", string.Empty);
            return System.Net.WebUtility.HtmlDecode(metin).Trim();
        }

        private static string Kisalt(string metin, int uzunluk) =>
            metin.Length <= uzunluk ? metin : metin[..uzunluk];

        private static string IsbnTemizle(string metin) => Regex.Replace(metin, "[\\s-]", string.Empty);

        private static bool IsbnMi(string metin)
        {
            var temiz = IsbnTemizle(metin);
            return Regex.IsMatch(temiz, "^(\\d{13}|\\d{9}[\\dXx])$");
        }
    }
}
