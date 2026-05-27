using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WideEvents.Abstractions;
using WideEvents.Core.Context;

namespace WideEvents.AspNetCore;

public sealed class WideEventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WideEventMiddleware> _logger;
    private readonly IEnumerable<IHttpWideEventEnricher> _enrichers;
    private readonly IWideEventExporter _exporter;

    public WideEventMiddleware(
        RequestDelegate next, 
        ILogger<WideEventMiddleware> logger, 
        IEnumerable<IHttpWideEventEnricher> enrichers,
        IWideEventExporter exporter)
    {
        _next = next;
        _logger = logger;
        _enrichers = enrichers;
        _exporter = exporter;
    }

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
            WideEvent.Add("error.type", ex.GetType().Name);
            WideEvent.Add("error.message", ex.Message);
            
            throw;
        }
        finally
        {
            WideEvent.Add("duration_ms", Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            await _exporter.ExportAsync(wideEvent.Build(), context.RequestAborted);
        }
    }
}
