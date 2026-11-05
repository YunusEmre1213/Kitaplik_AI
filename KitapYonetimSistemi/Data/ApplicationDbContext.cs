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
        public DbSet<Yazar> Yazarlar { get; set; }
        public DbSet<KitapYazar> KitapYazarlari { get; set; }
        public DbSet<Kategori> Kategoriler { get; set; }
        public DbSet<KitapKategori> KitapKategorileri { get; set; }
        public DbSet<Yayinevi> Yayinevleri { get; set; }
        public DbSet<ImportLog> ImportLoglari { get; set; }

     
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

            // Kitap katalog alanları: indekslenebilmesi için uzunluk sınırları
            builder.Entity<Kitap>(k =>
            {
                k.Property(x => x.KitapAdi).HasMaxLength(400);
                k.Property(x => x.YazarAdi).HasMaxLength(400);
                k.Property(x => x.Tur).HasMaxLength(100);
                k.Property(x => x.Dil).HasMaxLength(10);
                k.Property(x => x.Isbn10).HasMaxLength(10);
                k.Property(x => x.Isbn13).HasMaxLength(13);
                k.Property(x => x.YayinTarihi).HasMaxLength(50);
                k.Property(x => x.OpenLibraryEditionKey).HasMaxLength(20);
                k.Property(x => x.OpenLibraryWorkKey).HasMaxLength(20);

                // Her baskı bir kez bulunur, aynı ISBN birden fazla baskıda tekrar edebilir (indeks benzersiz değil)
                k.HasIndex(x => x.OpenLibraryEditionKey).IsUnique().HasFilter("[OpenLibraryEditionKey] IS NOT NULL");
                k.HasIndex(x => x.OpenLibraryWorkKey);
                k.HasIndex(x => x.Isbn13);
                k.HasIndex(x => x.Isbn10);
                k.HasIndex(x => x.KitapAdi);
                k.HasIndex(x => x.YazarAdi);
                k.HasIndex(x => x.EklenmeTarihi);
                k.HasIndex(x => x.Tur);
                k.HasIndex(x => x.Dil);

                k.HasOne(x => x.Yayinevi).WithMany().HasForeignKey(x => x.YayineviId).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Yazar>(y =>
            {
                y.Property(x => x.Ad).HasMaxLength(300).IsRequired();
                y.Property(x => x.OpenLibraryKey).HasMaxLength(20);
                y.HasIndex(x => x.OpenLibraryKey).IsUnique().HasFilter("[OpenLibraryKey] IS NOT NULL");
                y.HasIndex(x => x.Ad);
            });

            builder.Entity<KitapYazar>(ky =>
            {
                ky.HasKey(x => new { x.KitapId, x.YazarId });
                ky.HasOne(x => x.Kitap).WithMany(k => k.Yazarlar).HasForeignKey(x => x.KitapId).OnDelete(DeleteBehavior.Cascade);
                ky.HasOne(x => x.Yazar).WithMany(y => y.Kitaplar).HasForeignKey(x => x.YazarId).OnDelete(DeleteBehavior.Cascade);
                ky.HasIndex(x => x.YazarId);
            });

            builder.Entity<Kategori>(k =>
            {
                k.Property(x => x.Ad).HasMaxLength(100).IsRequired();
                k.HasIndex(x => x.Ad).IsUnique();
            });

            builder.Entity<KitapKategori>(kk =>
            {
                kk.HasKey(x => new { x.KitapId, x.KategoriId });
                kk.HasOne(x => x.Kitap).WithMany(k => k.Kategoriler).HasForeignKey(x => x.KitapId).OnDelete(DeleteBehavior.Cascade);
                kk.HasOne(x => x.Kategori).WithMany(k => k.Kitaplar).HasForeignKey(x => x.KategoriId).OnDelete(DeleteBehavior.Cascade);
                kk.HasIndex(x => x.KategoriId);
            });

            builder.Entity<Yayinevi>(y =>
            {
                y.Property(x => x.Ad).HasMaxLength(300).IsRequired();
                y.HasIndex(x => x.Ad).IsUnique();
            });

            builder.Entity<ImportLog>(l =>
            {
                l.Property(x => x.Kaynak).HasMaxLength(50).IsRequired();
                l.Property(x => x.Surum).HasMaxLength(100).IsRequired();
                l.Property(x => x.Durum).HasMaxLength(20).IsRequired();
                l.HasIndex(x => new { x.Kaynak, x.Surum });
            });

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