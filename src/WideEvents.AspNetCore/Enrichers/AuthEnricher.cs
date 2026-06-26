using Microsoft.AspNetCore.Http;
using WideEvents.Abstractions;

namespace WideEvents.AspNetCore.Enrichers;

/// <summary>
/// Enricher that reads a configured claim from the authenticated user and writes its
/// value into the wide event on each request.
/// </summary>
public sealed class AuthEnricher(AuthEnricherOptions options) : IHttpWideEventEnricher
{
    /// <inheritdoc/>
    /// <remarks>No-op when the claim is absent or the user is unauthenticated.</remarks>
    public void EnrichRequest(HttpContext context, IWideEventContext wideEvent)
    {
        var value = context.User?.FindFirst(options.ClaimType)?.Value;
        wideEvent.Add(options.FieldName, value);
    }
}
