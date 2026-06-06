using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WideEvents.Core.Context;
using Xunit;

namespace WideEvents.AspNetCore.Tests;

public sealed class WideEventExtensionsTests
{
    [Fact]
    public void AddWideEvents_WithoutOptions_DoesNotRegisterAuthEnricher()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddWideEvents();

        var provider = services.BuildServiceProvider();
        var enrichers = provider.GetServices<IHttpWideEventEnricher>();
        enrichers.Should().NotContain(e => e is AuthEnricher);
    }

    [Fact]
    public void AddWideEvents_WithUseAuthEnricher_RegistersAuthEnricher()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddWideEvents(o => o.UseAuthEnricher());

        var provider = services.BuildServiceProvider();
        var enrichers = provider.GetServices<IHttpWideEventEnricher>();
        enrichers.Should().Contain(e => e is AuthEnricher);
    }

    [Fact]
    public void AddWideEvents_AlwaysRegistersDefaultHttpEnricher()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddWideEvents();

        var provider = services.BuildServiceProvider();
        var enrichers = provider.GetServices<IHttpWideEventEnricher>();
        enrichers.Should().Contain(e => e is DefaultHttpEnricher);
    }

    [Fact]
    public void AddWideEvents_WithCustomAuthEnricherOptions_HonorsConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWideEvents(o => o.UseAuthEnricher(a =>
        {
            a.ClaimType = "sub";
            a.FieldName = "auth.uid";
        }));
        var provider = services.BuildServiceProvider();
        var enricher = provider.GetServices<IHttpWideEventEnricher>().OfType<AuthEnricher>().Single();

        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user_789")]));
        var wideEvent = new WideEventContext(enrichers: []);
        enricher.EnrichRequest(ctx, wideEvent);

        var built = wideEvent.Build();
        var auth = built["auth"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        auth["uid"].Should().Be("user_789");
    }
}
