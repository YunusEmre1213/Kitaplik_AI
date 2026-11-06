namespace KitapYonetimSistemi.Models
{
    // _Kapak bileşeninin girdisi: gerçek kapak varsa o, yoksa başlıktan üretilen kapak gösterilir.
    public record KapakModel(string? Url, string Baslik, string? Yazar, string Boyut = "md")
    {
        // Aynı başlık her zaman aynı renk çiftini alır
        public int Renk
        {
            get
            {
                var toplam = 0;
                foreach (var c in Baslik ?? string.Empty)
                    toplam = (toplam * 31 + c) & 0x7FFFFFFF;
                return toplam % 8;
            }
        }

        public static KapakModel Of(Kitap k, string boyut = "md") => new(k.KapakResmiUrl, k.KitapAdi, k.YazarAdi, boyut);
    }
}
