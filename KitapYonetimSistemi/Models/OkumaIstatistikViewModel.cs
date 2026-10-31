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

        // Yıllık hedef
        public int Yil { get; set; }
        public int? HedefKitapSayisi { get; set; }
        public int BuYilOkunanSayisi { get; set; }
        public int BuYilOkunanSayfa { get; set; }
        public List<OkumaGunlugu> SonGunlukler { get; set; } = new();

        public double HedefYuzdesi => HedefKitapSayisi > 0
            ? Math.Min(100, (double)BuYilOkunanSayisi / HedefKitapSayisi.Value * 100)
            : 0;

        // Hedefe yetişmek için yıl sonuna kadar ayda kaç kitap okunmalı
        public double? AylikGerekenKitap
        {
            get
            {
                if (HedefKitapSayisi is not > 0) return null;
                var kalanKitap = HedefKitapSayisi.Value - BuYilOkunanSayisi;
                if (kalanKitap <= 0) return 0;
                var kalanAy = Math.Max(1, 12 - DateTime.Now.Month + 1);
                return Math.Round((double)kalanKitap / kalanAy, 1);
            }
        }

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