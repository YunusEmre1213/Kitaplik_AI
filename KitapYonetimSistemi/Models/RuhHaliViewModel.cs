namespace KitapYonetimSistemi.Models
{
    public class RuhHaliViewModel
    {
        public string? Mood { get; set; }
        public string? Hata { get; set; }
        public List<RuhHaliOneri>? Oneriler { get; set; }
    }

    public class RuhHaliOneri
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        // Öneri bizim kataloğumuzda varsa kitabın id'si
        public int? KatalogKitapId { get; set; }
    }
}
