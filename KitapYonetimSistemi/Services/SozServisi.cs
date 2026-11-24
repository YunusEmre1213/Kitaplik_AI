using KitapYonetimSistemi.Data;
using KitapYonetimSistemi.Models;
using Microsoft.EntityFrameworkCore;

namespace KitapYonetimSistemi.Services
{
    // Okuma sözlerinin durum, ilerleme ve tempo bilgisi (söz sayfaları ve Bugün ekranı ortak kullanır)
    public class SozServisi
    {
        private readonly ApplicationDbContext _context;

        public SozServisi(ApplicationDbContext context)
        {
            _context = context;
        }

        // Sahibin kitaplık kaydından yalnızca sayfa ve okuma durumu okunur. Sözlerde Kitap ve Tanikler yüklü olmalı.
        public async Task<Dictionary<int, SozKarti>> KartlariOlusturAsync(List<OkumaSozu> sozler, DateTime simdi)
        {
            var sonuc = new Dictionary<int, SozKarti>();
            if (sozler.Count == 0) return sonuc;

            var kitapIdleri = sozler.Select(s => s.KitapId).Distinct().ToList();
            var sahipIdleri = sozler.Select(s => s.SahipId).Distinct().ToList();
            var kayitlar = await _context.KullaniciKitapliklari.AsNoTracking()
                .Where(kk => kitapIdleri.Contains(kk.KitapId) && sahipIdleri.Contains(kk.KullaniciId))
                .Select(kk => new { kk.KitapId, kk.KullaniciId, kk.MevcutSayfa, kk.OkumaDurumu, kk.OkumaBitisTarihi })
                .ToListAsync();
            var adlar = await AdlarAsync(sahipIdleri);

            foreach (var s in sozler)
            {
                var k = kayitlar.FirstOrDefault(x => x.KitapId == s.KitapId && x.KullaniciId == s.SahipId);
                var bitirdi = k?.OkumaDurumu == OkumaDurumu.Okundu;
                var toplam = s.Kitap?.SayfaSayisi;
                var mevcut = bitirdi && toplam.HasValue ? toplam.Value : k?.MevcutSayfa ?? 0;
                sonuc[s.Id] = new SozKarti
                {
                    Soz = s,
                    SahipAdi = adlar.GetValueOrDefault(s.SahipId, "silinmiş okur"),
                    Durum = SozKurali.Durum(s.Bitis, bitirdi, k?.OkumaBitisTarihi, simdi),
                    KalanGun = SozKurali.KalanGun(s.Bitis, simdi),
                    Yuzde = bitirdi ? 100 : SozKurali.Yuzde(mevcut, toplam),
                    MevcutSayfa = mevcut,
                    ToplamSayfa = toplam,
                    GunlukGereken = bitirdi ? 0 : SozKurali.GunlukGereken(mevcut, toplam, s.Bitis, simdi),
                    TanikSayisi = s.Tanikler.Count
                };
            }
            return sonuc;
        }

        public async Task<Dictionary<string, string>> AdlarAsync(IEnumerable<string> idler)
        {
            var liste = idler.Distinct().ToList();
            var kullanicilar = await _context.Users.AsNoTracking()
                .Where(u => liste.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();
            return kullanicilar.ToDictionary(u => u.Id, u => string.IsNullOrEmpty(u.UserName) ? "okur" : u.UserName.Split('@')[0]);
        }
    }
}
