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
        public DbSet<KenarNotu> KenarNotlari { get; set; }
        public DbSet<SayfaTepkisi> SayfaTepkileri { get; set; }
        public DbSet<OkumaOturumu> OkumaOturumlari { get; set; }
        public DbSet<OkumaOdasi> OkumaOdalari { get; set; }
        public DbSet<OdaUyesi> OdaUyeleri { get; set; }
        public DbSet<OdaMesaji> OdaMesajlari { get; set; }
        public DbSet<Oneri> Oneriler { get; set; }
        public DbSet<OneriKabul> OneriKabulleri { get; set; }
        public DbSet<OkumaSezonu> OkumaSezonlari { get; set; }
        public DbSet<SezonUyesi> SezonUyeleri { get; set; }
        public DbSet<SezonMesaji> SezonMesajlari { get; set; }
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

            // Okuma oturumları (kullanıcı, kitaplık kaydı üzerinden bilinir; tek silme zinciri)
            builder.Entity<OkumaOturumu>(o =>
            {
                o.HasOne(x => x.KullaniciKitaplik).WithMany(k => k.Oturumlar).HasForeignKey(x => x.KullaniciKitaplikId).OnDelete(DeleteBehavior.Cascade);
                o.HasIndex(x => new { x.KullaniciKitaplikId, x.Baslangic });
                o.HasIndex(x => x.Bitis);
            });

            // Kenar notları
            builder.Entity<KenarNotu>(n =>
            {
                n.Property(x => x.Metin).HasMaxLength(KenarNotu.EnFazlaUzunluk);
                n.HasOne(x => x.Kitap).WithMany().HasForeignKey(x => x.KitapId).OnDelete(DeleteBehavior.Cascade);
                n.HasOne(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId).OnDelete(DeleteBehavior.Cascade);
                n.HasIndex(x => new { x.KitapId, x.SayfaNo });
                n.HasIndex(x => x.KullaniciId);
            });

            // Sayfa tepkileri: kullanıcı başına sayfada tek tepki
            builder.Entity<SayfaTepkisi>(t =>
            {
                t.HasOne(x => x.Kitap).WithMany().HasForeignKey(x => x.KitapId).OnDelete(DeleteBehavior.Cascade);
                t.HasOne(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId).OnDelete(DeleteBehavior.Cascade);
                t.HasIndex(x => new { x.KitapId, x.KullaniciId, x.SayfaNo }).IsUnique();
            });

            // Ortak okuma odaları: oluşturan için NoAction (çoklu silme zinciri olmasın), üyelik ve mesajlar kullanıcıyla silinir
            builder.Entity<OkumaOdasi>(o =>
            {
                o.Property(x => x.Ad).HasMaxLength(OkumaOdasi.AdUzunlugu).IsRequired();
                o.Property(x => x.Kod).HasMaxLength(OkumaOdasi.KodUzunlugu).IsRequired();
                o.HasIndex(x => x.Kod).IsUnique();
                o.HasOne(x => x.Kitap).WithMany().HasForeignKey(x => x.KitapId).OnDelete(DeleteBehavior.Cascade);
                o.HasOne(x => x.Olusturan).WithMany().HasForeignKey(x => x.OlusturanId).OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<OdaUyesi>(u =>
            {
                u.HasOne(x => x.Oda).WithMany(o => o.Uyeler).HasForeignKey(x => x.OdaId).OnDelete(DeleteBehavior.Cascade);
                u.HasOne(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId).OnDelete(DeleteBehavior.Cascade);
                u.HasIndex(x => new { x.OdaId, x.KullaniciId }).IsUnique();
                u.HasIndex(x => x.KullaniciId);
            });

            builder.Entity<OdaMesaji>(m =>
            {
                m.Property(x => x.Metin).HasMaxLength(OdaMesaji.EnFazlaUzunluk);
                m.HasOne(x => x.Oda).WithMany(o => o.Mesajlar).HasForeignKey(x => x.OdaId).OnDelete(DeleteBehavior.Cascade);
                m.HasOne(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId).OnDelete(DeleteBehavior.Cascade);
                m.HasIndex(x => new { x.OdaId, x.Tarih });
            });

            // Öneri zinciri: kullanıcılara bilerek veritabanı bağı yok (silinen okurun halkası zincirde kalır, admin silerken elle temizlenir)
            builder.Entity<Oneri>(o =>
            {
                o.Property(x => x.Kod).HasMaxLength(Oneri.KodUzunlugu).IsRequired();
                o.Property(x => x.Not).HasMaxLength(Oneri.NotUzunlugu);
                o.Property(x => x.GonderenId).HasMaxLength(450);
                o.HasIndex(x => x.Kod).IsUnique();
                o.HasIndex(x => x.GonderenId);
                o.HasOne(x => x.Kitap).WithMany().HasForeignKey(x => x.KitapId).OnDelete(DeleteBehavior.Cascade);
                o.HasOne(x => x.OncekiKabul).WithMany().HasForeignKey(x => x.OncekiKabulId).OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<OneriKabul>(k =>
            {
                k.Property(x => x.KullaniciId).HasMaxLength(450).IsRequired();
                k.HasOne(x => x.Oneri).WithMany(o => o.Kabuller).HasForeignKey(x => x.OneriId).OnDelete(DeleteBehavior.Cascade);
                k.HasIndex(x => new { x.KullaniciId, x.KitapId }).IsUnique();
                k.HasIndex(x => x.OneriId);
            });

            // Okuma sezonları (dizi gibi ortak okuma): oluşturan için NoAction, üyelik ve mesajlar kullanıcıyla silinir
            builder.Entity<OkumaSezonu>(s =>
            {
                s.Property(x => x.Ad).HasMaxLength(OkumaSezonu.AdUzunlugu).IsRequired();
                s.HasOne(x => x.Kitap).WithMany().HasForeignKey(x => x.KitapId).OnDelete(DeleteBehavior.Cascade);
                s.HasOne(x => x.Olusturan).WithMany().HasForeignKey(x => x.OlusturanId).OnDelete(DeleteBehavior.NoAction);
                s.HasIndex(x => x.Baslangic);
            });

            builder.Entity<SezonUyesi>(u =>
            {
                u.HasOne(x => x.Sezon).WithMany(s => s.Uyeler).HasForeignKey(x => x.SezonId).OnDelete(DeleteBehavior.Cascade);
                u.HasOne(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId).OnDelete(DeleteBehavior.Cascade);
                u.HasIndex(x => new { x.SezonId, x.KullaniciId }).IsUnique();
                u.HasIndex(x => x.KullaniciId);
            });

            builder.Entity<SezonMesaji>(m =>
            {
                m.Property(x => x.Metin).HasMaxLength(SezonMesaji.EnFazlaUzunluk);
                m.HasOne(x => x.Sezon).WithMany(s => s.Mesajlar).HasForeignKey(x => x.SezonId).OnDelete(DeleteBehavior.Cascade);
                m.HasOne(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId).OnDelete(DeleteBehavior.Cascade);
                m.HasIndex(x => new { x.SezonId, x.Bolum, x.Tarih });
            });

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