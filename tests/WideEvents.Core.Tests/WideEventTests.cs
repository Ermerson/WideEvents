using FluentAssertions;
using Moq;
using WideEvents.Abstractions;
using WideEvents.Core.Context;
using Xunit;

namespace WideEvents.Core.Tests;

public sealed class WideEventTests : IDisposable
{
    // Garante que o estado global seja restaurado após cada teste
    public WideEventTests()
    {
        WideEvent.SetFactory(static () => new WideEventContext());
        WideEvent.Reset();
    }

    public void Dispose()
    {
        WideEvent.SetFactory(static () => new WideEventContext());
        WideEvent.Reset();
    }

    // ── Comportamento básico ───────────────────────────────────────────────────

    [Fact]
    public void Current_IsNeverNull()
    {
        WideEvent.Current.Should().NotBeNull();
    }

    [Fact]
    public void Current_ReturnsSameInstance_WithinSameContext()
    {
        var first = WideEvent.Current;
        var second = WideEvent.Current;

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Add_DelegatesToCurrentContext()
    {
        WideEvent.Add("user.id", "user_456");

        WideEvent.Current.Build().Should().ContainKey("user");
    }

    [Fact]
    public void Reset_ClearsAccumulatedAttributes()
    {
        WideEvent.Add("outcome", "error");
        WideEvent.Reset();

        WideEvent.Current.Build().Should().NotContainKey("outcome");
    }

    [Fact]
    public void Reset_AfterReset_CurrentCreatesNewContext()
    {
        var before = WideEvent.Current;
        WideEvent.Reset();
        var after = WideEvent.Current;

        after.Should().NotBeSameAs(before);
    }

    // ── SetFactory ─────────────────────────────────────────────────────────────

    [Fact]
    public void SetFactory_Null_Throws()
    {
        var act = () => WideEvent.SetFactory(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetFactory_ReplacesContextCreation()
    {
        var fake = new Mock<IWideEventContext>().Object;
        WideEvent.SetFactory(() => fake);
        WideEvent.Reset(); // força criação pela nova factory

        WideEvent.Current.Should().BeSameAs(fake);
    }

    [Fact]
    public void SetFactory_NewFactoryUsedOnEveryReset()
    {
        var callCount = 0;
        WideEvent.SetFactory(() =>
        {
            callCount++;
            return new WideEventContext();
        });

        WideEvent.Reset();
        _ = WideEvent.Current; // 1ª criação

        WideEvent.Reset();
        _ = WideEvent.Current; // 2ª criação

        callCount.Should().Be(2);
    }

    [Fact]
    public void SetFactory_AllowsCustomIWideEventContext_Implementation()
    {
        var mock = new Mock<IWideEventContext>();
        mock.Setup(c => c.Build()).Returns(new Dictionary<string, object?> { ["custom"] = true });

        WideEvent.SetFactory(() => mock.Object);
        WideEvent.Reset();

        WideEvent.Add("key", "value");
        mock.Verify(c => c.Add("key", "value"), Times.Once);
    }
}
