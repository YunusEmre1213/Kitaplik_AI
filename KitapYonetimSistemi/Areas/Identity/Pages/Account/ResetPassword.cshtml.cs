using System.ComponentModel.DataAnnotations;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KitapYonetimSistemi.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailKodServisi _kodServisi;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, EmailKodServisi kodServisi)
        {
            _userManager = userManager;
            _kodServisi = kodServisi;
        }

        [BindProperty(SupportsGet = true)]
        public string? Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Kodu girin.")]
        public string Kod { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Yeni şifre gerekli.")]
        [DataType(DataType.Password)]
        public string YeniSifre { get; set; } = string.Empty;

        [BindProperty]
        [DataType(DataType.Password)]
        [Compare(nameof(YeniSifre), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string YeniSifreTekrar { get; set; } = string.Empty;

        public string? Mesaj { get; set; }

        public IActionResult OnGet()
        {
            if (string.IsNullOrWhiteSpace(Email))
                return RedirectToPage("./ForgotPassword");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var kullanici = string.IsNullOrWhiteSpace(Email) ? null : await _userManager.FindByEmailAsync(Email);
            if (kullanici == null)
            {
                ModelState.AddModelError(string.Empty, "Kod hatalı ya da süresi dolmuş. Yeni bir kod isteyebilirsiniz.");
                return Page();
            }

            var (basarili, hata) = await _kodServisi.KodKontrolAsync(kullanici, Kod, EmailKodServisi.SifreSifirlama);
            if (!basarili)
            {
                ModelState.AddModelError(string.Empty, hata!);
                return Page();
            }

            // Kod doğrulandı, Identity'nin kendi şifre sıfırlama jetonuyla şifreyi değiştir
            var jeton = await _userManager.GeneratePasswordResetTokenAsync(kullanici);
            var sonuc = await _userManager.ResetPasswordAsync(kullanici, jeton, YeniSifre);

            if (!sonuc.Succeeded)
            {
                foreach (var error in sonuc.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            // E-postayı alabildiğini kanıtladığı için e-postası da doğrulanmış sayılır
            if (!kullanici.EmailConfirmed)
            {
                kullanici.EmailConfirmed = true;
                await _userManager.UpdateAsync(kullanici);
            }

            return RedirectToPage("./ResetPasswordConfirmation");
        }

        public async Task<IActionResult> OnPostYenidenGonderAsync()
        {
            var kullanici = string.IsNullOrWhiteSpace(Email) ? null : await _userManager.FindByEmailAsync(Email);

            if (kullanici != null)
            {
                try
                {
                    if (!await _kodServisi.GonderAsync(kullanici, EmailKodServisi.SifreSifirlama))
                    {
                        ModelState.AddModelError(string.Empty, "Yeni kod istemek için lütfen bir dakika bekleyin.");
                        return Page();
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty, "E-posta gönderilemedi. Lütfen daha sonra tekrar deneyin.");
                    return Page();
                }
            }

            ModelState.Clear();
            Mesaj = "Yeni kod e-posta adresinize gönderildi.";
            return Page();
        }
    }
}
