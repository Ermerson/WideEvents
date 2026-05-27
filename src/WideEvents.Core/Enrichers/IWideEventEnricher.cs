namespace WideEvents.Core.Enrichers;

public interface IWideEventEnricher
{
    void Enrich(Dictionary<string, object?> root);
}
