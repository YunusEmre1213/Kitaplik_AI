namespace KitapYonetimSistemi.Models
{
    // Sayfa kapılı yorumların kimin için açık olduğunu belirleyen saf kural.
    public static class YorumGorunurluk
    {
        // Okuyucunun bu yorumu görmeye hakkı var mı?
        public static bool Acik(int? yorumSayfasi, bool yazarMi, bool okuyucuBitirdi, int? okuyucuSayfasi)
        {
            if (yorumSayfasi is null) return true;      // sayfa kapısı yok
            if (yazarMi || okuyucuBitirdi) return true;  // yazar kendi yorumunu, bitiren her şeyi görür
            return okuyucuSayfasi.HasValue && okuyucuSayfasi.Value >= yorumSayfasi.Value;
        }
    }

    public class YorumGorunumu
    {
        public Yorum Yorum { get; set; } = null!;
        public bool Kilitli { get; set; }
    }
}
