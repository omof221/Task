using LogService.Domain.Enums;
using Shared.Domain.BaseEntities;

namespace LogService.Domain.Entities;

/// <summary>
/// Log kayıt domain entity'si.
/// BaseEntity'den Id, CreatedAt, UpdatedAt devralır.
/// </summary>
public class LogEntry : BaseEntity
{
    public AppLogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Meta { get; set; }

    public LogEntry() { }

    public static LogEntry Create(AppLogLevel level, string message, string? source = null, string? meta = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new LogEntry
        {
            Id        = Guid.NewGuid(),
            Level     = level,
            Message   = message,
            Source    = source,
            Meta      = meta,
            CreatedAt = DateTime.UtcNow
        };
    }
}
