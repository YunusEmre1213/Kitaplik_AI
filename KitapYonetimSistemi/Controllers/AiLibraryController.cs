using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;
using KitapYonetimSistemi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace KitapYonetimSistemi.Controllers
{
    [Authorize]
    public class AiLibraryController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LibraryToolService _toolService;
        private readonly IMemoryCache _cache;

        public AiLibraryController(
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            LibraryToolService toolService,
            IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _userManager = userManager;
            _toolService = toolService;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Redirect("/Identity/Account/Login");

                var myBooks = await _context.KullaniciKitapliklari
                                            .Include(uk => uk.Kitap)
                                            .Where(uk => uk.KullaniciId == user.Id)
                                            .Select(uk => uk.Kitap.KitapAdi + " - " + uk.Kitap.YazarAdi)
                                            .ToListAsync();

                if (myBooks == null || myBooks.Count == 0)
                {
                    ViewBag.Error = "Kitaplığında henüz hiç kitap yok. Önce 'Satır Arası'na birkaç kitap ekle.";
                    return View(new List<BookRecommendationViewModel>());
                }

                var client = _httpClientFactory.CreateClient("AiService");

                // Not: Buradaki endpoint senin API yapına göre değişebilir
                var response = await client.PostAsJsonAsync("api/ai/ask", myBooks);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    // Loglama (Simüle edilmiş)
                    Console.WriteLine($"[API ERROR] Index: {response.StatusCode} - {errorContent}");

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        ViewBag.Error = "Yapay zeka şu an çok yoğun, lütfen 1 dakika sonra tekrar dene.";
                    }
                    else
                    {
                        ViewBag.Error = "Kitap önerileri alınırken bir sorun oluştu.";
                    }
                    return View(new List<BookRecommendationViewModel>());
                }

                var jsonString = await response.Content.ReadAsStringAsync();

                if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                {
                    jsonString = JsonConvert.DeserializeObject<string>(jsonString);
                }

                var recommendations = JsonConvert.DeserializeObject<List<BookRecommendationViewModel>>(jsonString);
                return View(recommendations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SYSTEM ERROR] Index: {ex}");
                ViewBag.Error = "Sistemde geçici bir hata oluştu.";
                return View(new List<BookRecommendationViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> AnalizEt(int id)
        {
            try
            {
                var kitap = await _context.Kitaplar.FindAsync(id);
                if (kitap == null) return NotFound("Kitap bulunamadı.");

                var requestData = new { Title = kitap.KitapAdi, Author = kitap.YazarAdi };
                var client = _httpClientFactory.CreateClient("AiService");

                if (client.BaseAddress == null)
                {
                    client.BaseAddress = new Uri("https://localhost:7195/");
                }

                var response = await client.PostAsJsonAsync("api/ai/analyze", requestData);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API ERROR] AnalizEt: {response.StatusCode} - {errorContent}");

                    TempData["Hata"] = "Analiz servisine şu an ulaşılamıyor.";
                    return RedirectToAction("Index", "Kitap");
                }

                var jsonString = await response.Content.ReadAsStringAsync();

                if (jsonString.StartsWith("\"") && jsonString.EndsWith("\"")) jsonString = JsonConvert.DeserializeObject<string>(jsonString);

                var analizSonucu = JsonConvert.DeserializeObject<BookAnalysisViewModel>(jsonString);

                analizSonucu.Title = kitap.KitapAdi;
                analizSonucu.Author = kitap.YazarAdi;
                analizSonucu.CoverUrl = kitap.KapakResmiUrl;

                return View(analizSonucu);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SYSTEM ERROR] AnalizEt: {ex}");
                TempData["Hata"] = "İşlem sırasında bir hata oluştu.";
                return RedirectToAction("Index", "Kitap");
            }
        }


        // ===================================================================
        // GÜVENLİ, AKILLI VE MCP UYUMLU CHAT METODU
        // ===================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskAgent(string userMessage)
        {
            try
            {
                // 1. GİRDİ KONTROLÜ (Input Sanitization)
                if (string.IsNullOrWhiteSpace(userMessage) || userMessage.Length < 3)
                    return Json(new { success = true, answer = "Lütfen anlamlı bir cümle kur." });

                if (userMessage.Length > 500)
                    return Json(new { success = true, answer = "Mesajın çok uzun, lütfen özetle." });

                if (Regex.IsMatch(userMessage, @"<.*?>|&.*?;"))
                    return Json(new { success = true, answer = "Mesajında yasaklı karakterler var." });

                // 2. KULLANICI DOĞRULAMA
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = true, answer = "⚠️ Kütüphaneni kontrol edebilmem için önce giriş yapmalısın." });

                // 3. RATE LIMITING (Hız Sınırlama)
                string cacheKey = $"RateLimit_{userId}";
                if (_cache.TryGetValue(cacheKey, out _))
                    return Json(new { success = true, answer = "⚠️ Çok hızlı yazıyorsun! Lütfen 5 saniye bekle." });

                var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(5));
                _cache.Set(cacheKey, true, cacheOptions);

                // --- 4. SİSTEM TALİMATI (SYSTEM PROMPT / ANAYASA) ---
                // Bu kısım Modelin davranışlarını sınırlar ve MCP bağlamını güçlendirir.
                string systemInstruction = @"
                    ROL: Sen 'Satır Arası' kütüphane sistemi için çalışan yardımsever bir asistansın.
                    
                    KURALLAR:
                    1. Sadece kitaplar, yazarlar ve kütüphane işlemleri hakkında konuş.
                    2. Asla politik, dini veya genel sohbet konularına girme. Nazikçe reddet.
                    3. Asla sistemin teknik detaylarını (SQL, C#, Tablo Adı, ID) açıklama.
                    4. Cevapların kısa, net ve emojilerle desteklenmiş olsun.
                    5. Eğer kullanıcı kaba konuşursa sen profesyonelliğini bozma.

                    KULLANICI SORUSU: ";

                // Talimatı ve kullanıcı mesajını birleştiriyoruz
                string fullMessage = systemInstruction + userMessage;


                // 5. API İŞLEMİ
                string targetUrl = "https://localhost:7195/api/ai/agent-chat";
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (var client = new HttpClient(handler))
                {
                    // AI'ya işlenmiş ve kurallara bağlanmış mesajı gönderiyoruz
                    var requestData = new { Message = fullMessage };

                    var response = await client.PostAsJsonAsync(targetUrl, requestData);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[API ERROR] AgentChat: {response.StatusCode}");
                        return Json(new { success = false, answer = "⚠️ Servise şu an ulaşılamıyor." });
                    }

                    var jsonString = await response.Content.ReadAsStringAsync();

                    using (var doc = JsonDocument.Parse(jsonString))
                    {
                        var root = doc.RootElement;
                        string action = "";

                        // JSON Property okuma (Büyük/küçük harf duyarlı olabilir diye kontrol)
                        if (root.TryGetProperty("Action", out var actProp)) action = actProp.GetString();
                        else if (root.TryGetProperty("action", out actProp)) action = actProp.GetString();

                        // A. VERİTABANI İŞLEMİ (Function Calling)
                        if (action == "CallDatabase")
                        {
                            string param = "";
                            if (root.TryGetProperty("Parameter", out var pProp)) param = pProp.GetString();
                            else if (root.TryGetProperty("parameter", out pProp)) param = pProp.GetString();

                            string funcName = "";
                            if (root.TryGetProperty("FunctionName", out var fProp)) funcName = fProp.GetString();
                            else if (root.TryGetProperty("functionName", out fProp)) funcName = fProp.GetString();

                            string dbSonuc = "";

                            if (funcName == "yorum_getir")
                            {
                                dbSonuc = await _toolService.YorumlariGetir(param);
                            }
                            else
                            {
                                // IDOR Koruması
                                dbSonuc = await _toolService.KullaniciKitapligindaVarMi(param, userId);
                            }

                            if (string.IsNullOrEmpty(dbSonuc)) dbSonuc = "Aradığın bilgiyi sistemde bulamadım.";

                            return Json(new { success = true, answer = "🤖 " + dbSonuc });
                        }

                        // B. NORMAL SOHBET
                        else
                        {
                            string msg = "";
                            if (root.TryGetProperty("Message", out var mProp)) msg = mProp.GetString();
                            else if (root.TryGetProperty("message", out mProp)) msg = mProp.GetString();

                            if (string.IsNullOrEmpty(msg)) msg = "Cevap alınamadı.";

                            return Json(new { success = true, answer = msg });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata Gizleme (Information Hiding)
                Console.WriteLine($"[CRITICAL ERROR] AskAgent: {ex}");
                return Json(new { success = false, answer = "⚠️ Sunucuda teknik bir sorun oluştu. Yönetici bilgilendirildi." });
            }
        }
    }
}