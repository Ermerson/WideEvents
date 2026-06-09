using WideEvents.AspNetCore.Enrichers;

namespace WideEvents.AspNetCore;

public sealed class WideEventsOptions
{
    internal AuthEnricherOptions? AuthEnricher { get; private set; }

    public WideEventsOptions UseAuthEnricher(Action<AuthEnricherOptions>? configure = null)
    {
        AuthEnricher ??= new AuthEnricherOptions();
        configure?.Invoke(AuthEnricher);
        return this;
    }
}
