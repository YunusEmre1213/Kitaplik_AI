namespace KitapYonetimSistemi.Models
{
    public class OkumaIstatistikViewModel
    {
        public int OkunacakSayisi { get; set; }
        public int OkuyorumSayisi { get; set; }
        public int OkunduSayisi { get; set; }
        public int ToplamKitapSayisi { get; set; }

        public List<KullaniciKitaplik> SonOkunanKitaplar { get; set; }
        public List<KullaniciKitaplik> SimdiOkuduklarim { get; set; }
        public List<AylikOkumaIstatistik> AylikOkumaSayilari { get; set; }

        public double OkunmaYuzdesi => ToplamKitapSayisi > 0
            ? (double)OkunduSayisi / ToplamKitapSayisi * 100
            : 0;
    }

    public class AylikOkumaIstatistik
    {
        public int Yil { get; set; }
        public int Ay { get; set; }
        public int KitapSayisi { get; set; }

        public string AyAdi => new DateTime(Yil, Ay, 1).ToString("MMMM yyyy");
    }
}