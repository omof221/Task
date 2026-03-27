using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Application.Interfaces;
using ProductService.Infrastructure.Cache;
using ProductService.Infrastructure.Messaging;
using ProductService.Infrastructure.Persistence;
using ProductService.Infrastructure.Repositories;
using StackExchange.Redis;

namespace ProductService.Infrastructure.Extensions;

/// <summary>
/// Infrastructure katmanı DI kayıt helper'ı.
/// OCP / DIP: Yeni implementasyon eklendiğinde bu metot genişletilir;
///            Application katmanı değişmez.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddProductInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isTesting = false)
    {
        // ── Veritabanı ────────────────────────────────────────────
        if (isTesting)
        {
            // Statik InMemoryDatabaseRoot: tüm DbContext instance'ları aynı
            // in-memory store'u paylaşır → requestler arası veri görünürlüğü sağlanır.
            var root = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();
            services.AddDbContext<ProductDbContext>(opt =>
                opt.UseInMemoryDatabase("ProductIntegrationTestDb", root));
        }
        else
        {
            services.AddDbContext<ProductDbContext>(opt =>
                opt.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        }

        // ── Repository ────────────────────────────────────────────
        services.AddScoped<IProductRepository, ProductRepository>();

        // ── Redis Cache ───────────────────────────────────────────
        if (!isTesting)
        {
            var redisConn = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConn));
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            // Test ortamında null-safe in-memory cache
            services.AddScoped<ICacheService, NullCacheService>();
        }

        // ── Event Publisher ───────────────────────────────────────
        // "Messaging:Provider" = "RabbitMQ" | "Kafka"
        var messagingProvider = configuration["Messaging:Provider"] ?? "RabbitMQ";

        if (isTesting)
        {
            services.AddScoped<IEventPublisher, NullEventPublisher>();
        }
        else if (messagingProvider.Equals("Kafka", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        }
        else
        {
            services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        }

        return services;
    }
}
