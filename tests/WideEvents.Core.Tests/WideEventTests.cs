using FluentAssertions;
using WideEvents.Core.Context;
using Xunit;

namespace WideEvents.Core.Tests;

public sealed class WideEventTests
{
    public WideEventTests() => WideEvent.Reset();

    [Fact]
    public void Current_IsNeverNull()
    {
        WideEvent.Current.Should().NotBeNull();
    }

    [Fact]
    public void Add_DelegatesToCurrentContext()
    {
        WideEvent.Add("user.id", "user_456");

        WideEvent.Current.Build()
            .Should().ContainKey("user");
    }

    [Fact]
    public void Reset_ClearsAccumulatedAttributes()
    {
        WideEvent.Add("outcome", "error");
        WideEvent.Reset();

        WideEvent.Current.Build().Should().NotContainKey("outcome");
    }
}
