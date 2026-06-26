using Microsoft.AspNetCore.Http;
using WideEvents.Abstractions;

namespace WideEvents.AspNetCore;

/// <summary>
/// Enriches a wide event with attributes derived from the HTTP request or response.
/// Implement and register via DI to add new fields without modifying the middleware.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EnrichRequest"/> is called before the next middleware runs (always).
/// </para>
/// <para>
/// <see cref="EnrichResponse"/> is called only on the <b>success path</b> — i.e.,
/// when the pipeline completes without throwing. If the pipeline throws an unhandled
/// exception, <see cref="EnrichResponse"/> is <b>not</b> invoked. In that case the
/// middleware captures <c>error.type</c> and <c>error.message</c> directly and
/// re-throws. If your enricher needs to add data regardless of outcome, do it in
/// <see cref="EnrichRequest"/> or read from the <see cref="Microsoft.AspNetCore.Http.HttpContext"/>
/// inside <see cref="EnrichResponse"/> defensively (e.g., check
/// <c>context.Response.StatusCode</c> only after the pipeline succeeds).
/// </para>
/// </remarks>
public interface IHttpWideEventEnricher
{
    /// <summary>Called before passing control to the next middleware.</summary>
    void EnrichRequest(HttpContext context, IWideEventContext wideEvent);

    /// <summary>
    /// Called after the next middleware returns on the success path only.
    /// Not invoked when the pipeline throws an unhandled exception.
    /// </summary>
    void EnrichResponse(HttpContext context, IWideEventContext wideEvent)
    {
    }
}