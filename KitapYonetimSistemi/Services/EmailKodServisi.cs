using KitapYonetimSistemi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Caching.Memory;

namespace KitapYonetimSistemi.Services
{
    // Link yerine 6 haneli kod ile e-posta doğrulaması yapar.
    // Böylece e-posta başka bir cihazdan (telefon vb.) açılsa bile doğrulama çalışır.
    public class EmailKodServisi
    {
        private const string Amac = "EmailConfirmation";
        private const int EnFazlaDeneme = 5;
        private static readonly TimeSpan YenidenGondermeSuresi = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan DenemeSuresi = TimeSpan.FromMinutes(10);

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IMemoryCache _cache;

        public EmailKodServisi(UserManager<ApplicationUser> userManager, IEmailSender emailSender, IMemoryCache cache)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _cache = cache;
        }

        // Kod gönderir. Çok sık istenirse göndermez ve false döner.
        public async Task<bool> GonderAsync(ApplicationUser kullanici)
        {
            var bekleKey = $"KodBekle_{kullanici.Id}";
            if (_cache.TryGetValue(bekleKey, out _))
                return false;

            var kod = await _userManager.GenerateUserTokenAsync(kullanici, TokenOptions.DefaultEmailProvider, Amac);

            await _emailSender.SendEmailAsync(kullanici.Email!, "Satır Arası - Doğrulama kodunuz",
                $"<p>Merhaba,</p><p>Doğrulama kodunuz:</p><h2 style='letter-spacing:4px'>{kod}</h2>" +
                "<p>Kod birkaç dakika geçerlidir. Bu kaydı siz yapmadıysanız bu e-postayı yok sayabilirsiniz.</p>");

            _cache.Set(bekleKey, true, YenidenGondermeSuresi);
            return true;
        }

        public async Task<(bool Basarili, string? Hata)> DogrulaAsync(ApplicationUser kullanici, string kod)
        {
            var denemeKey = $"KodDeneme_{kullanici.Id}";
            var deneme = _cache.TryGetValue(denemeKey, out int sayi) ? sayi : 0;

            if (deneme >= EnFazlaDeneme)
                return (false, "Çok fazla hatalı deneme yaptınız. Lütfen 10 dakika sonra tekrar deneyin.");

            kod = (kod ?? string.Empty).Trim();

            if (!await _userManager.VerifyUserTokenAsync(kullanici, TokenOptions.DefaultEmailProvider, Amac, kod))
            {
                _cache.Set(denemeKey, deneme + 1, DenemeSuresi);
                return (false, "Kod hatalı ya da süresi dolmuş. Yeni bir kod isteyebilirsiniz.");
            }

            kullanici.EmailConfirmed = true;
            await _userManager.UpdateAsync(kullanici);
            _cache.Remove(denemeKey);
            return (true, null);
        }
    }
}
