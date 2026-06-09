namespace WideEvents.Core.Builder;

/// <summary>
/// Builds the final wide event by merging all context sources:
/// <see cref="System.Diagnostics.Activity.Current"/> trace data, active
/// <c>ILogger</c> scopes, and the <c>WideEvent</c> AsyncLocal buffer.
/// </summary>
public interface IWideEventBuilder
{
    /// <summary>
    /// Produces a fully-merged, nested wide event dictionary.
    /// Merge precedence (highest wins): <c>WideEvent.Add()</c> values
    /// &gt; Activity tags &gt; scope values.
    /// </summary>
    IReadOnlyDictionary<string, object?> Build();
}
