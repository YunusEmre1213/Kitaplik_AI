using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;

namespace KitapYonetimSistemi.Services
{
    // Sayfa ilerlemesini kitaplık kaydına işleyen ortak kural (kitaplık, oturum ve sezon akışları kullanır)
    public static class IlerlemeIsleyici
    {
        // Kitap bu işlemle bittiyse true döner. Kayıtta Kitap yüklü olmalı.
        public static bool Uygula(ApplicationDbContext context, KullaniciKitaplik kayit, int sayfa, string? not)
        {
            var toplamSayfa = kayit.Kitap?.SayfaSayisi;
            kayit.MevcutSayfa = sayfa;
            // Okumaya devam eden kitabı bırakmış sayılmaz
            kayit.BirakmaNedeni = null;
            kayit.BirakmaTarihi = null;

            // İlk sayfa girilince okunacak kitap otomatik "Okuyorum" olur
            if (kayit.OkumaDurumu == OkumaDurumu.Okunacak && sayfa > 0)
            {
                kayit.OkumaDurumu = OkumaDurumu.Okuyorum;
                kayit.OkumayaBaslamaTarihi ??= DateTime.Now;
            }

            // Son sayfaya gelince kitap otomatik "Okundu" olur
            var bitirdi = false;
            if (toplamSayfa.HasValue && sayfa == toplamSayfa.Value && kayit.OkumaDurumu != OkumaDurumu.Okundu)
            {
                kayit.OkumaDurumu = OkumaDurumu.Okundu;
                kayit.OkumayaBaslamaTarihi ??= DateTime.Now;
                kayit.OkumaBitisTarihi = DateTime.Now;
                bitirdi = true;
            }

            context.OkumaGunlukleri.Add(new OkumaGunlugu
            {
                KullaniciKitaplikId = kayit.Id,
                Sayfa = sayfa,
                Not = not,
                Tarih = DateTime.Now
            });
            return bitirdi;
        }
    }
}
