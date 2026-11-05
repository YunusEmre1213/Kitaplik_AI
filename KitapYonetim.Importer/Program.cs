using KitapYonetim.Importer;
using Microsoft.Extensions.Configuration;

Ayarlar ayarlar;
try
{
    ayarlar = Ayarlar.Oku(args);
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message == "yardim" ? Ayarlar.Yardim : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}{Ayarlar.Yardim}");
    return ex.Message == "yardim" ? 0 : 1;
}

var yapilandirma = new ConfigurationBuilder()
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var baglanti = ayarlar.Baglanti ?? yapilandirma["ConnectionStrings:DefaultConnection"];
if (string.IsNullOrWhiteSpace(baglanti))
{
    Console.WriteLine("Bağlantı dizesi bulunamadı. --baglanti verin ya da ConnectionStrings:DefaultConnection ayarını tanımlayın.");
    return 1;
}

Gunluk.HataDosyasi(ayarlar.HataLogYolu);

// Ctrl+C: o ana kadar okunanlar yazılır, devam noktası kaydedilir
using var iptal = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Gunluk.Bilgi("Durdurma istendi, mevcut parti yazılıp çıkılacak...");
    iptal.Cancel();
};

var db = new Veritabani(baglanti);
db.StagingHazirla();
var aktarici = new Aktarici(ayarlar, db, iptal.Token);

try
{
    if (ayarlar.Asamalar.Contains("editions"))
        aktarici.Editions(Dosya(ayarlar.EditionsDosyasi, "--editions"));

    if (ayarlar.Asamalar.Contains("works"))
        aktarici.Works(Dosya(ayarlar.WorksDosyasi, "--works"));

    if (ayarlar.Asamalar.Contains("authors"))
        aktarici.Authors(Dosya(ayarlar.AuthorsDosyasi, "--authors"));

    if (ayarlar.Asamalar.Contains("birlestir"))
        aktarici.Birlestir();

    Gunluk.Bilgi("Bitti.");
    return 0;
}
catch (OperationCanceledException)
{
    Gunluk.Bilgi("Durduruldu. Aynı komutu tekrar çalıştırarak kaldığı yerden devam edebilirsiniz.");
    return 2;
}
catch (FileNotFoundException ex)
{
    Console.WriteLine(ex.Message);
    return 1;
}

static string Dosya(string? yol, string secenek)
{
    if (string.IsNullOrWhiteSpace(yol))
        throw new FileNotFoundException($"{secenek} dosya yolu verilmedi.");
    if (!File.Exists(yol))
        throw new FileNotFoundException($"Dosya bulunamadı: {yol}");
    return yol;
}

public partial class Program { }
