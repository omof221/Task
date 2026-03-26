using LogService.Application.Interfaces;
using LogService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LogService.Infrastructure.Logging;

/// <summary>
/// IStructuredLogger'ın Serilog tabanlı implementasyonu.
/// Level bazlı yönlendirme:
///   INFO / WARNING  → Seq (genel gözlemlenebilirlik)
///   ERROR / CRITICAL → ELK (alert + analiz)
/// OCP: Yeni sink eklemek için bu sınıfı değiştirmeden
///      Serilog pipeline konfigürasyonu genişletilebilir.
/// </summary>
public sealed class SerilogStructuredLogger : IStructuredLogger
{
    private readonly ILogger<SerilogStructuredLogger> _logger;

    public SerilogStructuredLogger(ILogger<SerilogStructuredLogger> logger)
        => _logger = logger;

    public void Log(AppLogLevel level, string message, string? source = null, string? meta = null)
    {
        // Serilog'un yapılandırılmış log özelliklerini kullanıyoruz.
        // Seq sink tüm seviyeleri alır; ELK sink Error+ seviyelerini filtreler.
        // Bu ayrım Serilog pipeline'da MinimumLevel.Override ile yapılandırılır.
        switch (level)
        {
            case AppLogLevel.INFO:
                _logger.LogInformation(
                    "[{Source}] {Message} | Meta: {Meta}", source, message, meta);
                break;
            case AppLogLevel.WARNING:
                _logger.LogWarning(
                    "[{Source}] {Message} | Meta: {Meta}", source, message, meta);
                break;
            case AppLogLevel.ERROR:
                _logger.LogError(
                    "[{Source}] {Message} | Meta: {Meta}", source, message, meta);
                break;
            case AppLogLevel.CRITICAL:
                _logger.LogCritical(
                    "[{Source}] {Message} | Meta: {Meta}", source, message, meta);
                break;
        }
    }
}
