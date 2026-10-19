<div align="center">

# 📚 Satır Arası

**Kitap yönetimi, kişisel kitaplık, topluluk tartışmaları ve yapay zekâ destekli okuma asistanı**

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-5C2D91)
![EF Core](https://img.shields.io/badge/EF%20Core-9.0-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?logo=microsoftsqlserver&logoColor=white)
![Gemini](https://img.shields.io/badge/Google-Gemini%202.5%20Flash-4285F4?logo=google&logoColor=white)

</div>

---

## Proje Hakkında

**Satır Arası**, okurların kitap keşfettiği, kendi kitaplıklarını yönettiği, kitaplara yorum ve puan verdiği ve diğer okurlarla tartıştığı bir web uygulamasıdır. Yöneticiler kitap kataloğunu ve kullanıcıları yönetir. Google Gemini tabanlı yapay zekâ servisi ise kitap önerisi, spoiler içermeyen kitap analizi ve kütüphane verisiyle konuşabilen bir sohbet asistanı sunar.

Proje iki servisten oluşur:

| Servis | Rolü |
|---|---|
| `KitapYonetimSistemi` | Ana web uygulaması: ASP.NET Core MVC, Identity, EF Core |
| `KitapYonetim.AI.Service` | Gemini ile konuşan, ana uygulamanın kullandığı yapay zekâ Web API'si |

## Özellikler

### Kullanıcı
- Kayıt olma ve giriş yapma (ASP.NET Core Identity)
- Kitap kataloğunu listeleme, kitap detayı görüntüleme
- Kitaplara yorum yazma ve 1–5 arası puan verme (puan sonradan güncellenebilir)
- **Kişisel kitaplık:** kitap ekleme/çıkarma, kitaplığı herkese açık ya da özel yapma
- **Okuma durumu takibi:** Okunacak, Okuyorum, Okundu. Başlama ve bitiş tarihleri otomatik tutulur
- **Okuma istatistikleri:** durumlara göre kitap sayıları, son okunanlar, aylık okuma dağılımı
- Diğer kullanıcıların herkese açık kitaplıklarına göz atma
- Tartışma forumu: konu açma ve konulara yorum yazma

### Yapay Zekâ
- **Kitap önerisi:** kullanıcının kitaplığına bakarak yeni kitaplar ve gerekçelerini önerir
- **Kitap analizi:** özet, tema, "neden okunmalı" ve benzer kitaplar, spoiler vermeden
- **Sohbet asistanı:** Gemini function calling ile kütüphane verisine bağlanır. Kullanıcının kitaplığındaki bir kitabın durumunu ve bir kitabın yorumlarını sorgulayabilir

### Yönetici Paneli
- Gösterge paneli: toplam sayılar, bu ay eklenen kitaplar, en çok yorumlanan ve en yüksek puanlı kitaplar
- Kitap ekleme, düzenleme, silme ve kapak resmi yükleme
- Yorum yönetimi: kitaba ve kullanıcıya göre filtreleme, silme
- Kullanıcı yönetimi: rol atama, kullanıcı silme

## Mimari

```
┌──────────────────────┐    HTTPS + servis anahtarı    ┌──────────────────────────┐        ┌────────────────┐
│  KitapYonetimSistemi │ ────────────────────────────▶ │  KitapYonetim.AI.Service │ ─────▶ │  Google Gemini │
│  (ASP.NET Core MVC)  │ ◀──────────────────────────── │       (Web API)          │ ◀───── │      API       │
└──────────┬───────────┘     öneri / analiz / chat     └──────────────────────────┘        └────────────────┘
           │ EF Core
           ▼
   ┌───────────────┐
   │  SQL Server   │
   └───────────────┘
```

Uygulama, **MVC** deseniyle katmanlara ayrılmıştır: Controller'lar istekleri yönetir, ViewModel'ler görünümlere veri taşır, `ApplicationDbContext` veri erişimini sağlar, `LibraryToolService` ise yapay zekâ asistanının kullandığı veritabanı araçlarını barındırır.

### Sohbet asistanı akışı

1. Tarayıcı mesajı ana uygulamaya gönderir.
2. Ana uygulama mesajı doğrular, hız sınırını kontrol eder ve AI servisine iletir.
3. AI servisi mesajı, tanımlı araçlarla birlikte Gemini'ye gönderir.
4. Gemini bir araç çağırmak isterse AI servisi bunu ana uygulamaya bildirir.
5. Ana uygulama aracı **oturumdaki kullanıcının kimliğiyle** çalıştırır ve sonucu döner.

Kullanıcı kimliği hiçbir zaman modelden alınmaz. Bu yüzden asistan başka bir kullanıcının kitaplığını sorgulayamaz.

## Teknolojiler

| Katman | Teknoloji |
|---|---|
| Web | ASP.NET Core MVC (.NET 9), Razor, Bootstrap |
| Kimlik ve yetki | ASP.NET Core Identity, rol tabanlı yetkilendirme |
| Veri erişimi | Entity Framework Core 9, Code First migrations |
| Veritabanı | SQL Server |
| Yapay zekâ | Google Gemini 2.5 Flash (REST, function calling) |
| API dokümantasyonu | Swagger (Swashbuckle) |

## Tasarım Kararları ve Güvenlik

- **İki servisli yapı:** Yapay zekâ mantığı ayrı bir serviste tutulur. Böylece Gemini anahtarı yalnızca bu serviste bulunur ve servis bağımsız olarak değiştirilebilir ya da ölçeklenebilir.
- **Servisler arası kimlik doğrulama:** AI servisine yalnızca ortak gizli anahtarı bilen istemci erişebilir.
- **Rol tabanlı yetkilendirme:** `Admin` ve `User` rolleri ayrıdır. Yöneticiler kendi rollerini değiştiremez ve kendi hesaplarını silemez.
- **CSRF koruması:** Durum değiştiren tüm işlemlerde anti-forgery token doğrulanır.
- **Yetki ihlali (IDOR) koruması:** Yapay zekâ araçları yalnızca giriş yapmış kullanıcının verisine erişir.
- **Yapay zekâ girdi güvenliği:** Mesaj uzunluğu ve içeriği doğrulanır, kullanıcı başına hız sınırı uygulanır, sistem talimatı konuyu kütüphane işlemleriyle sınırlar.
- **Dosya yükleme güvenliği:** Kapak resimleri için uzantı beyaz listesi ve rastgele dosya adı kullanılır.
- **Gizli bilgi yönetimi:** Anahtarlar ve bağlantı bilgileri kod deposunda tutulmaz. Yapılandırma üzerinden verilir.
- **Açık yönlendirme engeli:** Yönlendirmelerde yalnızca yerel adreslere izin verilir.

## Veritabanı

Code First yaklaşımı kullanılır.

| Varlık | Açıklama |
|---|---|
| `Kitap` | Ad, yazar, yayın yılı, açıklama, kapak resmi, ortalama puan |
| `Yorum` | Kitaba yapılan kullanıcı yorumu |
| `KitapPuan` | Kullanıcının kitaba verdiği puan (1–5) |
| `KullaniciKitaplik` | Kullanıcı–kitap ilişkisi: görünürlük, okuma durumu, başlama ve bitiş tarihi |
| `Tartisma` / `Comment` | Forum konusu ve konu yorumları |
| `ApplicationUser` | Identity kullanıcısı |

## Proje Yapısı

```
├── KitapYonetimSistemi/            # Ana MVC uygulaması
│   ├── Areas/Identity/             # Kayıt ve giriş sayfaları
│   ├── Controllers/                # Admin, Kitap, Tartisma, AiLibrary, Home
│   ├── Data/                       # DbContext, SeedData, Migrations
│   ├── Models/                     # Varlıklar ve ViewModel'ler
│   ├── Services/                   # LibraryToolService
│   ├── Views/                      # Razor görünümleri
│   └── wwwroot/                    # Statik dosyalar
└── KitapYonetim.AI.Service/        # Gemini Web API
    └── Controllers/                # Öneri, analiz ve sohbet uçları
```

---

<div align="center">

**Yunus Emre Söyler** tarafından geliştirilmiştir.

</div>
