using Microsoft.Extensions.Logging;
using WideEvents.Abstractions;

namespace WideEvents.AspNetCore;

/// <summary>
/// Default <see cref="IWideEventExporter"/> implementation that emits the wide event via
/// <see cref="ILogger"/> using the destructuring operator <c>@</c>.
/// </summary>
public sealed class LoggerWideEventExporter : IWideEventExporter
{
    private readonly ILogger<LoggerWideEventExporter> _logger;

    /// <summary>Initializes the exporter with the logger instance.</summary>
    public LoggerWideEventExporter(ILogger<LoggerWideEventExporter> logger)
        => _logger = logger;

    /// <inheritdoc/>
    public Task ExportAsync(
        IReadOnlyDictionary<string, object?> wideEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WideEvent: {@WideEvent}", wideEvent);
        return Task.CompletedTask;
    }
}
