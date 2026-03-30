using LogService.Domain.Entities;

namespace LogService.Application.Interfaces;


/// Log kayıt deposu sözleşmesi.
/// yalnızca ihtiyaç duyulan metotlar burada tanımlıdır.
/// Application katmanı Infrastructure'ı bilmez; bu arayüzü kullanır.

public interface ILogRepository
{
    Task AddAsync(LogEntry entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
