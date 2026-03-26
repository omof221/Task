using Microsoft.Extensions.DependencyInjection;

namespace LogService.Application.Extensions;

/// <summary>
/// Application katmanı DI kayıt helper'ı.
/// OCP: Yeni handler/behavior eklendiğinde bu metot değişmez;
///       MediatR assembly taraması yeni handler'ı otomatik bulur.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddLogApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly));

        return services;
    }
}
