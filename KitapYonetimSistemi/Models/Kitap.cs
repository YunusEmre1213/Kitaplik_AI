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

        // İlişkiler
        public ICollection<Yorum>? Yorumlar { get; set; }
        public ICollection<KullaniciKitaplik>? KullaniciKitapliklari { get; set; }

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