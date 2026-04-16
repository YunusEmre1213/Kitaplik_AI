using KitapYonetimSistemi.Data;

namespace KitapYonetimSistemi.Models
{
    public class Tartisma
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public ICollection<Comment> Comments { get; set; }

    }
}
