namespace WideEvents.Core.Context;

public interface IWideEventEnricher
{
    void Enrich(Dictionary<string, object?> root);
}