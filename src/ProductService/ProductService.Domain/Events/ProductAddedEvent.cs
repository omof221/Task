using Shared.Domain.Events;

namespace ProductService.Domain.Events;

/// <summary>
/// Yeni ürün eklendiğinde yayımlanan integration event.
/// IIntegrationEvent kontratı sayesinde LogService ve diğer
/// servisler bu tipi bilmeden event'i consume edebilir.
/// </summary>
public sealed class ProductAddedEvent : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType => nameof(ProductAddedEvent);

    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
}
