using KitapYonetimSistemi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. VERİTABANI VE KİMLİK (IDENTITY) AYARLARI
// ==========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<KitapYonetimSistemi.Services.LibraryToolService>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // Email doğrulamasını gevşet
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Şifre gereksinimlerini azalt (Test için)
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// ==========================================
// 2. AI SERVİSİ (HTTP CLIENT) ENTEGRASYONU
// ==========================================
// Buradaki adresin AI PROJESİNİN (BookStore.AI.Service) adresi olduğundan emin ol.
// Genellikle "http" değil "https" olur. Eğer çalışmazsa https://localhost:XXXX dene.
builder.Services.AddHttpClient("AiService", client =>
{
    // Adresin DOĞRU olduğunu 405 hatasıyla kanıtladın. Aynen kalsın.
    client.BaseAddress = new Uri("https://localhost:7195/");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // İŞTE SİHİRLİ KOD BURASI:
    // Lokaldeki SSL sertifika hatasını görmezden gelmemizi sağlar.
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
    };
});

var app = builder.Build();

// ==========================================
// 3. SEED DATA (BAŞLANGIÇ VERİLERİ)
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        // Loglama yapılabilir
        Console.WriteLine("Seed data oluşturulurken hata: " + ex.Message);
    }
}

// ==========================================
// 4. HTTP REQUEST PIPELINE
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();