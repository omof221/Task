using LogService.Domain.Enums;

namespace LogService.Application.Interfaces;


/// Yapılandırılmış log servisi sözleşmesi.
/// OCP: Yeni sink (Seq, ELK, Console) bu arayüzün implementasyonu eklenerek devreye alınır.

public interface IStructuredLogger
{

    void Log(AppLogLevel level, string message, string? source = null, string? meta = null);
}
