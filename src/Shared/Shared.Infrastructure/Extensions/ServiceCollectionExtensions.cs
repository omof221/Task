using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Middleware;
using StackExchange.Redis;

namespace Shared.Infrastructure.Extensions;


/// Ortak altyapı bileşenlerini DI container'a kaydeden extension metotları.
/// DIP: Üst katmanlar bu helper'ları kullanarak alt katman implementasyonlarına

public static class ServiceCollectionExtensions
{
   
    public static IServiceCollection AddRedis(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(connectionString));
        return services;
    }

  
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }

 
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
