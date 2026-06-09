using System.Diagnostics;
using WideEvents.Core.Context;
using WideEvents.Core.Logging;

namespace WideEvents.Core.Builder;

/// <summary>
/// Default <see cref="IWideEventBuilder"/> implementation.
/// Merges three data sources in ascending precedence order:
/// <list type="number">
///   <item>Scope values from <c>IExternalScopeProvider.ForEachScope()</c></item>
///   <item><see cref="Activity.Current"/> trace IDs and tags</item>
///   <item><see cref="WideEvent.Drain()"/> AsyncLocal buffer (highest priority)</item>
/// </list>
/// The merged flat dictionary is then expanded by <see cref="WideEventStructureBuilder"/>
/// into a nested hierarchy.
/// </summary>
public sealed class WideEventBuilder : IWideEventBuilder
{
    private readonly WideEventLoggerProvider _provider;

    /// <summary>Initializes the builder with the provider that holds the active scope provider.</summary>
    public WideEventBuilder(WideEventLoggerProvider provider)
        => _provider = provider;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Build()
    {
        var flat = new Dictionary<string, object?>();

        // 1. Scope values — lowest precedence
        _provider.ScopeProvider.ForEachScope(
            (scope, state) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> kvps)
                    foreach (var kvp in kvps)
                        if (kvp.Value is not null)
                            state[kvp.Key] = kvp.Value;
            },
            flat);

        // 2. Activity data — middle precedence
        var activity = Activity.Current;
        if (activity is not null)
        {
            flat["trace_id"] = activity.TraceId.ToString();
            flat["span_id"] = activity.SpanId.ToString();
            flat["trace_flags"] = activity.ActivityTraceFlags.ToString();
            foreach (var tag in activity.TagObjects)
                if (tag.Value is not null)
                    flat[tag.Key] = tag.Value;
        }

        // 3. WideEvent AsyncLocal buffer — highest precedence; also clears the context
        foreach (var (key, value) in WideEvent.Drain())
            flat[key] = value;

        return WideEventStructureBuilder.Build(flat);
    }
}
