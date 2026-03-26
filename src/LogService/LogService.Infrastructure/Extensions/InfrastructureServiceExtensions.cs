using LogService.Application.Interfaces;
using LogService.Infrastructure.Logging;
using LogService.Infrastructure.Messaging;
using LogService.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogService.Infrastructure.Extensions;

/// <summary>
/// Infrastructure katmanı DI kayıt helper'ı.
/// OCP / DIP: Yeni implementasyon eklendiğinde Application katmanı değişmez.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddLogInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration,
        bool                    isTesting = false)
    {
        // ── Repository ──────────────────────────────────────────
        services.AddSingleton<ILogRepository, InMemoryLogRepository>();

        // ── Structured Logger ────────────────────────────────────
        services.AddSingleton<IStructuredLogger, SerilogStructuredLogger>();

        // ── RabbitMQ Event Consumer ──────────────────────────────
        // Test ortamında RabbitMQ bağlantısı kurulmaz.
        if (!isTesting)
        {
            services.AddHostedService<LogEventConsumer>();
        }

        return services;
    }
}
