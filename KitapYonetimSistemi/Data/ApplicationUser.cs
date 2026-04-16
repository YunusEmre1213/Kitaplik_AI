using KitapYonetimSistemi.Models;
using Microsoft.AspNetCore.Identity;

namespace KitapYonetimSistemi.Data
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Yorum>? Yorumlar { get; set; }
        public ICollection<KullaniciKitaplik>? Kitapligim { get; set; }
        public ICollection<KitapPuan>? VerdigiPuanlar { get; set; }
    }
}