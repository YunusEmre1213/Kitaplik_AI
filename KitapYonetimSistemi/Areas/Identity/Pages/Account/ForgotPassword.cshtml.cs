using System.ComponentModel.DataAnnotations;
using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KitapYonetimSistemi.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailKodServisi _kodServisi;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, EmailKodServisi kodServisi, ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _kodServisi = kodServisi;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "E-posta adresi gerekli.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string Email { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var kullanici = await _userManager.FindByEmailAsync(Email);

            // Hesabın var olup olmadığı sızdırılmasın diye her durumda aynı sayfaya yönlendirilir
            if (kullanici != null)
            {
                try
                {
                    await _kodServisi.GonderAsync(kullanici, EmailKodServisi.SifreSifirlama);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Şifre sıfırlama kodu gönderilemedi.");
                    ModelState.AddModelError(string.Empty, "E-posta gönderilemedi. Lütfen daha sonra tekrar deneyin.");
                    return Page();
                }
            }

            return RedirectToPage("./ResetPassword", new { email = Email });
        }
    }
}
