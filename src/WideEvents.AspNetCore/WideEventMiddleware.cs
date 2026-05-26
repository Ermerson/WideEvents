using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WideEvents.Core.Context;

namespace WideEvents.AspNetCore;

public sealed class WideEventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WideEventMiddleware> _logger;
    
    public WideEventMiddleware(RequestDelegate next, ILogger<WideEventMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var start = Stopwatch.GetTimestamp();
        
        try
        {
            WideEvent.Add("http.method", context.Request.Method);
            WideEvent.Add("http.path", context.Request.Path.Value);

            await _next(context);
            
            WideEvent.Add("http.status_code", context.Response.StatusCode);
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

            _logger.LogInformation("WideEvent: {@WideEvent}", WideEvent.Current?.Build());
            
            WideEvent.Reset();
        }
    }
}
