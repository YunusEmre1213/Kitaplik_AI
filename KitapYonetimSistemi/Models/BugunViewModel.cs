namespace KitapYonetimSistemi.Models
{
    public class BugunViewModel
    {
        public string Selam { get; set; } = string.Empty;
        public string Ad { get; set; } = string.Empty;
        public int Seri { get; set; }
        public bool BugunOkundu { get; set; }
        public int BugunSayfa { get; set; }
        public int BugunDakika { get; set; }
        public string? AktifOturumKitap { get; set; }
        public List<KullaniciKitaplik> Okuyorlar { get; set; } = new();
        public List<BugunSezon> Sezonlar { get; set; } = new();
        public List<BugunOda> Odalar { get; set; } = new();
        public List<BugunOneri> Oneriler { get; set; } = new();
        public List<BugunKapsul> Kapsuller { get; set; } = new();
        public List<Kitap> Yeniler { get; set; } = new();
        public bool Bos => Okuyorlar.Count == 0 && Sezonlar.Count == 0 && Odalar.Count == 0;
    }

    public class BugunSezon
    {
        public int SezonId { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string KitapAdi { get; set; } = string.Empty;
        public SezonGorevi Gorev { get; set; } = new(SezonGorevDurumu.Tamam, 0, 0);
        public int SayfaBasi { get; set; }
        public int SayfaSonu { get; set; }
        public int BolumSayisi { get; set; }
        public DateTime Baslangic { get; set; }
    }

    public class BugunOda
    {
        public int OdaId { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string KitapAdi { get; set; } = string.Empty;
        public int BenimSayfam { get; set; }
        // Benden ilerideki üyelerin sayısı ve en ilerideki üyenin sayfası
        public int IlerideKisi { get; set; }
        public int EnIlerideSayfa { get; set; }
        public string? SonMesajYazari { get; set; }
        public DateTime? SonMesajTarihi { get; set; }
    }

    public class BugunOneri
    {
        public int OneriId { get; set; }
        public string KitapAdi { get; set; } = string.Empty;
        public string KabulEden { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }
    }

    public class BugunKapsul
    {
        public int KitapId { get; set; }
        public string KitapAdi { get; set; } = string.Empty;
        public int SayfaNo { get; set; }
        public string Metin { get; set; } = string.Empty;
        public DateTime Yazildi { get; set; }
    }
}
