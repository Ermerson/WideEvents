using Microsoft.AspNetCore.Http;
using WideEvents.Abstractions;

namespace WideEvents.AspNetCore;

/// <summary>
/// Enriches a wide event with attributes derived from the HTTP request or response.
/// Implement and register via DI to add new fields without modifying the middleware.
/// </summary>
public interface IHttpWideEventEnricher
{
    /// <summary>Called before passing control to the next middleware.</summary>
    void EnrichRequest(HttpContext context, IWideEventContext wideEvent);

    /// <summary>Called after the next middleware returns (success path).</summary>
    void EnrichResponse(HttpContext context, IWideEventContext wideEvent);
}