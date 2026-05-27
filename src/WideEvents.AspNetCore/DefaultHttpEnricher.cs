using Microsoft.AspNetCore.Http;
using WideEvents.Abstractions;

namespace WideEvents.AspNetCore;

internal sealed class DefaultHttpEnricher : IHttpWideEventEnricher
{
    public void EnrichRequest(HttpContext context, IWideEventContext wideEvent)
    {
        wideEvent.Add("http.method", context.Request.Method);
        wideEvent.Add("http.path", context.Request.Path.Value);
    }

    public void EnrichResponse(HttpContext context, IWideEventContext wideEvent)
    {
        wideEvent.Add("http.status_code", context.Response.StatusCode);
    }
}