# Design Patterns Analysis - Microservices Task Uygulaması

Bu dokümanda, Task microservices uygulamasında kullanılan tüm design patternları detaylı olarak açıklanmıştır.

---

## 📋 İçindekiler

1. [CQRS Pattern](#1-cqrs-pattern)
2. [MediatR Mediator Pattern](#2-mediatr-mediator-pattern)
3. [Repository Pattern](#3-repository-pattern)
4. [Dependency Injection (DI) Pattern](#4-dependency-injection-pattern)
5. [Clean Architecture (Layered Architecture)](#5-clean-architecture)
6. [Pipeline/Middleware Pattern](#6-pipelinemiddleware-pattern)
7. [Factory Pattern](#7-factory-pattern)
8. [Strategy Pattern](#8-strategy-pattern)
9. [Domain-Driven Design (DDD)](#9-domain-driven-design)
10. [Exception Handling Pattern](#10-exception-handling-pattern)
11. [Options Pattern](#11-options-pattern)
12. [Reverse Proxy Pattern (Gateway)](#12-reverse-proxy-pattern-gateway)
13. [Rate Limiting Pattern](#13-rate-limiting-pattern)
14. [Health Check Pattern](#14-health-check-pattern)
15. [12-Factor App Methodology](#15-12-factor-app-methodology)

---

## 1. CQRS Pattern

### Tanım
**CQRS (Command Query Responsibility Segregation)** - Komut ve sorgu işlemlerini ayrı olarak ele alan pattern.

### Uygulanma
Uygulamada Commands ve Queries tamamen ayrılmıştır:

#### Commands (Yazma İşlemleri)
```csharp
// AuthService/Application/Commands/LoginCommand.cs
public record LoginCommand(string Email, string Password) : IRequest<TokenResponse>;

// ProductService/Application/Commands/AddProductCommand.cs
public sealed record AddProductCommand(
    string Name,
string Description,
    decimal Price,
    int Stock
) : IRequest<ProductDto>;

// Diğer Commands:
// - RegisterCommand: Kullanıcı kaydı
// - RefreshTokenCommand: Token yenileme
// - UpdateProductCommand: Ürün güncelleme
```

#### Queries (Okuma İşlemleri)
```csharp
// ProductService/Application/Queries/GetProductsQuery.cs
public sealed record GetProductsQuery : IRequest<IEnumerable<ProductDto>>;

// ProductService/Application/Queries/GetProductByIdQuery.cs
public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto>;
```

### Faydaları
✅ **Performans**: Okuma ve yazma işlemleri bağımsız optimize edilebilir  
✅ **Skalabilite**: Farklı veritabanı stratejileri uygulanabilir (okuma replika vs yazma master)  
✅ **Açıklık**: Iş mantığı net bir şekilde Command/Query olarak ayrılır  
✅ **Test Edilebilirlik**: Her handler bağımsız test edilebilir  

---

## 2. MediatR Mediator Pattern

### Tanım
**Mediator Pattern** - Nesneler arasında haberleşmeyi merkezileştirilmiş bir nesne üzerinden sağlar.

### Uygulanma

#### MediatR Handler'ları
```csharp
// AuthService/Application/Commands/LoginCommandHandler.cs
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<TokenResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // İş mantığı
        var user = await _userManager.FindByEmailAsync(request.Email)
  ?? throw AuthException.InvalidCredentials();

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
 if (!passwordValid)
            throw AuthException.InvalidCredentials();

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
  await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
   AccessTokenExpiry: DateTime.UtcNow.AddMinutes(15)
        );
    }
}

// ProductService/Application/Queries/GetProductByIdQueryHandler.cs
public sealed class GetProductByIdQueryHandler 
    : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        // Cache'den kontrol et
   var cached = await _cache.GetAsync<ProductDto>($"product_{request.ProductId}", ct);
        if (cached != null)
            return cached;

        // Veritabanından al
    var product = await _repository.GetByIdAsync(request.ProductId, ct)
            ?? throw ProductNotFoundException.ById(request.ProductId);

        var dto = new ProductDto { /* ... */ };

        // Cache'e kaydet
      await _cache.SetAsync($"product_{request.ProductId}", dto, TimeSpan.FromHours(1), ct);

        return dto;
    }
}
```

#### DI Kaydı
```csharp
// AuthService/API/Program.cs
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly));

// ProductService/Application/Extensions/ApplicationServiceExtensions.cs
services.AddMediatR(cfg =>
{
  cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly);
    
    // Pipeline: Logging → Validation → Handler
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});
```

### Faydaları
✅ **Loose Coupling**: Handler'lar birbirleriyle bağımlı değil  
✅ **Centralized Logic**: Tüm request işlemesi bir yerden yönetilir  
✅ **Pipeline Support**: Cross-cutting concerns (validation, logging) kolayca eklenebilir  

---

## 3. Repository Pattern

### Tanım
**Repository Pattern** - Veritabanı erişimini abstraktlaştırarak kalıplar sunmaktadır.

### Uygulanma

```csharp
// ProductService/Application/Interfaces/IProductRepository.cs
public interface IProductRepository
{
Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

// ProductService/Infrastructure/Repositories/ProductRepository.cs
public sealed class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
  => _context = context;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    => await _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

  public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct)
   => await _context.Products.ToListAsync(ct);

    public async Task AddAsync(Product product, CancellationToken ct)
    {
        await _context.Products.AddAsync(product, ct);
  await SaveChangesAsync(ct);
    }

  public async Task UpdateAsync(Product product, CancellationToken ct)
    {
    _context.Products.Update(product);
        await SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
    var product = await GetByIdAsync(id, ct);
 if (product != null)
        {
            _context.Products.Remove(product);
        await SaveChangesAsync(ct);
     }
    }

    public async Task SaveChangesAsync(CancellationToken ct)
      => await _context.SaveChangesAsync(ct);
}

// AuthService/Infrastructure/Repositories/RefreshTokenRepository.cs
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _context;

    public RefreshTokenRepository(AuthDbContext context)
        => _context = context;

    public async Task AddAsync(RefreshToken token, CancellationToken ct)
     => await _context.RefreshTokens.AddAsync(token, ct);

    public async Task SaveChangesAsync(CancellationToken ct)
   => await _context.SaveChangesAsync(ct);
}
```

### Faydaları
✅ **Abstraction**: Veritabanı teknolojisinden bağımsızlaşma  
✅ **Testability**: In-memory implementations test için kolayca yazılabilir  
✅ **Single Responsibility**: Veri erişimi tek bir yerde  

---

## 4. Dependency Injection Pattern

### Tanım
**DI (Dependency Injection)** - Bağımlılıkları dışarıdan enjekte etme yoluyla loose coupling sağlar.

### Uygulanma

#### Constructor Injection
```csharp
// LoginCommandHandler'da tüm bağımlılıklar constructor'dan gelir
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
    UserManager<AppUser> userManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
 {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }
}
```

#### Service Registration
```csharp
// AuthService/API/Program.cs
// Repository & Service kaydı
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

// ProductService/Infrastructure/Extensions/InfrastructureServiceExtensions.cs
services.AddScoped<IProductRepository, ProductRepository>();

if (!isTesting)
{
    services.AddScoped<ICacheService, RedisCacheService>();
}
else
{
    services.AddScoped<ICacheService, NullCacheService>();
}
```

#### Lifetime Management
```csharp
// Transient: Her talep için yeni instance (stateless operations)
services.AddTransient<ISomeService, SomeService>();

// Scoped: Request başına bir instance (Entity Framework DbContext)
services.AddScoped<IProductRepository, ProductRepository>();
services.AddScoped<ProductDbContext>();

// Singleton: Uygulama yaşamı boyunca bir instance (heavy, stateless)
services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConn));
services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
```

### Faydaları
✅ **Loose Coupling**: Sınıflar concrete implementation'lara bağımlı değil  
✅ **Testability**: Mock'lar kolayca enjekte edilebilir  
✅ **Flexibility**: Runtime'da farklı implementasyonlar seçilebilir  

---

## 5. Clean Architecture

### Tanım
**Clean Architecture** - Uygulamayı independent, testable layer'lara ayıran mimarı.

### Katmanlar

```
┌─────────────────────────────────┐
│     API Layer (Controllers)    │  → HTTP requests/responses
├─────────────────────────────────┤
│   Application Layer (CQRS)       │  → Business logic, use cases
│   (Commands, Queries, Handlers)  │
├─────────────────────────────────┤
│   Domain Layer (Entities, Events)│  → Core business rules
├─────────────────────────────────┤
│   Infrastructure Layer  │  → DB, cache, messaging
│   (Repositories, Services)       │
└─────────────────────────────────┘
```

#### Proje Yapısı
```
AuthService/
├── AuthService.API/              # Layer 1: Web
│   ├── Controllers/
│   └── Program.cs
├── AuthService.Application/      # Layer 2: Application
│   ├── Commands/
│   ├── Interfaces/
│   ├── DTOs/
│   └── Extensions/
├── AuthService.Domain/           # Layer 3: Domain
│   ├── Entities/
│   └── Exceptions/
└── AuthService.Infrastructure/   # Layer 4: Infrastructure
    ├── Persistence/
    ├── Repositories/
    ├── Services/
    └── Extensions/

ProductService/         # Aynı yapı
Shared/      # Shared utilities
├── Shared.Domain/      # Base entities, interfaces
└── Shared.Infrastructure/      # Middleware, extensions
```

#### Layer'lar Arası Veri Akışı
```csharp
// 1. API - Request alındı
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost("login")]
 public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Request → Command dönüştürme
      var command = new LoginCommand(request.Email, request.Password);
        
        // Application layer'a yönlendir
      var result = await _mediator.Send(command);
      
        return Ok(result);
    }
}

// 2. Application - Handler (iş mantığı)
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(LoginCommand request, CancellationToken ct)
 {
        // Domain rules + Infrastructure services kullan
    var user = await _userManager.FindByEmailAsync(request.Email);
    
        // Domain entity operations
        var roles = await _userManager.GetRolesAsync(user);
        
 // Infrastructure: token generate & persist
        var token = _tokenService.GenerateAccessToken(user, roles);
        await _refreshTokenRepository.AddAsync(refreshToken, ct);
        
        // Response DTO döndür
        return new TokenResponse(token, ...);
    }
}

// 3. Domain - Entity & Rules
public class Product : BaseEntity
{
    public static Product Create(string name, string description, decimal price, int stock)
    {
        // Domain business rules burada enforce edilir
 ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0) throw new ArgumentException("Fiyat negatif olamaz.");
   if (stock < 0) throw new ArgumentException("Stok negatif olamaz.");
    
        return new Product { /* ... */ };
    }
}

// 4. Infrastructure - Persistence
public sealed class ProductRepository : IProductRepository
{
public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
   return await _context.Products
   .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
}
```

### Faydaları
✅ **Independent**: Layer'lar bağımsız test edilebilir  
✅ **Framework Agnostic**: Framework'ün iş mantığını kontrol etmesi az  
✅ **Maintainability**: Her katmanın sorumluluğu net  
✅ **Flexibility**: Layer implementasyonları değiştirebilir  

---

## 6. Pipeline/Middleware Pattern

### Tanım
**Pipeline Pattern** - Request işlemesini sıra halinde geçiş yaptıran middleware'ler.

### MediatR Pipeline Behaviors

```csharp
// ProductService/Application/Behaviors/LoggingBehavior.cs
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
  {
        var requestName = typeof(TRequest).Name;
        var startTime = DateTime.UtcNow;

  _logger.LogInformation("[CQRS] Başlıyor: {RequestName} {@Request}", requestName, request);

    try
    {
            // Chain'de sonraki behavior/handler'a geç
     var response = await next();
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
    _logger.LogInformation(
        "[CQRS] Tamamlandı: {RequestName} ({ElapsedMs}ms)", 
       requestName, elapsed);

    return response;
        }
        catch (Exception ex)
        {
     var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex,
    "[CQRS] Hata: {RequestName} ({ElapsedMs}ms) — {Error}",
    requestName, elapsed, ex.Message);
     throw;
        }
    }
}

// ProductService/Application/Behaviors/ValidationBehavior.cs
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
CancellationToken ct)
    {
      if (!_validators.Any())
    return await next();

        var context = new ValidationContext<TRequest>(request);

 var failures = (await Task.WhenAll(
        _validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
   .Where(f => f is not null)
        .ToList();

        if (failures.Count > 0)
      throw new ValidationException(failures);

  return await next();
  }
}
```

#### Pipeline Kaydı
```csharp
// ProductService/Application/Extensions/ApplicationServiceExtensions.cs
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly);

    // Sıra önemli!
    // 1. LoggingBehavior: Request logging
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    
    // 2. ValidationBehavior: Validation check
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
 
    // 3. Handler: Asıl iş mantığı
});
```

#### HTTP Middleware Pipeline
```csharp
// AuthService/API/Program.cs
var app = builder.Build();

// Middleware sırası!
app.UseMiddleware<ExceptionHandlingMiddleware>();  // 1. Hata yakala
app.UseSwagger();  // 2. Swagger
app.UseAuthentication(); // 3. Auth check
app.UseAuthorization();     // 4. Yetki check
app.MapControllers(); // 5. Route match
```

#### Request Flow Örneği
```
HTTP Request
    ↓
ExceptionHandlingMiddleware
    ↓
Authentication
    ↓
Authorization
    ↓
Controller
    ↓
MediatR Send(LoginCommand)
  ↓
LoggingBehavior (log başla)
    ↓
ValidationBehavior (validate et)
  ↓
LoginCommandHandler (iş mantığı)
    ↓
ValidationBehavior (success log)
    ↓
LoggingBehavior (sonuç log)
↓
HTTP Response
```

### Faydaları
✅ **SRP**: Her middleware bir sorumluluk  
✅ **OCP**: Yeni middleware eklenebilir, var olanlar değişmez  
✅ **Composability**: Middleware'ler kombinlenebilir  

---

## 7. Factory Pattern

### Tanım
**Factory Pattern** - Nesne oluşturmayı merkezileştirerek fleksibilite ve kontrol sağlar.

### Uygulanma

#### Domain Factory (Static Factory Methods)
```csharp
// ProductService/Domain/Entities/Product.cs
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;

 // Factory method: Business rules ile nesne oluştur
  public static Product Create(string name, string description, decimal price, int stock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0) throw new ArgumentException("Fiyat negatif olamaz.", nameof(price));
        if (stock < 0) throw new ArgumentException("Stok negatif olamaz.", nameof(stock));

        return new Product
        {
            Id = Guid.NewGuid(),
        Name = name,
            Description = description,
            Price = price,
            Stock = stock,
   IsActive = true,
            CreatedAt = DateTime.UtcNow
      };
    }

    // Update factory
    public void Update(string name, string description, decimal price, int stock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
   if (price < 0) throw new ArgumentException("Fiyat negatif olamaz.", nameof(price));
  if (stock < 0) throw new ArgumentException("Stok negatif olamaz.", nameof(stock));

        Name = name;
        Description = description;
Price = price;
        Stock = stock;
    UpdatedAt = DateTime.UtcNow;
    }
}

// Handler'da kullanım
public sealed class AddProductCommandHandler 
    : IRequestHandler<AddProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AddProductCommand request, CancellationToken ct)
    {
// Factory method kullan - business rules otomatik enforce edilir
        var product = Product.Create(
            request.Name,
          request.Description,
     request.Price,
            request.Stock);

        await _repository.AddAsync(product, ct);
        return MapToDto(product);
    }
}
```

#### DbContext Factory
```csharp
// AuthService/Infrastructure/Persistence/AuthDbContextFactory.cs
public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
  public AuthDbContext CreateDbContext(string[] args)
    {
     var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
  var configuration = new ConfigurationBuilder()
     .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), 
       "../AuthService.API"))
     .AddJsonFile("appsettings.json")
   .Build();

        optionsBuilder.UseSqlServer(
   configuration.GetConnectionString("DefaultConnection"));

        return new AuthDbContext(optionsBuilder.Options);
    }
}
```

#### Event Publisher Factory (Strategy Pattern ile birlikte)
```csharp
// ProductService/Infrastructure/Extensions/InfrastructureServiceExtensions.cs
var messagingProvider = configuration["Messaging:Provider"] ?? "RabbitMQ";

if (isTesting)
{
    services.AddScoped<IEventPublisher, NullEventPublisher>();
}
else if (messagingProvider.Equals("Kafka", StringComparison.OrdinalIgnoreCase))
{
    services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
}
else
{
    services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
}
```

### Faydaları
✅ **Encapsulation**: Nesne oluşturma mantığı kapsüllenir  
✅ **Validation**: Business rules merkezi bir yerde  
✅ **Flexibility**: İleride farklı nesne türleri eklenebilir  

---

## 8. Strategy Pattern

### Tanım
**Strategy Pattern** - Runtime'da farklı algoritmalar seçmeyi sağlar.

### Uygulanma

#### Cache Strategy
```csharp
// ProductService/Application/Interfaces/ICacheService.cs
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
}

// Redis implementasyonu
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
    {
     var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key);
     
      if (!value.HasValue)
   return null;

        return JsonSerializer.Deserialize<T>(value.ToString());
    }

  public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
  var db = _redis.GetDatabase();
  var json = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, expiry);
    }
}

// Test/Development için Null implementasyonu
public sealed class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
   => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => Task.CompletedTask;
}

// DI'da strategy seç
var isTesting = builder.Environment.IsEnvironment("Testing");
if (!isTesting)
{
    services.AddScoped<ICacheService, RedisCacheService>();
}
else
{
    services.AddScoped<ICacheService, NullCacheService>();
}
```

#### Event Publishing Strategy
```csharp
// ProductService/Application/Interfaces/IEventPublisher.cs
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IIntegrationEvent;
}

// RabbitMQ implementasyonu
public sealed class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IIntegrationEvent
    {
        using var channel = _connection.CreateModel();
        
        var exchangeName = typeof(T).Name;
        channel.ExchangeDeclare(exchange: exchangeName, type: "topic", durable: true);

        var json = JsonSerializer.Serialize(@event);
   var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(
            exchange: exchangeName,
     routingKey: $"{@event.AggregateId}",
            basicProperties: null,
      body: body);

        await Task.CompletedTask;
    }
}

// Kafka implementasyonu
public sealed class KafkaEventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;

 public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IIntegrationEvent
    {
        var topic = typeof(T).Name;
        var json = JsonSerializer.Serialize(@event);

        await _producer.ProduceAsync(
            topic,
          new Message<string, string> 
      { 
      Key = @event.AggregateId.ToString(), 
      Value = json 
            },
       ct);
    }
}

// Test implementasyonu
public sealed class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IIntegrationEvent
        => Task.CompletedTask; // Hiçbir şey yapma
}

// Database Strategy
if (isTesting)
{
    var root = new InMemoryDatabaseRoot();
  services.AddDbContext<ProductDbContext>(opt =>
    opt.UseInMemoryDatabase("ProductIntegrationTestDb", root));
}
else
{
    services.AddDbContext<ProductDbContext>(opt =>
        opt.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
}
```

### Handler'da Strategy Kullanım
```csharp
public sealed class AddProductCommandHandler 
    : IRequestHandler<AddProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICacheService _cache;

    public async Task<ProductDto> Handle(AddProductCommand request, CancellationToken ct)
    {
        // Domain entity oluştur
   var product = Product.Create(
         request.Name,
  request.Description,
      request.Price,
      request.Stock);

        // Repository strategy'si (SQL/InMemory) otomatik seçilir
        await _repository.AddAsync(product, ct);

        // Event publish strategy'si (RabbitMQ/Kafka/Null) otomatik seçilir
        var productAddedEvent = new ProductAddedEvent { /* ... */ };
        await _eventPublisher.PublishAsync(productAddedEvent, ct);

        // Cache strategy'si (Redis/Null) otomatik seçilir
        var dto = MapToDto(product);
        await _cache.SetAsync($"product_{product.Id}", dto, TimeSpan.FromHours(1), ct);

        return dto;
    }
}
```

### Faydaları
✅ **Flexibility**: Runtime'da algoritma değiştirebilir  
✅ **Testability**: Mock strategy'ler test için hazır  
✅ **Open/Closed**: Yeni stratejiler eklenebilir  

---

## 9. Domain-Driven Design (DDD)

### Tanım
**DDD** - İş mantığı domain katmanında (entities, value objects, aggregates) merkezi olarak yönetilir.

### Uygulanma

#### Domain Entities
```csharp
// ProductService/Domain/Entities/Product.cs
public class Product : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }

    // Domain Logic: Business rules entity'nin içinde
    public static Product Create(string name, string description, decimal price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
 throw new ArgumentException("Name boş olamaz");
        if (price < 0)
        throw new ArgumentException("Price negatif olamaz");
        if (stock < 0)
            throw new ArgumentException("Stock negatif olamaz");

        return new Product
        {
            Id = Guid.NewGuid(),
          Name = name,
            Description = description,
          Price = price,
   Stock = stock,
      IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string description, decimal price, int stock)
    {
        // Update validasyonları domain'de
      if (string.IsNullOrWhiteSpace(name))
 throw new ArgumentException("Name boş olamaz");

        Name = name;
        Description = description;
    Price = price;
  Stock = stock;
        UpdatedAt = DateTime.UtcNow;
    }

 public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

// AuthService/Domain/Entities/AppUser.cs
public class AppUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsEmailVerified { get; set; }

    // Domain logic
 public void VerifyEmail()
    {
        if (IsEmailVerified)
            throw new InvalidOperationException("Email zaten doğrulandı");
        
 IsEmailVerified = true;
  }
}
```

#### Domain Events
```csharp
// ProductService/Domain/Events/ProductAddedEvent.cs
public sealed record ProductAddedEvent(
    Guid ProductId,
    string Name,
    decimal Price,
    int Stock,
    DateTime CreatedAt) : IIntegrationEvent;

// ProductService/Domain/Events/ProductUpdatedEvent.cs
public sealed record ProductUpdatedEvent(
    Guid ProductId,
    string Name,
    decimal Price,
    int Stock,
    DateTime UpdatedAt) : IIntegrationEvent;
```

#### Domain Exceptions
```csharp
// Shared/Domain/Exceptions/DomainException.cs
public abstract class DomainException : Exception
{
    public int StatusCode { get; init; }

  protected DomainException(string message, int statusCode = 400) 
        : base(message)
    {
        StatusCode = statusCode;
    }
}

// AuthService/Domain/Exceptions/AuthException.cs
public sealed class AuthException : DomainException
{
    public AuthException(string message, int statusCode = 401)
     : base(message, statusCode) { }

    public static AuthException InvalidCredentials()
        => new("Geçersiz e-posta veya şifre", 401);

    public static AuthException UserNotFound()
        => new("Kullanıcı bulunamadı", 404);
}

// ProductService/Domain/Exceptions/ProductNotFoundException.cs
public sealed class ProductNotFoundException : DomainException
{
    public ProductNotFoundException(string message, int statusCode = 404)
        : base(message, statusCode) { }

    public static ProductNotFoundException ById(Guid id)
        => new($"Ürün ID {id} bulunamadı", 404);
}
```

#### Aggregate Root Concept
```csharp
// Product aggregate: Product + events
public class Product : BaseEntity
{
    private readonly List<DomainEvent> _domainEvents = new();

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void RaiseEvent(DomainEvent @event) => _domainEvents.Add(@event);

    public void ClearEvents() => _domainEvents.Clear();

    public static Product Create(string name, string description, decimal price, int stock)
    {
        var product = new Product { /* ... */ };
 
        // Event raise et
        product.RaiseEvent(new ProductAddedEvent(
            product.Id,
    product.Name,
      product.Price,
    product.Stock,
            product.CreatedAt));

        return product;
    }
}

// Handler'da events publish et
public sealed class AddProductCommandHandler 
  : IRequestHandler<AddProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public async Task<ProductDto> Handle(AddProductCommand request, CancellationToken ct)
    {
        var product = Product.Create(
    request.Name,
            request.Description,
 request.Price,
            request.Stock);

        await _repository.AddAsync(product, ct);

        // Domain events'i publish et
 foreach (var @event in product.DomainEvents)
    {
            await _eventPublisher.PublishAsync(@event, ct);
        }

        product.ClearEvents();

     return MapToDto(product);
    }
}
```

### Faydaları
✅ **Business Focus**: İş mantığı kod merkezinde  
✅ **Maintainability**: Business rules bir yerde toplanır  
✅ **Flexibility**: Domain changes entiteler güncellemeleri yeterli  

---

## 10. Exception Handling Pattern

### Tanım
**Global Exception Handler** - Tüm exception'ları merkezi bir yerde yakalayıp RFC 7807 format'ında response döner.

### Uygulanma

```csharp
// Shared/Infrastructure/Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
        await _next(context);
        }
        catch (Exception ex)
   {
       _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
     await HandleExceptionAsync(context, ex);
     }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode;
        string title;

        // 1. FluentValidation Exception
        if (exception is ValidationException validationEx)
        {
     statusCode = 400;
   title = "Validation Error";

    var validationProblem = new ValidationProblemDetails(
    validationEx.Errors
         .GroupBy(e => e.PropertyName)
         .ToDictionary(
               g => g.Key,
     g => g.Select(e => e.ErrorMessage).ToArray()))
            {
      Status = 400,
                Title = title,
       Instance = context.Request.Path
            };

            context.Response.ContentType = "application/problem+json";
     context.Response.StatusCode = 400;

            var json = JsonSerializer.Serialize(validationProblem, new JsonSerializerOptions
          {
 PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

       await context.Response.WriteAsync(json);
      return;
      }

      // 2. Domain Exception
        else if (exception is DomainException domainEx)
        {
            statusCode = domainEx.StatusCode;
            title = statusCode switch
            {
                400 => "Bad Request",
      401 => "Unauthorized",
                403 => "Forbidden",
      404 => "Not Found",
             409 => "Conflict",
      422 => "Unprocessable Entity",
                _ => "Domain Error"
   };
        }

        // 3. Framework/Infrastructure Exception'ları
      else
        {
   (statusCode, title) = exception switch
            {
          KeyNotFoundException => (404, "Not Found"),
                UnauthorizedAccessException => (401, "Unauthorized"),
    ArgumentNullException => (400, "Bad Request"),
                ArgumentException => (400, "Bad Request"),
                InvalidOperationException => (400, "Invalid Operation"),
    NotSupportedException => (400, "Not Supported"),
       OperationCanceledException => (499, "Request Cancelled"),
           _ => (500, "Internal Server Error")
         };
        }

        // RFC 7807 ProblemDetails format'ında response
        var problem = new ProblemDetails
     {
         Status = statusCode,
        Title = title,
  Detail = exception.Message,
         Instance = context.Request.Path
   };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var responseJson = JsonSerializer.Serialize(problem, new JsonSerializerOptions
   {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(responseJson);
    }
}

// Program.cs'de middleware kaydı
var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();  // Pipeline başına ekle
```

#### Exception Handling Örneği

```csharp
// 1. ValidationException → 400 Bad Request
[
    {
  "status": 400,
    "title": "Validation Error",
        "errors": {
 "email": ["Email geçerli bir e-posta adresi değildir"],
            "password": ["Şifre en az 8 karakter olmalıdır"]
        }
    }
]

// 2. ProductNotFoundException → 404 Not Found
{
    "status": 404,
    "title": "Not Found",
    "detail": "Ürün ID abc123 bulunamadı",
    "instance": "/api/products/abc123"
}

// 3. AuthException (InvalidCredentials) → 401 Unauthorized
{
    "status": 401,
    "title": "Unauthorized",
    "detail": "Geçersiz e-posta veya şifre",
    "instance": "/api/auth/login"
}

// 4. Genel Server Error → 500 Internal Server Error
{
    "status": 500,
    "title": "Internal Server Error",
  "detail": "Beklenmeyen bir hata oluştu",
    "instance": "/api/products"
}
```

### Faydaları
✅ **Consistency**: Tüm exception'lar aynı format'ta  
✅ **Logging**: Tüm hatalar merkezi yerde loglanır  
✅ **User Friendly**: Error response'lar RFC 7807 standard'ı takip eder  

---

## 11. Options Pattern

### Tanım
**Options Pattern** - Configuration'ı strongly-typed classes üzerinden manage etme.

### Uygulanma

#### Lazy Configuration (JWT Settings)

```csharp
// appsettings.json
{
 "JwtSettings": {
      "SecretKey": "your-super-secret-key-at-least-32-characters-long",
     "Issuer": "AuthService",
        "Audience": "MicroserviceSolution",
        "ExpiryMinutes": 15
    }
}

// AuthService/API/Program.cs
builder.Services
  .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, config) =>
    {
        var jwtSection = config.GetSection("JwtSettings");
        var secret = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey yapılandırılmamış.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
      ValidateIssuer = true,
      ValidateAudience = true,
ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
          ValidIssuer = jwtSection["Issuer"],
          ValidAudience = jwtSection["Audience"],
 IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

// ProductService/API/Program.cs - aynı pattern
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, config) =>
    {
        var jwtSection = config.GetSection("JwtSettings");
      var secret = jwtSection["SecretKey"] ?? string.Empty;
        
        options.TokenValidationParameters = new TokenValidationParameters
    {
     ValidateIssuer = true,
        ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
          ValidIssuer = config["JwtSettings:Issuer"] ?? "AuthService",
         ValidAudience = config["JwtSettings:Audience"] ?? "MicroserviceSolution",
         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });
```

#### Rate Limiting Options

```csharp
// Gateway/Program.cs
builder.Services.AddRateLimiter(options =>
{
    // Named policy: 100 request/minute/IP
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    // Auth endpoints için stricter policy: 10 request/minute
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
 limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
```

### Faydaları
✅ **Type Safety**: Configuration strongly-typed  
✅ **Lazy Loading**: Configuration middleware invocation'da okunur  
✅ **Validation**: Options validate edilebilir  

---

## 12. Reverse Proxy Pattern (Gateway)

### Tanım
**API Gateway** - Tüm client request'lerinin giriş noktası, backend servislerine yönlendirme yapar.

### Uygulanma

```csharp
// Gateway/Program.cs
// YARP (Yet Another Reverse Proxy) kullanılıyor
var builder = WebApplication.CreateBuilder(args);

// ── YARP Reverse Proxy ────────────────────────────────────────
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// appsettings.json
{
    "ReverseProxy": {
        "Routes": [
        {
      "RouteId": "auth-route",
        "ClusterId": "auth-cluster",
        "Match": { "Path": "/api/auth/**" },
   "RateLimitPolicy": "auth"
         },
            {
     "RouteId": "product-route",
    "ClusterId": "product-cluster",
      "Match": { "Path": "/api/products/**" },
    "RateLimitPolicy": "fixed"
          }
        ],
        "Clusters": [
            {
   "ClusterId": "auth-cluster",
            "Destinations": {
      "auth-server": { "Address": "http://localhost:5001" }
 }
            },
 {
            "ClusterId": "product-cluster",
    "Destinations": {
           "product-server": { "Address": "http://localhost:5002" }
        }
   }
        ]
    }
}

// Gateway'de JWT auth + rate limiting
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
      limiterOptions.Window = TimeSpan.FromMinutes(1);
    });

    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
     limiterOptions.PermitLimit = 10;
     limiterOptions.Window = TimeSpan.FromMinutes(1);
    });
});

var app = builder.Build();

// Middleware sırası
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.Run();
```

#### Request Flow

```
Client
  ↓ HTTP Request
Gateway (Port 5000)
  ↓ RateLimiter check
  ↓ JWT validation
  ↓ Route matching (/api/auth/... → auth-cluster)
  ↓ YARP proxy
AuthService (Port 5001)
  ↓ Response döndür
  ↓
Gateway (response rewrite)
  ↓
Client ← HTTP Response
```

### Faydaları
✅ **Single Entry Point**: Tüm client request'leri merkezde  
✅ **Cross-cutting Concerns**: Gateway'de auth, rate limiting, logging  
✅ **Service Discovery**: Backend servisler saklı  
✅ **Load Balancing**: Birden fazla instance'a dağıtım  

---

## 13. Rate Limiting Pattern

### Tanım
**Rate Limiting** - Belirli bir zaman diliminde maksimum request sayısını sınırlandırma.

### Uygulanma

```csharp
// Gateway/Program.cs
builder.Services.AddRateLimiter(options =>
{
    // Policy 1: Genel API endpoint'leri için
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;           // 100 istek
      limiterOptions.Window = TimeSpan.FromMinutes(1);  // 1 dakikada
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;      // Bekle queue'ye max 10 istek
    });

    // Policy 2: Auth endpoint'leri için (daha katı)
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;   // 10 istek
        limiterOptions.Window = TimeSpan.FromMinutes(1);
limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    // Limit aşıldığında dönen status code
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// appsettings.json (YARP ile entegrasyon)
{
    "ReverseProxy": {
      "Routes": [
   {
 "RouteId": "auth-route",
    "ClusterId": "auth-cluster",
                "Match": { "Path": "/api/auth/**" },
         "RateLimitPolicy": "auth"  // Katı limit
       },
     {
       "RouteId": "product-route",
        "ClusterId": "product-cluster",
  "Match": { "Path": "/api/products/**" },
      "RateLimitPolicy": "fixed"  // Normal limit
            }
     ]
    }
}

// Middleware pipeline'a ekle
var app = builder.Build();
app.UseRateLimiter();  // Tüm route'lardan önce
app.MapReverseProxy();
```

#### Response (Limit Aşıldığında)
```
HTTP/1.1 429 Too Many Requests
Content-Type: application/problem+json

{
    "type": "https://tools.ietf.org/html/rfc6585#section-4",
    "title": "Too Many Requests",
    "status": 429,
    "detail": "The request quota for this service has been exceeded."
}

Response Headers:
Retry-After: 43  (43 saniye sonra tekrar dene)
```

### Faydaları
✅ **Protection**: DoS attack'larından koruma  
✅ **Fair Usage**: Kaynakların eşit dağılımı  
✅ **Predictability**: Sistem stabil kalır  

---

## 14. Health Check Pattern

### Tanım
**Health Checks** - Uygulamanın ve dependencies'nin status'unu kontrolü.

### Uygulanma

```csharp
// AuthService/API/Program.cs
var healthChecks = builder.Services.AddHealthChecks();

// SqlServer health check (sadece non-testing ortamlarda)
if (!builder.Environment.IsEnvironment("Testing"))
{
    healthChecks.AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
 name: "sqlserver",
        tags: ["db", "auth"]);
}

// ProductService/API/Program.cs
builder.Services.AddHealthChecks();

// Redis Health Check
healthChecks.AddRedis(
  redisConnectionString: "localhost:6379",
    name: "redis",
    tags: ["cache", "product"]);

// Gateway/Program.cs
builder.Services.AddHealthChecks();

// Middleware'da expose et
app.MapHealthChecks("/health");

// Şu endpoint'ler oluşturulur:
// GET /health → Summary
// GET /health/live → Liveness check
// GET /health/ready → Readiness check
```

#### Response Formatı

```json
// GET /health
{
"status": "Healthy",
  "checks": {
        "sqlserver": {
       "status": "Healthy",
            "duration": "00:00:00.0234567"
        },
     "redis": {
       "status": "Healthy",
        "duration": "00:00:00.0123456"
 }
    },
    "totalDuration": "00:00:00.0358023"
}

// Eğer birisi down ise:
{
    "status": "Unhealthy",
    "checks": {
      "redis": {
        "status": "Unhealthy",
          "description": "Connection refused",
            "duration": "00:00:01.5234567",
   "exception": "StackExchange.Redis.RedisConnectionException"
   }
    }
}
```

### Faydaları
✅ **Monitoring**: Container orchestration (Kubernetes) tarafından monitore edilebilir  
✅ **Auto-recovery**: Unhealthy pods otomatik restart edilebilir  
✅ **Load Balancing**: Healthy instance'lara traffic yönlendirme  

---

## 15. 12-Factor App Methodology

### Tanım
**12-Factor App** - Ölçeklenebilir ve maintainable uygulamalar yazmak için best practices.

### Uygulanma

#### 1. Codebase
```
✅ Tek kod tabanı, environment'a göre deploy edilir
   Repository: https://github.com/omof221/Task
   Branch: test/v1.0.0
```

#### 2. Dependencies
```csharp
// .csproj'de açıkça declare edilir
<ItemGroup>
    <PackageReference Include="MediatR" Version="12.0.0" />
    <PackageReference Include="FluentValidation" Version="11.0.0" />
    <PackageReference Include="EntityFramework" Version="8.0.0" />
</ItemGroup>

// NuGet restore otomatik bağımlılık yönetir
```

#### 3. Config
```csharp
// appsettings.json + environment değişkenleri
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{environment}.json")
    .AddEnvironmentVariables();

// ConnectionString örneği
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Environment variable: ConnectionStrings__DefaultConnection
```

#### 4. Backing Services
```csharp
// DB, Cache, Message Queue resource olarak ele alın
// Runtime'da bağlantıları konfigüre et

if (!builder.Environment.IsEnvironment("Testing"))
{
  // Production: Gerçek SQL Server
    builder.Services.AddDbContext<ProductDbContext>(opt =>
        opt.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
    
    // Production: Gerçek Redis
    services.AddScoped<ICacheService, RedisCacheService>();
}
else
{
    // Testing: InMemory DB
    services.AddDbContext<ProductDbContext>(opt =>
        opt.UseInMemoryDatabase("TestDb"));
    
    // Testing: Null Cache
    services.AddScoped<ICacheService, NullCacheService>();
}
```

#### 5. Build/Run/Release
```
Build: dotnet build
Run: dotnet run
Release: Docker image ve deployment
```

#### 6. Processes
```
✅ Stateless architecture
   - Her request bağımsız işlenebilir
   - Session state kullanılmaz
   - Horizontal scaling mümkün
```

#### 7. Port Binding
```csharp
// Uygulamalar self-contained HTTP servers
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Port'u environment'tan oku
var port = Environment.GetEnvironmentVariable("PORT") ?? "5001";
await app.RunAsync($"http://0.0.0.0:{port}");
```

#### 8. Concurrency
```
Process Type | Instances | Port
─────────────┼──────────┼─────
API          | 3        | 5001-5003
Worker       | 2        | -
```

#### 9. Disposability
```csharp
// Fast startup
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Graceful shutdown
await app.RunAsync();
```

#### 10. Dev/Prod Parity
```csharp
// Same code, different config
if (builder.Environment.IsEnvironment("Testing"))
{
    // Testing: InMemory
    services.AddDbContext<ProductDbContext>(opt =>
opt.UseInMemoryDatabase("TestDb"));
}
else if (builder.Environment.IsDevelopment())
{
    // Dev: LocalDB
    services.AddDbContext<ProductDbContext>(opt =>
  opt.UseSqlServer("Data Source=(localdb)\\mssqllocaldb;..."));
}
else
{
    // Production: Azure SQL / Production DB
    services.AddDbContext<ProductDbContext>(opt =>
        opt.UseSqlServer(configuration.GetConnectionString("ProductionDb")));
}
```

#### 11. Logs
```csharp
// Stdout'a yaz; centralized logging çözüm tarafından topla
builder.Logging.AddConsole();

// Örnek log output
_logger.LogInformation("[CQRS] Başlıyor: {RequestName} {@Request}", 
    requestName, request);

// Docker/Kubernetes tarafından toplanır
// Seq, ELK, Application Insights gibi araçlarla analiz edilir
```

#### 12. Admin Processes
```
Migration çalıştırma:
$ dotnet ef database update

Seed data:
$ dotnet run --seed

Farklı environment'larda çalıştırma:
$ ASPNETCORE_ENVIRONMENT=Production dotnet run
```

### Faydaları
✅ **Scalability**: Horizontal scaling kolay  
✅ **Maintainability**: Dev/Prod aynı kod  
✅ **Reliability**: Stateless tasarım hata toleransı  
✅ **Consistency**: Tüm ortamlarda aynı davranış  

---

## 📊 Pattern Özeti Tablosu

| Pattern | Katman | Amaç | Faydası |
|---------|--------|------|---------|
| **CQRS** | Application | Komut/Sorgu ayrımı | Performance, clarity |
| **MediatR** | Application | Request mediation | Loose coupling |
| **Repository** | Infrastructure | DB abstraction | Testability |
| **DI** | All | Dependency injection | Flexibility |
| **Clean Architecture** | All | Layered design | Maintainability |
| **Pipeline/Middleware** | Application/API | Request processing | SRP, OCP |
| **Factory** | Domain/Infrastructure | Object creation | Encapsulation |
| **Strategy** | Infrastructure | Algorithm selection | Runtime flexibility |
| **DDD** | Domain | Business logic | Domain focus |
| **Exception Handling** | API | Global error handling | Consistency |
| **Options Pattern** | API | Configuration | Type safety |
| **API Gateway** | Gateway | Route management | Single entry point |
| **Rate Limiting** | Gateway | Request limiting | Protection |
| **Health Checks** | Infrastructure | Status monitoring | Reliability |
| **12-Factor** | All | Best practices | Scalability |

---

## 🔄 Request Flow (Tam Örnek)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. CLIENT REQUEST                 │
│ POST /api/products      │
│ Authorization: Bearer eyJhbG...              │
│ Body: { "name": "Laptop", "price": 1000 }         │
└──────────────────────┬──────────────────────────────────────────┘
     ↓
┌──────────────────────────────────────────────────────────────────┐
│ 2. GATEWAY (Port 5000) - YARP Reverse Proxy    │
│ ✓ Rate Limiter: 100 req/min policy check       │
│ ✓ JWT Authentication: Token validation               │
│ ✓ Authorization: User permissions check             │
│ → Route: /api/products/** → ProductService cluster (5002)       │
└──────────────────────┬──────────────────────────────────────────┘
               ↓
┌──────────────────────────────────────────────────────────────────┐
│ 3. PRODUCTSERVICE API (Port 5002)    │
│ → ExceptionHandlingMiddleware (global catch)         │
│ → Authentication middleware (JWT validation)    │
│ → Authorization middleware (role check)          │
│ → Route Handler: ProductController.CreateProduct()  │
└──────────────────────┬──────────────────────────────────────────┘
                 ↓
┌──────────────────────────────────────────────────────────────────┐
│ 4. CONTROLLER        │
│ var command = new AddProductCommand(               │
│     request.Name, request.Description, request.Price, ...)  │
│ var result = await _mediator.Send(command);          │
└──────────────────────┬──────────────────────────────────────────┘
            ↓
┌──────────────────────────────────────────────────────────────────┐
│ 5. MEDIATR PIPELINE         │
│ ① LoggingBehavior: Log request start       │
│ ② ValidationBehavior:       │
│    - Run AddProductCommandValidator          │
│    - Check: Name != empty, Price >= 0, Stock >= 0     │
│    - If fails: throw ValidationException → 400 Bad Request │
│ ③ AddProductCommandHandler: Process command         │
└──────────────────────┬──────────────────────────────────────────┘
       ↓
┌──────────────────────────────────────────────────────────────────┐
│ 6. HANDLER - CQRS COMMAND PROCESSING        │
│          │
│ a) Domain Factory:      │
│    var product = Product.Create(name, description, price);     │
│    - Validates: name != null, price >= 0, stock >= 0      │
│    - Raises: ProductAddedEvent            │
│      │
│ b) Repository (Strategy Pattern):          │
│    await _repository.AddAsync(product, ct);        │
│    - Real: SQL Server INSERT    │
│    - Test: InMemory store INSERT     │
│     │
│ c) Event Publishing (Strategy Pattern):         │
│    await _eventPublisher.PublishAsync(productAddedEvent, ct);  │
│    - Real: RabbitMQ/Kafka publish          │
│    - Test: Null publisher (no-op) │
│              │
│ d) Cache (Strategy Pattern):           │
│    await _cache.SetAsync($"product_{id}", dto, expiry);        │
│    - Real: Redis SET             │
│    - Test: Null cache (no-op)     │
│         │
│ e) Return DTO:         │
│  return new ProductDto { Id, Name, Price, ... }; │
└──────────────────────┬──────────────────────────────────────────┘
         ↓
┌──────────────────────────────────────────────────────────────────┐
│ 7. RESPONSE PIPELINE  │
│ ✓ LoggingBehavior: Log elapsed time (500ms)           │
│ ✓ Handler returns: ProductDto         │
│ ✓ Controller returns: Ok(productDto) → 200   │
└──────────────────────┬──────────────────────────────────────────┘
            ↓
┌──────────────────────────────────────────────────────────────────┐
│ 8. EXCEPTION HANDLING (if error occurred)        │
│         │
│ ProductNotFoundException         │
│ → DomainException(statusCode: 404)           │
│ → ExceptionHandlingMiddleware catch │
│ → ProblemDetails (RFC 7807)  │
│ → JSON response  │
│ {    │
│     "status": 404,            │
│     "title": "Not Found",    │
│     "detail": "Ürün ID abc123 bulunamadı"    │
│ }                 │
└──────────────────────┬──────────────────────────────────────────┘
   ↓
┌──────────────────────────────────────────────────────────────────┐
│ 9. GATEWAY RESPONSE         │
│ ← ProductService response            │
│ ← Gateway response rewrite       │
│ HTTP/1.1 200 OK                  │
│ Content-Type: application/json            │
│ {    │
│     "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",    │
│     "name": "Laptop",   │
│     "price": 1000,            │
│     ...              │
│ }         │
└──────────────────────┬──────────────────────────────────────────┘
     ↓
┌──────────────────────────────────────────────────────────────────┐
│ 10. CLIENT RECEIVES RESPONSE         │
│ HTTP/1.1 200 OK           │
│ { "id": "...", "name": "Laptop", ... }           │
└──────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Özet

Bu microservices uygulaması **modern .NET best practices**'i kapsamlı olarak uygulamaktadır:

✅ **Architecture**: Clean Architecture + DDD  
✅ **Communication**: CQRS + MediatR  
✅ **Data Access**: Repository Pattern  
✅ **Dependency**: Full DI (Loose Coupling)  
✅ **Validation**: FluentValidation + Pipeline behaviors  
✅ **Error Handling**: Global exception handler (RFC 7807)  
✅ **Cross-Cutting**: Middleware + Pipeline behaviors  
✅ **Configuration**: Options pattern + 12-Factor  
✅ **Gateway**: YARP reverse proxy + Rate limiting  
✅ **Observability**: Logging + Health checks
✅ **Testing**: Strategy pattern allows easy mocking  
✅ **Production**: Stateless, scalable, maintainable  

