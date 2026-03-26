using LogService.Domain.Enums;

namespace LogService.Application.Interfaces;

/// <summary>
/// Yapılandırılmış log servisi sözleşmesi.
/// OCP: Yeni sink (Seq, ELK, Console) bu arayüzün implementasyonu eklenerek devreye alınır.
/// </summary>
public interface IStructuredLogger
{
    /// <summary>
    /// Seviyeye göre uygun sink'e yönlendirerek log kaydeder.
    /// INFO/WARNING → Seq, ERROR/CRITICAL → ELK.
    /// </summary>
    void Log(AppLogLevel level, string message, string? source = null, string? meta = null);
}
