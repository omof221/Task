using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Middleware;
using StackExchange.Redis;

namespace Shared.Infrastructure.Extensions;

/// <summary>
/// Ortak altyapı bileşenlerini DI container'a kaydeden extension metotları.
/// DIP: Üst katmanlar bu helper'ları kullanarak alt katman implementasyonlarına
/// doğrudan bağımlı olmaktan kurtulur.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Redis bağlantısını DI'ya kaydeder.
    /// 12-Factor Config: Bağlantı adresi ortam değişkeninden okunur.
    /// </summary>
    public static IServiceCollection AddRedis(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(connectionString));
        return services;
    }

    /// <summary>
    /// Global exception handling middleware'ini pipeline'a ekler.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }

    /// <summary>
    /// Health check endpoint'lerini /health altında expose eder.
    /// </summary>
    public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health");
        });
        return app;
    }
}
