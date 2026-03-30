using System.Text;
using System.Text.Json;
using LogService.Application.Commands;
using LogService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LogService.Infrastructure.Messaging;


/// RabbitMQ'dan ProductAddedEvent / ProductUpdatedEvent tüketen background servisi.
/// Event-Driven entegrasyon: ProductService → RabbitMQ → LogService → WriteLogCommand.

public sealed class LogEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration       _configuration;
    private readonly ILogger<LogEventConsumer> _logger;

    private IConnection? _connection;
    private IChannel?    _channel;

    private const string ExchangeName = "product.events";
    private const string QueueName    = "logservice.product.events";

    public LogEventConsumer(
        IServiceScopeFactory       scopeFactory,
        IConfiguration             configuration,
        ILogger<LogEventConsumer>  logger)
    {
        _scopeFactory  = scopeFactory;
        _configuration = configuration;
        _logger        = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"]     ?? "localhost",
                Port     = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest"
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel    = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type:     ExchangeType.Topic,
                durable:  true,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue:      QueueName,
                durable:    true,
                exclusive:  false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // ProductService'in yayımladığı tüm event routing key'lerini dinle
            await _channel.QueueBindAsync(QueueName, ExchangeName, "product.*", cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += HandleMessageAsync;

            await _channel.BasicConsumeAsync(
                queue:       QueueName,
                autoAck:     false,
                consumer:    consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("LogEventConsumer RabbitMQ kuyruğunu dinliyor: {Queue}", QueueName);

            // Uygulama kapanana kadar bekle
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogEventConsumer başlatılamadı. RabbitMQ bağlantısı kurulamıyor olabilir.");
        }
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var body    = ea.Body.ToArray();
            var json    = Encoding.UTF8.GetString(body);
            var routing = ea.RoutingKey;

            // routing key'e göre log mesajı oluştur
            var (level, message) = routing switch
            {
                "product.added"   => (AppLogLevel.INFO,    $"[ProductAddedEvent] {json}"),
                "product.updated" => (AppLogLevel.INFO,    $"[ProductUpdatedEvent] {json}"),
                _                 => (AppLogLevel.WARNING,  $"[UnknownEvent:{routing}] {json}")
            };

            using var scope   = _scopeFactory.CreateScope();
            var mediator      = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new WriteLogCommand(
                Level:   level,
                Message: message,
                Source:  "ProductService",
                Meta:    json));

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event işlenirken hata oluştu.");
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
