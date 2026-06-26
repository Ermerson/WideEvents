using System.Security.Claims;
using WideEvents.Core.Constants;

namespace WideEvents.AspNetCore.Enrichers;

/// <summary>Configuration for <see cref="AuthEnricher"/>.</summary>
public sealed class AuthEnricherOptions
{
    /// <summary>Claim type to read from the authenticated user. Defaults to <see cref="ClaimTypes.NameIdentifier"/>.</summary>
    public string ClaimType { get; set; } = ClaimTypes.NameIdentifier;

    /// <summary>Wide-event field name where the claim value is written. Defaults to <c>"user.id"</c>.</summary>
    public string FieldName { get; set; } = WideEventFieldNames.UserId;
}
