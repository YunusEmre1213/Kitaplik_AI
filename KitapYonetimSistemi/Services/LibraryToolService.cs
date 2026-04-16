using KitapYonetimSistemi.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace KitapYonetimSistemi.Services
{
    public class LibraryToolService
    {
        private readonly ApplicationDbContext _context;

        public LibraryToolService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // 1. KULLANICI KİTAPLIĞI KONTROLÜ (Kişisel)
        // ---------------------------------------------------------
        public async Task<string> KullaniciKitapligindaVarMi(string kitapAdi, string userId)
        {
            if (string.IsNullOrEmpty(kitapAdi)) return "Kitap adı anlaşılmadı.";
            if (string.IsNullOrEmpty(userId)) return "Senin kim olduğunu bulamadım, lütfen giriş yap.";

            var kayit = await _context.KullaniciKitapliklari
                .Include(kk => kk.Kitap)
                .Where(kk => kk.KullaniciId == userId && kk.Kitap.KitapAdi.ToLower().Contains(kitapAdi.ToLower()))
                .FirstOrDefaultAsync();

            if (kayit == null)
            {
                return $"'{kitapAdi}' senin kişisel kitaplığında ekli değil. Eklemek ister misin? 📚";
            }

            string durumMesaji = "";

            // DÜZELTME BURADA YAPILDI: (int) ekledik 👇
            // Enum olan değeri sayıya (integer) çevirip kıyaslıyoruz.
            switch ((int)kayit.OkumaDurumu)
            {
                case 1: durumMesaji = "ve okunacaklar listende bekliyor ⏳"; break;
                case 2: durumMesaji = "ve şu an bunu okuyorsun 📖"; break;
                case 3: durumMesaji = "ve bunu daha önce okumuşsun ✅"; break;
                default: durumMesaji = "ve listende ekli 👍"; break;
            }
        

            return $"Evet! '{kayit.Kitap.KitapAdi}' senin kitaplığında mevcut {durumMesaji}.";
        }

        // ---------------------------------------------------------
        // 2. KİTAP YORUMLARINI GETİR (Genel)
        // ---------------------------------------------------------
        public async Task<string> YorumlariGetir(string kitapAdi)
        {
            if (string.IsNullOrEmpty(kitapAdi)) return "Hangi kitabın yorumlarını istediğini anlayamadım.";

            // Önce kitabı genel 'Kitaplar' tablosunda bulalım
            var kitap = await _context.Kitaplar
                .FirstOrDefaultAsync(k => k.KitapAdi.ToLower().Contains(kitapAdi.ToLower()));

            // Kitap hiç yoksa yorum da yoktur
            if (kitap == null)
                return $"'{kitapAdi}' isimli kitabı genel kütüphanemizde bulamadım, bu yüzden yorumları getiremiyorum.";

            // Kitaba ait yorumları çek (En yeniden eskiye)
            var yorumlar = await _context.Yorumlar
                .Where(y => y.KitapId == kitap.Id)
                .OrderByDescending(y => y.YorumTarihi)
                .Take(5) // Çok uzun olmasın diye son 5 yorum
                .Select(y => y.YorumMetni)
                .ToListAsync();

            if (yorumlar.Count == 0)
            {
                return $"'{kitap.KitapAdi}' kitabı için henüz hiç yorum yapılmamış. İlk yorumu sen yapabilirsin! ✍️";
            }

            // Yorumları alt alta maddeler halinde birleştir
            string yorumListesi = string.Join("\n- ", yorumlar);

            return $"📢 '{kitap.KitapAdi}' hakkında yapılan son yorumlar:\n- {yorumListesi}";
        }
    }
}