# Design Patterns - Visual Diagrams

## 1. Clean Architecture Layers

```
┌──────────────────────────────────────────────────────────────────────┐
│         PRESENTATION LAYER   │
│        (API / HTTP Controllers)           │
│ │
│  POST /api/products  →  ProductsController.Create(Request)           │
│  GET /api/products/{id}  →  ProductsController.GetById(Query)        │
└────────────────────────────┬─────────────────────────────────────────┘
     ↓
┌──────────────────────────────────────────────────────────────────────┐
│         APPLICATION LAYER      │
│        (CQRS / Business Logic)          │
│ │
│  ┌─ Commands ────────────────────────┐            │
│  │ - AddProductCommand      │           │
│  │ - UpdateProductCommand │            │
│  │ - LoginCommand        │    │
│  │ - RegisterCommand      │             │
│  └───────────────────────────────────┘         │
│    │
│  ┌─ Queries ─────────────────────────┐         │
│  │ - GetProductByIdQuery  │        │
│  │ - GetProductsQuery                │     │
│  │ - GetUserByEmailQuery             │         │
│  └───────────────────────────────────┘   │
│        │
│  ┌─ Handlers (MediatR) ───────────────────────────────────────────┐ │
│  │ - AddProductCommandHandler → IRequestHandler<Command>        │ │
│  │ - GetProductByIdQueryHandler → IRequestHandler<Query> │ │
│  └────────────────────────────────────────────────────────────────┘ │
│         │
│  ┌─ Pipeline Behaviors ──────────────────────────────────────────┐  │
│  │ - LoggingBehavior     (Request/Response logging)             │  │
│  │ - ValidationBehavior  (FluentValidation)          │  │
│  └────────────────────────────────────────────────────────────────┘  │
└────────────────────────────┬─────────────────────────────────────────┘
         ↓
┌──────────────────────────────────────────────────────────────────────┐
│     DOMAIN LAYER          │
│       (Business Rules / Entities)    │
│  │
│  ┌─ Entities ────────────────────────┐    │
│  │ - Product (Root Aggregate)        │              │
│  │ - AppUser     │          │
│  │ - RefreshToken        │      │
│  └───────────────────────────────────┘ │
│             │
│  ┌─ Domain Events ──────────────────┐      │
│  │ - ProductAddedEvent              │     │
│  │ - ProductUpdatedEvent       │    │
│  │ - UserRegisteredEvent            │              │
│  └──────────────────────────────────┘      │
│      │
│  ┌─ Exceptions ─────────────────────┐      │
│  │ - DomainException (base)         │      │
│  │ - ProductNotFoundException     │     │
│  │ - AuthException    │     │
│  └──────────────────────────────────┘            │
│        │
│  Domain Invariants:   │
│  - Price >= 0    │
│  - Stock >= 0             │
│  - Name must not be empty          │
│  - Email must be valid           │
└────────────────────────────┬─────────────────────────────────────────┘
           ↓
┌──────────────────────────────────────────────────────────────────────┐
│             INFRASTRUCTURE LAYER      │
│              (Persistence / External Services)   │
│      │
│  ┌─ Repositories ────────────────────────────────────────────────┐  │
│  │ - IProductRepository ← ProductRepository              │  │
│  │ - IRefreshTokenRepository ← RefreshTokenRepository     │  │
│  └─────────────────────────────────────────────────────────────────┘  │
│              │
│  ┌─ Database (EF Core) ────────────────────────────────────────┐   │
│  │ - SqlServer (Production)   │   │
│  │ - InMemory (Testing)    │   │
│  │ - LocalDB (Development)         │   │
│  └─────────────────────────────────────────────────────────────┘   │
│             │
│  ┌─ Services ─────────────────────────────────────────────────┐    │
│  │ - ITokenService ← TokenService                │    │
│  │ - ICacheService ← RedisCacheService | NullCacheService   ││
│  │ - IEventPublisher ← RabbitMqEventPublisher | KafkaEventPub│  │
│  └────────────────────────────────────────────────────────────┘    │
│   │
│  ┌─ DbContext ────────────────────────────────────────────────┐    │
│  │ - ProductDbContext → OnModelCreating(), Migrations        │    │
│  │ - AuthDbContext → Identity setup  │    │
│  └────────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────────┘

Dependencies:
- Presentation → Application (MediatR)
- Application → Domain (Interfaces)
- Application → Infrastructure (Interfaces)
- Infrastructure → Domain (Implements)
- ✓ NO circular dependencies
- ✓ Only downward dependencies
```

---

## 2. CQRS Pattern Flow

```
┌─────────────────────────────────────────────────────────────────┐
│         CLIENT REQUEST    │
└─────────────────────────────────────────────────────────────────┘
 ↓
    ┌──────────────────┴──────────────────┐
    ↓↓
   ╔════════════╗ ╔════════════╗
   ║  COMMAND   ║ ║   QUERY    ║
   ║ (CREATE)   ║      ║  (READ)    ║
   ╚════════════╝               ╚════════════╝
      ↓     ↓
   ┌─────────────────┐       ┌──────────────────┐
   │ AddProductCmd   │   │ GetProductQuery  │
   │ UpdateProductCmd│        │ GetProductsQuery │
   │ LoginCommand    │ │ GetUserQuery     │
   │ RegisterCommand │              │ GetOrdersQuery   │
   └────────┬────────┘         └────────┬─────────┘
    ↓ ↓
   ┌─────────────────────────────────┐  ┌───────────────────────────┐
   │   COMMAND HANDLER      │  │    QUERY HANDLER          │
   │     │  │      │
   │ 1. Validate input          │  │ 1. Load from cache        │
   │ 2. Check permissions    │  │ 2. If miss: load from DB  │
   │ 3. Apply domain logic          │  │ 3. Serialize to DTO      │
   │ 4. Persist to DB   │  │ 4. Return DTO            │
   │ 5. Raise events     │  │     │
   │ 6. Publish events        │  │ (No side effects)         │
   │ 7. Invalidate cache  │  │           │
 │            │  │      │
   │ Side Effects: YES ✓         │  │ Side Effects: NO ✗       │
   │ Database Write: YES  │  │ Database Write: NO    │
   │ Optimization: Complex  │  │ Optimization: Caching    │
   └────────┬────────────────────────┘  └────────┬──────────────────┘
         ↓          ↓
   ┌─────────────────────────────────┐  ┌───────────────────────────┐
   │  DOMAIN OPERATIONS    │  │  READ OPERATIONS   │
│        │  │     │
   │ • Entity.Create() - Factory     │  │ • Direct DB query         │
   │ • Entity.Update() - Methods     │  │ • Report views         │
   │ • Validate invariants           │  │ • Denormalized data  │
   │ • Raise domain events           │  │ • Optimized indexes       │
   └────────┬────────────────────────┘  └────────┬──────────────────┘
            ↓↓
   ┌─────────────────────────────────┐  ┌───────────────────────────┐
   │  PERSISTENCE   │  │  CACHE         │
   │         │  │        │
   │ • Save aggregate root  │  │ • Read-optimized views    │
   │ • Publish events          │  │ • Query results cached    │
   │ • Update state        │  │ • TTL: 1 hour     │
   └────────┬────────────────────────┘  └────────┬──────────────────┘
            ↓           ↓
   ┌─────────────────────────────────┐  ┌───────────────────────────┐
   │  RESPONSE   │  │  RESPONSE        │
   │  DTO / 200 OK    │  │  Cached DTO / 200 OK      │
 └─────────────────────────────────┘  └───────────────────────────┘

Benefit:
• Commands scaled differently from Queries
• Query performance via caching/indexing
• Write consistency critical
• Read performance optimized
```

---

## 3. Dependency Injection & Interfaces

```
┌─────────────────────────────────────────────────────────────────┐
│                 HANDLER               │
│  (depends on interfaces, not implementations)           │
│         │
│  public class AddProductCommandHandler      │
│  {              │
│      private readonly IProductRepository _repository;  ────┐   │
│      private readonly IEventPublisher _eventPublisher; ─┐  │   │
│      private readonly ICacheService _cache;         ┐ │  │   │
│            │ │ │  │   │
│      public AddProductCommandHandler(      │ │ │  │   │
│          IProductRepository repo,        │ │ │  │   │
│          IEventPublisher pub, │ │ │  │   │
│          ICacheService cache)       │ │ │  │   │
│      {      │ │ │  │ │
│    _repository = repo;       │ │ │  │   │
│          _eventPublisher = pub;       │ │ │  │   │
│  _cache = cache;   │ │ │  │   │
│      }    │ │ │  │   │
│  } │ │ │  │   │
└─────────────────────────────────────────────────────┼─┼─┼──────┘
          │ │ │
        ┌─────────────────────────────────────────────┘ │ │
        │  ┌──────────────────────────────────────────┘ │
        │  │  ┌───────────────────────────────────────┘
 │  │  │
      ↓  ↓  ↓
┌─────────────────────────────────────────────────────────────────┐
│            DEPENDENCY INJECTION CONTAINER        │
│       │
│ Production Environment:          │
│ ┌──────────────────────────────────────────────────────────┐  │
│ │ IProductRepository → ProductRepository     │  │
│ │ IEventPublisher           → RabbitMqEventPublisher      │  │
│ │ ICacheService             → RedisCacheService    │  │
│ └──────────────────────────────────────────────────────────┘  │
│    │
│ Testing Environment:             │
│ ┌──────────────────────────────────────────────────────────┐  │
│ │ IProductRepository      → Mock<IProductRepository>    │  │
│ │ IEventPublisher    → NullEventPublisher  │  │
│ │ ICacheService          → NullCacheService            │  │
│ └──────────────────────────────────────────────────────────┘  │
│   │
│ Development Environment:    │
│ ┌──────────────────────────────────────────────────────────┐  │
│ │ IProductRepository        → ProductRepository (LocalDB) │  │
│ │ IEventPublisher     → NullEventPublisher          │  │
│ │ ICacheService             → NullCacheService        │  │
│ └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
 ↓
        ┌───────────────┼───────────────┐
        ↓ ↓             ↓
┌──────────────┐  ┌─────────────┐  ┌────────────┐
│ Repository   │  │ Event Pub   │  │ Cache  │
│     │  │           │  │ │
│ Prod:        │  │ Prod:       │  │ Prod:      │
│ • SqlServer  │  │ • RabbitMQ  │  │ • Redis  │
│ • EF Core    │  │ • Kafka     │  │ • StackExch│
│              │  │             │  │      │
│ Test:        │  │ Test:       │  │ Test:      │
│ • InMemory   │  │ • Null (no) │  │ • Null(no) │
│ • Mock       │  │  │  │ • Mock     │
└──────────────┘  └─────────────┘  └────────────┘

Program.cs Registration:
services.AddScoped<IProductRepository, ProductRepository>();
services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
services.AddScoped<ICacheService, RedisCacheService>();

// Handler otomatik constructed:
var handler = serviceProvider.GetRequiredService<AddProductCommandHandler>();
// Constructor'a tüm dependencies otomatik enjekte edilir ✓
```

---

## 4. MediatR Pipeline Execution

```
┌───────────────────────────────────────────────────────────────────┐
│         CLIENT REQUEST                 │
│  POST /api/products { name: "Laptop", price: 1000 }        │
└───────────────────────┬─────────────────────────────────────────┘
      ↓
┌───────────────────────────────────────────────────────────────────┐
│   CONTROLLER                 │
│  var cmd = new AddProductCommand(...)           │
│  var result = await _mediator.Send(cmd);  │
└───────────────────────┬─────────────────────────────────────────┘
           ↓
┌─────────────────────────────────────────────────────────────────┐
│     MEDIATR PIPELINE       │
│    │
│  ╔═══════════════════════════════════════════════════════════╗ │
│  ║ 1️⃣  LOGGING BEHAVIOR (IPipelineBehavior<,>)         ║ │
│  ║         ║ │
│  ║  Enter: "[CQRS] Başlıyor: AddProductCommand"     ║ │
│  ║  Start Timer                   ║ │
│  ║  ✓ RequestHandlerDelegate next() çağır       ║ │
│  ╚═══════════════════════════════════════════════════════════╝ │
│     ↓         │
│  ╔═══════════════════════════════════════════════════════════╗ │
│  ║ 2️⃣  VALIDATION BEHAVIOR (IPipelineBehavior<,>)    ║ │
│  ║   ║ │
│  ║  Get Validators for AddProductCommand:                  ║ │
│  ║  • AddProductCommandValidator          ║ │
│  ║        ║ │
│  ║  Validate:          ║ │
│  ║  • Name not empty? ✓  ║ │
│  ║  • Price >= 0? ✓              ║ │
│  ║  • Stock >= 0? ✓ ║ │
│  ║   ║ │
│  ║  if (failures.Any())         ║ │
│  ║    throw new ValidationException(failures);          ║ │
│  ║        ║ │
│  ║  ✓ RequestHandlerDelegate next() çağır            ║ │
│  ╚═══════════════════════════════════════════════════════════╝ │
│   ↓       │
│  ╔═══════════════════════════════════════════════════════════╗ │
│  ║ 3️⃣  ADD PRODUCT COMMAND HANDLER      ║ │
│  ║     (IRequestHandler<AddProductCommand, ProductDto>)     ║ │
│  ║      ║ │
│  ║  Handle(command, cancellationToken):      ║ │
│  ║          ║ │
│  ║  a) Create Product (Domain Factory)          ║ │
│  ║     var product = Product.Create(...)       ║ │
│  ║     - Validate: name, price >= 0, stock >= 0            ║ │
│  ║     - Id = Guid.NewGuid()    ║ │
│  ║     - CreatedAt = DateTime.UtcNow  ║ │
│  ║          ║ │
│  ║  b) Save to Repository        ║ │
│  ║   await _repository.AddAsync(product, ct)      ║ │
│  ║     - ProductDbContext.Products.AddAsync(product)        ║ │
│  ║     - SaveChangesAsync() to Database     ║ │
│  ║       ║ │
│  ║  c) Publish Event         ║ │
│  ║     var evt = new ProductAddedEvent(product.Id, ...)     ║ │
│  ║   await _eventPublisher.PublishAsync(evt, ct)          ║ │
│  ║  - RabbitMQ: Declare exchange, publish message ║ │
│  ║     - Other services subscribe: products created    ║ │
│  ║        ║ │
│  ║  d) Cache     ║ │
│  ║     var dto = MapToDto(product)      ║ │
│  ║     await _cache.SetAsync($"product_{id}", dto, 1h, ct) ║ │
│  ║     - Redis: SET with TTL     ║ │
│  ║            ║ │
│  ║  e) Return DTO        ║ │
│  ║     return dto;  // ProductDto { Id, Name, Price, ... }  ║ │
│  ╚═══════════════════════════════════════════════════════════╝ │
│ ↓     │
│  ╔═══════════════════════════════════════════════════════════╗ │
│  ║ 2️⃣  VALIDATION BEHAVIOR (Exit)     ║ │
│  ║  • No errors         ║ │
│  ║  • Continue with next      ║ │
│  ╚═══════════════════════════════════════════════════════════╝ │
│      ↓         │
│  ╔═══════════════════════════════════════════════════════════╗ │
│  ║ 1️⃣  LOGGING BEHAVIOR (Exit)            ║ │
│  ║  • Elapsed Time: 250ms    ║ │
│  ║  • Status: Success ✓      ║ │
│  ║  • Return: ProductDto ║ │
│  ║  ║ │
│  ║  Log: "[CQRS] Tamamlandı: AddProductCommand (250ms)"    ║ │
│  ╚═══════════════════════════════════════════════════════════╝ │
└─────────────────────────────────────────────────────────────────┘
        ↓
┌───────────────────────────────────────────────────────────────────┐
│                    CONTROLLER  │
│  return CreatedAtAction(nameof(GetProduct), ...);         │
│  HTTP Status: 201 Created   │
│  Location: /api/products/{id}         │
│  Body: { id, name, price, ... }   │
└───────────────────────┬─────────────────────────────────────────┘
     ↓
┌───────────────────────────────────────────────────────────────────┐
│       HTTP RESPONSE      │
│  HTTP/1.1 201 Created               │
│  Content-Type: application/json         │
│  Location: /api/products/f47ac10b-58cc-4372-a567-0e02b2c3d479    │
│  Body:         │
│  {  │
│    "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",     │
│    "name": "Laptop",         │
│    "price": 1000, │
│  "stock": 5,        │
│    "isActive": true   │
│  }   │
└───────────────────────────────────────────────────────────────────┘

Exception Handling Flow:
┌──────────────────────────────────────┐
│ Validation Fails (price: -100)      │
└──────────────────┬───────────────────┘
       ↓
        ValidationBehavior exception:
        throw new ValidationException({
            { "price", ["Price must be >= 0"] }
      })
              ↓
        LoggingBehavior catch:
 _logger.LogError(ex, ...)
        re-throw exception
      ↓
ExceptionHandlingMiddleware:
  catch (ValidationException ex)
        → 400 Bad Request
        → RFC 7807 ProblemDetails
→ JSON response
```

---

## 5. Strategy Pattern for Services

```
┌──────────────────────────────────────────────────────────────────┐
│         INTERFACE (Abstraction)    │
│       │
│  public interface ICacheService        │
│  {      │
│  Task<T?> GetAsync<T>(string key, CancellationToken ct);   │
│      Task SetAsync<T>(string key, T value, TimeSpan? expiry);  │
│      Task RemoveAsync(string key, CancellationToken ct);       │
│  }      │
└──────────────────────────────────────────────────────────────────┘
   ↑
         ┌─────────────────┼─────────────────┐
      │      │     │
    Production         Development  Testing
       │  │             │
   ↓     ↓     ↓
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│RedisCacheServ│  │NullCacheSer  │  │NullCacheSer  │
│  VICE     │  │  VICE   │  │  VICE        │
├──────────────┤  ├──────────────┤  ├──────────────┤
│GetAsync:     │  │GetAsync:     ││GetAsync:     │
│ -Redis GET   │  │ return null  │  │ return null  │
│ -Deserialize │  │     │  │              │
│      │  │SetAsync:     │  │SetAsync:     │
│SetAsync:     │  │ noop │  │ noop    │
│ -Serialize   │  │    │  │          │
│ -Redis SET   │  │RemoveAsync:  │  │RemoveAsync:  │
│ -TTL expiry  │  │ noop      │  │ noop         │
│          │  │    │  │              │
│RemoveAsync:  │  │   │  │       │
│ -Redis DEL   │  │         │  │              │
└──────────────┘  └──────────────┘  └──────────────┘

Runtime Selection (DI):
┌──────────────────────────────────────────────────────┐
│  Program.cs        │
│        │
│  var isTesting = env.IsEnvironment("Testing");      │
│   │
│  if (!isTesting)     │
│  {│
│    // Production: Use Redis         │
│    services.AddScoped<ICacheService,    │
│        RedisCacheService>();          │
│  }      │
│  else                 │
│  {      │
│    // Testing: Use Null (no side effects)          │
│    services.AddScoped<ICacheService, │
│        NullCacheService>();  │
│  }       │
└──────────────────────────────────────────────────────┘

Handler (Unaware of Implementation):
┌──────────────────────────────────────────────────────┐
│  Handler       │
│       │
│  public AddProductCommandHandler(             │
│      ICacheService cache)  // Abstraction only!     │
│  {         │
│ _cache = cache;        │
│  }         │
│   │
│  Handle(request, ct):          │
│  {      │
│// Which impl? Handler doesn't care!             │
│ await _cache.SetAsync("key", dto, 1h, ct);      │
││
│    // At runtime:         │
│    // Production → Redis.SetAsync()            │
│    // Testing → NullCacheService.SetAsync() (noop)  │
│  }         │
└──────────────────────────────────────────────────────┘
```

---

## 6. Request Flow Through All Layers

```
┌──────────────────────────────────────────────────────────────────┐
│ CLIENT        │
│ POST /api/products           │
│ Authorization: Bearer token...       │
│ Content-Type: application/json        │
│ Body: { "name": "Laptop", "price": 1500, "stock": 5 }  │
└──────────────────────┬──────────────────────────────────────────┘
 ↓
┌──────────────────────────────────────────────────────────────────┐
│ API LAYER (Gateway - YARP Reverse Proxy)   │
│        │
│ 1. Rate Limiter: 100 requests/min
│    → Check: IP not exceeded? ✓   │
│       │
│ 2. JWT Authentication: Validate token                 │
│    → Verify signature, expiry, issuer ✓  │
│        │
│ 3. Authorization: Check permissions      │
│    → User has required roles? ✓     │
│      │
│ 4. Route to backend (YARP):                  │
│    /api/products/** → ProductService cluster       │
└──────────────┬───────────────────────────────────────────────────┘
           ↓
┌──────────────────────────────────────────────────────────────────┐
│ PRESENTATION LAYER (ProductService.API)           │
│     │
│ ProductsController.Create(CreateProductRequest)      │
││
│ Request Deserialization:     │
│ { "name": "Laptop", "price": 1500, "stock": 5 }       │
│  ↓        │
│ CreateProductRequest dto │
└──────────────┬───────────────────────────────────────────────────┘
    ↓
┌──────────────────────────────────────────────────────────────────┐
│ APPLICATION LAYER (CQRS + MediatR)          │
│       │
│ Controller: CreateProductCommand cmd =                │
│   new(request.Name, request.Description, ...);     │
│         │
│ IMediator.Send(cmd)          │
│    ↓      │
│ ┌─ LOGGING BEHAVIOR ─────────────────────────────────────────┐ │
│ │ Log: "[CQRS] Başlıyor: AddProductCommand"              │ │
│ │ Start: startTime = DateTime.UtcNow     │ │
│ │ Request: AddProductCommand { ... }       │ │
│ └────────────────┬──────────────────────────────────────────┘ │
│           ↓  │
│ ┌─ VALIDATION BEHAVIOR ──────────────────────────────────────┐ │
│ │ Get Validators:          │ │
│ │ • AddProductCommandValidator.Validate(cmd)             │ │
│ │   │ │
│ │ Rules:     │ │
│ │ • Name != null && !empty ✓          │ │
│ │ • Description not empty ✓ │ │
│ │ • Price >= 0 ✓      │ │
│ │ • Stock >= 0 ✓            │ │
│ │          │ │
│ │ if (failures.Any()) → throw ValidationException   │ │
│ └────────────────┬──────────────────────────────────────────┘ │
│        ↓            │
│ ┌─ ADD PRODUCT COMMAND HANDLER ──────────────────────────────┐ │
│ │ Handle(cmd, cancellationToken)     │ │
│ │          │ │
│ │ DI resolved:   │ │
│ │ • IProductRepository _repository           │ │
│ │ • IEventPublisher _eventPublisher   │ │
│ │ • ICacheService _cache        │ │
│ └────────────────┬──────────────────────────────────────────┘ │
└──────────────────┼──────────────────────────────────────────────┘
       ↓
┌──────────────────────────────────────────────────────────────────┐
│ DOMAIN LAYER (Business Rules)      │
│  │
│ Factory Method:        │
│ var product = Product.Create(          │
│     name: "Laptop",          │
│     description: "Gaming laptop",         │
│     price: 1500,         │
│     stock: 5)    │
│    │
│ Validation (Domain Invariants):          │
│ ✓ Name not null/empty              │
│ ✓ Price >= 0    │
│ ✓ Stock >= 0        │
│    │
│ Entity Created:     │
│ Product {            │
│   Id: Guid.NewGuid(),│
│   Name: "Laptop",     │
│   Description: "Gaming laptop",          │
│   Price: 1500,     │
│ Stock: 5,          │
│   IsActive: true,     │
│   CreatedAt: DateTime.UtcNow,         │
│   DomainEvents: [ProductAddedEvent] │
│ }         │
└──────────────────┬──────────────────────────────────────────────┘
      ↓
┌──────────────────────────────────────────────────────────────────┐
│ INFRASTRUCTURE LAYER (Persistence)           │
│             │
│ Repository:           │
│ await _repository.AddAsync(product, ct)                │
│    ↓         │
│ DbContext:          │
│ _context.Products.AddAsync(product)       │
│ _context.SaveChangesAsync(ct)        │
│    ↓            │
│ Database (SQL Server):  │
│ INSERT INTO Products │
│ (Id, Name, Description, Price, Stock, IsActive, CreatedAt)     │
│ VALUES     │
│ ('guid', 'Laptop', 'Gaming...', 1500, 5, 1, '2025-01-15...')   │
│    ↓      │
│ ✓ 1 row inserted       │
└──────────────────┬──────────────────────────────────────────────┘
        ↓
┌──────────────────────────────────────────────────────────────────┐
│ EVENT PUBLISHING (Domain Events)              │
│           │
│ Event:      │
│ new ProductAddedEvent(     │
│     ProductId: guid,   │
│     Name: "Laptop",   │
│     Price: 1500,        │
│     Stock: 5,                  │
│     CreatedAt: DateTime.UtcNow)            │
│      │
│ Strategy (IEventPublisher):  │
│ await _eventPublisher.PublishAsync(evt, ct)          │
│    ↓ (Prod → RabbitMQ)    │
│ RabbitMQ:                 │
│ • Exchange: ProductAddedEvent        │
│ • Routing Key: {guid} │
│ • Message: { id, name, price, stock, createdAt }   │
│    ↓             │
│ Other Services Subscribe:   │
│ • Order Service → Update inventory       │
│ • Analytics Service → Record event        │
│ • Notification Service → Send email  │
└──────────────────┬──────────────────────────────────────────────┘
           ↓
┌──────────────────────────────────────────────────────────────────┐
│ CACHING (Strategy Pattern)   │
│     │
│ Cache:     │
│ await _cache.SetAsync(         │
│key: "product_{guid}",  │
│     value: ProductDto { id, name, price, stock, isActive },    │
│     expiry: TimeSpan.FromHours(1),          │
│     cancellationToken)           │
│    ↓ (Prod → Redis)           │
│ Redis:             │
│ SET product_{guid} {serialized dto} EX 3600      │
│    ↓        │
│ (Test → NullCacheService.SetAsync = noop)          │
└──────────────────┬──────────────────────────────────────────────┘
     ↓
┌──────────────────────────────────────────────────────────────────┐
│ RESPONSE MAPPING          │
│    │
│ Handler returns:       │
│ ProductDto {          │
│   Id: guid,      │
│   Name: "Laptop",         │
│   Description: "Gaming laptop",   │
│   Price: 1500,           │
│   Stock: 5,    │
│   IsActive: true     │
│ }     │
│    ↓       │
│ Pipeline returns to Controller        │
│    ↓     │
│ Controller returns:       │
│ CreatedAtAction(nameof(GetProduct),             │
│   new { id = dto.Id },       │
│   dto)      │
└──────────────────┬──────────────────────────────────────────────┘
  ↓
┌──────────────────────────────────────────────────────────────────┐
│ HTTP RESPONSE (201 Created)              │
│            │
│ HTTP/1.1 201 Created     │
│ Content-Type: application/json          │
│ Location: /api/products/f47ac10b-58cc-4372-a567-0e02b2c3d479   │
│ X-Elapsed-Ms: 250              │
│   │
│ Body: │
│ {    │
│   "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",  │
│   "name": "Laptop",      │
│   "description": "Gaming laptop", │
│   "price": 1500,    │
│   "stock": 5,     │
│   "isActive": true              │
│ }               │
└──────────────┬───────────────────────────────────────────────────┘
     ↓
┌──────────────────────────────────────────────────────────────────┐
│ CLIENT RECEIVES RESPONSE    │
│        │
│ Status: 201 Created ✓     │
│ Resource Location: /api/products/{id}   │
│ Data: ProductDto   │
│            │
│ Client can now:       │
│ • Navigate to /api/products/{id} to get full product│
│ • Update the product at PUT /api/products/{id}        │
│ • Delete the product at DELETE /api/products/{id}          │
└──────────────────────────────────────────────────────────────────┘

Total Flow Time: ~250ms
• Validation: 5ms
• DB Insert: 50ms
• Event Publish: 100ms
• Cache Write: 30ms
• Response Serialization: 20ms
• Network: 45ms
```

---

## 7. Exception Handling Flow (RFC 7807)

```
Handler Exception:

1. ValidationException (FluentValidation)
   ↓
   ValidationBehavior throws:
   throw new ValidationException({
       { "price", ["Price must be >= 0"] },
       { "stock", ["Stock must be >= 0"] }
   })
   ↓
   ExceptionHandlingMiddleware catches
   ↓
   HTTP 400 Bad Request
   Content-Type: application/problem+json
   {
       "type": "https://example.com/problems/validation-error",
       "title": "Validation Error",
       "status": 400,
       "errors": {
           "price": ["Price must be >= 0"],
           "stock": ["Stock must be >= 0"]
       },
       "instance": "/api/products"
   }

2. DomainException (ProductNotFoundException)
   ↓
   Handler throws:
   throw ProductNotFoundException.ById(productId)
   ↓
   ExceptionHandlingMiddleware catches
   ↓
   HTTP 404 Not Found
   Content-Type: application/problem+json
   {
       "type": "https://example.com/problems/not-found",
       "title": "Not Found",
  "status": 404,
     "detail": "Ürün ID abc123 bulunamadı",
       "instance": "/api/products/abc123"
   }

3. AuthException (InvalidCredentials)
   ↓
   Handler throws:
   throw AuthException.InvalidCredentials()
   ↓
   ExceptionHandlingMiddleware catches
   ↓
   HTTP 401 Unauthorized
   Content-Type: application/problem+json
   {
       "type": "https://example.com/problems/unauthorized",
       "title": "Unauthorized",
 "status": 401,
       "detail": "Geçersiz e-posta veya şifre",
       "instance": "/api/auth/login"
 }

4. Framework Exception (ArgumentException)
   ↓
   ExceptionHandlingMiddleware catches
   ↓
   HTTP 400 Bad Request
   {
       "type": "https://example.com/problems/bad-request",
    "title": "Bad Request",
       "status": 400,
       "detail": "Argument exception message",
       "instance": "/api/products"
   }

5. Unhandled Exception (anything else)
   ↓
   ExceptionHandlingMiddleware catches
   ↓
   HTTP 500 Internal Server Error
   {
       "type": "https://example.com/problems/internal-error",
       "title": "Internal Server Error",
       "status": 500,
       "detail": "An unexpected error occurred",
       "instance": "/api/products"
   }

All responses:
• RFC 7807 ProblemDetails format
• Consistent structure
• Detailed error information
• Machine-readable type URL
• Logged for analysis
```

---

## 8. 12-Factor App Implementation

```
┌──────────────────────────────────────────────────────────────────┐
│ 12-FACTOR APP METHODOLOGY  │
├──────────────────────────────────────────────────────────────────┤
│            │
│ 1. CODEBASE    │
│    One codebase tracked in version control      │
│    Repository: https://github.com/omof221/Task        │
│    Branch: test/v1.0.0      │
│    ✓ All microservices in one repo       │
│           │
│ 2. DEPENDENCIES      │
│    Explicitly declared in project files           │
│    .csproj: <PackageReference Include="..." />  │
│    ✓ No implicit dependencies │
│       │
│ 3. CONFIG   │
│    Stored in environment variables    │
│    appsettings.json + env override         │
│    ✓ ConnectionStrings__DefaultConnection        │
│    ✓ JwtSettings__SecretKey        │
│    ✓ Messaging__Provider    │
│    │
│ 4. BACKING SERVICES                 │
│    Treat as attached resources             │
│    Production: SqlServer, Redis, RabbitMQ           │
│    Testing: InMemory, NullCache, NullPublisher  │
│    ✓ Can swap implementations without code change      │
│                 │
│ 5. BUILD/RUN/RELEASE       │
│    Build: dotnet build   │
│    Run: dotnet run              │
│    Release: Docker image → deployment          │
│    ✓ Strict separation  │
│           │
│ 6. PROCESSES     │
│    Stateless execution   │
│    No session state in process memory       │
│    ✓ Horizontal scaling possible       │
│          │
│ 7. PORT BINDING                 │
│    Export services as HTTP       │
│    Services: 5001, 5002, 5003 (Gateway)            │
│    ✓ Self-contained servers            │
│        │
│ 8. CONCURRENCY   │
│    Process types can be scaled independently   │
│    AuthService: 3 instances            │
│    ProductService: 3 instances │
│    Gateway: 1 instance (or more)               │
│    ✓ Load balanced      │
│    │
│ 9. DISPOSABILITY                  │
│    Fast startup/shutdown       │
│    Migrations run on startup (dev)       │
│    Graceful shutdown on SIGTERM         │
│    ✓ Responsive to signals   │
│  │
│ 10. DEV/PROD PARITY     │
│     Same code in dev/test/production      │
│     Config differences only (env vars)   │
│     ✓ Minimize surprises    │
│          │
│ 11. LOGS     │
│     Write to stdout (not files)         │
│     Centralized logging (Seq, ELK, Application Insights) │
│     ✓ No log file management             │
│     │
│ 12. ADMIN PROCESSES    │
│     Run as one-off tasks      │
│     dotnet ef database update (migrations) │
│     dotnet run --seed (seed data)        │
│     ✓ Separate from regular processes   │
│             │
└──────────────────────────────────────────────────────────────────┘

Local Development:
┌─────────────────────┐
│ Program.cs       │
│           │
│ env = "Development" │
│ Config from:  │
│ • appsettings.json  │
│ • user secrets      │
│ • env vars          │
│           │
│ Services:  │
│ • LocalDB       │
│ • NullCache         │
│ • NullPublisher     │
└─────────────────────┘

Testing Environment:
┌─────────────────────┐
│ Program.cs          │
│   │
│ env = "Testing"     │
│ Config from:        │
│ • appsettings.json  │
│ • env vars (CI/CD)  │
│              │
│ Services:           │
│ • InMemory DB       │
│ • NullCache         │
│ • NullPublisher  │
└─────────────────────┘

Production Environment:
┌─────────────────────┐
│ Docker Container    │
│    │
│ env = "Production"  │
│ Config from:        │
│ • env vars (secret) │
│ • ConfigMap (K8s)   │
│       │
│ Services:           │
│ • Azure SQL DB      │
│ • Azure Redis       │
│ • RabbitMQ       │
└─────────────────────┘

All three use SAME code, DIFFERENT config ✓
```

