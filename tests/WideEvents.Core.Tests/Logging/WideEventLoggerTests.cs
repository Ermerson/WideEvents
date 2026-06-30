using FluentAssertions;
using Microsoft.Extensions.Logging;
using WideEvents.Core.Logging;
using Xunit;

namespace WideEvents.Core.Tests.Logging;

public sealed class WideEventLoggerTests
{
    private static WideEventLogger CreateLogger(
        AsyncLocal<Dictionary<string, object?>?> store,
        LoggerExternalScopeProvider scopeProvider,
        LogLevel minimumLevel = LogLevel.Information)
        => new(scopeProvider, store, minimumLevel);

    private static void LogInfo(WideEventLogger logger)
        => logger.Log(LogLevel.Information, 0, "msg", null, (s, _) => s);

    // ── IsEnabled ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(LogLevel.Trace, false)]
    [InlineData(LogLevel.Debug, false)]
    [InlineData(LogLevel.Information, true)]
    [InlineData(LogLevel.Warning, true)]
    [InlineData(LogLevel.Error, true)]
    [InlineData(LogLevel.Critical, true)]
    public void IsEnabled_RespectsMinimumLevel(LogLevel logLevel, bool expected)
    {
        var logger = CreateLogger(new(), new());

        logger.IsEnabled(logLevel).Should().Be(expected);
    }

    // ── Log / scope capture ────────────────────────────────────────────────────

    [Fact]
    public void Log_WithActiveKvpScope_CapturesScopeIntoStore()
    {
        var store = new AsyncLocal<Dictionary<string, object?>?>();
        var scopeProvider = new LoggerExternalScopeProvider();
        var logger = CreateLogger(store, scopeProvider);

        using var _ = logger.BeginScope(new Dictionary<string, object?> { ["user.id"] = "u-1" });
        LogInfo(logger);

        store.Value.Should().ContainKey("user.id").WhoseValue.Should().Be("u-1");
    }

    [Fact]
    public void Log_BelowMinimumLevel_DoesNotCaptureScope()
    {
        var store = new AsyncLocal<Dictionary<string, object?>?>();
        var scopeProvider = new LoggerExternalScopeProvider();
        var logger = CreateLogger(store, scopeProvider, LogLevel.Information);

        using var _ = logger.BeginScope(new Dictionary<string, object?> { ["debug.key"] = "v" });
        logger.Log(LogLevel.Debug, 0, "msg", null, (s, _) => s);

        store.Value.Should().BeNull();
    }

    [Fact]
    public void Log_MultipleCalls_AccumulateAllScopes()
    {
        var store = new AsyncLocal<Dictionary<string, object?>?>();
        var scopeProvider = new LoggerExternalScopeProvider();
        var logger = CreateLogger(store, scopeProvider);

        using (logger.BeginScope(new Dictionary<string, object?> { ["key1"] = "val1" }))
            LogInfo(logger);

        using (logger.BeginScope(new Dictionary<string, object?> { ["key2"] = "val2" }))
            LogInfo(logger);

        store.Value.Should().ContainKey("key1").WhoseValue.Should().Be("val1");
        store.Value.Should().ContainKey("key2").WhoseValue.Should().Be("val2");
    }

    [Fact]
    public void Log_ScopeWithNullValue_IsNotCaptured()
    {
        var store = new AsyncLocal<Dictionary<string, object?>?>();
        var scopeProvider = new LoggerExternalScopeProvider();
        var logger = CreateLogger(store, scopeProvider);

        using var _ = logger.BeginScope(new Dictionary<string, object?> { ["nullable"] = null });
        LogInfo(logger);

        store.Value.Should().NotContainKey("nullable");
    }

    [Fact]
    public void Log_NonKvpScope_IsIgnoredSafely()
    {
        var store = new AsyncLocal<Dictionary<string, object?>?>();
        var scopeProvider = new LoggerExternalScopeProvider();
        var logger = CreateLogger(store, scopeProvider);

        using var _ = logger.BeginScope("string-scope");
        LogInfo(logger);

        store.Value.Should().BeEmpty();
    }
}
