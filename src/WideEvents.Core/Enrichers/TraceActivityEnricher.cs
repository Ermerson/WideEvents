using System.Diagnostics;

namespace WideEvents.Core.Enrichers;

public sealed class TraceActivityEnricher : IWideEventEnricher
{
    public void Enrich(Dictionary<string, object?> root)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        root["trace_id"] = activity.TraceId.ToString();
        root["span_id"] = activity.SpanId.ToString();
        root["trace_flags"] = activity.ActivityTraceFlags.ToString();
    }
}