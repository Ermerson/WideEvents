using Microsoft.Extensions.Logging;
using WideEvents.Abstractions;


namespace WideEvents.Core.Exporters;

public sealed class LoggerWideEventExporter : IWideEventExporter
{
    private readonly ILogger<LoggerWideEventExporter> _logger;

    public LoggerWideEventExporter(ILogger<LoggerWideEventExporter> logger)
        => _logger = logger;

    public Task ExportAsync(IReadOnlyDictionary<string, object?> wideEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WideEvent: {@WideEvent}", wideEvent);
        return Task.CompletedTask;
    }
}