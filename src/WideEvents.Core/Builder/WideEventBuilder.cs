using System.Diagnostics;
using WideEvents.Core.Context;
using WideEvents.Core.Enrichers;
using WideEvents.Core.Logging;

namespace WideEvents.Core.Builder;

/// <summary>
/// Default <see cref="IWideEventBuilder"/> implementation.
/// Merges data sources in ascending precedence order:
/// <list type="number">
///   <item>
///     Scope values — captured at log-call time (includes disposed scopes) plus any
///     scopes still active at build time via <c>IExternalScopeProvider.ForEachScope()</c>.
///   </item>
///   <item><see cref="System.Diagnostics.Activity.Current"/> tag objects.</item>
///   <item><see cref="WideEvent.Drain()"/> AsyncLocal buffer.</item>
///   <item>Enrichers — applied to the nested result after structure building (highest priority).</item>
/// </list>
/// The merged flat dictionary is expanded by <see cref="WideEventStructureBuilder"/> into a
/// nested hierarchy before enrichers run.
/// </summary>
public sealed class WideEventBuilder : IWideEventBuilder
{
    private readonly WideEventLoggerProvider _provider;
    private readonly IReadOnlyList<IWideEventEnricher> _enrichers;

    /// <summary>
    /// Initializes the builder with the provider that holds the active scope provider
    /// and the enrichers applied after the event is built.
    /// </summary>
    public WideEventBuilder(WideEventLoggerProvider provider, IEnumerable<IWideEventEnricher> enrichers)
    {
        _provider = provider;
        _enrichers = enrichers.ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Build()
    {
        var flat = new Dictionary<string, object?>();

        foreach (var (key, value) in _provider.DrainCapturedScopes())
            flat[key] = value;

        _provider.ScopeProvider.ForEachScope((scope, state) =>
            {
                if (scope is not IEnumerable<KeyValuePair<string, object?>> kvps) return;

                foreach (var kvp in kvps)
                    if (kvp.Value is not null)
                        state[kvp.Key] = kvp.Value;
            },
            flat);

        var activity = Activity.Current;
        if (activity is not null)
            foreach (var tag in activity.TagObjects)
                if (tag.Value is not null)
                    flat[tag.Key] = tag.Value;

        foreach (var (key, value) in WideEvent.Drain())
            flat[key] = value;

        var result = WideEventStructureBuilder.Build(flat);

        foreach (var enricher in _enrichers)
            enricher.Enrich(result);

        return result;
    }
}
