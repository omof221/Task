using FluentAssertions;
using LogService.Application.Commands;
using LogService.Application.Interfaces;
using LogService.Domain.Entities;
using LogService.Domain.Enums;
using Moq;

namespace LogService.Tests.Unit;


/// WriteLogCommandHandler birim testleri.
/// SRP: Her test tek bir davranışı doğrular.
/// Moq: ILogRepository ve IStructuredLogger izole edilir.

public class WriteLogCommandHandlerTests
{
    private readonly Mock<ILogRepository>    _repoMock   = new();
    private readonly Mock<IStructuredLogger> _loggerMock = new();
    private readonly WriteLogCommandHandler  _handler;

    public WriteLogCommandHandlerTests()
        => _handler = new WriteLogCommandHandler(_repoMock.Object, _loggerMock.Object);

    // ── AddAsync çağrılmalı ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_AddsEntryToRepository()
    {
        var command = new WriteLogCommand(AppLogLevel.INFO, "Test mesajı", "TestSource");

        await _handler.Handle(command, CancellationToken.None);

        _repoMock.Verify(r =>
            r.AddAsync(It.Is<LogEntry>(e =>
                e.Message == "Test mesajı" &&
                e.Level   == AppLogLevel.INFO &&
                e.Source  == "TestSource"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── SaveChangesAsync çağrılmalı ──────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_SavesChanges()
    {
        var command = new WriteLogCommand(AppLogLevel.WARNING, "Uyarı", "Svc");

        await _handler.Handle(command, CancellationToken.None);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── IStructuredLogger.Log çağrılmalı ─────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_DelegatesToStructuredLogger()
    {
        var command = new WriteLogCommand(AppLogLevel.ERROR, "Hata oluştu", "Api", "{\"code\":500}");

        await _handler.Handle(command, CancellationToken.None);

        _loggerMock.Verify(l =>
            l.Log(AppLogLevel.ERROR, "Hata oluştu", "Api", "{\"code\":500}"), Times.Once);
    }

    // ── Döndürülen değer boş olmamalı ────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNonEmptyGuid()
    {
        var command = new WriteLogCommand(AppLogLevel.INFO, "Mesaj");

        var id = await _handler.Handle(command, CancellationToken.None);

        id.Should().NotBeEmpty();
    }

    // ── Boş mesaj ile LogEntry.Create hata vermeli ────────────────────────

    [Fact]
    public async Task Handle_EmptyMessage_ThrowsArgumentException()
    {
        var command = new WriteLogCommand(AppLogLevel.INFO, "");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── CRITICAL seviyesi logger'a iletilmeli ────────────────────────────

    [Theory]
    [InlineData(AppLogLevel.INFO)]
    [InlineData(AppLogLevel.WARNING)]
    [InlineData(AppLogLevel.ERROR)]
    [InlineData(AppLogLevel.CRITICAL)]
    public async Task Handle_AllLevels_DelegatesCorrectLevelToLogger(AppLogLevel level)
    {
        var command = new WriteLogCommand(level, "Seviye testi");

        await _handler.Handle(command, CancellationToken.None);

        _loggerMock.Verify(l => l.Log(level, "Seviye testi", null, null), Times.Once);
    }
}
