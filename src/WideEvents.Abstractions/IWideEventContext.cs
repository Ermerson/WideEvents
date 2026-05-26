namespace WideEvents.Abstractions;

/// <summary>
/// Accumulates attributes for a single unit of work and builds the resulting wide event.
/// </summary>
public interface IWideEventContext
{
    /// <summary>
    /// Adds or overwrites an attribute. Dotted names (e.g. <c>payment.method</c>)
    /// are expanded into nested objects when the event is built. Null values are ignored.
    /// </summary>
    void Add(string name, object? value);

    /// <summary>
    /// Materializes the accumulated attributes into a single structured wide event.
    /// </summary>
    IReadOnlyDictionary<string, object?> Build();
}
