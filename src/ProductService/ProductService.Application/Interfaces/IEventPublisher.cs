using Shared.Domain.Events;

namespace ProductService.Application.Interfaces;

/// Mesaj/event yayımlama sözleşmesi.
/// ISP: Tek sorumluluk — event publish.
/// OCP / DIP: RabbitMQ veya Kafka implementasyonu
///            bu arayüz üzerinden değiştirilebilir.
public interface IEventPublisher
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : IIntegrationEvent;
}
