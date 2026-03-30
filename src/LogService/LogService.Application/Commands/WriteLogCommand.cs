using LogService.Domain.Enums;
using MediatR;

namespace LogService.Application.Commands;


/// Log kayıt komutu.
///  yalnızca tek bir log yazma işlemi tanımlar.

public record WriteLogCommand(
    AppLogLevel Level,
    string      Message,
    string?     Source = null,
    string?     Meta   = null) : IRequest<Guid>;
