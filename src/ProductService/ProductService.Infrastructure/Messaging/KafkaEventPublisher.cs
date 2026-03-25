using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductService.Application.Interfaces;
using Shared.Domain.Events;
using System.Text.Json;

namespace ProductService.Infrastructure.Messaging;

/// <summary>
/// IEventPublisher'ın Kafka implementasyonu (alternatif).
/// OCP / DIP: RabbitMqEventPublisher ile aynı arayüzü implement eder.
/// Program.cs'te hangisinin kullanılacağı konfigürasyonla seçilir:
///   "Messaging:Provider": "RabbitMQ" | "Kafka"
/// Topic adı: event türünün adı (ör. "ProductAddedEvent")
/// </summary>
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KafkaEventPublisher(
        ILogger<KafkaEventPublisher> logger,
        IConfiguration configuration)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks             = Acks.All,  // En az bir lider + tüm ISR onayı
            MessageTimeoutMs = 5000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : IIntegrationEvent
    {
        try
        {
            var topic = typeof(T).Name;
            var json  = JsonSerializer.Serialize(integrationEvent, JsonOptions);

            var message = new Message<string, string>
            {
                Key   = integrationEvent.EventId.ToString(),
                Value = json
            };

            var result = await _producer.ProduceAsync(topic, message, ct);

            _logger.LogInformation(
                "[Kafka] Event yayımlandı: {Topic} | Partition: {Partition} | Offset: {Offset}",
                topic, result.Partition, result.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kafka] Event yayımlanamadı: {EventType}", typeof(T).Name);
        }
    }

    public void Dispose() => _producer.Dispose();
}
