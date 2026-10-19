var builder = WebApplication.CreateBuilder(args);

// =============================================================
// 1. SERVÝSLERÝN EKLENMESÝ
// =============================================================

// Controller desteðini ekle (API uçlarýný çalýþtýrýr)
builder.Services.AddControllers();

// HTTP Client servisini ekle (BUNU EKLEMEZSEN GOOGLE'A GÝDEMEZSÝN!)
builder.Services.AddHttpClient();

// Swagger / OpenAPI Test Arayüzü Ayarlarý
// Bu sayede tarayýcýdan https://localhost:7195/swagger adresine girebilirsin.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// =============================================================
// 2. UYGULAMA AYARLARI (MIDDLEWARE)
// =============================================================

// Geliþtirme ortamýndaysak Swagger'ý aç
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Arayüzü aktif eder
}

app.UseHttpsRedirection();

// Sadece anahtarÄ± bilen istemciler (MVC uygulamasÄ±) AI servisini kullanabilir
var servisAnahtari = app.Configuration["AiService:ApiKey"];
app.Use(async (context, next) =>
{
    if (!app.Environment.IsDevelopment() && string.IsNullOrEmpty(servisAnahtari))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return;
    }

    if (!string.IsNullOrEmpty(servisAnahtari) &&
        context.Request.Headers["X-Api-Key"] != servisAnahtari)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await next();
});

app.UseAuthorization();

// Controller rotalarýný haritala (404 hatasýný çözen kýsým burasý)
app.MapControllers();

app.Run();