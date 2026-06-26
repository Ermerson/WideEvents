using Microsoft.AspNetCore.Http;
using WideEvents.Abstractions;
using WideEvents.Core.Constants;

namespace WideEvents.AspNetCore.Enrichers;

internal sealed class DefaultHttpEnricher : IHttpWideEventEnricher
{
    public void EnrichRequest(HttpContext context, IWideEventContext wideEvent)
    {
        wideEvent.Add(WideEventFieldNames.HttpMethod, context.Request.Method);
        wideEvent.Add(WideEventFieldNames.HttpPath, context.Request.Path.Value);
    }

    public void EnrichResponse(HttpContext context, IWideEventContext wideEvent)
    {
        wideEvent.Add(WideEventFieldNames.HttpStatusCode, context.Response.StatusCode);
    }
}