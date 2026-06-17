using WideEvents.AspNetCore.Enrichers;

namespace WideEvents.AspNetCore;

/// <summary>Fluent configuration object passed to <see cref="WideEventExtensions.AddWideEvents"/>.</summary>
public sealed class WideEventsOptions
{
    internal AuthEnricherOptions? AuthEnricher { get; private set; }
    internal bool TraceEnricher { get; private set; }

    /// <summary>
    /// Enables the <see cref="AuthEnricher"/> and optionally configures which claim and
    /// field name it uses. Defaults to <c>ClaimTypes.NameIdentifier</c> → <c>"user.id"</c>.
    /// </summary>
    public WideEventsOptions UseAuthEnricher(Action<AuthEnricherOptions>? configure = null)
    {
        AuthEnricher ??= new AuthEnricherOptions();
        configure?.Invoke(AuthEnricher);
        return this;
    }

    /// <summary>
    /// Enables the <see cref="WideEvents.Core.Enrichers.TraceActivityEnricher"/>, which injects
    /// <c>trace_id</c>, <c>span_id</c>, and <c>trace_flags</c> from <see cref="System.Diagnostics.Activity.Current"/>
    /// into every wide event.
    /// </summary>
    public WideEventsOptions UseTraceEnricher()
    {
        TraceEnricher = true;
        return this;
    }
}
