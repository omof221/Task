namespace LogService.Domain.Enums;


/// Yapılandırılmış log seviyeleri.
/// ERROR ve CRITICAL seviyesi → ELK'e yönlendirilir.
/// INFO ve WARNING → Seq'e yönlendirilir.

public enum AppLogLevel
{
    INFO,
    WARNING,
    ERROR,
    CRITICAL
}
