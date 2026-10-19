using Microsoft.AspNetCore.Identity;

namespace KitapYonetimSistemi.Data
{
    public static class SeedData
    {
        private const string EskiVarsayilanSifre = "Admin123!";

        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            foreach (var rol in new[] { "Admin", "User" })
            {
                if (!await roleManager.RoleExistsAsync(rol))
                {
                    await roleManager.CreateAsync(new IdentityRole(rol));
                }
            }

            // Admin bilgileri koda yazılmaz; user-secrets veya ortam değişkenlerinden okunur
            var adminEmail = configuration["Admin:Email"];
            var adminPassword = configuration["Admin:Password"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                Console.WriteLine("Admin:Email / Admin:Password ayarlanmadığı için admin kullanıcısı oluşturulmadı.");
                return;
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var sonuc = await userManager.CreateAsync(adminUser, adminPassword);
                if (!sonuc.Succeeded)
                {
                    Console.WriteLine("Admin oluşturulamadı: " + string.Join(", ", sonuc.Errors.Select(e => e.Description)));
                    return;
                }
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else if (await userManager.CheckPasswordAsync(adminUser, EskiVarsayilanSifre))
            {
                // Eski varsayılan şifre hâlâ geçerliyse ayarlanan şifreyle değiştir
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                await userManager.ResetPasswordAsync(adminUser, token, adminPassword);
            }
        }
    }
}
