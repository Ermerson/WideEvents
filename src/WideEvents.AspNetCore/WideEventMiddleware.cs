using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using WideEvents.Abstractions;
using WideEvents.Core.Context;

namespace WideEvents.AspNetCore;

public sealed class WideEventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnumerable<IHttpWideEventEnricher> _enrichers;
    private readonly IWideEventExporter _exporter;

    public WideEventMiddleware(
        RequestDelegate next,
        IEnumerable<IHttpWideEventEnricher> enrichers,
        IWideEventExporter exporter)
    {
        _next = next;
        _enrichers = enrichers;
        _exporter = exporter;
    }

    // IWideEventContext é injetado como Scoped via Invoke() — um contexto por request.
    // Registrado no DI como _ => WideEvent.Current para que chamadas a WideEvent.Add()
    // no código do handler refiram a mesma instância.
    public async Task Invoke(HttpContext context, IWideEventContext wideEvent)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            foreach (var enricher in _enrichers)
                enricher.EnrichRequest(context, wideEvent);

            await _next(context);

            foreach (var enricher in _enrichers)
                enricher.EnrichResponse(context, wideEvent);
        }
        catch (Exception ex)
        {
            wideEvent.Add("error.type", ex.GetType().Name);
            wideEvent.Add("error.message", ex.Message);
            throw;
        }
        finally
        {
            wideEvent.Add("duration_ms", Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            await _exporter.ExportAsync(wideEvent.Build(), context.RequestAborted);
            WideEvent.Reset(); // limpa o AsyncLocal ao fim da request
        }
    }
}
