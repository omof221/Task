using LogService.Domain.Enums;

namespace LogService.Application.DTOs;

/// <summary>
/// LogEntry API/response transfer nesnesi.
/// </summary>
public record LogEntryDto(
    Guid        Id,
    AppLogLevel Level,
    string      Message,
    string?     Source,
    string?     Meta,
    DateTime    CreatedAt);
