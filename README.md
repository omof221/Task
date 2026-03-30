# MicroserviceSolution

> **Kod Deposu:** https://github.com/omof221/Task

.NET 9 tabanlı mikroservis mimarisi — Onion Architecture + CQRS + JWT + Redis + RabbitMQ + Event-Driven

---

## Mimari Genel Bakış

```
Client → Nginx (80) → YARP Gateway (5166/5000)
                            ├── AuthService    (5213) — JWT + Microsoft Identity + Policy-Based Auth
                            ├── ProductService (5112) — CQRS + Redis Cache + RabbitMQ Events
                            └── LogService     (5051) — Serilog + Seq + ELK
                                    ↑
                           RabbitMQ Consumer
                           (ProductAddedEvent, ProductUpdatedEvent)
```

Her servis **Onion Architecture** ile 4 katmana ayrılmıştır:

| Katman | Sorumluluk |
|--------|------------|
| **Domain** | Entity, event, exception — sıfır dış bağımlılık |
| **Application** | CQRS handler'ları, arayüzler, DTO'lar — MediatR |
| **Infrastructure** | EF Core, Redis, RabbitMQ implementasyonları |
| **API** | Controller'lar, Program.cs, Swagger |

---

## Gereksinimler

| Araç | Versiyon | Zorunlu |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/9.0) | 9.0+ | ✅ |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | 24+ | ✅ |
| SQL Server Express | 2019+ | ✅ (lokal geliştirme) |
| Git | herhangi | ✅ |

---

## Kurulum

### 1. Repoyu Klonla

```powershell
git clone https://github.com/omof221/Task.git
cd Task
git checkout test/v1.0.0
```

### 2. Altyapı Servislerini Başlat (Docker)

```powershell
cd docker
docker compose up -d redis rabbitmq seq
cd ..
```

Kontrol:
```powershell
docker ps
# ms_redis, ms_rabbitmq, ms_seq → Up olmalı
```

### 3. Veritabanı

Proje **SQL Server Express** kullanır. Bağlantı adresi: `.\SQLEXPRESS`

- `AuthDb` ve `ProductDb` veritabanları uygulama ilk çalıştığında **otomatik oluşturulur** (EF Core Migration).
- SQL Server Express kurulu ve çalışır durumda olmalı.

### 4. Ortam Değişkenlerini Ayarla

Her terminal oturumunda servisi başlatmadan önce çalıştır:

```powershell
$env:JWT_SECRET_KEY     = "SuperSecretKey_MicroserviceSolution_2026!!"
$env:REDIS_CONNECTION   = "localhost:6379"
$env:RABBITMQ_HOST      = "localhost"
$env:MESSAGING_PROVIDER = "RabbitMQ"
```

> **Not:** `JWT_SECRET_KEY` minimum 32 karakter olmalıdır. Tüm servisler aynı değeri kullanır.

---

## Çalıştırma (Lokal Geliştirme)

**4 ayrı terminal** aç:

**Terminal 1 — AuthService**
```powershell
$env:JWT_SECRET_KEY = "SuperSecretKey_MicroserviceSolution_2026!!"
dotnet run --project src/AuthService/AuthService.API
# → http://localhost:5213
```

**Terminal 2 — ProductService**
```powershell
$env:JWT_SECRET_KEY     = "SuperSecretKey_MicroserviceSolution_2026!!"
$env:REDIS_CONNECTION   = "localhost:6379"
$env:RABBITMQ_HOST      = "localhost"
$env:MESSAGING_PROVIDER = "RabbitMQ"
dotnet run --project src/ProductService/ProductService.API
# → http://localhost:5112
```

**Terminal 3 — LogService**
```powershell
dotnet run --project src/LogService/LogService.API
# → http://localhost:5051
```

**Terminal 4 — Gateway**
```powershell
$env:JWT_SECRET_KEY = "SuperSecretKey_MicroserviceSolution_2026!!"
dotnet run --project src/Gateway
# → http://localhost:5166
```

---

## Docker ile Tam Dağıtım

```powershell
cd docker

# .env dosyasını oluştur
copy .env.example .env
# .env içindeki SA_PASSWORD ve JWT_SECRET değerlerini düzenle

# Tüm servisleri derle ve başlat
docker compose up -d --build

# Durum kontrolü
docker compose ps
```

---

## Servis URL'leri

### Lokal Geliştirme

| Servis | URL | Swagger |
|--------|-----|---------|
| AuthService | http://localhost:5213 | http://localhost:5213/swagger |
| ProductService | http://localhost:5112 | http://localhost:5112/swagger |
| LogService | http://localhost:5051 | http://localhost:5051/swagger |
| Gateway | http://localhost:5166 | — |
| Seq Log Viewer | http://localhost:5342 | — |
| RabbitMQ Yönetim | http://localhost:15672 | guest / guest |

### Docker Ortamı

| Servis | URL |
|--------|-----|
| Nginx (Ana Giriş) | http://localhost:80 |
| Gateway | http://localhost:5000 |
| AuthService | http://localhost:5001/swagger |
| ProductService | http://localhost:5002/swagger |
| LogService | http://localhost:5003/swagger |
| Seq Log Viewer | http://localhost:5342 |
| RabbitMQ Yönetim | http://localhost:15672 |

---

## API Endpoint'leri

### AuthService — `/api/auth`

| Method | Endpoint | Yetki | Açıklama |
|--------|----------|-------|----------|
| `POST` | `/register` | — | Kullanıcı kaydı. `role`: `"User"` veya `"Admin"` |
| `POST` | `/login` | — | JWT access token + refresh token döner |
| `POST` | `/refresh` | — | Refresh token ile yeni token çifti üretir |
| `GET` | `/me` | JWT | Giriş yapmış kullanıcının bilgisi |
| `GET` | `/admin/users` | `AdminOnly` Policy | Tüm kullanıcıları listeler (Custom Handler) |

### ProductService — `/api/products`

| Method | Endpoint | Yetki | Açıklama |
|--------|----------|-------|----------|
| `GET` | `/` | — | Ürün listesi — Redis cache-aside |
| `GET` | `/{id}` | — | Tekil ürün — Redis cache-aside |
| `POST` | `/` | — | Ürün ekle → `ProductAddedEvent` → RabbitMQ |
| `PUT` | `/{id}` | `UserOrAdmin` Policy | Ürün güncelle → cache invalidate |

### LogService — `/api/logs`

| Method | Endpoint | Yetki | Açıklama |
|--------|----------|-------|----------|
| `POST` | `/` | — | Log kaydı yaz (`0`=INFO, `1`=WARNING, `2`=ERROR, `3`=CRITICAL) |

---

## Authorization Yapısı

Proje iki katmanlı yetkilendirme kullanır:

| Policy | İzin Verilen Roller | Kullanıldığı Yer |
|--------|---------------------|-----------------|
| `AdminOnly` | `Admin` | `GET /api/auth/admin/users` |
| `UserOrAdmin` | `User`, `Admin` | `PUT /api/products/{id}` |

**Custom `HasRoleHandler`** — her yetkilendirme kararı (başarılı/red) structured log olarak kaydedilir.

---

## End-to-End Akış

```
1. POST /api/auth/register    → Kullanıcı oluştur (User veya Admin rolü)
2. POST /api/auth/login       → JWT + RefreshToken al
3. POST /api/products         → Ürün ekle
                                  └→ ProductAddedEvent → RabbitMQ
                                       └→ LogService tüketir → Seq'e yazar
4. GET  /api/products         → Liste (1. istek: DB, sonrakiler: Redis ~0ms)
5. PUT  /api/products/{id}    → Güncelle (UserOrAdmin policy, JWT gerekli)
                                  └→ Cache invalidate → ProductUpdatedEvent
6. GET  /api/auth/admin/users → Tüm kullanıcılar (AdminOnly policy)
7. POST /api/logs             → Manuel log (ERROR/CRITICAL → ELK)
```

---

## Testleri Çalıştırma

```powershell
# Tüm testler (46 test)
dotnet test

# Sadece unit testler
dotnet test --filter "FullyQualifiedName~Unit"

# Sadece integration testler
dotnet test --filter "FullyQualifiedName~Integration"

# Coverage raporu
dotnet test --collect:"XPlat Code Coverage"
```

**Test dağılımı:**

| Proje | Unit | Integration | Toplam |
|-------|------|-------------|--------|
| AuthService.Tests | 9 | 8 | 17 |
| ProductService.Tests | 10 | 10 | 20 |
| LogService.Tests | 9 | — | 9 |
| **Toplam** | **28** | **18** | **46** |

---

## Ortam Değişkenleri

| Değişken | Açıklama | Varsayılan |
|----------|----------|------------|
| `JWT_SECRET_KEY` | JWT imzalama anahtarı (min 32 karakter) | **zorunlu** |
| `REDIS_CONNECTION` | Redis bağlantı dizesi | `localhost:6379` |
| `RABBITMQ_HOST` | RabbitMQ host | `localhost` |
| `RABBITMQ_USER` | RabbitMQ kullanıcı | `guest` |
| `RABBITMQ_PASS` | RabbitMQ şifre | `guest` |
| `MESSAGING_PROVIDER` | `RabbitMQ` veya `Kafka` | `RabbitMQ` |
| `SEQ_URL` | Seq ingest URL | `http://localhost:5341` |
| `ELASTIC_URL` | Elasticsearch URL | `http://localhost:9200` |
| `SA_PASSWORD` | SQL Server SA şifresi (Docker) | `YourStrong!Passw0rd` |

---

## Rate Limiting (Gateway)

| Policy | Limit | Pencere | Endpoint |
|--------|-------|---------|----------|
| `auth` | 10 istek | 1 dakika | `/api/auth/*` |
| `fixed` | 100 istek | 1 dakika | `/api/products/*`, `/api/logs/*` |

Limit aşıldığında: `429 Too Many Requests`

---

## Branch Stratejisi

```
test/v1.0.0   ← aktif geliştirme + test
     ↓ (code review)
prod/v1.0.0   ← production
```

**Commit formatı:** `type(scope): message`

---

## Teknoloji Yığını

| Kategori | Teknoloji |
|----------|-----------|
| Framework | .NET 9 / ASP.NET Core |
| Veritabanı | SQL Server Express + EF Core 9 |
| Cache | Redis 7 — StackExchange.Redis (Cache-Aside) |
| Message Broker | RabbitMQ 3.13 (+ Kafka opsiyonel) |
| CQRS | MediatR 12 + Pipeline Behaviors |
| Validation | FluentValidation 11 |
| Logging | Serilog + Seq + Elasticsearch |
| Gateway | YARP 2.3 + Rate Limiting |
| Auth | Microsoft Identity + JWT Bearer + Custom Policy Handler |
| Testing | xUnit + Moq + FluentAssertions + WebApplicationFactory |
| Container | Docker multi-stage + Docker Compose |
| CI/CD | GitHub Actions |

---

## Design Pattern'lar

| Pattern | Kullanıldığı Yer |
|---------|-----------------|
| Onion Architecture | Tüm servisler |
| CQRS | ProductService, AuthService, LogService |
| Mediator | MediatR — tüm handler'lar |
| Pipeline Behavior | Logging + Validation (otomatik) |
| Repository | Tüm servisler |
| Cache-Aside | ProductService — Redis |
| Factory Method | `Product.Create()`, `LogEntry.Create()` |
| Null Object | `NullCacheService`, `NullEventPublisher` (test) |
| Strategy | `IEventPublisher` → RabbitMQ / Kafka / Null |
| Observer | `LogEventConsumer` — RabbitMQ subscriber |
| Token Rotation | AuthService — Refresh token güvenliği |
| API Gateway | YARP |

---

## 12-Factor Uyumluluk

| Faktör | Uygulama |
|--------|----------|
| Codebase | Tek repo, `src/` altında tüm servisler |
| Dependencies | NuGet, versiyonlar `.csproj`'da sabit |
| Config | Tüm sırlar ortam değişkenlerinden okunur |
| Backing Services | SQL Server, Redis, RabbitMQ bağımsız |
| Build/Release/Run | Docker multi-stage, GitHub Actions |
| Processes | Stateless servisler |
| Port Binding | Her servis kendi portunda |
| Concurrency | Yatay ölçekleme destekli |
| Disposability | `IAsyncDisposable`, graceful shutdown |
| Dev/Prod Parity | `docker-compose.override.yml` |
| Logs | Serilog stdout → Seq + ELK |
| Admin Processes | `dotnet ef database update` |

---

## SOLID Prensipleri

| Prensip | Uygulama |
|---------|----------|
| **SRP** | Her handler, her repository tek sorumlu |
| **OCP** | Yeni event/handler mevcut kodu değiştirmez |
| **LSP** | `BaseEntity` kalıtım hiyerarşisi tutarlı |
| **ISP** | `IProductRepository`, `ICacheService`, `IEventPublisher` ayrı arayüzler |
| **DIP** | Application katmanı Infrastructure'a değil arayüzlere bağımlı |
