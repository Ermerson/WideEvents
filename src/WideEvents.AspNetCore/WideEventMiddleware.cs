using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WideEvents.Abstractions;
using WideEvents.Core.Builder;
using WideEvents.Core.Context;

namespace WideEvents.AspNetCore;

/// <summary>
/// ASP.NET Core middleware that wraps each HTTP request in a wide-event context,
/// runs registered <see cref="IHttpWideEventEnricher"/> implementations, and exports
/// the built event after the response is complete.
/// <para>
/// Request metadata (method, path) is pushed as an <c>ILogger</c> scope so that
/// <see cref="IWideEventBuilder"/> can read it via <c>IExternalScopeProvider</c>.
/// Enrichers and application code add further data through <c>WideEvent.Add()</c>.
/// </para>
/// </summary>
public sealed class WideEventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnumerable<IHttpWideEventEnricher> _enrichers;
    private readonly IWideEventExporter _exporter;
    private readonly ILogger<WideEventMiddleware> _logger;
    private readonly IWideEventBuilder _builder;

    /// <summary>Initializes the middleware with its pipeline dependencies.</summary>
    public WideEventMiddleware(
        RequestDelegate next,
        IEnumerable<IHttpWideEventEnricher> enrichers,
        IWideEventExporter exporter,
        ILogger<WideEventMiddleware> logger,
        IWideEventBuilder builder)
    {
        _next = next;
        _enrichers = enrichers;
        _exporter = exporter;
        _logger = logger;
        _builder = builder;
    }

    /// <summary>
    /// Processes the request: opens a scope with request metadata, enriches on entry,
    /// invokes the pipeline, captures <c>error.*</c> fields on exception, and exports
    /// the built event on completion via <see cref="IWideEventBuilder.Build"/>.
    /// </summary>
    public async Task Invoke(HttpContext context, IWideEventContext wideEvent)
    {
        var requestScope = new Dictionary<string, object?>
        {
            ["http.method"] = context.Request.Method,
            ["http.path"] = context.Request.Path.Value,
        };
        using var scope = _logger.BeginScope(requestScope);
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
            var built = _builder.Build();
            await _exporter.ExportAsync(built, context.RequestAborted);
        }
    }
}
