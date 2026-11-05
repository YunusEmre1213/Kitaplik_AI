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
        public DbSet<OkumaHedefi> OkumaHedefleri { get; set; }
        public DbSet<OkumaGunlugu> OkumaGunlukleri { get; set; }
        public DbSet<Alinti> Alintilar { get; set; }
        public DbSet<AlintiBegeni> AlintiBegenileri { get; set; }

     
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Aynı ISBN ya da dış kaynak kimliği ile ikinci bir kitap eklenemez
            builder.Entity<Kitap>()
                .Property(k => k.Isbn).HasMaxLength(20);
            builder.Entity<Kitap>()
                .Property(k => k.DisKaynakId).HasMaxLength(40);

            builder.Entity<Kitap>()
                .HasIndex(k => k.Isbn)
                .IsUnique()
                .HasFilter("[Isbn] IS NOT NULL");

            builder.Entity<Kitap>()
                .HasIndex(k => k.DisKaynakId)
                .IsUnique()
                .HasFilter("[DisKaynakId] IS NOT NULL");

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

            // Okuma hedefi: kullanıcı başına yılda bir hedef
            builder.Entity<OkumaHedefi>()
                .HasOne(h => h.Kullanici)
                .WithMany()
                .HasForeignKey(h => h.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OkumaHedefi>()
                .HasIndex(h => new { h.KullaniciId, h.Yil })
                .IsUnique();

            // Okuma günlüğü
            builder.Entity<OkumaGunlugu>()
                .HasOne(g => g.KullaniciKitaplik)
                .WithMany(k => k.Gunlukler)
                .HasForeignKey(g => g.KullaniciKitaplikId)
                .OnDelete(DeleteBehavior.Cascade);

            // Alıntılar
            builder.Entity<Alinti>()
                .HasOne(a => a.Kitap)
                .WithMany(k => k.Alintilar)
                .HasForeignKey(a => a.KitapId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Alinti>()
                .HasOne(a => a.Kullanici)
                .WithMany()
                .HasForeignKey(a => a.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AlintiBegeni>()
                .HasOne(b => b.Alinti)
                .WithMany(a => a.Begeniler)
                .HasForeignKey(b => b.AlintiId)
                .OnDelete(DeleteBehavior.Cascade);

            // Birden fazla silme zinciri oluşmaması için NoAction, kullanıcı silinirken elle temizlenir
            builder.Entity<AlintiBegeni>()
                .HasOne(b => b.Kullanici)
                .WithMany()
                .HasForeignKey(b => b.KullaniciId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AlintiBegeni>()
                .HasIndex(b => new { b.AlintiId, b.KullaniciId })
                .IsUnique();

            // YORUM - TARTIŞMA
            builder.Entity<Comment>()
                .HasOne(c => c.Tartisma)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TartismaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
       
    }
}