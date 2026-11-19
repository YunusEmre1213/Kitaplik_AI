namespace KitapYonetimSistemi.Models
{
    // Okurun bir kitabı neden bıraktığı
    public enum BirakmaNedeni
    {
        Yavas = 1,
        Agir = 2,
        ZamanDegil = 3,
        Beklenti = 4,
        Diger = 5
    }

    public static class BirakmaNedeniMetni
    {
        public static string Etiket(BirakmaNedeni n) => n switch
        {
            BirakmaNedeni.Yavas => "Çok yavaş ilerledi",
            BirakmaNedeni.Agir => "Dili ya da konusu ağırdı",
            BirakmaNedeni.ZamanDegil => "Zamanı değildi",
            BirakmaNedeni.Beklenti => "Beklediğim gibi çıkmadı",
            _ => "Başka bir sebep"
        };
    }
}
