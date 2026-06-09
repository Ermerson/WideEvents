using WideEvents.AspNetCore.Enrichers;

namespace WideEvents.AspNetCore;

/// <summary>Fluent configuration object passed to <see cref="WideEventExtensions.AddWideEvents"/>.</summary>
public sealed class WideEventsOptions
{
    internal AuthEnricherOptions? AuthEnricher { get; private set; }

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
}
