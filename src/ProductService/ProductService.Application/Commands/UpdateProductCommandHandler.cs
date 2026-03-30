using MediatR;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Events;
using ProductService.Domain.Exceptions;

namespace ProductService.Application.Commands;

/// UpdateProductCommand handler'ı.
/// SRP: Güncelleme iş akışını yönetir.
/// Akış: Ürünü bul → domain update → DB kaydet → event yayımla → cache temizle.
/// Cache Invalidation: Hem liste hem de tekil ürün cache'i silinir.

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IEventPublisher    _eventPublisher;
    private readonly ICacheService      _cacheService;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        IEventPublisher eventPublisher,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _eventPublisher    = eventPublisher;
        _cacheService      = cacheService;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        // 1. Ürünü getir — bulunamazsa domain exception (404)
        var product = await _productRepository.GetByIdAsync(request.Id, ct)
            ?? throw new ProductNotFoundException(request.Id);

        var oldPrice = product.Price;

        // 2. Domain entity güncelle (iş kuralları Product.Update içinde)
        product.Update(request.Name, request.Description, request.Price, request.Stock);

        // 3. Veritabanına yaz
        await _productRepository.UpdateAsync(product, ct);
        await _productRepository.SaveChangesAsync(ct);

        // 4. Integration event yayımla
        await _eventPublisher.PublishAsync(new ProductUpdatedEvent
        {
            ProductId   = product.Id,
            ProductName = product.Name,
            OldPrice    = oldPrice,
            NewPrice    = product.Price,
            NewStock    = product.Stock
        }, ct);

        // 5. Cache invalidation — hem liste hem tekil cache silinir
        await _cacheService.RemoveAsync(CacheKeys.ProductList);
        await _cacheService.RemoveAsync(CacheKeys.ProductById(product.Id));

        return ToDto(product);
    }

    private static ProductDto ToDto(Domain.Entities.Product p) => new(
        p.Id, p.Name, p.Description, p.Price, p.Stock, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
