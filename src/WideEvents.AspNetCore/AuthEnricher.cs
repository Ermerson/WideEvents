using Microsoft.AspNetCore.Http;
using WideEvents.Abstractions;

namespace WideEvents.AspNetCore;

public sealed class AuthEnricher(AuthEnricherOptions options) : IHttpWideEventEnricher
{
    public void EnrichRequest(HttpContext context, IWideEventContext wideEvent)
    {
        var value = context.User?.FindFirst(options.ClaimType)?.Value;
        wideEvent.Add(options.FieldName, value);
    }

    public void EnrichResponse(HttpContext context, IWideEventContext wideEvent) { }
}
