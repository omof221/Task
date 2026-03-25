using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Application.Behaviors;

namespace ProductService.Application.Extensions;

/// <summary>
/// Application katmanı DI kayıt helper'ı.
/// SRP: DI konfigürasyonu tek yerde toplanır.
/// OCP: Yeni behavior / validator eklendiğinde bu metot değişmez.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddProductApplication(this IServiceCollection services)
    {
        // MediatR — tüm handler ve behavior'lar bu assembly'den taranır
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly);

            // Pipeline: Logging → Validation → Handler
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // FluentValidation — tüm validator'lar bu assembly'den otomatik taranır
        services.AddValidatorsFromAssembly(
            typeof(ApplicationServiceExtensions).Assembly,
            includeInternalTypes: true);

        return services;
    }
}
