namespace KitapYonetimSistemi.Models
{
    public class BookAnalysisViewModel
    {
        public string Title { get; set; }  // Kitabın gerçek adı
        public string Author { get; set; } // Yazarın gerçek adı
        public string CoverUrl { get; set; } // Kapak resmi için

        // API'den gelecek veriler:
        public string Summary { get; set; }
        public string Theme { get; set; }
        public List<string> Reasons { get; set; }
        public string SimilarBooks { get; set; }
    }
}