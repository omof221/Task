using LogService.Domain.Entities;

namespace LogService.Application.Interfaces;

/// <summary>
/// Log kayıt deposu sözleşmesi.
/// ISP: yalnızca ihtiyaç duyulan metotlar burada tanımlıdır.
/// DIP: Application katmanı Infrastructure'ı bilmez; bu arayüzü kullanır.
/// </summary>
public interface ILogRepository
{
    Task AddAsync(LogEntry entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
