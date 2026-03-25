using Shared.Domain.Events;

namespace ProductService.Domain.Events;

/// <summary>
/// Ürün güncellendiğinde yayımlanan integration event.
/// JWT ile korunan Update endpoint'i çağrıldığında tetiklenir.
/// </summary>
public sealed class ProductUpdatedEvent : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType => nameof(ProductUpdatedEvent);

    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public int NewStock { get; init; }
}
