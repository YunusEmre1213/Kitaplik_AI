using System.Globalization;
using System.Text;

namespace KitapYonetimSistemi.Models
{
    // CSV dosyasından okunan tek bir kitap satırı
    public record IceAktarilanSatir(
        int SatirNo,
        string Baslik,
        string Yazar,
        string? Isbn,
        int? Puan,
        OkumaDurumu Durum,
        DateTime? OkunmaTarihi);

    // Goodreads dışa aktarımı ve Türkçe/İngilizce başlıklı genel CSV dosyaları için saf ayrıştırıcı.
    public static class KitaplikCsv
    {
        public const int EnFazlaSatir = 1000;

        private static readonly string[] BaslikAdlari = { "title", "kitap adi", "kitap", "ad", "adi", "baslik", "kitap ismi", "eser adi" };
        private static readonly string[] YazarAdlari = { "author", "yazar", "yazari", "yazar adi", "yazarlar" };
        private static readonly string[] IsbnAdlari = { "isbn13", "isbn", "isbn10", "isbn 13", "isbn 10", "isbn no" };
        private static readonly string[] PuanAdlari = { "my rating", "rating", "puan", "benim puanim", "puanim", "verdigim puan" };
        private static readonly string[] DurumAdlari = { "exclusive shelf", "shelf", "bookshelves", "raf", "durum", "okuma durumu", "liste" };
        private static readonly string[] TarihAdlari = { "date read", "okunma tarihi", "bitis tarihi", "okuma tarihi", "okundugu tarih" };

        // Başlıkları ve harf farklarını eşitler: küçük harf, aksansız, tek boşluk
        public static string Katla(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var sb = new StringBuilder();
            foreach (var c in s.Trim().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                var k = c switch { 'ı' => 'i', 'İ' => 'i', 'ß' => 's', _ => char.ToLowerInvariant(c) };
                sb.Append(char.IsLetterOrDigit(k) ? k : ' ');
            }
            return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        // Yalnızca rakam ve X bırakır; Goodreads'in ="978..." biçimini de çözer. Geçerli ISBN değilse null.
        public static string? IsbnTemizle(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var temiz = new string(s.Where(c => char.IsDigit(c) || c is 'x' or 'X').ToArray()).ToUpperInvariant();
            return temiz.Length is 10 or 13 ? temiz : null;
        }

        public static OkumaDurumu DurumCoz(string? raf)
        {
            var k = Katla(raf);
            if (k.Length == 0) return OkumaDurumu.Okunacak;
            // Goodreads birden çok raf virgülle verebilir; "read" kelimesi tek başına geçmeli
            var kelimeler = k.Split(' ');
            if (k.Contains("currently reading") || k.Contains("okuyorum") || k.Contains("suan okudugum") || k.Contains("okunuyor"))
                return OkumaDurumu.Okuyorum;
            if (k.Contains("to read") || k.Contains("okunacak") || k.Contains("okumak istiyorum") || k.Contains("okuyacagim"))
                return OkumaDurumu.Okunacak;
            if (kelimeler.Contains("read") || k.Contains("okundu") || k.Contains("okudum") || k.Contains("bitti") || k.Contains("okunan"))
                return OkumaDurumu.Okundu;
            return OkumaDurumu.Okunacak;
        }

        public static DateTime? TarihCoz(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            string[] bicimler = { "yyyy/MM/dd", "yyyy-MM-dd", "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy" };
            return DateTime.TryParseExact(s.Trim(), bicimler, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : null;
        }

        // CSV metnini alanlara böler: tırnaklı alanlar, çift tırnak kaçışı ve satır içi yeni satır desteklenir.
        public static List<List<string>> Ayristir(string metin, char ayirici)
        {
            var satirlar = new List<List<string>>();
            var satir = new List<string>();
            var alan = new StringBuilder();
            var tirnakta = false;

            for (var i = 0; i < metin.Length; i++)
            {
                var c = metin[i];
                if (tirnakta)
                {
                    if (c == '"')
                    {
                        if (i + 1 < metin.Length && metin[i + 1] == '"') { alan.Append('"'); i++; }
                        else tirnakta = false;
                    }
                    else alan.Append(c);
                }
                else if (c == '"') tirnakta = true;
                else if (c == ayirici) { satir.Add(alan.ToString()); alan.Clear(); }
                else if (c is '\r' or '\n')
                {
                    if (c == '\r' && i + 1 < metin.Length && metin[i + 1] == '\n') i++;
                    satir.Add(alan.ToString()); alan.Clear();
                    if (satir.Any(a => a.Length > 0)) satirlar.Add(satir);
                    satir = new List<string>();
                }
                else alan.Append(c);
            }
            satir.Add(alan.ToString());
            if (satir.Any(a => a.Length > 0)) satirlar.Add(satir);
            return satirlar;
        }

        // İlk satırdaki en sık geçen ayırıcıyı seçer (virgül, noktalı virgül ya da sekme)
        public static char AyiriciBul(string metin)
        {
            var ilk = metin.Split('\n', 2)[0];
            var adaylar = new[] { ',', ';', '\t' };
            return adaylar.OrderByDescending(a => ilk.Count(c => c == a)).First();
        }

        public record Sonuc(List<IceAktarilanSatir> Satirlar, int Atlanan, bool BaslikBulundu, bool Kirpildi);

        public static Sonuc Oku(string metin)
        {
            metin = metin.TrimStart('﻿');
            var tablo = Ayristir(metin, AyiriciBul(metin));
            if (tablo.Count < 2) return new Sonuc(new(), 0, false, false);

            var basliklar = tablo[0].Select(Katla).ToList();
            int Sutun(string[] adlar)
            {
                foreach (var ad in adlar)
                {
                    var i = basliklar.IndexOf(ad);
                    if (i >= 0) return i;
                }
                return -1;
            }

            var bs = Sutun(BaslikAdlari);
            if (bs < 0) return new Sonuc(new(), 0, false, false);
            var yz = Sutun(YazarAdlari);
            var isbn = Sutun(IsbnAdlari);
            var isbnYedek = basliklar.FindIndex(b => b.StartsWith("isbn") && basliklar.IndexOf(b) != isbn);
            var pz = Sutun(PuanAdlari);
            var du = Sutun(DurumAdlari);
            var ta = Sutun(TarihAdlari);

            string Al(List<string> r, int i) => i >= 0 && i < r.Count ? r[i].Trim() : string.Empty;

            var sonuc = new List<IceAktarilanSatir>();
            var atlanan = 0;
            var kirpildi = false;
            for (var n = 1; n < tablo.Count; n++)
            {
                var r = tablo[n];
                var baslik = Al(r, bs);
                if (baslik.Length == 0) { atlanan++; continue; }
                if (sonuc.Count >= EnFazlaSatir) { kirpildi = true; break; }

                var isbnDegeri = IsbnTemizle(Al(r, isbn)) ?? IsbnTemizle(Al(r, isbnYedek));
                var puan = int.TryParse(Al(r, pz).Split(new[] { ',', '.' })[0], out var p) && p is >= 1 and <= 5 ? p : (int?)null;
                sonuc.Add(new IceAktarilanSatir(n + 1, baslik, Al(r, yz), isbnDegeri, puan, DurumCoz(Al(r, du)), TarihCoz(Al(r, ta))));
            }
            return new Sonuc(sonuc, atlanan, true, kirpildi);
        }
    }
}
