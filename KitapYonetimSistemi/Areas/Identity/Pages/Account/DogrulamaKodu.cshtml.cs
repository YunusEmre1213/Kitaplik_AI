using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KitapYonetimSistemi.Areas.Identity.Pages.Account
{
    public class DogrulamaKoduModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly EmailKodServisi _kodServisi;

        public DogrulamaKoduModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EmailKodServisi kodServisi)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _kodServisi = kodServisi;
        }

        [BindProperty(SupportsGet = true)]
        public string? Email { get; set; }

        [BindProperty]
        public string? Kod { get; set; }

        public string? Mesaj { get; set; }
        public string? Hata { get; set; }

        public IActionResult OnGet()
        {
            if (string.IsNullOrWhiteSpace(Email))
                return RedirectToPage("./Login");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var kullanici = string.IsNullOrWhiteSpace(Email) ? null : await _userManager.FindByEmailAsync(Email);
            if (kullanici == null)
            {
                Hata = "Kod hatalı ya da süresi dolmuş. Yeni bir kod isteyebilirsiniz.";
                return Page();
            }

            if (kullanici.EmailConfirmed)
                return RedirectToPage("./Login");

            var (basarili, hata) = await _kodServisi.DogrulaAsync(kullanici, Kod ?? string.Empty);
            if (!basarili)
            {
                Hata = hata;
                return Page();
            }

            await _signInManager.SignInAsync(kullanici, isPersistent: false);
            return LocalRedirect("~/");
        }

        public async Task<IActionResult> OnPostYenidenGonderAsync()
        {
            var kullanici = string.IsNullOrWhiteSpace(Email) ? null : await _userManager.FindByEmailAsync(Email);

            // Hesap var olup olmadığı sızdırılmasın diye aynı mesaj gösterilir
            if (kullanici != null && !kullanici.EmailConfirmed)
            {
                try
                {
                    var gonderildi = await _kodServisi.GonderAsync(kullanici);
                    if (!gonderildi)
                    {
                        Hata = "Yeni kod istemek için lütfen bir dakika bekleyin.";
                        return Page();
                    }
                }
                catch (Exception)
                {
                    Hata = "E-posta gönderilemedi. Lütfen daha sonra tekrar deneyin.";
                    return Page();
                }
            }

            Mesaj = "Yeni kod e-posta adresinize gönderildi.";
            return Page();
        }
    }
}
