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

app.UseAuthorization();

// Controller rotalarýný haritala (404 hatasýný çözen kýsým burasý)
app.MapControllers();

app.Run();