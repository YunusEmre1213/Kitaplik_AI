<div align="center">

# 📚 Satır Arası

**Kitap yönetimi, kişisel kitaplık, topluluk tartışmaları ve yapay zekâ destekli okuma asistanı**

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-5C2D91)
![EF Core](https://img.shields.io/badge/EF%20Core-9.0-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Express-CC2927?logo=microsoftsqlserver&logoColor=white)
![Gemini](https://img.shields.io/badge/Google-Gemini%202.5%20Flash-4285F4?logo=google&logoColor=white)

</div>

---

## İçindekiler

- [Proje Hakkında](#proje-hakkında)
- [Özellikler](#özellikler)
- [Mimari](#mimari)
- [Teknolojiler](#teknolojiler)
- [Proje Yapısı](#proje-yapısı)
- [Kurulum](#kurulum)
- [Yapılandırma](#yapılandırma)
- [Çalıştırma](#çalıştırma)
- [Docker ile Çalıştırma](#docker-ile-çalıştırma)
- [Roller ve Yetkilendirme](#roller-ve-yetkilendirme)
- [AI Servisi API'si](#ai-servisi-apisi)
- [Veritabanı](#veritabanı)
- [Güvenlik](#güvenlik)
- [Canlıya Alma Kontrol Listesi](#canlıya-alma-kontrol-listesi)
- [Yol Haritası](#yol-haritası)

---

## Proje Hakkında

**Satır Arası**, okurların kitap keşfettiği, kendi kitaplıklarını yönettiği, kitaplara yorum ve puan verdiği ve diğer okurlarla tartıştığı bir web uygulamasıdır. Yöneticiler kitap kataloğunu ve kullanıcıları yönetir. Google Gemini tabanlı yapay zekâ servisi ise kitap önerisi, spoiler içermeyen kitap analizi ve kütüphane verisiyle konuşabilen bir sohbet asistanı sunar.

Çözüm iki projeden oluşur:

| Proje | Açıklama |
|---|---|
| `KitapYonetimSistemi` | Ana web uygulaması (ASP.NET Core MVC + Identity + EF Core) |
| `KitapYonetim.AI.Service` | Gemini ile konuşan, ana uygulamanın kullandığı yapay zekâ Web API'si |

## Özellikler

### 👤 Kullanıcı
- Kayıt olma, giriş yapma (ASP.NET Core Identity)
- Kitap kataloğunu listeleme ve kitap detayı görüntüleme
- Kitaplara **yorum** yazma ve **1–5 arası puan** verme (puan sonradan güncellenebilir)
- **Kişisel kitaplık:** kitabı kitaplığa ekleme/çıkarma, kitaplığı herkese açık ya da özel yapma
- **Okuma durumu takibi:** `Okunacak`, `Okuyorum`, `Okundu`. Başlama ve bitiş tarihleri otomatik tutulur
- **Okuma istatistikleri:** durumlara göre kitap sayıları, son okunanlar, şu an okunanlar, aylık okuma dağılımı
- Diğer kullanıcıların herkese açık kitaplıklarına göz atma
- **Tartışma forumu:** konu açma ve konulara yorum yazma

### 🤖 Yapay Zekâ
- **Kitap önerisi:** kullanıcının kitaplığına bakarak 3 yeni kitap ve gerekçesini önerir
- **Kitap analizi:** özet, tema, "neden okunmalı" maddeleri ve benzer kitaplar (spoiler vermeden)
- **Sohbet asistanı (function calling):** model, kullanıcının sorusuna göre veritabanı araçlarını çağırır:
  - `kitap_kontrol`: kitap kullanıcının kitaplığında mı, hangi okuma durumunda?
  - `yorum_getir`: bir kitabın son yorumları
- Girdi doğrulama, hız sınırlama (kullanıcı başına 5 sn) ve konu kısıtlayan sistem talimatı

### 🛠️ Yönetici Paneli
- Gösterge paneli: toplam kitap/kullanıcı/yorum/kitaplık sayıları, bu ay eklenen kitaplar, en çok yorumlanan ve en yüksek puanlı kitaplar
- Kitap ekleme, düzenleme, silme (kapak resmi yükleme: JPG, PNG, GIF)
- Yorum yönetimi: tüm yorumlar, kitaba ve kullanıcıya göre filtreleme, silme
- Kullanıcı yönetimi: rol değiştirme ve kullanıcı silme

## Mimari

```
┌──────────────────────┐        HTTPS + X-Api-Key        ┌──────────────────────────┐        ┌────────────────┐
│  KitapYonetimSistemi │ ──────────────────────────────▶ │  KitapYonetim.AI.Service │ ─────▶ │  Google Gemini │
│  (ASP.NET Core MVC)  │ ◀────────────────────────────── │       (Web API)          │ ◀───── │      API       │
└──────────┬───────────┘      öneri / analiz / chat      └──────────────────────────┘        └────────────────┘
           │ EF Core
           ▼
   ┌───────────────┐
   │  SQL Server   │
   └───────────────┘
```

**Sohbet asistanı akışı:** Tarayıcı mesajı MVC'ye gönderir → MVC mesajı AI servisine iletir → Gemini bir araç çağırmak isterse AI servisi bunu `CallDatabase` yanıtıyla MVC'ye bildirir → MVC aracı `LibraryToolService` üzerinden **mevcut kullanıcı kimliğiyle** çalıştırır ve sonucu döner. Kullanıcı kimliği hiçbir zaman modelden alınmaz, bu yüzden başkasının kitaplığı sorgulanamaz.

## Teknolojiler

| Katman | Teknoloji |
|---|---|
| Web | ASP.NET Core MVC (.NET 9), Razor, Bootstrap |
| Kimlik | ASP.NET Core Identity (rol tabanlı) |
| Veri erişimi | Entity Framework Core 9, Code First migrations |
| Veritabanı | SQL Server (Express) |
| Yapay zekâ | Google Gemini 2.5 Flash (REST, function calling) |
| API dokümantasyonu | Swagger / Swashbuckle (AI servisi, Development) |
| Konteyner | Docker, Docker Compose (isteğe bağlı) |

## Proje Yapısı

```
KitapYonetimSistemi/
├── KitapYonetimSistemi/            # Ana MVC uygulaması
│   ├── Areas/Identity/             # Kayıt / giriş sayfaları
│   ├── Controllers/                # Admin, Kitap, Tartisma, AiLibrary, Home
│   ├── Data/                       # DbContext, SeedData, Migrations
│   ├── Models/                     # Varlıklar ve ViewModel'ler
│   ├── Services/                   # LibraryToolService (AI araçları)
│   ├── Views/                      # Razor görünümleri
│   └── wwwroot/                    # Statik dosyalar, yüklenen kapaklar
├── KitapYonetim.AI.Service/        # Gemini Web API
│   └── Controllers/RecommendationController.cs
├── docker-compose.yml
└── KitapYonetimSistemi.sln
```

## Kurulum

### Gereksinimler
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server veya SQL Server Express (LocalDB de kullanılabilir)
- [Google AI Studio](https://aistudio.google.com/) üzerinden alınmış bir **Gemini API anahtarı**
- (İsteğe bağlı) Docker Desktop
- `dotnet-ef` aracı: `dotnet tool install --global dotnet-ef`

### Adımlar

```bash
# 1. Depoyu klonlayın
git clone https://github.com/YunusEmre1213/Kitaplik_AI.git
cd Kitaplik_AI

# 2. Bağımlılıkları geri yükleyin
dotnet restore

# 3. Gizli ayarları tanımlayın (aşağıdaki "Yapılandırma" bölümüne bakın)

# 4. Veritabanını oluşturun
cd KitapYonetimSistemi
dotnet ef database update
```

## Yapılandırma

Gizli değerler **repoda tutulmaz.** `appsettings.json` dosyaları git tarafından yok sayılır. Geliştirme ortamında [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) kullanılır.

**Ana uygulama (`KitapYonetimSistemi`):**

```bash
cd KitapYonetimSistemi
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS;Database=KitapYonetimSistemi;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet user-secrets set "Admin:Email" "admin@ornek.com"
dotnet user-secrets set "Admin:Password" "GucluBirSifre123!"
dotnet user-secrets set "AiService:ApiKey" "<iki-servis-arasi-gizli-anahtar>"
# İsteğe bağlı, varsayılan https://localhost:7195/
dotnet user-secrets set "AiService:BaseUrl" "https://localhost:7195/"
```

**AI servisi (`KitapYonetim.AI.Service`):**

```bash
cd KitapYonetim.AI.Service
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "<gemini-api-anahtarınız>"
dotnet user-secrets set "AiService:ApiKey" "<ana-uygulamadakiyle-aynı-anahtar>"
```

| Anahtar | Açıklama |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server bağlantı dizesi |
| `Admin:Email`, `Admin:Password` | İlk açılışta oluşturulacak yönetici hesabı. Tanımlı değilse admin oluşturulmaz |
| `AiService:ApiKey` | Ana uygulama ile AI servisi arasındaki ortak gizli anahtar (iki tarafta aynı olmalı) |
| `AiService:BaseUrl` | AI servisinin adresi |
| `Gemini:ApiKey` | Google Gemini API anahtarı |

Canlı ortamda aynı değerler ortam değişkeni olarak verilir (`:` yerine `__`), örneğin `ConnectionStrings__DefaultConnection`, `Gemini__ApiKey`.

> Güçlü bir servis anahtarı üretmek için: `openssl rand -base64 32`

## Çalıştırma

Sohbet, öneri ve analiz özellikleri için **iki proje de aynı anda** çalışmalıdır.

**Visual Studio:** Solution'a sağ tık → *Configure Startup Projects* → *Multiple startup projects* → iki projeyi de **Start** yapın ve `https` profilini seçin.

**Komut satırı (iki ayrı terminal):**

```bash
# Terminal 1 – AI servisi (https://localhost:7195)
cd KitapYonetim.AI.Service
dotnet run --launch-profile https

# Terminal 2 – Ana uygulama (https://localhost:7190)
cd KitapYonetimSistemi
dotnet run --launch-profile https
```

AI servisinin Swagger arayüzü (Development): `https://localhost:7195/swagger`

## Docker ile Çalıştırma

Depodaki `docker-compose.yml`, bir SQL Server 2022 konteyneri ve ana uygulamayı ayağa kaldırır.

```bash
docker compose up --build
```

> ⚠️ `docker-compose.yml` içindeki `MSSQL_SA_PASSWORD` örnek bir değerdir. Gerçek kullanımda değiştirin ve ortam değişkeniyle verin. Konteyner içinde gizli değerleri (`Gemini__ApiKey`, `AiService__ApiKey`, `Admin__*`, bağlantı dizesi) ortam değişkeni olarak aktarın.

## Roller ve Yetkilendirme

| Rol | Erişim |
|---|---|
| **Ziyaretçi** | Ana sayfa, tartışma listesi ve detayları |
| **User** | Katalog, yorum/puan, kitaplık, okuma istatistikleri, AI asistanı, tartışma açma/yorum yazma |
| **Admin** | Yönetici paneli: kitap, yorum ve kullanıcı yönetimi |

Kayıt olan her kullanıcıya otomatik olarak `User` rolü verilir. `Admin` rolü yalnızca yapılandırmadaki hesaba tanımlanır ya da başka bir yönetici tarafından atanır. Yöneticiler kendi rollerini değiştiremez ve kendi hesaplarını silemez.

## AI Servisi API'si

Tüm uçlar `X-Api-Key` başlığı gerektirir (Development'ta anahtar tanımlı değilse zorunlu değildir).

| Yöntem | Uç | Açıklama |
|---|---|---|
| `GET` | `/api/ai/test` | Servis sağlık kontrolü |
| `POST` | `/api/ai/ask` | Gövde: kitap listesi (`["Kitap - Yazar", ...]`). 3 kitap önerisi döner |
| `POST` | `/api/ai/analyze` | Gövde: `{ "Title": "...", "Author": "..." }`. Spoilersız analiz döner |
| `POST` | `/api/ai/agent-chat` | Gövde: `{ "Message": "..." }`. Yanıt `Reply` ya da `CallDatabase` aksiyonu |

## Veritabanı

Code First yaklaşımı kullanılır. Ana varlıklar:

| Varlık | Açıklama |
|---|---|
| `Kitap` | Ad, yazar, yayın yılı, açıklama, kapak resmi, ortalama puan |
| `Yorum` | Kitaba yapılan kullanıcı yorumu |
| `KitapPuan` | Kullanıcının kitaba verdiği puan (1–5) |
| `KullaniciKitaplik` | Kullanıcı–kitap ilişkisi: görünürlük, okuma durumu, başlama/bitiş tarihi |
| `Tartisma` / `Comment` | Forum konusu ve konu yorumları |
| `ApplicationUser` | Identity kullanıcısı |

Yeni bir migration eklemek için:

```bash
cd KitapYonetimSistemi
dotnet ef migrations add MigrationAdi
dotnet ef database update
```

## Güvenlik

- Tüm durum değiştiren işlemlerde **CSRF koruması** (`[ValidateAntiForgeryToken]`)
- Rol tabanlı yetkilendirme (`[Authorize(Roles = ...)]`)
- AI aracı çağrıları oturumdaki kullanıcı kimliğiyle sınırlıdır (IDOR koruması)
- AI servisine yalnızca ortak gizli anahtarı bilen istemci erişebilir
- Gemini anahtarı URL yerine istek başlığında gönderilir, böylece günlüklere düşmez
- Yüklenen kapak resimleri için uzantı beyaz listesi ve rastgele dosya adı
- Açık yönlendirme (open redirect) engeli
- Gizli bilgiler repoda tutulmaz (User Secrets / ortam değişkenleri)
- Sertifika doğrulaması yalnızca Development ortamında gevşetilir

## Canlıya Alma Kontrol Listesi

- [ ] Gerçek bir SQL Server bağlantı dizesi tanımlandı
- [ ] Güçlü bir `Admin:Password` belirlendi
- [ ] Gemini anahtarı yenilendi ve ortam değişkeniyle verildi
- [ ] `AiService:ApiKey` iki serviste de aynı ve rastgele üretilmiş
- [ ] AI servisi herkese açık ağdan erişilemeyecek şekilde yapılandırıldı
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] HTTPS ve geçerli sertifika kullanılıyor
- [ ] Veritabanı yedekleme planı hazır

## Yol Haritası

- [ ] Kayıt tarihinin kullanıcı modelinde tutulması
- [ ] Kullanıcı silinirken puan ve tartışmaların da temizlenmesi
- [ ] Sayfalama ve arama/filtreleme
- [ ] Birim ve entegrasyon testleri
- [ ] CI/CD (GitHub Actions)

## Katkıda Bulunma

Hata bildirimleri ve öneriler için bir issue açabilirsiniz. Değişiklik önermek için depoyu fork'layıp ayrı bir dalda çalışın ve pull request gönderin.

---

<div align="center">

**Yunus Emre Söyler** tarafından geliştirilmiştir.

</div>
