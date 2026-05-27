using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace WideEvents.AspNetCore.Tests;

public sealed class LoggerWideEventExporterTests
{
    [Fact]
    public async Task ExportAsync_LogsAtInformationLevel()
    {
        var logger = new Mock<ILogger<LoggerWideEventExporter>>();
        var exporter = new LoggerWideEventExporter(logger.Object);
        var wideEvent = new Dictionary<string, object?> { ["outcome"] = "ok" };

        await exporter.ExportAsync(wideEvent);

        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportAsync_ReturnsCompletedTask()
    {
        var logger = new Mock<ILogger<LoggerWideEventExporter>>();
        var exporter = new LoggerWideEventExporter(logger.Object);

        var task = exporter.ExportAsync(new Dictionary<string, object?>());

        await task; // não deve lançar
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task ExportAsync_RespectsCancellationToken_DoesNotThrow()
    {
        var logger = new Mock<ILogger<LoggerWideEventExporter>>();
        var exporter = new LoggerWideEventExporter(logger.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // LoggerWideEventExporter é síncrono — cancellation não afeta
        var act = () => exporter.ExportAsync(new Dictionary<string, object?>(), cts.Token);
        await act.Should().NotThrowAsync();
    }
}
