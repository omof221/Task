# 🏗️ Design Patterns Documentation - Index

Bu klasör, **Task Microservices Uygulaması**'nda kullanılan tüm design pattern'ları kapsamlı olarak açıklamaktadır.

---

## 📚 Dokümantasyon Dosyaları

### 1. **DESIGN_PATTERNS_ANALYSIS.md** - Detaylı Analiz
**İçerik:** 15 adet design pattern'ın detaylı açıklaması

Anlatılan Pattern'lar:
- ✅ CQRS Pattern
- ✅ MediatR Mediator Pattern
- ✅ Repository Pattern
- ✅ Dependency Injection (DI) Pattern
- ✅ Clean Architecture (Layered Architecture)
- ✅ Pipeline/Middleware Pattern
- ✅ Factory Pattern
- ✅ Strategy Pattern
- ✅ Domain-Driven Design (DDD)
- ✅ Exception Handling Pattern
- ✅ Options Pattern
- ✅ Reverse Proxy Pattern (API Gateway)
- ✅ Rate Limiting Pattern
- ✅ Health Check Pattern
- ✅ 12-Factor App Methodology

**Ne İçeriyor?**
- Pattern tanımı ve temel konseptler
- Uygulamada nerede kullanıldığı
- Kod örnekleri (.NET 9)
- Faydaları ve avantajları
- Request flow açıklaması

**Okuma Süresi:** ~30 dakika

---

### 2. **DESIGN_PATTERNS_CODE_EXAMPLES.cs** - Çalışan Kod Örnekleri
**İçerik:** Tüm pattern'ların gerçek, çalışan kod örneği

Başlıklar:
- 1. CQRS + MediatR Pattern
- 2. Clean Architecture - Layered Design
- 3. Repository Pattern + Dependency Injection
- 4. Pipeline Behavior Pattern (Cross-cutting Concerns)
- 5. Strategy Pattern (Runtime Algoritma Seçimi)
- 6. Domain-Driven Design (DDD)
- 7. Global Exception Handling (RFC 7807)
- 8. Options Pattern (Configuration)
- 9. Testing with Strategies (Unit & Integration Tests)
- 10. API Gateway Pattern (YARP)
- 11. Health Check Pattern
- 12. Rate Limiting Pattern

**Ne İçeriyor?**
- Copy-paste ready kod snippets
- Prod/Test ortamları
- Mock objects ve integration tests
- Real world examples

**Okuma Süresi:** ~20 dakika

---

### 3. **DESIGN_PATTERNS_VISUAL_DIAGRAMS.md** - Görsel Diyagramlar
**İçerik:** ASCII art ve flow diagram'ları ile görsel açıklamalar

Diyagramlar:
- 1. Clean Architecture Layers (5 katman visual)
- 2. CQRS Pattern Flow (yazma vs okuma)
- 3. Dependency Injection & Interfaces
- 4. MediatR Pipeline Execution (adım adım)
- 5. Strategy Pattern for Services (prod/test seçimi)
- 6. Complete Request Flow (tüm katmanlar)
- 7. Exception Handling Flow (RFC 7807)
- 8. 12-Factor App Implementation

**Ne İçeriyor?**
- ASCII diagram'ları (terminal uyumlu)
- Request flow görselleştirmesi
- Pipeline sırası ve data akışı
- Dependency relationships

**Okuma Süresi:** ~15 dakika

---

### 4. **DESIGN_PATTERNS_QUICK_REFERENCE.md** - Hızlı Referans
**İçerik:** Kısa, özet ve pratik rehber

Bölümler:
- 🔄 Desenleri Hızlı Bulma Rehberi
- 🔍 Design Pattern Seçme Rehberi
- ✅ Pattern Checklist
- 📖 Pattern → Dosya Eşleşmesi
- 🚀 Quick Start: Yeni Feature Ekleme
- 📚 Referans Linkler
- 💡 İpuçları

**Ne İçeriyor?**
- Her pattern'ın 30 saniye özeti
- Ne zaman hangi pattern'ı seçer
- Dosya lokasyonları
- Sorunu çözme flow'ları
- Developer checklist

**Okuma Süresi:** ~5 dakika

---

## 🎯 Okuma Stratejileri

### Seçenek 1: Yeni başlayan developer
1. Önce **DESIGN_PATTERNS_QUICK_REFERENCE.md** oku (5 min)
2. Sonra **DESIGN_PATTERNS_ANALYSIS.md** oku (30 min) - Pattern'ları temelde anla
3. **DESIGN_PATTERNS_VISUAL_DIAGRAMS.md** oku (15 min) - Flow'ları gör
4. **DESIGN_PATTERNS_CODE_EXAMPLES.cs** ile oyna (20 min) - Code yazıp test et

**Toplam:** ~70 dakika

### Seçenek 2: Tecrübeli developer
1. **DESIGN_PATTERNS_QUICK_REFERENCE.md** oku (5 min) - Hızlı özet
2. **DESIGN_PATTERNS_CODE_EXAMPLES.cs** oku (15 min) - Kod örnekleri
3. Gerekirse **DESIGN_PATTERNS_ANALYSIS.md**'de araştır

**Toplam:** ~20 dakika

### Seçenek 3: Spesifik pattern için
1. **DESIGN_PATTERNS_QUICK_REFERENCE.md**'de pattern'ı bul
2. **DESIGN_PATTERNS_ANALYSIS.md**'de detaylı oku
3. **DESIGN_PATTERNS_CODE_EXAMPLES.cs**'de örneği incele
4. **DESIGN_PATTERNS_VISUAL_DIAGRAMS.md**'de flow'unu gör

**Toplam:** ~15 dakika/pattern

---

## 🗂️ Dosya Lokasyonları

### CQRS Pattern
- 📄 Analysis: DESIGN_PATTERNS_ANALYSIS.md → Section 1
- 💻 Code: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 1
- 📊 Diagram: DESIGN_PATTERNS_VISUAL_DIAGRAMS.md → Section 2
- 🔑 Quick Ref: DESIGN_PATTERNS_QUICK_REFERENCE.md → CQRS Pattern

### Repository Pattern
- 📄 Analysis: DESIGN_PATTERNS_ANALYSIS.md → Section 3
- 💻 Code: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 3
- 🔑 Quick Ref: DESIGN_PATTERNS_QUICK_REFERENCE.md → Repository Pattern

### Clean Architecture
- 📄 Analysis: DESIGN_PATTERNS_ANALYSIS.md → Section 5
- 💻 Code: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 2
- 📊 Diagram: DESIGN_PATTERNS_VISUAL_DIAGRAMS.md → Section 1
- 🔑 Quick Ref: DESIGN_PATTERNS_QUICK_REFERENCE.md → Clean Architecture

### DI Pattern
- 📄 Analysis: DESIGN_PATTERNS_ANALYSIS.md → Section 4
- 💻 Code: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 3
- 📊 Diagram: DESIGN_PATTERNS_VISUAL_DIAGRAMS.md → Section 3
- 🔑 Quick Ref: DESIGN_PATTERNS_QUICK_REFERENCE.md → DI

### Pipeline/Middleware
- 📄 Analysis: DESIGN_PATTERNS_ANALYSIS.md → Section 6
- 💻 Code: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 4
- 📊 Diagram: DESIGN_PATTERNS_VISUAL_DIAGRAMS.md → Section 4
- 🔑 Quick Ref: DESIGN_PATTERNS_QUICK_REFERENCE.md → Pipeline

### Strategy Pattern
- 📄 Analysis: DESIGN_PATTERNS_ANALYSIS.md → Section 8
- 💻 Code: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 5
- 📊 Diagram: DESIGN_PATTERNS_VISUAL_DIAGRAMS.md → Section 5
- 🔑 Quick Ref: DESIGN_PATTERNS_QUICK_REFERENCE.md → Strategy

### DDD (Domain-Driven Design)
- 📄 Analysis: DESIGN_PATTERNS_ANALYSIS.md → Section 9
- 💻 Code: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 6
- 🔑 Quick Ref: DESIGN_PATTERNS_QUICK_REFERENCE.md → DDD

### Exception Handling
- 📄 Analysis: DESIGN_PATTERNS_ANALYSIS.md → Section 10
- 💻 Code: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 7
- 📊 Diagram: DESIGN_PATTERNS_VISUAL_DIAGRAMS.md → Section 7
- 🔑 Quick Ref: DESIGN_PATTERNS_QUICK_REFERENCE.md → Exception

### API Gateway
- 📄 Analysis: DESIGN_PATTERNS_ANALYSIS.md → Section 12
- 💻 Code: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 10
- 📊 Diagram: DESIGN_PATTERNS_VISUAL_DIAGRAMS.md → Section 6
- 🔑 Quick Ref: DESIGN_PATTERNS_QUICK_REFERENCE.md → Gateway

### Testing
- 💻 Code: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 9
- 🔑 Quick Ref: DESIGN_PATTERNS_QUICK_REFERENCE.md → Strategy

### 12-Factor App
- 📄 Analysis: DESIGN_PATTERNS_ANALYSIS.md → Section 15
- 📊 Diagram: DESIGN_PATTERNS_VISUAL_DIAGRAMS.md → Section 8
- 🔑 Quick Ref: DESIGN_PATTERNS_QUICK_REFERENCE.md → 12-Factor

---

## 📊 Pattern Özet Tablosu

| # | Pattern | Kategori | Faydası | Zorluk |
|----|---------|----------|---------|--------|
| 1 | CQRS | Architecture | Yazma/Okuma optimizasyonu | Orta |
| 2 | MediatR | Communication | Request mediation, pipeline | Kolay |
| 3 | Repository | Data Access | DB abstraction, testable | Kolay |
| 4 | DI | Dependency Mgmt | Loose coupling, flexible | Kolay |
| 5 | Clean Arch | Architecture | Layered design, maintainable | Orta |
| 6 | Pipeline/Middleware | CrossCutting | Centralized concerns | Orta |
| 7 | Factory | Creation | Encapsulation, validation | Kolay |
| 8 | Strategy | Selection | Runtime flexibility | Orta |
| 9 | DDD | Domain | Business logic focused | Zor |
| 10 | Exception Handling | Error Mgmt | Consistent responses | Kolay |
| 11 | Options | Configuration | Type-safe config | Kolay |
| 12 | API Gateway | Integration | Single entry point | Orta |
| 13 | Rate Limiting | Protection | DoS protection | Kolay |
| 14 | Health Checks | Monitoring | Status tracking | Kolay |
| 15 | 12-Factor | Best Practices | Scalable, maintainable | Orta |

---

## 🧭 Pattern Seçme Karar Ağacı

```
Sorun nedir?
├─ Veri erişimi test etmek istiyorum
│  └─ Repository Pattern (+ Mock)
├─ Komut ve sorgu iş mantığı karışıyor
│  └─ CQRS Pattern
├─ Validation, logging otomatik eklemek istiyorum
│  └─ Pipeline Behavior Pattern
├─ DB/Cache arasında değiş-tokuş
│  └─ Strategy Pattern
├─ Error response format tutarsız
│  └─ Global Exception Handler
├─ Backend servislerine erişim kontrol etmek istiyorum
│  └─ API Gateway (YARP)
├─ Test ortamında DB kullanmamak istiyorum
│  └─ InMemory Database + Strategy Pattern
├─ Configuration ortamlar arasında değişiyor
│  └─ Options Pattern + 12-Factor
├─ Business rules genel koda karışıyor
│└─ Domain-Driven Design (DDD)
└─ DoS attack'lardan korumak istiyorum
   └─ Rate Limiting
```

---

## 💻 Kod Örnekleri Hızlı Erişim

### CQRS Command Yazma
```csharp
// File: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 1
public record AddProductCommand(...) : IRequest<ProductDto>;
public class AddProductCommandHandler : IRequestHandler<AddProductCommand, ProductDto> { }
```

### Repository Oluşturma
```csharp
// File: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 3
public interface IProductRepository { }
public class ProductRepository : IProductRepository { }
```

### DI Registration
```csharp
// File: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 3
builder.Services.AddScoped<IProductRepository, ProductRepository>();
```

### Pipeline Behavior
```csharp
// File: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 4
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { }
```

### Exception Handling
```csharp
// File: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 7
public class ExceptionHandlingMiddleware { }
```

### Unit Test
```csharp
// File: DESIGN_PATTERNS_CODE_EXAMPLES.cs → Section 9
var repositoryMock = new Mock<IProductRepository>();
var handler = new AddProductCommandHandler(repositoryMock.Object);
```

---

## 🎓 Öğrenme Hedeflerine Göre Okuma Listesi

### Hedef: CQRS Pattern'ını anlamak
1. **DESIGN_PATTERNS_QUICK_REFERENCE.md** - CQRS (2 min)
2. **DESIGN_PATTERNS_ANALYSIS.md** - Section 1 (5 min)
3. **DESIGN_PATTERNS_VISUAL_DIAGRAMS.md** - Section 2 (3 min)
4. **DESIGN_PATTERNS_CODE_EXAMPLES.cs** - Section 1 (5 min)
5. Realde implement et - Product service'de (20 min)

### Hedef: Test edilebilir kod yazmak
1. **DESIGN_PATTERNS_QUICK_REFERENCE.md** - DI & Repository (3 min)
2. **DESIGN_PATTERNS_ANALYSIS.md** - Section 3, 4 (10 min)
3. **DESIGN_PATTERNS_CODE_EXAMPLES.cs** - Section 3, 9 (10 min)
4. Realde test yaz (30 min)

### Hedef: Handler yazmak
1. **DESIGN_PATTERNS_QUICK_REFERENCE.md** - Handler checklist (2 min)
2. **DESIGN_PATTERNS_CODE_EXAMPLES.cs** - Section 1, 4 (10 min)
3. **DESIGN_PATTERNS_ANALYSIS.md** - Section 1, 6 (8 min)
4. Realde handler yaz (30 min)

### Hedef: Yeni microservice eklemek
1. **DESIGN_PATTERNS_QUICK_REFERENCE.md** - Clean Architecture (3 min)
2. **DESIGN_PATTERNS_ANALYSIS.md** - Section 5 (8 min)
3. **DESIGN_PATTERNS_VISUAL_DIAGRAMS.md** - Section 1 (4 min)
4. **DESIGN_PATTERNS_CODE_EXAMPLES.cs** - Section 2 (10 min)
5. Realde service setup (45 min)

---

## 🔗 Referans: Repository'deki Dosyalar

### Başlangıç Projeleri
- `src/AuthService/` - Authentication microservice
- `src/ProductService/` - Product management microservice
- `src/LogService/` - Logging microservice (bonus)
- `src/Gateway/` - API Gateway (YARP)

### Katman Referans
- **API Layer:** `*/API/` → Controllers
- **Application Layer:** `*/Application/` → Commands, Queries, Handlers
- **Domain Layer:** `*/Domain/` → Entities, Events, Exceptions
- **Infrastructure Layer:** `*/Infrastructure/` → Repositories, Services, DbContext

### Test Projeleri
- `tests/AuthService.Tests/`
- `tests/ProductService.Tests/`
- `tests/LogService.Tests/`

---

## ✨ Bonus: Pattern Kombinasyonları

### Sık Kullanılan Kombinasyonlar

1. **CQRS + MediatR + Pipeline Behavior**
   - Command → Validation → Handler
- Tüm application layer'da

2. **Repository + DI + Strategy**
 - Prod: SqlServer
   - Test: InMemory / Mock
   - Kod aynı kalır

3. **Clean Architecture + DDD + Exception Handling**
   - Domain logic korunan
   - Exception'lar domain'den fırlatılır
   - Middleware tarafından handle edilir

4. **API Gateway + Rate Limiting + Health Checks**
   - Gateway'de rate limit check
   - Health checks her servis'te
   - Kubernetes integrated

5. **Options Pattern + 12-Factor + Environment Config**
   - appsettings.json
   - Environment override
   - Same code, different config

---

## 📞 Sık Sorulan Sorular

**S: Pattern'ları hepsini aynı anda mı kullanmalıyım?**
C: Hayır. Sorunun gerektirdiği pattern'ı seç. Bu uygulamada tümü kullanılıyor çünkü enterprise grade.

**S: Hangi pattern'dan başlamalıyım?**
C: Clean Architecture + DI temeldir. Ondan sonra CQRS + MediatR.

**S: Test yazarken hangi pattern'lar yardımcı?**
C: Repository + DI + Strategy. Mock'lar kolayca enjekte edilebilir.

**S: Performance problem yaşıyorum, hangi pattern?**
C: CQRS (separate read/write) + Strategy Pattern (caching seçimi).

**S: Error handling'i merkezi yap mak istiyorum?**
C: Global Exception Handler + Domain Exceptions.

**S: Yeni developer'ları nasıl onboard ederim?**
C: QUICK_REFERENCE.md → ANALYSIS.md → CODE_EXAMPLES.cs → DIAGRAMS.md sırası.

---

## 📝 Lisans

Bu dokümantasyon, GitHub repository'de (https://github.com/omof221/Task) bulunan Task Microservices Uygulaması'nı açıklamaktadır.

---

## 🚀 Sonraki Adımlar

1. **QUICK_REFERENCE.md** ile hızlı başla
2. **İlgilendiğin pattern'ı** ANALYSIS.md'de detaylı oku
3. **CODE_EXAMPLES.cs**'de gerçek kod gör
4. **DIAGRAMS.md**'de flow'ları görselleştir
5. **Realde implement et** ve test yaz
6. **Geri beslenme** ver ve dokümentasyonu güncelle

---

**Son Güncelleme:** 2025-01-15  
**Versiyon:** 1.0  
**Yazarlar:** AI Programming Assistant (GitHub Copilot)  
**Target Framework:** .NET 9  

