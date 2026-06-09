using Microsoft.Extensions.Logging;

namespace WideEvents.Core.Logging;

/// <summary>
/// Lightweight <see cref="ILogger"/> whose only purpose is scope propagation.
/// All <c>Log</c> calls are no-ops; <see cref="BeginScope{TState}"/> pushes state
/// to the shared <see cref="IExternalScopeProvider"/> so that
/// <c>WideEventBuilder</c> can later read it via <c>ForEachScope</c>.
/// </summary>
internal sealed class WideEventLogger : ILogger
{
    private readonly IExternalScopeProvider _scopeProvider;

    internal WideEventLogger(IExternalScopeProvider scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _scopeProvider.Push(state);

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => false;

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) { }
}
