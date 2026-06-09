using System.Security.Claims;

namespace WideEvents.AspNetCore.Enrichers;

public sealed class AuthEnricherOptions
{
    public string ClaimType { get; set; } = ClaimTypes.NameIdentifier;
    public string FieldName { get; set; } = "user.id";
}
