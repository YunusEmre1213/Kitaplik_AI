using KitapYonetimSistemi.Data;

namespace KitapYonetimSistemi.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TartismaId { get; set; }
        public Tartisma Tartisma { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
