using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using WideEvents.AspNetCore.Enrichers;
using WideEvents.Core.Context;
using Xunit;

namespace WideEvents.AspNetCore.Tests;

public sealed class AuthEnricherTests
{
    [Fact]
    public void EnrichRequest_AddsFieldWithClaimValue()
    {
        var enricher = new AuthEnricher(new AuthEnricherOptions());
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user_123")]));
        var wideEvent = new WideEventContext(enrichers: []);

        enricher.EnrichRequest(ctx, wideEvent);

        var built = wideEvent.Build();
        var user = built["user"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        user["id"].Should().Be("user_123");
    }

    [Fact]
    public void EnrichRequest_WhenClaimMissing_IgnoresNullValue()
    {
        // WideEventContext.Add ignora null — chave não deve aparecer no evento
        var enricher = new AuthEnricher(new AuthEnricherOptions());
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity());
        var wideEvent = new WideEventContext(enrichers: []);

        enricher.EnrichRequest(ctx, wideEvent);

        wideEvent.Build().Should().NotContainKey("user");
    }

    [Fact]
    public void EnrichRequest_UsesCustomClaimTypeAndFieldName()
    {
        var options = new AuthEnricherOptions { ClaimType = "sub", FieldName = "auth.subject" };
        var enricher = new AuthEnricher(options);
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("sub", "subject_abc")]));
        var wideEvent = new WideEventContext(enrichers: []);

        enricher.EnrichRequest(ctx, wideEvent);

        var built = wideEvent.Build();
        var auth = built["auth"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        auth["subject"].Should().Be("subject_abc");
    }

    [Fact]
    public void EnrichRequest_WhenUserIsNull_IgnoresNullValue()
    {
        var enricher = new AuthEnricher(new AuthEnricherOptions());
        var ctx = new DefaultHttpContext();
        ctx.User = null!;
        var wideEvent = new WideEventContext(enrichers: []);

        enricher.EnrichRequest(ctx, wideEvent);

        wideEvent.Build().Should().NotContainKey("user");
    }

    [Fact]
    public void EnrichResponse_DoesNotAddAnyField()
    {
        var enricher = new AuthEnricher(new AuthEnricherOptions());
        var wideEvent = new WideEventContext(enrichers: []);

        enricher.EnrichResponse(new DefaultHttpContext(), wideEvent);

        wideEvent.Build().Should().BeEmpty();
    }
}
