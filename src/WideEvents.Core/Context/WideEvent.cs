using WideEvents.Abstractions;

namespace WideEvents.Core.Context;

public static class WideEvent
{
    private static readonly AsyncLocal<IWideEventContext?> _current = new();

    public static IWideEventContext Current
    {
        get
        {
            _current.Value ??= new WideEventContext();
            return _current.Value!;
        }
    }

    public static void Add(string key, object? value)
        => Current.Add(key, value);

    public static void Reset()
        => _current.Value = null;
}
