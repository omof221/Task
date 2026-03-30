using ProductService.Application.Interfaces;
using Shared.Domain.Events;

namespace ProductService.Infrastructure.Messaging;


/// Test ortamı için no-op IEventPublisher.
/// RabbitMQ/Kafka bağlantısı olmadan integration test çalışabilir.
public sealed class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : IIntegrationEvent
        => Task.CompletedTask;
}
