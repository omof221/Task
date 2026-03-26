using LogService.Domain.Enums;
using MediatR;

namespace LogService.Application.Commands;

/// <summary>
/// Log kayıt komutu.
/// SRP: yalnızca tek bir log yazma işlemi tanımlar.
/// </summary>
public record WriteLogCommand(
    AppLogLevel Level,
    string      Message,
    string?     Source = null,
    string?     Meta   = null) : IRequest<Guid>;
