using Microsoft.Extensions.Logging;
using WideEvents.Abstractions;


namespace WideEvents.Core.Exporters;

/// <summary>
/// Default <see cref="IWideEventExporter"/> implementation that exports wide events
/// via <c>ILogger.LogInformation()</c> with the built event as a structured property.
/// </summary>
public sealed class LoggerWideEventExporter : IWideEventExporter
{
    private readonly ILogger<LoggerWideEventExporter> _logger;

    /// <summary>Initializes the exporter with the logger instance.</summary>
    public LoggerWideEventExporter(ILogger<LoggerWideEventExporter> logger)
        => _logger = logger;

    /// <inheritdoc/>
    public Task ExportAsync(IReadOnlyDictionary<string, object?> wideEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WideEvent: {@WideEvent}", wideEvent);
        return Task.CompletedTask;
    }
}