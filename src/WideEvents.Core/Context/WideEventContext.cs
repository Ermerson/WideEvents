using System.Collections;
using System.Diagnostics;
using WideEvents.Abstractions;
using WideEvents.Core.Enrichers;

namespace WideEvents.Core.Context;

public sealed class WideEventContext : IWideEventContext, IEnumerable<KeyValuePair<string, object?>>
{
    private readonly Dictionary<string, object?> _attributes = new();
    private readonly IReadOnlyList<IWideEventEnricher> _enrichers;
    
    public WideEventContext(IEnumerable<IWideEventEnricher>? enrichers = null)
        => _enrichers = enrichers?.ToList()
            ?? new List<IWideEventEnricher> { new TraceActivityEnricher() };
    
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

        foreach (var enricher in _enrichers)
            enricher.Enrich(root);

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

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        => _attributes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
