using WideEvents.Abstractions;

namespace WideEvents.Core.Context;

/// <summary>
/// Static facade that provides ambient access to the <see cref="IWideEventContext"/>
/// for the current async flow via <see cref="AsyncLocal{T}"/>.
/// </summary>
public static class WideEvent
{
    private static readonly AsyncLocal<IWideEventContext?> CurrentContext = new();

    /// <summary>Gets (or lazily creates) the context for the current async flow.</summary>
    public static IWideEventContext Current
        => CurrentContext.Value ??= WideEventContextFactory.Create();

    /// <summary>Adds or overwrites an attribute on the current context.</summary>
    public static void Add(string key, object? value)
        => Current.Add(key, value);

    /// <summary>Clears the context for the current async flow.</summary>
    public static void Reset()
        => CurrentContext.Value = null;

    /// <summary>
    /// Replaces the factory used to create new contexts.
    /// Useful in tests to substitute a custom <see cref="IWideEventContext"/> implementation.
    /// </summary>
    public static void SetFactory(Func<IWideEventContext> factory)
        => WideEventContextFactory.SetFactory(factory);
}