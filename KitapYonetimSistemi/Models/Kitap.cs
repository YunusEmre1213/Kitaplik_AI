namespace KitapYonetimSistemi.Models
{
    public class Kitap
    {
        public int Id { get; set; }
        public string KitapAdi { get; set; } = string.Empty;
        public string YazarAdi { get; set; } = string.Empty;
        public int YayinYili { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public DateTime EklenmeTarihi { get; set; }
        public string? KapakResmiUrl { get; set; }
        public string? Tur { get; set; }

        // Dış kaynaktan (Google Books) eklenen kitaplarda tekrarı önlemek için
        public string? Isbn { get; set; }
        public string? DisKaynakId { get; set; }

        [System.ComponentModel.DataAnnotations.Range(1, 20000, ErrorMessage = "Sayfa sayısı 1 ile 20000 arasında olmalıdır.")]
        public int? SayfaSayisi { get; set; }

        // İlişkiler
        public ICollection<Yorum>? Yorumlar { get; set; }
        public ICollection<KullaniciKitaplik>? KullaniciKitapliklari { get; set; }
        public ICollection<Alinti>? Alintilar { get; set; }

        // YENİ: Puan ilişkisi
        public ICollection<KitapPuan>? Puanlar { get; set; }

        // YENİ: Ortalama puan hesaplama (Computed Property)
        public double OrtalamaPuan => Puanlar != null && Puanlar.Any()
            ? Puanlar.Average(p => p.Puan)
            : 0;

        // YENİ: Toplam puan sayısı
        public int ToplamPuanSayisi => Puanlar?.Count ?? 0;
    }
}