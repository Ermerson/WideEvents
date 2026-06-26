using System.Diagnostics;
using WideEvents.Core.Constants;

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

        root[WideEventFieldNames.TraceId] = activity.TraceId.ToString();
        root[WideEventFieldNames.SpanId] = activity.SpanId.ToString();
        root[WideEventFieldNames.TraceFlags] = activity.ActivityTraceFlags.ToString();
    }
}