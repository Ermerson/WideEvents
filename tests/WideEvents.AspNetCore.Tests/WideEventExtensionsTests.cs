using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
}
