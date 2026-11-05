using System.Text.RegularExpressions;

namespace KitapYonetim.Importer;

// Open Library konu başlıklarını sitenin Türkçe kategorilerine eşleyen merkezi kural listesi.
// Open Library konuları doğrudan kategori olarak kullanılmaz.
public static class KategoriEsleme
{
    // Sıra önemlidir: bir kitabın birden fazla kategorisi olabilir, "Tur" alanı için ilk eşleşen (öncelikli) kategori seçilir.
    private static readonly (string Kategori, string[] Desenler)[] Kurallar =
    {
        ("Bilim Kurgu",      new[] { @"\bscience fiction\b", @"\bsci-?fi\b", @"\bbilim kurgu\b" }),
        ("Fantastik",        new[] { @"\bfantasy\b", @"\bfantastik\b", @"\bwizards?\b", @"\bdragons?\b" }),
        ("Polisiye",         new[] { @"\bdetective\b", @"\bmystery\b", @"\bmysteries\b", @"\bcrime\b", @"\bthrillers?\b", @"\bsuspense\b", @"\bpolisiye\b" }),
        ("Korku",            new[] { @"\bhorror\b", @"\bghost stories\b", @"\bgothic\b", @"\bkorku\b" }),
        ("Romantik",         new[] { @"\bromance\b", @"\blove stories\b", @"\bromantik\b" }),
        ("Macera",           new[] { @"\badventure\b", @"\bmacera\b" }),
        ("Tarih",            new[] { @"\bhistory\b", @"\bhistorical\b", @"\btarih\b" }),
        ("Felsefe",          new[] { @"\bphilosophy\b", @"\bfelsefe\b" }),
        ("Psikoloji",        new[] { @"\bpsychology\b", @"\bpsikoloji\b", @"\bpsikoloji\b" }),
        ("Biyografi",        new[] { @"\bbiography\b", @"\bautobiography\b", @"\bmemoirs?\b", @"\bbiyografi\b", @"\bhatira\b" }),
        ("Kişisel Gelişim",  new[] { @"\bself-?help\b", @"\bself-improvement\b", @"\bmotivational\b", @"\bkisisel gelisim\b" }),
        ("Şiir",             new[] { @"\bpoetry\b", @"\bpoems?\b", @"\bsiir\b" }),
        ("Hikâye",           new[] { @"\bshort stories\b", @"\bhikaye\b", @"\bshort story\b" }),
        ("Çocuk",            new[] { @"\bjuvenile\b", @"\bchildren'?s?\b", @"\bpicture books?\b", @"\bcocuk\b" }),
        ("Bilim",            new[] { @"\bscience\b", @"\bphysics\b", @"\bbiology\b", @"\bmathematics\b", @"\bastronomy\b", @"\bnature\b", @"\bbilim\b" }),
        ("Roman",            new[] { @"^fiction$", @"\bnovels?\b", @"\bgeneral fiction\b", @"\bliterary fiction\b", @"\bromanlar\b", @"^roman$" }),
    };

    private static readonly (string Kategori, Regex[] Desenler)[] Derlenmis =
        Kurallar.Select(k => (k.Kategori, k.Desenler.Select(d => new Regex(d, RegexOptions.Compiled | RegexOptions.CultureInvariant)).ToArray())).ToArray();

    // Kütüphane konu başlıklarında sık geçen ama ilgili kategoriye ait olmayan kalıplar.
    // Örn. "Turkish poetry -- History and criticism" bir tarih kitabı değil, edebiyat eleştirisidir.
    private static readonly Dictionary<string, Regex[]> Istisnalar = new()
    {
        ["Tarih"] = new[]
        {
            new Regex(@"history and criticism|history of literature|literary history|history and theory", RegexOptions.Compiled),
            new Regex(@"(natural|medical|computer|art) history", RegexOptions.Compiled)
        },
        ["Bilim"] = new[]
        {
            new Regex(@"(social|political|library|computer|domestic|christian) sciences?", RegexOptions.Compiled)
        }
    };

    public static IEnumerable<string> TumKategoriler => Kurallar.Select(k => k.Kategori);

    // Bir konu başlığının düştüğü kategoriler (öncelik sırasına göre)
    public static List<string> Esle(string konu)
    {
        var normal = Normallestir(konu);
        var sonuc = new List<string>();
        foreach (var (kategori, desenler) in Derlenmis)
            if (desenler.Any(d => d.IsMatch(normal)) &&
                !(Istisnalar.TryGetValue(kategori, out var istisna) && istisna.Any(i => i.IsMatch(normal))))
                sonuc.Add(kategori);
        return sonuc;
    }

    // Küçük harfe çevirir, Türkçe karakterleri ve noktalamayı sadeleştirir
    private static string Normallestir(string konu)
    {
        var k = konu.Trim().ToLowerInvariant()
            .Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g').Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c').Replace('â', 'a').Replace('î', 'i');
        return Regex.Replace(k, @"[^a-z0-9' -]+", " ").Trim();
    }
}
