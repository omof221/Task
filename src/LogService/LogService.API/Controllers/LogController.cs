using LogService.Application.Commands;
using LogService.Application.DTOs;
using LogService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogService.API.Controllers;

/// <summary>
/// Log servisi HTTP API'si.
/// POST /api/logs — WriteLogCommand gönderir ve oluşturulan log kaydının ID'sini döner.
/// </summary>
[ApiController]
[Route("api/logs")]
public sealed class LogController : ControllerBase
{
    private readonly IMediator _mediator;

    public LogController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Yeni log kaydı oluşturur.
    /// </summary>
    /// <param name="request">Log isteği</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Oluşturulan log kaydının ID'si</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WriteLog(
        [FromBody] WriteLogRequest request,
        CancellationToken ct)
    {
        var command = new WriteLogCommand(
            Level:   request.Level,
            Message: request.Message,
            Source:  request.Source,
            Meta:    request.Meta);

        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(WriteLog), new { id }, id);
    }
}

/// <summary>Log yazma isteği modeli.</summary>
public record WriteLogRequest(
    AppLogLevel Level,
    string      Message,
    string?     Source = null,
    string?     Meta   = null);
