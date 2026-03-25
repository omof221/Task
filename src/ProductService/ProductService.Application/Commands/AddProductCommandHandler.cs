using MediatR;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Domain.Events;

namespace ProductService.Application.Commands;

/// <summary>
/// AddProductCommand handler'ı.
/// SRP: Yalnızca ürün oluşturma iş akışını yönetir.
/// Akış: DB'ye async yaz → event yayımla → cache'i geçersiz kıl.
/// </summary>
public sealed class AddProductCommandHandler : IRequestHandler<AddProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICacheService _cacheService;

    public AddProductCommandHandler(
        IProductRepository productRepository,
        IEventPublisher eventPublisher,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _eventPublisher    = eventPublisher;
        _cacheService      = cacheService;
    }

    public async Task<ProductDto> Handle(AddProductCommand request, CancellationToken ct)
    {
        // 1. Domain entity oluştur (domain kuralları Product.Create içinde)
        var product = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.Stock);

        // 2. Veritabanına async olarak yaz
        await _productRepository.AddAsync(product, ct);
        await _productRepository.SaveChangesAsync(ct);

        // 3. Integration event yayımla — LogService ve diğer servisler dinler
        await _eventPublisher.PublishAsync(new ProductAddedEvent
        {
            ProductId   = product.Id,
            ProductName = product.Name,
            Price       = product.Price,
            Stock       = product.Stock
        }, ct);

        // 4. Cache invalidation — liste cache'i güncelliğini yitirir
        await _cacheService.RemoveAsync(CacheKeys.ProductList);

        // 5. DTO olarak dön
        return ToDto(product);
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Description, p.Price, p.Stock, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
