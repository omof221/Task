# MicroserviceSolution

.NET 9 tabanlı mikroservis mimarisi — Onion Architecture + CQRS + JWT + Redis + Event-Driven

## Mimari Genel Bakış

```
Client → Nginx (80) → YARP Gateway (5000)
                            ├── AuthService    (5001) — JWT + Microsoft Identity
                            ├── ProductService (5002) — CQRS + Redis Cache + Events
                            └── LogService     (5003) — Serilog + Seq + ELK
                                    ↑
                           RabbitMQ Event Consumer
                           (ProductAddedEvent, ProductUpdatedEvent)
```

Her servis **Onion Architecture** ile katmanlıdır:

| Katman | Sorumluluk |
|--------|------------|
| **Domain** | Entity, event, exception — sıfır dış bağımlılık |
| **Application** | CQRS handler'ları, arayüzler, DTO'lar — MediatR |
| **Infrastructure** | EF Core, Redis, RabbitMQ implementasyonları |
| **API** | Controller'lar, Program.cs, Swagger |

---

## Hızlı Başlangıç

### Gereksinimler

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) 24+
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (lokal geliştirme için)

### Docker ile Çalıştırma

```bash
# 1. Repoyu klonlayın
git clone <repo-url>
cd MicroserviceSolution

# 2. Ortam değişkenlerini ayarlayın
cp .env.example .env
# .env dosyasını açın ve JWT_SECRET_KEY değerini değiştirin (min 32 karakter)

# 3. Tüm servisleri başlatın
cd docker
docker-compose up -d

# 4. Servislerin sağlığını kontrol edin
docker-compose ps
curl http://localhost:5000/health   # Gateway
curl http://localhost:5001/health   # AuthService
curl http://localhost:5002/health   # ProductService
curl http://localhost:5003/health   # LogService
```

### Servis URL'leri

| Servis | URL | Açıklama |
|--------|-----|----------|
| Nginx (Entry Point) | http://localhost:80 | Ana giriş noktası |
| API Gateway | http://localhost:5000 | YARP Reverse Proxy |
| Auth Service | http://localhost:5001/swagger | Swagger UI |
| Product Service | http://localhost:5002/swagger | Swagger UI |
| Log Service | http://localhost:5003/swagger | Swagger UI |
| RabbitMQ Yönetim | http://localhost:15672 | guest/guest |
| Seq (Log Viewer) | http://localhost:5342 | Yapılandırılmış log |
| Elasticsearch | http://localhost:9200 | ERROR/CRITICAL loglar |

---

## Lokal Geliştirme (.NET SDK)

```bash
# Bağımlılıkları yükle ve derle
dotnet restore
dotnet build

# Tüm testleri çalıştır (≥45 test, 0 hata)
dotnet test

# Altyapıyı Docker'da başlat, servisleri lokalde çalıştır
cd docker && docker-compose up -d postgres redis rabbitmq seq && cd ..

dotnet run --project src/AuthService/AuthService.API
dotnet run --project src/ProductService/ProductService.API
dotnet run --project src/LogService/LogService.API
dotnet run --project src/Gateway
```

---

## API Endpoint'leri

### Auth Service (`/api/auth`)

| Method | Endpoint | Auth | Açıklama |
|--------|----------|------|----------|
| POST | `/register` | — | Yeni kullanıcı kaydı → `TokenResponse` |
| POST | `/login` | — | JWT + Refresh token döner |
| POST | `/refresh` | — | Refresh token ile yeni access token |
| GET | `/me` | JWT | Giriş yapmış kullanıcı bilgisi |

### Product Service (`/api/products`)

| Method | Endpoint | Auth | Açıklama |
|--------|----------|------|----------|
| GET | `/` | — | Ürünleri listele (Redis cache-aside) |
| GET | `/{id}` | — | Ürün detayı (Redis cache-aside) |
| POST | `/` | — | Ürün ekle → `ProductAddedEvent` → cache invalidate |
| PUT | `/{id}` | JWT | Ürün güncelle → `ProductUpdatedEvent` → cache invalidate |

### Log Service (`/api/logs`)

| Method | Endpoint | Auth | Açıklama |
|--------|----------|------|----------|
| POST | `/` | — | Log kaydı yaz (INFO/WARNING→Seq, ERROR/CRITICAL→ELK) |

---

## End-to-End Akış

```
1. POST /api/auth/register       → Kullanıcı oluştur
2. POST /api/auth/login          → JWT + RefreshToken al
3. POST /api/products            → Ürün ekle
                                    → ProductAddedEvent → RabbitMQ
                                    → LogService consumer → WriteLogCommand → Seq log
4. GET  /api/products            → Ürün listesi (ilk istek: DB, sonraki: Redis)
5. PUT  /api/products/{id}       → Ürün güncelle (JWT gerekli)
                                    → Cache invalidate → ProductUpdatedEvent
6. POST /api/logs                → Manuel log kaydı (ERROR → ELK'e gider)
```

---

## Ortam Değişkenleri

| Değişken | Açıklama | Varsayılan |
|----------|----------|------------|
| `JWT_SECRET_KEY` | JWT HMAC-SHA256 imzalama anahtarı (min 32 kar.) | *(zorunlu)* |
| `JWT_ISSUER` | JWT issuer | `AuthService` |
| `JWT_AUDIENCE` | JWT audience | `MicroserviceSolution` |
| `JWT_EXPIRY_MINUTES` | Access token ömrü (dakika) | `60` |
| `POSTGRES_USER` | PostgreSQL kullanıcı adı | `msuser` |
| `POSTGRES_PASSWORD` | PostgreSQL şifresi | `mspassword` |
| `POSTGRES_DB` | Veritabanı adı | `microservicedb` |
| `REDIS_CONNECTION` | Redis bağlantı dizesi | `localhost:6379` |
| `RABBITMQ_HOST` | RabbitMQ sunucu adresi | `localhost` |
| `RABBITMQ_USER` | RabbitMQ kullanıcı | `guest` |
| `RABBITMQ_PASS` | RabbitMQ şifresi | `guest` |
| `SEQ_URL` | Seq log toplayıcı URL | `http://localhost:5341` |
| `ELASTIC_URL` | Elasticsearch URL | `http://localhost:9200` |
| `AUTH_SERVICE_URL` | Gateway → AuthService hedef URL | `http://localhost:5001` |
| `PRODUCT_SERVICE_URL` | Gateway → ProductService hedef URL | `http://localhost:5002` |
| `LOG_SERVICE_URL` | Gateway → LogService hedef URL | `http://localhost:5003` |

---

## Test Stratejisi

| Katman | Araç | Kapsam |
|--------|------|--------|
| Unit | xUnit + Moq + FluentAssertions | Handler'lar, TokenService, Validator'lar |
| Integration | WebApplicationFactory + InMemory DB | Controller E2E (HTTP seviyesi) |

```bash
# Sadece unit testler
dotnet test --filter "FullyQualifiedName~Unit"

# Sadece integration testler
dotnet test --filter "FullyQualifiedName~Integration"

# Tümü + coverage raporu
dotnet test --collect:"XPlat Code Coverage"
```

---

## Rate Limiting (Gateway)

| Policy | Limit | Pencere | Hedef |
|--------|-------|---------|-------|
| `auth` | 10 istek | 1 dakika | `/api/auth/*` — brute-force koruması |
| `fixed` | 100 istek | 1 dakika | `/api/products/*`, `/api/logs/*` |

429 Too Many Requests döndüğünde:
```json
{ "status": 429, "title": "Too Many Requests" }
```

---

## Branch Stratejisi

```
test/v1.0.0   ← geliştirme + test branch'i (aktif)
     ↓ (code review + merge)
prod/v1.0.0   ← production branch'i
```

**Commit formatı:** `type(scope): message`

```
feat(auth):    add refresh token rotation
feat(product): add Redis cache-aside pattern
feat(log):     add RabbitMQ event consumer
fix(product):  resolve InMemory shared root issue
chore(docker): update compose healthchecks
```

---

## Teknoloji Yığını

| Katman | Teknoloji |
|--------|-----------|
| Framework | .NET 9 / ASP.NET Core |
| ORM | Entity Framework Core 9 + PostgreSQL (Npgsql) |
| Cache | StackExchange.Redis (Cache-Aside Pattern) |
| Message Broker | RabbitMQ 7.x (+ Kafka opsiyonel) |
| CQRS | MediatR 12 + Pipeline Behaviors |
| Validation | FluentValidation 11 |
| Logging | Serilog + Seq (INFO/WARN) + Elasticsearch (ERROR/CRITICAL) |
| Gateway | YARP 2.3 + Rate Limiting |
| Auth | Microsoft Identity + JWT Bearer (HMAC-SHA256) |
| Testing | xUnit + Moq + FluentAssertions + WebApplicationFactory |
| Container | Docker (multi-stage build) + Docker Compose |
| CI/CD | GitHub Actions (build + test + Docker push) |

---

## 12-Factor Uyumluluk

| Faktör | Uygulama |
|--------|----------|
| **Codebase** | Tek repo, tüm servisler `src/` altında |
| **Dependencies** | NuGet, açık versiyonlar `.csproj`'da |
| **Config** | Tüm sırlar ortam değişkenlerinden okunur |
| **Backing Services** | PostgreSQL, Redis, RabbitMQ ayrı container |
| **Build/Release/Run** | Docker multi-stage, GitHub Actions CI/CD |
| **Processes** | Stateless servisler; session Redis'te |
| **Port Binding** | Her servis kendi portunda |
| **Concurrency** | Yatay ölçekleme destekli (stateless) |
| **Disposability** | Graceful shutdown, `IAsyncDisposable` |
| **Dev/Prod Parity** | `docker-compose.override.yml` farkı minimize eder |
| **Logs** | Serilog stdout → Seq + ELK merkezi toplama |
| **Admin Processes** | EF Migrations ayrı `dotnet ef database update` |

---

## SOLID Prensipleri

| Prensip | Uygulama |
|---------|----------|
| **SRP** | Her handler tek sorumluluğa sahip |
| **OCP** | Yeni event/handler eklemek mevcut kodu değiştirmez |
| **LSP** | `BaseEntity` kalıtım hiyerarşisi tutarlı |
| **ISP** | `IProductRepository`, `ICacheService`, `IEventPublisher` ayrı |
| **DIP** | Application katmanı Infrastructure'a değil arayüzlere bağımlı |
