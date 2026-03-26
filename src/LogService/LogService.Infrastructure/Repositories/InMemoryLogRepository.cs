using LogService.Application.Interfaces;
using LogService.Domain.Entities;

namespace LogService.Infrastructure.Repositories;

/// <summary>
/// ILogRepository'nin in-memory implementasyonu.
/// Production'da Npgsql/Mongo ile değiştirilebilir (OCP).
/// DIP: Application katmanı bu sınıfı bilmez; ILogRepository kullanır.
/// </summary>
public sealed class InMemoryLogRepository : ILogRepository
{
    private readonly List<LogEntry> _store = [];

    public Task AddAsync(LogEntry entry, CancellationToken ct = default)
    {
        _store.Add(entry);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask; // In-memory: persist gerekmez.
}
