using KitapYonetimSistemi.Models;
using Microsoft.AspNetCore.Identity;

namespace KitapYonetimSistemi.Data
{
    public class ApplicationUser : IdentityUser
    {
        // Eski kullanıcılar için kayıt tarihi bilinmiyor, bu yüzden null olabilir
        public DateTime? KayitTarihi { get; set; }

        // Okuma ikizi eşleşmesine katılım (kapalı başlar): açık olanlar birbirinin sayfa tepkileriyle eşleştirilir
        public bool IkizOlabilir { get; set; }

        public ICollection<Yorum>? Yorumlar { get; set; }
        public ICollection<KullaniciKitaplik>? Kitapligim { get; set; }
        public ICollection<KitapPuan>? VerdigiPuanlar { get; set; }
    }
}