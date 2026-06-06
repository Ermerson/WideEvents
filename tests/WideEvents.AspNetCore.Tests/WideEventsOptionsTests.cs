using System.Security.Claims;
using FluentAssertions;
using Xunit;

namespace WideEvents.AspNetCore.Tests;

public sealed class WideEventsOptionsTests
{
    [Fact]
    public void WithoutUseAuthEnricher_AuthEnricherIsNull()
    {
        var options = new WideEventsOptions();

        options.AuthEnricher.Should().BeNull();
    }

    [Fact]
    public void UseAuthEnricher_WithNoArgs_SetsDefaults()
    {
        var options = new WideEventsOptions();

        options.UseAuthEnricher();

        options.AuthEnricher.Should().NotBeNull();
        options.AuthEnricher!.ClaimType.Should().Be(ClaimTypes.NameIdentifier);
        options.AuthEnricher!.FieldName.Should().Be("user.id");
    }

    [Fact]
    public void UseAuthEnricher_WithCustomConfig_OverridesDefaults()
    {
        var options = new WideEventsOptions();

        options.UseAuthEnricher(o =>
        {
            o.ClaimType = "sub";
            o.FieldName = "auth.subject";
        });

        options.AuthEnricher!.ClaimType.Should().Be("sub");
        options.AuthEnricher!.FieldName.Should().Be("auth.subject");
    }

    [Fact]
    public void UseAuthEnricher_ReturnsItself_ForChaining()
    {
        var options = new WideEventsOptions();

        var result = options.UseAuthEnricher();

        result.Should().BeSameAs(options);
    }

    [Fact]
    public void UseAuthEnricher_CalledTwice_AccumulatesConfig()
    {
        var options = new WideEventsOptions();

        options.UseAuthEnricher(o => o.ClaimType = "sub");
        options.UseAuthEnricher(o => o.FieldName = "auth.id");

        options.AuthEnricher!.ClaimType.Should().Be("sub");
        options.AuthEnricher!.FieldName.Should().Be("auth.id");
    }
}
