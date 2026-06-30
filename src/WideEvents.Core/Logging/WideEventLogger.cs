using Microsoft.Extensions.Logging;

namespace WideEvents.Core.Logging;

/// <summary>
/// <see cref="ILogger"/> that captures active scope key-value pairs into a per-request
/// <see cref="AsyncLocal{T}"/> store on every <c>Log</c> call. This lets
/// <c>WideEventBuilder</c> include scope data from scopes that are already disposed
/// by the time <c>Build()</c> runs — provided at least one log call occurred while
/// the scope was active.
/// </summary>
internal sealed class WideEventLogger : ILogger
{
    private readonly IExternalScopeProvider _scopeProvider;
    private readonly AsyncLocal<Dictionary<string, object?>?> _store;
    private readonly LogLevel _minimumLevel;

    internal WideEventLogger(
        IExternalScopeProvider scopeProvider,
        AsyncLocal<Dictionary<string, object?>?> store,
        LogLevel minimumLevel)
    {
        _scopeProvider = scopeProvider;
        _store = store;
        _minimumLevel = minimumLevel;
    }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _scopeProvider.Push(state);

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimumLevel;

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        _store.Value ??= new Dictionary<string, object?>();
        _scopeProvider.ForEachScope(
            (scope, dict) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> kvps)
                    foreach (var kvp in kvps)
                        if (kvp.Value is not null) dict[kvp.Key] = kvp.Value;
            },
            _store.Value);
    }
}
