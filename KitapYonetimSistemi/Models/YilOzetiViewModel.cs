namespace KitapYonetimSistemi.Models
{
    // Kullanıcının bir yıllık okuma özeti
    public class YilOzetiViewModel
    {
        public int Yil { get; set; }
        public List<int> Yillar { get; set; } = new();

        public List<KullaniciKitaplik> Biten { get; set; } = new();
        public int KitapSayisi => Biten.Count;
        public int SayfaSayisi { get; set; }
        public int Dakika { get; set; }
        public int EnUzunSeri { get; set; }
        public int? HedefKitapSayisi { get; set; }

        // Ocak..Aralık: o ay biten kitap sayısı
        public int[] AylikDagilim { get; set; } = new int[12];
        public int EnCokOkunanAy => AylikDagilim.Max() == 0 ? 0 : Array.IndexOf(AylikDagilim, AylikDagilim.Max()) + 1;

        public Kitap? EnUzunKitap { get; set; }
        public Kitap? EnKisaKitap { get; set; }
        public Kitap? IlkKitap { get; set; }
        public Kitap? SonKitap { get; set; }
        public (Kitap Kitap, int Puan)? EnYuksekPuanli { get; set; }

        public List<(string Tur, int Sayi)> Turler { get; set; } = new();
        public int AlintiSayisi { get; set; }
        public int KenarNotuSayisi { get; set; }

        public bool Bos => Biten.Count == 0 && Dakika == 0;
    }
}
