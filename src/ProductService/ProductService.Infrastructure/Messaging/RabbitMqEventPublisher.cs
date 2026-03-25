using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductService.Application.Interfaces;
using RabbitMQ.Client;
using Shared.Domain.Events;
using System.Text;
using System.Text.Json;

namespace ProductService.Infrastructure.Messaging;

/// <summary>
/// IEventPublisher'ın RabbitMQ implementasyonu (RabbitMQ.Client 7.x async API).
/// OCP / DIP: KafkaEventPublisher aynı arayüzü implement eder;
///            DI'da hangisinin kullanılacağı konfigürasyonla belirlenir.
/// Exchange: "product.events" (topic)
/// Routing key: event türünün adı (ör. "ProductAddedEvent")
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly string _hostName;
    private readonly string _exchangeName;
    private IConnection? _connection;
    private IChannel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RabbitMqEventPublisher(
        ILogger<RabbitMqEventPublisher> logger,
        IConfiguration configuration)
    {
        _logger       = logger;
        _hostName     = configuration["Messaging:RabbitMQ:Host"] ?? "localhost";
        _exchangeName = configuration["Messaging:RabbitMQ:Exchange"] ?? "product.events";
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : IIntegrationEvent
    {
        try
        {
            await EnsureConnectionAsync(ct);

            var routingKey = typeof(T).Name;
            var json       = JsonSerializer.Serialize(integrationEvent, JsonOptions);
            var body       = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                ContentType  = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId    = integrationEvent.EventId.ToString(),
                Timestamp    = new AmqpTimestamp(
                    new DateTimeOffset(integrationEvent.OccurredOn).ToUnixTimeSeconds())
            };

            await _channel!.BasicPublishAsync(
                exchange:        _exchangeName,
                routingKey:      routingKey,
                mandatory:       false,
                basicProperties: props,
                body:            body,
                cancellationToken: ct);

            _logger.LogInformation(
                "[RabbitMQ] Event yayımlandı: {EventType} | EventId: {EventId}",
                routingKey, integrationEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[RabbitMQ] Event yayımlanamadı: {EventType}", typeof(T).Name);
            // Fire-and-forget: event publish hatası iş akışını durdurmaz
        }
    }

    private async Task EnsureConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            return;

        var factory = new ConnectionFactory { HostName = _hostName };
        _connection = await factory.CreateConnectionAsync(ct);
        _channel    = await _connection.CreateChannelAsync(cancellationToken: ct);

        await _channel.ExchangeDeclareAsync(
            exchange:   _exchangeName,
            type:       ExchangeType.Topic,
            durable:    true,
            autoDelete: false,
            cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel   is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
