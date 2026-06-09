using Microsoft.Extensions.Logging;

namespace WideEvents.Core.Logging;

/// <summary>
/// <see cref="ILoggerProvider"/> that participates in the shared
/// <see cref="IExternalScopeProvider"/> infrastructure. When any logger in the
/// application calls <c>BeginScope()</c>, the scope data is also visible to
/// <c>WideEventBuilder</c> via <see cref="ScopeProvider"/>.
/// </summary>
public sealed class WideEventLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    /// <inheritdoc/>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <summary>
    /// The active scope provider. Exposed internally so that
    /// <c>WideEventBuilder</c> can enumerate active scopes at build time.
    /// </summary>
    internal IExternalScopeProvider ScopeProvider => _scopeProvider;

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
        => new WideEventLogger(_scopeProvider);

    /// <inheritdoc/>
    public void Dispose() { }
}
