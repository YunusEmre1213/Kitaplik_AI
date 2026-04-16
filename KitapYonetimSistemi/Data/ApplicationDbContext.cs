using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Kitap> Kitaplar { get; set; }
        public DbSet<Yorum> Yorumlar { get; set; }
        public DbSet<KullaniciKitaplik> KullaniciKitapliklari { get; set; }
        public DbSet<KitapPuan> KitapPuanlari { get; set; }
        public DbSet<Tartisma> Tartismalar { get; set; }
        public DbSet<Comment> Comments { get; set; }

     
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Yorum ilişkileri
            builder.Entity<Yorum>()
                .HasOne(y => y.Kitap)
                .WithMany(k => k.Yorumlar)
                .HasForeignKey(y => y.KitapId);

            builder.Entity<Yorum>()
                .HasOne(y => y.Kullanici)
                .WithMany(k => k.Yorumlar)
                .HasForeignKey(y => y.KullaniciId);

            // Kitaplık ilişkileri
            builder.Entity<KullaniciKitaplik>()
                .HasOne(kk => kk.Kullanici)
                .WithMany(k => k.Kitapligim)
                .HasForeignKey(kk => kk.KullaniciId);

            builder.Entity<KullaniciKitaplik>()
                .HasOne(kk => kk.Kitap)
                .WithMany(k => k.KullaniciKitapliklari)
                .HasForeignKey(kk => kk.KitapId);

            // YENİ: Puan ilişkileri
            builder.Entity<KitapPuan>()
                .HasOne(kp => kp.Kitap)
                .WithMany(k => k.Puanlar)
                .HasForeignKey(kp => kp.KitapId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<KitapPuan>()
                .HasOne(kp => kp.Kullanici)
                .WithMany(k => k.VerdigiPuanlar)
                .HasForeignKey(kp => kp.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            // YENİ: Bir kullanıcı bir kitaba sadece bir kez puan verebilir
            builder.Entity<KitapPuan>()
                .HasIndex(kp => new { kp.KitapId, kp.KullaniciId })
                .IsUnique();

            builder.Entity<Tartisma>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // YORUM - KULLANICI

            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // YORUM - TARTIŞMA
            builder.Entity<Comment>()
                .HasOne(c => c.Tartisma)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TartismaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
       
    }
}