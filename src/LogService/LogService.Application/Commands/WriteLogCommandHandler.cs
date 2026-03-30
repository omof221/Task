using LogService.Application.Interfaces;
using LogService.Domain.Entities;
using MediatR;

namespace LogService.Application.Commands;

/// WriteLogCommand handler'ı.
/// depolama + seviye bazlı yönlendirme bu sınıfta koordine edilir.
/// Infrastructure'a değil arayüzlere bağımlıdır.

public sealed class WriteLogCommandHandler : IRequestHandler<WriteLogCommand, Guid>
{
    private readonly ILogRepository    _repository;
    private readonly IStructuredLogger _logger;

    public WriteLogCommandHandler(ILogRepository repository, IStructuredLogger logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task<Guid> Handle(WriteLogCommand request, CancellationToken ct)
    {
        var entry = LogEntry.Create(request.Level, request.Message, request.Source, request.Meta);

        await _repository.AddAsync(entry, ct);
        await _repository.SaveChangesAsync(ct);

        // INFO/WARNING → Seq | ERROR/CRITICAL → ELK (IStructuredLogger implementasyonu halleder)
        _logger.Log(request.Level, request.Message, request.Source, request.Meta);

        return entry.Id;
    }
}
