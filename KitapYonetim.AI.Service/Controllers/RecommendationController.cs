using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace BookStore.AI.Service.Controllers
{
    [Route("api/ai")]
    [ApiController]
    public class RecommendationController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RecommendationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("API Controller Çalışıyor! Port ve Adres Doğru.");
        }

        // ==========================================
        // 1. MEVCUT KİTAP ÖNERİ FONKSİYONU
        // ==========================================
        [HttpPost("ask")]
        public async Task<IActionResult> GenerateRecommendation([FromBody] List<string> userBooks)
        {
            if (userBooks == null || userBooks.Count == 0)
                return BadRequest("Liste boş.");

            string apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return BadRequest("API Key yok.");

            // Temizlik: API Key'de boşluk varsa silelim
            apiKey = apiKey.Trim();

            string booksText = string.Join(", ", userBooks);
            string prompt = $@"
                Sen bir kütüphanecisin. Kullanıcının sevdiği kitaplar: {booksText}.
                Ona sevebileceği 3 yeni kitap öner.
                Cevabı SADECE şu JSON formatında ver:
                [ {{ ""Title"": ""Kitap Adı"", ""Author"": ""Yazar"", ""Reason"": ""Sebep"" }} ]
            ";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            // DÜZELTİLDİ: URL ARTIK TERTEMİZ (Parantezler silindi)
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

            try
            {
                // HttpClient'ı burada taze oluşturuyoruz (En güvenli yöntem)
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey.Trim());
                    var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        return StatusCode((int)response.StatusCode, $"Google Hatası: {error}");
                    }

                    var responseString = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(responseString))
                    {
                        var candidates = doc.RootElement.GetProperty("candidates");
                        if (candidates.GetArrayLength() > 0)
                        {
                            string aiText = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                            aiText = aiText.Replace("```json", "").Replace("```", "").Trim();
                            return Ok(aiText);
                        }
                        return BadRequest("Google boş cevap döndü.");
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        // ==========================================
        // 2. MEVCUT KİTAP ANALİZ FONKSİYONU
        // ==========================================
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeBook([FromBody] BookAnalysisRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Title))
                return BadRequest("Kitap adı boş olamaz.");

            string apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return BadRequest("API Key yok.");
            apiKey = apiKey.Trim();

            string prompt = $@"
                Sen usta bir edebiyat eleştirmenisin.
                Kitap: {request.Title}, Yazar: {request.Author}.
        
                Bu kitabı henüz okumamış biri için SPOILER VERMEDEN analiz et.
                Cevabı SADECE şu JSON formatında ver (Markdown veya ```json etiketi kullanma):
                {{
                    ""Summary"": ""Kitabın konusu (Max 2 cümle)."",
                    ""Theme"": ""Kitabın ana teması ve hissettirdiği duygu."",
                    ""Reasons"": [
                        ""Neden okunmalı madde 1"",
                        ""Neden okunmalı madde 2"",
                        ""Neden okunmalı madde 3""
                    ],
                    ""SimilarBooks"": ""Bu kitabı seven şunları da sever: X, Y...""
                }}
            ";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            // DÜZELTİLDİ: URL Temizlendi
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey.Trim());
                    var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        return StatusCode((int)response.StatusCode, error);
                    }

                    var responseString = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(responseString))
                    {
                        var candidates = doc.RootElement.GetProperty("candidates");
                        if (candidates.GetArrayLength() > 0)
                        {
                            string aiText = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                            aiText = aiText.Replace("```json", "").Replace("```", "").Trim();
                            return Ok(aiText);
                        }
                        return BadRequest("Analiz oluşturulamadı.");
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        // ==========================================
        // 3. MCP / AGENT İÇİN AKILLI CHAT
        // ==========================================
        [HttpPost("agent-chat")]
        public async Task<IActionResult> AgentChat([FromBody] AgentRequest request)
        {
            try
            {
                string apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrEmpty(apiKey)) return BadRequest("API Key yok.");

                // 1. TOOL TANIMLARI (Artık 2 tane yeteneği var!)
                var toolsPayload = new
                {
                    function_declarations = new object[] {
                // Yetenek 1: Kitap Var mı?
                new {
                    name = "kitap_kontrol",
                    description = "Kütüphanede kitabın olup olmadığını ve detaylarını kontrol eder.",
                    parameters = new { type = "OBJECT", properties = new { kitap_adi = new { type = "STRING" } }, required = new[] { "kitap_adi" } }
                },
                // Yetenek 2: Yorumları Getir (YENİ)
                new {
                    name = "yorum_getir",
                    description = "Bir kitap hakkında yapılmış kullanıcı yorumlarını ve eleştirileri getirir.",
                    parameters = new { type = "OBJECT", properties = new { kitap_adi = new { type = "STRING" } }, required = new[] { "kitap_adi" } }
                }
            }
                };

                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = request.Message } } } },
                    tools = new[] { toolsPayload }
                };

                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey.Trim());
                    var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode, new { error = $"Google Hatası: {responseString}" });

                    using (JsonDocument doc = JsonDocument.Parse(responseString))
                    {
                        if (!doc.RootElement.TryGetProperty("candidates", out var candidates)) return BadRequest(new { error = "Google cevap vermedi." });

                        var firstPart = candidates[0].GetProperty("content").GetProperty("parts")[0];

                        // A. Fonksiyon Çağrısı Yakalama
                        if (firstPart.TryGetProperty("functionCall", out var functionCall))
                        {
                            string funcName = functionCall.GetProperty("name").GetString(); // Hangi fonksiyon?
                            var args = functionCall.GetProperty("args");
                            string aranacak = args.TryGetProperty("kitap_adi", out var val) ? val.GetString() : "";

                            // MVC'ye hem fonksiyon adını hem parametreyi gönderiyoruz
                            return Ok(new { Action = "CallDatabase", FunctionName = funcName, Parameter = aranacak });
                        }

                        // B. Normal Cevap
                        if (firstPart.TryGetProperty("text", out var textProp))
                        {
                            return Ok(new { Action = "Reply", Message = textProp.GetString() });
                        }
                    }
                    return Ok(new { Action = "Reply", Message = "Anlaşılamadı." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Sunucu Hatası: {ex.Message}" });
            }
        }
    }

    public class BookAnalysisRequest { public string Title { get; set; } public string Author { get; set; } }
    public class AgentRequest { public string Message { get; set; } }
}