using MediatR;
using Microsoft.Extensions.Logging;

namespace ProductService.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — otomatik request/response loglama.
/// SRP: Log sorumluluğu handler'lardan ayrıştırılır.
/// Her MediatR isteği için istek adı, süre ve başarı/hata bilgisi loglanır.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var startTime   = DateTime.UtcNow;

        _logger.LogInformation("[CQRS] Başlıyor: {RequestName} {@Request}", requestName, request);

        try
        {
            var response = await next();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "[CQRS] Tamamlandı: {RequestName} ({ElapsedMs}ms)", requestName, elapsed);

            return response;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex,
                "[CQRS] Hata: {RequestName} ({ElapsedMs}ms) — {Error}",
                requestName, elapsed, ex.Message);
            throw;
        }
    }
}
