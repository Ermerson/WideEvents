using WideEvents.Abstractions;

namespace WideEvents.Core.Context;

public static class WideEvent
{
    private static readonly AsyncLocal<IWideEventContext?> _current = new();
    
    public static IWideEventContext Current
        => _current.Value ??= WideEventContextFactory.Create();

    public static void Add(string key, object? value)
        => Current.Add(key, value);

    public static void Reset()
        => _current.Value = null;
    
    public static void SetFactory(Func<IWideEventContext> factory)
        => WideEventContextFactory.SetFactory(factory);
}