using System.Security.Claims;
using WideEvents.Abstractions;
using WideEvents.AspNetCore;

namespace WideEvents.Sample.Api.Enricher;

public sealed class AuthEnricher : IHttpWideEventEnricher
{
    public void EnrichRequest(HttpContext context, IWideEventContext wideEvent)
    {
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        wideEvent.Add("user.id", userId);
    }

    public void EnrichResponse(HttpContext context, IWideEventContext wideEvent) { }
}