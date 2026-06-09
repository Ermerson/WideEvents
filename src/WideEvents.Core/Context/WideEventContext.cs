using System.Collections;
using System.Diagnostics;
using WideEvents.Abstractions;
using WideEvents.Core.Enrichers;

namespace WideEvents.Core.Context;

/// <summary>
/// Default <see cref="IWideEventContext"/> implementation. Accumulates flat key-value
/// attributes and materializes them into a nested dictionary on <see cref="Build"/>.
/// Also implements <see cref="IEnumerable{T}"/> so the raw attribute bag can be pushed
/// as an <c>ILogger</c> scope.
/// </summary>
public sealed class WideEventContext : IWideEventContext, IEnumerable<KeyValuePair<string, object?>>
{
    private readonly Dictionary<string, object?> _attributes = new();
    private readonly IReadOnlyList<IWideEventEnricher> _enrichers;

    /// <summary>
    /// Creates a new context, optionally supplying a custom set of enrichers.
    /// When <paramref name="enrichers"/> is <see langword="null"/>, defaults to
    /// <see cref="TraceActivityEnricher"/>.
    /// </summary>
    public WideEventContext(IEnumerable<IWideEventEnricher>? enrichers = null)
        => _enrichers = enrichers?.ToList()
            ?? new List<IWideEventEnricher> { new TraceActivityEnricher() };

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    public void Add(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (value is not null)
            _attributes[name] = value;
    }

    /// <inheritdoc/>
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

    /// <summary>Iterates the raw (flat) attribute bag.</summary>
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        => _attributes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
