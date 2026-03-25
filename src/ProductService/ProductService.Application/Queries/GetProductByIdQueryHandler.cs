using MediatR;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Exceptions;

namespace ProductService.Application.Queries;

/// <summary>
/// GetProductByIdQuery handler'ı — tekil ürün için Cache-Aside.
/// Cache miss durumunda DB'den yükler; bulunamazsa 404 exception.
/// </summary>
public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService      _cacheService;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public GetProductByIdQueryHandler(IProductRepository productRepository, ICacheService cacheService)
    {
        _productRepository = productRepository;
        _cacheService      = cacheService;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var cacheKey = CacheKeys.ProductById(request.Id);

        // Cache hit
        var cached = await _cacheService.GetAsync<ProductDto>(cacheKey, ct);
        if (cached is not null)
            return cached;

        // Cache miss — DB'den yükle
        var product = await _productRepository.GetByIdAsync(request.Id, ct)
            ?? throw new ProductNotFoundException(request.Id);

        var dto = new ProductDto(
            product.Id, product.Name, product.Description,
            product.Price, product.Stock, product.IsActive,
            product.CreatedAt, product.UpdatedAt);

        // Redis'e yaz
        await _cacheService.SetAsync(cacheKey, dto, CacheTtl, ct);

        return dto;
    }
}
