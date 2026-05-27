using WideEvents.Abstractions;

namespace WideEvents.Core.Context;

internal static class WideEventContextFactory
{
    private static Func<IWideEventContext> _factory = static () => new WideEventContext();

    internal static void SetFactory(Func<IWideEventContext> factory)
        => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    internal static IWideEventContext Create() => _factory();
}