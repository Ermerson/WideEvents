using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WideEvents.Abstractions;
using WideEvents.Core.Context;

namespace WideEvents.AspNetCore;

public sealed class WideEventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnumerable<IHttpWideEventEnricher> _enrichers;
    private readonly IWideEventExporter _exporter;
    private readonly ILogger<WideEventMiddleware> _logger;

    public WideEventMiddleware(
        RequestDelegate next,
        IEnumerable<IHttpWideEventEnricher> enrichers,
        IWideEventExporter exporter,
        ILogger<WideEventMiddleware> logger)
    {
        _next = next;
        _enrichers = enrichers;
        _exporter = exporter;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, IWideEventContext wideEvent)
    {
        using var scope = _logger.BeginScope(wideEvent);
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
            WideEvent.Reset();
        }
    }
}
