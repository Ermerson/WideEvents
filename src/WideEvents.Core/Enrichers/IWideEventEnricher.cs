namespace WideEvents.Core.Enrichers;

/// <summary>
/// Pluggable hook that injects additional fields into the wide-event dictionary at build time.
/// </summary>
public interface IWideEventEnricher
{
    /// <summary>Called once per <c>Build()</c> with the fully materialized root dictionary.</summary>
    void Enrich(Dictionary<string, object?> root);
}
