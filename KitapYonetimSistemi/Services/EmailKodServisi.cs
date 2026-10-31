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
        public const string EmailDogrulama = "EmailConfirmation";
        public const string SifreSifirlama = "ResetPassword";
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
        public async Task<bool> GonderAsync(ApplicationUser kullanici, string amac = EmailDogrulama)
        {
            var bekleKey = $"KodBekle_{amac}_{kullanici.Id}";
            if (_cache.TryGetValue(bekleKey, out _))
                return false;

            var kod = await _userManager.GenerateUserTokenAsync(kullanici, TokenOptions.DefaultEmailProvider, amac);

            var konu = amac == SifreSifirlama ? "Satır Arası - Şifre sıfırlama kodunuz" : "Satır Arası - Doğrulama kodunuz";
            var aciklama = amac == SifreSifirlama ? "Şifre sıfırlama kodunuz:" : "Doğrulama kodunuz:";
            var uyari = amac == SifreSifirlama ? "Şifre sıfırlamayı siz istemediyseniz bu e-postayı yok sayabilirsiniz, şifreniz değişmez." : "Bu kaydı siz yapmadıysanız bu e-postayı yok sayabilirsiniz.";

            await _emailSender.SendEmailAsync(kullanici.Email!, konu,
                $"<p>Merhaba,</p><p>{aciklama}</p><h2 style='letter-spacing:4px'>{kod}</h2>" +
                $"<p>Kod birkaç dakika geçerlidir. {uyari}</p>");

            _cache.Set(bekleKey, true, YenidenGondermeSuresi);
            return true;
        }

        // Kodu doğrular, hatalı denemeleri sayar. Hesap üzerinde hiçbir değişiklik yapmaz.
        public async Task<(bool Basarili, string? Hata)> KodKontrolAsync(ApplicationUser kullanici, string kod, string amac)
        {
            var denemeKey = $"KodDeneme_{amac}_{kullanici.Id}";
            var deneme = _cache.TryGetValue(denemeKey, out int sayi) ? sayi : 0;

            if (deneme >= EnFazlaDeneme)
                return (false, "Çok fazla hatalı deneme yaptınız. Lütfen 10 dakika sonra tekrar deneyin.");

            kod = (kod ?? string.Empty).Trim();

            if (!await _userManager.VerifyUserTokenAsync(kullanici, TokenOptions.DefaultEmailProvider, amac, kod))
            {
                _cache.Set(denemeKey, deneme + 1, DenemeSuresi);
                return (false, "Kod hatalı ya da süresi dolmuş. Yeni bir kod isteyebilirsiniz.");
            }

            _cache.Remove(denemeKey);
            return (true, null);
        }

        public async Task<(bool Basarili, string? Hata)> DogrulaAsync(ApplicationUser kullanici, string kod)
        {
            var sonuc = await KodKontrolAsync(kullanici, kod, EmailDogrulama);
            if (!sonuc.Basarili)
                return sonuc;

            kullanici.EmailConfirmed = true;
            await _userManager.UpdateAsync(kullanici);
            return (true, null);
        }
    }
}
