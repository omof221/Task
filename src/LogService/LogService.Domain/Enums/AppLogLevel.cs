namespace LogService.Domain.Enums;

/// <summary>
/// Yapılandırılmış log seviyeleri.
/// ERROR ve CRITICAL seviyesi → ELK'e yönlendirilir.
/// INFO ve WARNING → Seq'e yönlendirilir.
/// </summary>
public enum AppLogLevel
{
    INFO,
    WARNING,
    ERROR,
    CRITICAL
}
