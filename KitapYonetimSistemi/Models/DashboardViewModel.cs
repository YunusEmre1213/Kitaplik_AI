namespace KitapYonetimSistemi.Models
{
    public class DashboardViewModel
    {
        public int ToplamKitapSayisi { get; set; }
        public int ToplamKullaniciSayisi { get; set; }
        public int ToplamYorumSayisi { get; set; }
        public int ToplamKitaplikSayisi { get; set; }
        public int BuAyEklenenKitapSayisi { get; set; }
        public int BuAyKayitOlanKullaniciSayisi { get; set; }

        // En çok yorumlanan kitaplar
        public List<KitapYorumIstatistik> EnCokYorumlananKitaplar { get; set; }

        // Son eklenen kitaplar
        public List<Kitap> SonEklenenKitaplar { get; set; }
        public List<Kitap> EnYuksekPuanliKitaplar { get; internal set; }
    }

    public class KitapYorumIstatistik
    {
        public string KitapAdi { get; set; }
        public int YorumSayisi { get; set; }
    }
}