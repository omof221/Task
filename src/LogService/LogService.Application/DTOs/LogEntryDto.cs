using LogService.Domain.Enums;

namespace LogService.Application.DTOs;


/// LogEntry API/response transfer nesnesi.

public record LogEntryDto(
    Guid        Id,
    AppLogLevel Level,
    string      Message,
    string?     Source,
    string?     Meta,
    DateTime    CreatedAt);
