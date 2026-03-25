# MicroserviceSolution

.NET 9 tabanlı mikroservis mimarisi — Onion Architecture + CQRS + JWT + Redis + Event-Driven

## Mimari Genel Bakış

```
Client → Nginx (80) → YARP Gateway (5000)
                            ├── AuthService (5001)   — JWT + Microsoft Identity
                            ├── ProductService (5002) — CQRS + Redis Cache + Events
                            └── LogService (5003)    — Serilog + Seq + ELK
```

Her servis **Onion Architecture** ile katmanlıdır:
- **Domain** — Dış bağımlılık yok; entity, event, exception
- **Application** — CQRS handler'ları, arayüzler, DTO'lar
- **Infrastructure** — EF Core, Redis, RabbitMQ implementasyonları
- **API** — Controller'lar, Program.cs

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
# .env dosyasını düzenleyin (özellikle JWT_SECRET'i değiştirin)

# 3. Tüm servisleri başlatın
cd docker
docker-compose up -d

# 4. Servislerin sağlığını kontrol edin
docker-compose ps
```

### Servis URL'leri

| Servis | URL | Açıklama |
|--------|-----|----------|
| Nginx (Entry Point) | http://localhost:80 | Ana giriş noktası |
| API Gateway | http://localhost:5000 | YARP Gateway |
| Auth Service | http://localhost:5001/swagger | Swagger UI |
| Product Service | http://localhost:5002/swagger | Swagger UI |
| Log Service | http://localhost:5003/swagger | Swagger UI |
| RabbitMQ UI | http://localhost:15672 | guest/guest |
| Seq (Log Viewer) | http://localhost:5342 | Structured log viewer |

## Lokal Geliştirme (.NET SDK)

```bash
# Solution'ı restore et ve derle
dotnet restore
dotnet build

# Testleri çalıştır
dotnet test

# Belirli bir servisi çalıştır (önce Docker'da altyapıyı başlat)
cd docker && docker-compose up -d postgres redis rabbitmq seq && cd ..
dotnet run --project src/AuthService/AuthService.API
```

## API Endpoint'leri

### Auth Service (`/api/auth`)
| Method | Endpoint | Auth | Açıklama |
|--------|----------|------|----------|
| POST | `/register` | — | Yeni kullanıcı kaydı |
| POST | `/login` | — | JWT + Refresh token döner |
| POST | `/refresh` | — | Access token yeniler |

### Product Service (`/api/products`)
| Method | Endpoint | Auth | Açıklama |
|--------|----------|------|----------|
| GET | `/` | — | Ürünleri listele (Redis cache) |
| GET | `/{id}` | — | Ürün detayı |
| POST | `/` | — | Ürün ekle (event fire) |
| PUT | `/{id}` | JWT | Ürün güncelle (cache invalidate) |

### Log Service (`/api/logs`)
| Method | Endpoint | Auth | Açıklama |
|--------|----------|------|----------|
| POST | `/` | — | Log kaydı yaz |

## Ortam Değişkenleri

| Değişken | Açıklama | Varsayılan |
|----------|----------|------------|
| `POSTGRES_USER` | PostgreSQL kullanıcı adı | `msuser` |
| `POSTGRES_PASSWORD` | PostgreSQL şifresi | `mspassword` |
| `JWT_SECRET` | JWT imzalama anahtarı | *(zorunlu)* |
| `JWT_ISSUER` | JWT issuer | `MicroserviceAuth` |
| `JWT_AUDIENCE` | JWT audience | `MicroserviceClients` |
| `RABBITMQ_USER` | RabbitMQ kullanıcı | `guest` |
| `RABBITMQ_PASSWORD` | RabbitMQ şifresi | `guest` |

## Branch Stratejisi

```
test/v1.0.0   ← geliştirme ve test branch'i
     ↓ (merge after review)
prod/v1.0.0   ← production branch'i
```

**Commit kuralları:** Her gün düzenli commit atılır. Format: `type(scope): message`

Örnekler:
- `feat(auth): add refresh token rotation`
- `fix(product): resolve cache invalidation bug`
- `chore(docker): update compose healthchecks`

## Teknoloji Yığını

| Katman | Teknoloji |
|--------|-----------|
| Framework | .NET 9 / ASP.NET Core |
| ORM | Entity Framework Core 9 + PostgreSQL |
| Cache | StackExchange.Redis |
| Message Broker | RabbitMQ (+ Kafka opsiyonel) |
| CQRS | MediatR 12 |
| Validation | FluentValidation 11 |
| Logging | Serilog + Seq + Elasticsearch |
| Gateway | YARP 2.3 |
| Auth | Microsoft Identity + JWT Bearer |
| Testing | xUnit + Moq + FluentAssertions |
| Container | Docker + Docker Compose |
| CI/CD | GitHub Actions |

## 12-Factor Uyumluluk

| Faktör | Uygulama |
|--------|----------|
| Codebase | Tek repo, tüm servisler `src/` altında |
| Dependencies | NuGet ile bağımlılık yönetimi |
| Config | Ortam değişkenleri (`.env`) |
| Backing Services | PostgreSQL, Redis, RabbitMQ bağımsız container |
| Build/Release/Run | Docker multi-stage build, CI/CD ayrımı |
| Processes | Stateless servisler, Redis ile session yönetimi |
| Port Binding | Her servis kendi portunda çalışır |
| Concurrency | Yatay ölçekleme destekli |
| Disposability | Graceful shutdown, using/IDisposable |
| Dev/Prod Parity | docker-compose.override.yml ile dev/prod farkı minimize |
| Logs | Serilog stdout + Seq merkezi toplama |
| Admin Processes | EF Migrations ayrı komutla çalışır |
