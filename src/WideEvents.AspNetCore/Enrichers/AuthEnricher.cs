using Microsoft.AspNetCore.Http;
using WideEvents.Abstractions;

namespace WideEvents.AspNetCore.Enrichers;

/// <summary>
/// Enricher that reads a configured claim from the authenticated user and writes its
/// value into the wide event on each request.
/// </summary>
/// <remarks>
/// When the request is unauthenticated or the configured claim is absent,
/// <see cref="EnrichRequest"/> is a silent no-op: <c>HttpContext.User.FindFirst()</c>
/// returns <see langword="null"/>, and <c>IWideEventContext.Add()</c> silently skips
/// null values. No exception is thrown and no log entry is written — the field is
/// simply absent from the emitted event. This is by design; observability
/// infrastructure must never interfere with request processing.
/// </remarks>
public sealed class AuthEnricher(AuthEnricherOptions options) : IHttpWideEventEnricher
{
    /// <inheritdoc/>
    public void EnrichRequest(HttpContext context, IWideEventContext wideEvent)
    {
        var value = context.User?.FindFirst(options.ClaimType)?.Value;
        wideEvent.Add(options.FieldName, value);
    }
}
