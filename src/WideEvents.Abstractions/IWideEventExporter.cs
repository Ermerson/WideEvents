namespace WideEvents.Abstractions;

/// <summary>
/// Sends a built wide event to a downstream destination (e.g. OTLP, Kafka, stdout).
/// </summary>
public interface IWideEventExporter
{
    /// <summary>
    /// Exports a single built wide event.
    /// </summary>
    Task ExportAsync(IReadOnlyDictionary<string, object?> wideEvent, CancellationToken cancellationToken = default);
}
