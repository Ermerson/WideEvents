namespace WideEvents.Core.Context;

public static class WideEvent
{
    private static readonly AsyncLocal<WideEventContext?> _current = new();
    
    public static WideEventContext Current
    {
        get
        {
            _current.Value ??= new();
            return _current.Value!;
        }
    }
    
    public static void Add(string key, object? value)
        => Current.Add(key, value);
    
    public static void Reset()
        => _current.Value = null;
}
