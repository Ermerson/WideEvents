using System.Diagnostics;

namespace WideEvents.Core.Enrichers;

/// <summary>
/// Built-in enricher that injects <c>trace_id</c>, <c>span_id</c>, and <c>trace_flags</c>
/// from <see cref="Activity.Current"/>. No-op when no activity is active.
/// </summary>
public sealed class TraceActivityEnricher : IWideEventEnricher
{
    /// <inheritdoc/>
    public void Enrich(Dictionary<string, object?> root)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        root["trace_id"] = activity.TraceId.ToString();
        root["span_id"] = activity.SpanId.ToString();
        root["trace_flags"] = activity.ActivityTraceFlags.ToString();
    }
}