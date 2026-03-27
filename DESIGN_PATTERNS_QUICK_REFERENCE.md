# Design Patterns - Quick Reference Guide

## 📚 Desenleri Hızlı Bulma Rehberi

### CQRS Pattern 🔄
**Nedir?** Command (yazma) ve Query (okuma) operasyonlarını ayrı tutma  
**Nerede?** `ProductService.Application`, `AuthService.Application`  
**Dosyalar:**
- Commands: `Commands/AddProductCommand.cs`, `Commands/LoginCommand.cs`
- Queries: `Queries/GetProductByIdQuery.cs`, `Queries/GetProductsQuery.cs`
- Handlers: `Commands/*Handler.cs`, `Queries/*Handler.cs`

**Faydası:** Yazma ve okuma için farklı optimizasyonlar, daha iyi performans

```csharp
// ❌ Eski yol
public Product GetAndUpdate(Guid id, UpdateDto dto) { /* ... */ }

// ✅ CQRS yolu
public Command AddProductCommand { /* yazma */ }
public Query GetProductByIdQuery { /* okuma */ }
```

---

### Repository Pattern 📦
**Nedir?** Veri erişimini abstraktlaştırma  
**Nerede?** `Infrastructure/Repositories`  
**Dosyalar:**
- Interface: `Application/Interfaces/IProductRepository.cs`
- Implementation: `Infrastructure/Repositories/ProductRepository.cs`

**Faydası:** DB değişimi kolay, test edilebilir, loose coupling

```csharp
// ❌ Eski yol - DbContext doğrudan kullanım
var product = _dbContext.Products.FirstOrDefault(p => p.Id == id);

// ✅ Repository yolu - Interface aracılığı
var product = await _repository.GetByIdAsync(id);
```

---

### Dependency Injection (DI) 💉
**Nedir?** Bağımlılıkları harici olarak sağlama  
**Nerede?** `Program.cs`  
**Dosyalar:** `Program.cs`, `Application/Extensions/ApplicationServiceExtensions.cs`

**Faydası:** Loose coupling, test edilebilir, framework bağımsız

```csharp
// ❌ Eski yol - Tight coupling
public class Handler {
  private readonly RedisCacheService _cache = new();
}

// ✅ DI yolu - Loose coupling
public class Handler {
    private readonly ICacheService _cache;
    public Handler(ICacheService cache) => _cache = cache;
}
```

**DI Kayıt:**
```csharp
// Transient - Her request yeni instance
services.AddTransient<IService, Service>();

// Scoped - Request başına bir instance  
services.AddScoped<IRepository, Repository>();

// Singleton - Uygulama yaşamı boyunca bir instance
services.AddSingleton<IConfiguration>();
```

---

### MediatR + CQRS 📨
**Nedir?** Request-response pattern'ı merkezileştirme  
**Nerede?** `Application` katmanında  
**Dosyalar:** `Commands/*Handler.cs`, `Queries/*Handler.cs`

**Faydası:** Pipeline behaviors, loose coupling, clean code

```csharp
// Kullanım
var result = await _mediator.Send(new AddProductCommand(...));

// MediatR bu komut için handler'ı bulur ve execute eder
// Handler otomatik bağımlılıkları alır
```

---

### Pipeline Behavior Pattern 🔗
**Nedir?** MediatR request'lerini middleware gibi işleme  
**Nerede?** `Application/Behaviors`  
**Dosyalar:**
- `LoggingBehavior.cs` - Request/response logging
- `ValidationBehavior.cs` - FluentValidation

**Faydası:** Cross-cutting concerns (logging, validation) merkezde

```csharp
// Sıra: Logging → Validation → Handler
// ┌─────────────────────┐
// │ LoggingBehavior     │
// │  ↓ │
// │ ValidationBehavior  │
// │  ↓      │
// │ Handler         │
// │  ↓         │
// │ ValidationBehavior  │
// │  ↓          │
// │ LoggingBehavior     │
// └─────────────────────┘
```

---

### Strategy Pattern 🎯
**Nedir?** Runtime'da algoritma seçimi  
**Nerede?** `Infrastructure`  
**Dosyalar:**
- Cache: `ICacheService` → `RedisCacheService`, `NullCacheService`
- Event: `IEventPublisher` → `RabbitMqEventPublisher`, `KafkaEventPublisher`, `NullEventPublisher`
- Database: SqlServer, InMemory, LocalDB

**Faydası:** Prod/Test ortamlarında farklı implementasyon, code değişmez

```csharp
// Production
services.AddScoped<ICacheService, RedisCacheService>();

// Testing
services.AddScoped<ICacheService, NullCacheService>();

// Handler aynı kalır - DI otomatik seçer ✓
```

---

### Factory Pattern 🏭
**Nedir?** Nesne oluşturmayı merkezileştirme  
**Nerede?** `Domain/Entities`, `Infrastructure/Persistence`  
**Dosyalar:**
- `Product.Create(...)` - Static factory
- `AuthDbContextFactory.cs` - DbContext factory

**Faydası:** Business rules merkezi, validation otomatik

```csharp
// ❌ Eski yol
var product = new Product { 
    Name = name, 
    Price = price 
};
// Validation yok!

// ✅ Factory yolu
var product = Product.Create(name, description, price, stock);
// Validation otomatik enforce edilir
```

---

### Domain-Driven Design (DDD) 🎯
**Nedir?** İş mantığı domain katmanında tutma  
**Nerede?** `Domain` katmanı  
**Dosyalar:**
- `Entities/Product.cs` - Domain entity
- `Events/ProductAddedEvent.cs` - Domain events
- `Exceptions/ProductNotFoundException.cs` - Domain exceptions

**Faydası:** Business rules bir yerde, clean separation

```csharp
// Product entity'de business logic
public class Product : BaseEntity {
    public static Product Create(string name, string description, decimal price, int stock) {
        // Validation burada
        if (price < 0) throw new ArgumentException("Price geçersiz");
     return new Product { /* ... */ };
    }

    public void Update(string name, string desc, decimal price, int stock) {
        // Update validation
        this.Name = name;
    }
}
```

---

### Clean Architecture (Layered) 🏗️
**Nedir?** Uygulamayı bağımsız katmanlara ayırma  
**Nerede?** Tüm projeler  
**Dosyalar:**
```
AuthService/
├── AuthService.API/    ← Presentation
├── AuthService.Application/   ← Application  
├── AuthService.Domain/        ← Domain
└── AuthService.Infrastructure/ ← Infrastructure
```

**Faydası:** Independent testing, maintainable, scalable

```
API → Application → Domain
  ↘   ↙
    Infrastructure
    
Dependencies: Only downward ✓
```

---

### Exception Handling Middleware 🛡️
**Nedir?** Global exception yakalayıp RFC 7807 format'ında dönme  
**Nerede?** `Shared.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs`  
**Dosyalar:**
- `ExceptionHandlingMiddleware.cs` - Global handler
- `Domain/Exceptions/DomainException.cs` - Base domain exception

**Faydası:** Tutarlı error response, merkezi logging, clean code

```csharp
// Tüm exception'lar RFC 7807 format'ında
{
    "status": 400,
    "title": "Validation Error",
    "detail": "Message",
    "instance": "/api/endpoint"
}
```

---

### Options Pattern (Configuration) ⚙️
**Nedir?** Strongly-typed configuration management  
**Nerede?** `Program.cs`  
**Dosyalar:**
- `appsettings.json` - Configuration source
- `Program.cs` - Options registration

**Faydası:** Type-safe config, lazy loading, validation

```csharp
// appsettings.json
{ "JwtSettings": { "SecretKey": "...", "Issuer": "..." } }

// Program.cs
builder.Services
    .AddOptions<JwtBearerOptions>(...)
    .Configure<IConfiguration>((options, config) => {
     var secret = config["JwtSettings:SecretKey"];
        // Config usage
    });
```

---

### API Gateway (YARP - Reverse Proxy) 🚪
**Nedir?** Tüm client request'lerinin giriş noktası  
**Nerede?** `Gateway` projesi  
**Dosyalar:**
- `Gateway/Program.cs` - Gateway config
- `appsettings.json` - Routes & clusters

**Faydası:** Single entry point, rate limiting, auth merkezde

```
Client → Gateway (5000)
         ├→ Auth (5001)
       ├→ Product (5002)
         └→ Log (5003)
```

---

### Rate Limiting 🚦
**Nedir?** Request sayısını sınırlandırma  
**Nerede?** `Gateway/Program.cs`  
**Dosyalar:** `Gateway/Program.cs`

**Faydası:** DoS protection, fair usage

```csharp
// 100 request/dakika per IP
options.AddFixedWindowLimiter("fixed", limiterOptions => {
    limiterOptions.PermitLimit = 100;
    limiterOptions.Window = TimeSpan.FromMinutes(1);
});
```

---

### Health Checks 💚
**Nedir?** Uygulamanın ve dependencies'nin status kontrolü  
**Nerede?** `Program.cs` dosyaları  
**Dosyalar:**
- `Shared.Infrastructure/HealthChecks/` - Custom health checks
- `Program.cs` - Registration & mapping

**Faydası:** Kubernetes monitoring, auto-recovery

```csharp
// Endpoint'ler
GET /health → Overall status
GET /health/live → Liveness
GET /health/ready → Readiness
```

---

### 12-Factor App Methodology 📋
**Nedir?** Scalable, maintainable uygulamalar için best practices  
**Nerede?** Tüm uygulamada  
**Dosyalar:** Tüm projeler

**12 Factor:**
1. **Codebase** - Tek repo
2. **Dependencies** - Explicit (.csproj)
3. **Config** - Environment variables
4. **Backing Services** - Pluggable resources
5. **Build/Run** - Strict separation
6. **Processes** - Stateless
7. **Port Binding** - Self-contained
8. **Concurrency** - Process types
9. **Disposability** - Fast startup/shutdown
10. **Dev/Prod** - Same code
11. **Logs** - Stdout
12. **Admin** - One-off tasks

---

## 🔍 Design Pattern Seçme Rehberi

### Sorun: Veri erişimini test etmek istiyorum
**Çözüm:** Repository Pattern
- Interface'i mock et → tüm DB layer'ı test et
- Dosya: `Application/Interfaces/IProductRepository.cs`

### Sorun: Command ve Query'ler karışıyor
**Çözüm:** CQRS Pattern
- Separate commands from queries
- Dosya: `Commands/`, `Queries/`

### Sorun: Validation, logging, circuit-breaking otomatik eklemek istiyorum
**Çözüm:** Pipeline Behavior Pattern
- MediatR behaviors ekle
- Dosya: `Application/Behaviors/`

### Sorun: Redis vs InMemory cache arasında değiş-tokuş yapmak istiyorum
**Çözüm:** Strategy Pattern
- `ICacheService` interface
- Prod/Test'te farklı implementasyon

### Sorun: Error handling karışık, response format tutarsız
**Çözüm:** Global Exception Handler
- Middleware ekle
- Dosya: `Shared.Infrastructure/Middleware/ExceptionHandlingMiddleware.cs`

### Sorun: Backend servislerine isteğe erişim kontrol etmek istiyorum
**Çözüm:** API Gateway (YARP)
- Rate limiting, auth merkezde
- Dosya: `Gateway/Program.cs`

### Sorun: Configuration ortamlar arasında değişiyor
**Çözüm:** Options Pattern + 12-Factor
- `appsettings.json` + environment variables
- Dosya: `Program.cs`

---

## ✅ Pattern Checklist

### Yeni Handler yazarken:
- [ ] `IRequest<T>` implement ediyor mu? (CQRS)
- [ ] `IRequestHandler<,>` implement ediyor mu?
- [ ] Constructor'a DI parametreler geçiyor mu?
- [ ] Validator yazıyor mu? (FluentValidation)
- [ ] Domain exceptions fırlatıyor mu?
- [ ] Events publish ediyor mu?
- [ ] Caching kullanıyor mu?

### Yeni Service yazarken:
- [ ] Interface tanımladı mı?
- [ ] Production implementasyonu var mı?
- [ ] Test implementasyonu (Null/Mock) var mı?
- [ ] DI'da kayıtlı mı?

### Yeni Controller yazarken:
- [ ] `IMediator` kullanıyor mu?
- [ ] Proper HTTP methods (GET, POST, PUT, DELETE)?
- [ ] Status codes doğru mu?
- [ ] DTOs kullanıyor mu?

### Deploy etmeden:
- [ ] Health checks çalışıyor mu?
- [ ] Exception handling test edildi mi?
- [ ] Rate limiting config doğru mu?
- [ ] Logging düzgün mü?
- [ ] Database migrations OK mi?

---

## 📖 Pattern → Dosya Eşleşmesi

| Pattern | Amaç | Dosya |
|---------|------|-------|
| CQRS | Yazma/Okuma ayrımı | `Commands/`, `Queries/` |
| MediatR | Request mediation | `Commands/*Handler.cs` |
| Repository | DB abstraction | `Interfaces/IRepository.cs` |
| DI | Dependency injection | `Program.cs` |
| Pipeline Behavior | Cross-cutting | `Behaviors/` |
| Factory | Object creation | `Domain/Entities/` |
| DDD | Business logic | `Domain/` |
| Exception Handling | Global error | `ExceptionHandlingMiddleware.cs` |
| Options Pattern | Configuration | `Program.cs`, `appsettings.json` |
| Strategy | Runtime selection | `Infrastructure/Services/` |
| API Gateway | Routing | `Gateway/Program.cs` |
| Rate Limiting | Request limiting | `Gateway/Program.cs` |
| Health Checks | Status monitoring | `Program.cs`, `HealthChecks/` |
| 12-Factor | Best practices | Tüm projeler |
| Clean Architecture | Layering | `API/`, `Application/`, `Domain/`, `Infrastructure/` |

---

## 🚀 Quick Start: Yeni Feature Ekleme

### 1. Yeni Command Yazmak
```
Adımlar:
1. Domain/Events/ → Event tanımla
2. Domain/Exceptions/ → Exception tanımla
3. Application/Commands/ → Command define et
4. Application/Commands/ → Handler yaz
5. Application/Validators/ → Validator yaz
6. Application/Interfaces/ → Service interface'i
7. Infrastructure/ → Service implementasyonu
8. API/Controllers/ → Controller endpoint'i
```

### 2. Yeni Query Yazmak
```
Adımlar:
1. Application/Queries/ → Query define et
2. Application/Queries/ → Handler yaz (cache ekle)
3. Application/Interfaces/ → Repository method
4. Infrastructure/Repositories/ → Implement et
5. API/Controllers/ → Controller endpoint'i
```

### 3. Yeni Service Ekleme
```
Adımlar:
1. Application/Interfaces/ → Interface tanımla
2. Infrastructure/Services/ → Prod implementasyon
3. Infrastructure/Services/ → Test implementasyon (Null)
4. Program.cs → DI kayıt
5. Handler'da enjekte et
```

---

## 📚 Referans Linkler

- **CQRS Pattern:** `Commands/`, `Queries/` klasörleri
- **MediatR:** `Commands/*Handler.cs` dosyaları
- **Repository:** `Infrastructure/Repositories/`
- **DI:** `Program.cs`
- **Exception Handling:** `Shared.Infrastructure/Middleware/`
- **API Gateway:** `Gateway/Program.cs`
- **Health Checks:** `Shared.Infrastructure/HealthChecks/`
- **Validators:** `Application/Validators/`
- **Domain:** `Domain/Entities/`, `Domain/Events/`, `Domain/Exceptions/`

---

## 💡 İpuçları

1. **Handler yazarken DI'a odaklan** - Constructor parametreleri eksik mi?
2. **Validator her command'a** - Null/empty kontrolü
3. **Exception domain katmanından fırlat** - App katmanında değil
4. **Cache invalidation yap** - Update sonrası
5. **Event publish et** - Diğer servisleri inform et
6. **Middleware sırası önemli** - Rate limit → Auth → Handler
7. **Test ortamında null services kullan** - Side effects yok
8. **Health checks production'da** - Kubernetes liveness/readiness
9. **Log handler'lar** - Debug zor, logsun her handler'a
10. **DTOs return et** - Domain entities expose etme

