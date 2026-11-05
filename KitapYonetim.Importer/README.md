# KitapYonetim.Importer

Open Library toplu veri dosyalarından (dump) Türkçe ve İngilizce kitapları SQL Server'a aktaran bağımsız konsol uygulaması.
Web sitesinden ayrı çalışır, site açıldığında dış servise gitmez.

## Nasıl çalışır

Open Library dump dosyaları `.txt.gz` biçimindedir ve çok büyüktür. Importer dosyaları **belleğe yüklemeden, satır satır** okur (RAM kullanımı ~90 MB).

| Aşama | Girdi | İş |
|---|---|---|
| `editions` | editions dump | Dil filtresiyle (`tur`, `eng`) baskıları okur, `import.*` staging tablolarına toplu yazar |
| `works` | works dump | Yalnızca editions'ta geçen eserlerin açıklama, konu ve yazar bilgisini alır |
| `authors` | authors dump | Yalnızca kitaplarda geçen yazarların adını alır |
| `birlestir` | staging tabloları | `Kitaplar`, `Yazarlar`, `KitapYazarlari`, `Kategoriler`, `KitapKategorileri`, `Yayinevleri` tablolarına 20.000'lik dilimlerle yazar (her dilim tek transaction) |

Birleştirmede:
- Her **baskı** ayrı `Kitaplar` kaydıdır (aynı eserin farklı baskıları tek kayda birleştirilmez). Baskılar `OpenLibraryWorkKey` ile ilişkilidir.
- Baskıda yazar yoksa eserdeki yazarlar kullanılır.
- Baskıda açıklama yoksa eserin açıklaması kullanılır. Var olan açıklamanın üzerine yazılmaz.
- Open Library konu başlıkları `KategoriEsleme.cs` içindeki merkezi kurallarla Türkçe kategorilere eşlenir (örn. `Science Fiction` → `Bilim Kurgu`). Bir kitap birden fazla kategoride olabilir.
- Eksik alanlar nedeniyle kayıt silinmez, alan boş kalır. Hiçbir alan uydurulmaz.
- Kapaklar indirilmez, yalnızca `covers.openlibrary.org` adresi saklanır.
- Başlıklar ve adlar Unicode NFC biçimine çevrilir (eski kayıtlardaki "ayrık" Türkçe harfler düzelir).

## Güvenilirlik

- **Kaldığı yerden devam:** Her parti, devam noktasıyla (`ImportLoglari.SonSatir`) aynı transaction'da yazılır. Süreç sert sonlandırılsa bile aynı komut kaldığı satırdan devam eder.
- **Tekrar çalıştırma güvenli:** Staging tablolarında ve hedef tablolarda anahtarlar benzersizdir, yazma "yoksa ekle" mantığındadır. Aynı veri tekrar aktarılınca mükerrer kayıt oluşmaz.
- **Hatalı kayıtlar atlanır**, satır numarası ve nedeni `import-hatalari.log` dosyasına yazılır.
- **Eksik indirilmiş dosya** çökmeden okunabildiği kadar işlenir. Kayıt anahtarına dosya boyutu dahil olduğu için dosya tamamlanınca yeniden taranır.
- `Ctrl+C`: o ana kadarki parti yazılır, devam noktası kaydedilir.

## Kullanım

Bağlantı dizesi ana uygulamayla aynı user-secrets deposundan (`ConnectionStrings:DefaultConnection`) okunur. `--baglanti` ile de verilebilir.

```
dotnet run --project KitapYonetim.Importer -c Release -- \
  --editions <editions.txt.gz> --works <works.txt.gz> --authors <authors.txt.gz> \
  --diller tur,eng --dil-basina-max 5000
```

Seçenekler: `--asamalar editions,works,authors,birlestir`, `--parti`, `--hata-log`, `--yeniden`. Ayrıntı: `--yardim`.

Dump dosyaları: <https://openlibrary.org/developers/dumps>. Dosyalar yüzlerce MB ile GB arasındadır; `editions` dosyasının tamamı gerekmez, `--dil-basina-max` ile yetinildiğinde ilk bölümü yeterlidir (eksik indirilmiş dosya da okunabilir).

## Bilinen sınırlar

- SQL Server Express'te veritabanı en fazla 10 GB olabilir. Milyonlarca baskı için Standart sürüm gerekir.
- Express sürümünde Full-Text Search yoktur; API araması `LIKE` ile yapılır (önce baştan eşleşme).
- Open Library'de açıklama ve konu bilgisi seyrektir; birçok kitapta bu alanlar boş kalır.
- Open Library verilerinin lisans koşulları bu kod tarafından doğrulanmamıştır; kullanmadan önce kontrol edin.
