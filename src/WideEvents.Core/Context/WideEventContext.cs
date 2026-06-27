using System.Collections;
using WideEvents.Abstractions;
using WideEvents.Core.Builder;
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
    /// Creates a new context with an optional set of enrichers applied during <see cref="Build"/>.
    /// When <paramref name="enrichers"/> is <see langword="null"/>, no enrichers are applied.
    /// The default enrichers for standalone use are configured in <see cref="WideEventContextFactory"/>.
    /// </summary>
    public WideEventContext(IEnumerable<IWideEventEnricher>? enrichers = null)
        => _enrichers = enrichers?.ToList() ?? [];

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
        var root = WideEventStructureBuilder.Build(_attributes);
        foreach (var enricher in _enrichers)
            enricher.Enrich(root);
        return root;
    }

    /// <summary>
    /// Returns a snapshot of the current raw attributes without nesting or enrichment.
    /// Called by <see cref="WideEvent.Drain"/> to extract data for <c>WideEventBuilder</c>.
    /// </summary>
    internal IReadOnlyDictionary<string, object?> Drain()
        => new Dictionary<string, object?>(_attributes);

    /// <summary>Iterates the raw (flat) attribute bag.</summary>
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        => _attributes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
