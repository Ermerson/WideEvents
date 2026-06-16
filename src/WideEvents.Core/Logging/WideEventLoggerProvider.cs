using Microsoft.Extensions.Logging;

namespace WideEvents.Core.Logging;

/// <summary>
/// <see cref="ILoggerProvider"/> that participates in the shared
/// <see cref="IExternalScopeProvider"/> infrastructure and accumulates scope data
/// into a per-request store via <see cref="WideEventLogger"/>. When any logger in
/// the application emits a log entry, the active scope key-value pairs are captured
/// so that <c>WideEventBuilder</c> can include them in the final wide event even
/// after those scopes have been disposed.
/// </summary>
public sealed class WideEventLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly AsyncLocal<Dictionary<string, object?>?> _capturedScopes = new();
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    /// <inheritdoc/>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <summary>
    /// The active scope provider. Exposed internally so that
    /// <c>WideEventBuilder</c> can enumerate scopes still open at build time.
    /// </summary>
    internal IExternalScopeProvider ScopeProvider => _scopeProvider;

    /// <summary>
    /// Returns and clears all scope key-value pairs accumulated via log calls since
    /// the last drain. Called once per request by <c>WideEventBuilder.Build()</c>.
    /// </summary>
    internal IReadOnlyDictionary<string, object?> DrainCapturedScopes()
    {
        var captured = _capturedScopes.Value ?? new Dictionary<string, object?>();
        _capturedScopes.Value = null;
        return captured;
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
        => new WideEventLogger(_scopeProvider, _capturedScopes);

    /// <inheritdoc/>
    public void Dispose() { }
}
