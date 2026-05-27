using Microsoft.Extensions.Logging;
using WideEvents.Abstractions;

namespace WideEvents.AspNetCore;

/// <summary>
/// Implementação padrão de <see cref="IWideEventExporter"/> que emite o wide event via
/// <see cref="ILogger"/> usando o operador de destructuring <c>@</c>.
/// </summary>
public sealed class LoggerWideEventExporter : IWideEventExporter
{
    private readonly ILogger<LoggerWideEventExporter> _logger;

    public LoggerWideEventExporter(ILogger<LoggerWideEventExporter> logger)
        => _logger = logger;

    public Task ExportAsync(
        IReadOnlyDictionary<string, object?> wideEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WideEvent: {@WideEvent}", wideEvent);
        return Task.CompletedTask;
    }
}
