using System.Diagnostics;

namespace WideEvents.Core.Context;

public sealed class WideEventContext
{
    private readonly Dictionary<string, object?> _events = new();
    
    public void Add(string name, object? @event)
    {
        if (@event is not null) 
            _events.Add(name, @event);
    }

    public IReadOnlyDictionary<string, object?> Build()
    {
        var activity = Activity.Current;
        
        if (activity is null) return _events;
        
        _events["trace_id"] = activity.TraceId;
        _events["span_id"] = activity.SpanId;
        _events["trace_flags"] = activity.ActivityTraceFlags;
        
        return _events;
    }
}