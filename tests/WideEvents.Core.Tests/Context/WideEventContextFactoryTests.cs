using FluentAssertions;
using Moq;
using WideEvents.Abstractions;
using WideEvents.Core.Context;
using WideEvents.Core.Enrichers;
using Xunit;

namespace WideEvents.Core.Tests.Context;

[Collection("WideEventStaticState")]
public sealed class WideEventContextFactoryTests : IDisposable
{
    private static readonly Func<IWideEventContext> DefaultFactory =
        static () => new WideEventContext([new TraceActivityEnricher()]);

    public WideEventContextFactoryTests()
        => WideEventContextFactory.SetFactory(DefaultFactory);

    public void Dispose()
        => WideEventContextFactory.SetFactory(DefaultFactory);

    // ── SetFactory ─────────────────────────────────────────────────────────────

    [Fact]
    public void SetFactory_Null_ThrowsArgumentNullException()
    {
        var act = () => WideEventContextFactory.SetFactory(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("factory");
    }

    [Fact]
    public void SetFactory_ReplacesFactory_SubsequentCreateUsesNewFactory()
    {
        var custom = new Mock<IWideEventContext>().Object;
        WideEventContextFactory.SetFactory(() => custom);

        WideEventContextFactory.Create().Should().BeSameAs(custom);
    }

    [Fact]
    public void SetFactory_CalledMultipleTimes_LastOneWins()
    {
        var first = new Mock<IWideEventContext>().Object;
        var second = new Mock<IWideEventContext>().Object;
        WideEventContextFactory.SetFactory(() => first);
        WideEventContextFactory.SetFactory(() => second);

        WideEventContextFactory.Create().Should().BeSameAs(second);
    }

    // ── Create ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_DefaultFactory_ReturnsWideEventContext()
    {
        var context = WideEventContextFactory.Create();

        context.Should().BeOfType<WideEventContext>();
    }

    [Fact]
    public void Create_DefaultFactory_EachCallReturnsNewInstance()
    {
        var first = WideEventContextFactory.Create();
        var second = WideEventContextFactory.Create();

        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public void Create_FactoryThrowingException_PropagatesException()
    {
        WideEventContextFactory.SetFactory(() => throw new InvalidOperationException("boom"));

        var act = () => WideEventContextFactory.Create();

        act.Should().Throw<InvalidOperationException>().WithMessage("boom");
    }

    // ── Thread safety ──────────────────────────────────────────────────────────

    [Fact]
    public void Create_ConcurrentCalls_AllReturnNonNullContext()
    {
        var results = new IWideEventContext[64];

        Parallel.For(0, 64, i => results[i] = WideEventContextFactory.Create());

        results.Should().AllSatisfy(ctx => ctx.Should().NotBeNull());
    }

    [Fact]
    public void SetFactory_ConcurrentWithCreate_NeverThrows()
    {
        Func<IWideEventContext> factoryA = static () => new WideEventContext([]);
        Func<IWideEventContext> factoryB = static () => new WideEventContext([]);

        var act = () => Parallel.For(0, 128, i =>
        {
            if (i % 3 == 0) WideEventContextFactory.SetFactory(factoryA);
            else if (i % 3 == 1) WideEventContextFactory.SetFactory(factoryB);
            else _ = WideEventContextFactory.Create();
        });

        act.Should().NotThrow();
    }
}
