using KitapYonetimSistemi.Data;

namespace KitapYonetimSistemi.Models
{
    // Birkaç kişinin aynı kitabı birlikte okuduğu oda. Üyeler birbirinin ilerlemesini görür,
    // mesajlar yazarın sayfasına bağlıdır ve geride kalan üye için kilitli kalır.
    public class OkumaOdasi
    {
        public const int EnFazlaUye = 6;
        public const int AdUzunlugu = 80;
        public const int KodUzunlugu = 8;

        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Kod { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }

        public int KitapId { get; set; }
        public Kitap? Kitap { get; set; }

        public string OlusturanId { get; set; } = string.Empty;
        public ApplicationUser? Olusturan { get; set; }

        public ICollection<OdaUyesi> Uyeler { get; set; } = new List<OdaUyesi>();
        public ICollection<OdaMesaji> Mesajlar { get; set; } = new List<OdaMesaji>();
    }

    public class OdaUyesi
    {
        public int Id { get; set; }
        public DateTime KatilmaTarihi { get; set; }

        public int OdaId { get; set; }
        public OkumaOdasi? Oda { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }
    }

    public class OdaMesaji
    {
        public const int EnFazlaUzunluk = 1000;

        public int Id { get; set; }
        // 0 = sayfa kapısı yok (kitaba başlamadan yazılmış), aksi halde yazarın o andaki sayfası
        public int SayfaNo { get; set; }
        public string Metin { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }

        public int OdaId { get; set; }
        public OkumaOdasi? Oda { get; set; }

        public string KullaniciId { get; set; } = string.Empty;
        public ApplicationUser? Kullanici { get; set; }
    }

    public class OdaUyeGorunumu
    {
        public string KullaniciId { get; set; } = string.Empty;
        public string Ad { get; set; } = string.Empty;
        public int Sayfa { get; set; }
        public bool Bitirdi { get; set; }
        public bool Ben { get; set; }
    }

    public class OdaMesajGorunumu
    {
        public string Ad { get; set; } = string.Empty;
        public int SayfaNo { get; set; }
        public string Metin { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }
        public bool Kilitli { get; set; }
        public bool Benim { get; set; }
        public int Id { get; set; }
    }

    public class OdaViewModel
    {
        public OkumaOdasi Oda { get; set; } = null!;
        public List<OdaUyeGorunumu> Uyeler { get; set; } = new();
        public List<OdaMesajGorunumu> Mesajlar { get; set; } = new();
        public int BenimSayfam { get; set; }
        public bool Bitirdim { get; set; }
        public int? KitapSayfaSayisi { get; set; }
        public int? KitaplikId { get; set; }
        public bool Olusturan { get; set; }
    }
}
