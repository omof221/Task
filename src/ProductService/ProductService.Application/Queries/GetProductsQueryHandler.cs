using MediatR;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;

namespace ProductService.Application.Queries;

/// GetProductsQuery handler'ı — Redis Cache-Aside Pattern.
/// Akış:
///   1. Redis'te ara (cache hit) → varsa doğrudan dön
///   2. Yoksa (cache miss) → DB'den yükle
///   3. Redis'e yaz (cache populate)
///   4. DTO listesi döndür
/// SRP: Yalnızca listeleme iş akışını yönetir.
public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService      _cacheService;

    // Cache TTL: 5 dakika — konfigürasyondan okunabilir hale getirilebilir
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public GetProductsQueryHandler(IProductRepository productRepository, ICacheService cacheService)
    {
        _productRepository = productRepository;
        _cacheService      = cacheService;
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        // Cache-Aside — Adım 1: Cache'te ara
        var cached = await _cacheService.GetAsync<List<ProductDto>>(CacheKeys.ProductList, ct);
        if (cached is not null)
            return cached;

        // Cache-Aside — Adım 2: DB'den yükle
        var products = await _productRepository.GetActiveAsync(ct);

        var dtos = products.Select(p => new ProductDto(
            p.Id, p.Name, p.Description, p.Price, p.Stock, p.IsActive, p.CreatedAt, p.UpdatedAt
        )).ToList();

        // Cache-Aside — Adım 3: Redis'e yaz
        await _cacheService.SetAsync(CacheKeys.ProductList, dtos, CacheTtl, ct);

        return dtos;
    }
}
