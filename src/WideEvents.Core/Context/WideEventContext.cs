using System.Diagnostics;
using WideEvents.Abstractions;

namespace WideEvents.Core.Context;

public sealed class WideEventContext : IWideEventContext
{
    private readonly Dictionary<string, object?> _attributes = new();

    public void Add(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (value is not null)
            _attributes[name] = value;
    }

    public IReadOnlyDictionary<string, object?> Build()
    {
        var root = new Dictionary<string, object?>();

        foreach (var (key, value) in _attributes)
            SetNested(root, key, value);

        var activity = Activity.Current;
        if (activity is not null)
        {
            root["trace_id"] = activity.TraceId.ToString();
            root["span_id"] = activity.SpanId.ToString();
            root["trace_flags"] = activity.ActivityTraceFlags.ToString();
        }

        return root;
    }

    private static void SetNested(Dictionary<string, object?> root, string key, object? value)
    {
        var segments = key.Split('.');
        var current = root;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (current.TryGetValue(segments[i], out var existing)
                && existing is Dictionary<string, object?> child)
            {
                current = child;
            }
            else
            {
                var created = new Dictionary<string, object?>();
                current[segments[i]] = created;
                current = created;
            }
        }

        current[segments[^1]] = value;
    }
}
