using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using WideEvents.Core.Builder;
using WideEvents.Core.Context;
using WideEvents.Core.Logging;
using Xunit;

namespace WideEvents.Core.Tests.Builder;

[Collection("WideEventStaticState")]
public sealed class WideEventBuilderTests : IDisposable
{
    private readonly WideEventLoggerProvider _provider = new();
    private WideEventBuilder Builder => new(_provider);

    public WideEventBuilderTests()
    {
        WideEvent.SetFactory(static () => new WideEventContext());
        WideEvent.Reset();
    }

    public void Dispose()
    {
        WideEvent.SetFactory(static () => new WideEventContext());
        WideEvent.Reset();
    }

    // ── AsyncLocal data ────────────────────────────────────────────────────────

    [Fact]
    public void Build_WideEventAddValues_AppearInResult()
    {
        WideEvent.Add("user.id", "u-1");

        var result = Builder.Build();

        var user = result["user"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        user["id"].Should().Be("u-1");
    }

    [Fact]
    public void Build_DrainsClearsContext_AfterBuild()
    {
        WideEvent.Add("outcome", "ok");
        Builder.Build();

        // After Build, Drain was called — draining again should yield empty dict
        WideEvent.Drain().Should().BeEmpty();
    }

    // ── Scope data ─────────────────────────────────────────────────────────────

    [Fact]
    public void Build_ScopeValues_AppearInResult()
    {
        ILogger logger = _provider.CreateLogger("test");
        using var scope = logger.BeginScope(
            new Dictionary<string, object?> { ["tenant"] = "acme" });

        var result = Builder.Build();

        result.Should().ContainKey("tenant").WhoseValue.Should().Be("acme");
    }

    [Fact]
    public void Build_AfterScopeDisposed_ScopeValuesAbsent()
    {
        ILogger logger = _provider.CreateLogger("test");
        var scope = logger.BeginScope(
            new Dictionary<string, object?> { ["tenant"] = "acme" });
        scope!.Dispose();

        var result = Builder.Build();

        result.Should().NotContainKey("tenant");
    }

    // ── Activity data ──────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithActivity_IncludesTraceFields()
    {
        using var activity = new Activity("test-op");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var result = Builder.Build();

        result.Should().ContainKey("trace_id");
        result.Should().ContainKey("span_id");
        result.Should().ContainKey("trace_flags");
        result["trace_id"].Should().Be(activity.TraceId.ToString());
    }

    [Fact]
    public void Build_WithoutActivity_OmitsTraceFields()
    {
        Activity.Current = null;

        var result = Builder.Build();

        result.Should().NotContainKey("trace_id");
        result.Should().NotContainKey("span_id");
    }

    [Fact]
    public void Build_ActivityTags_AppearInResult()
    {
        using var activity = new Activity("test-op");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        activity.SetTag("http.method", "GET");

        var result = Builder.Build();

        result.Should().ContainKey("http");
        var http = result["http"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        http["method"].Should().Be("GET");
    }

    // ── Merge precedence ───────────────────────────────────────────────────────

    [Fact]
    public void Build_LocalWinsOverScope_WhenSameKey()
    {
        ILogger logger = _provider.CreateLogger("test");
        using var scope = logger.BeginScope(
            new Dictionary<string, object?> { ["outcome"] = "from-scope" });

        WideEvent.Add("outcome", "from-local");

        var result = Builder.Build();

        result["outcome"].Should().Be("from-local");
    }

    [Fact]
    public void Build_LocalWinsOverActivity_WhenSameKey()
    {
        using var activity = new Activity("test-op");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        activity.SetTag("outcome", "from-activity");

        WideEvent.Add("outcome", "from-local");

        var result = Builder.Build();

        result["outcome"].Should().Be("from-local");
    }

    [Fact]
    public void Build_ActivityWinsOverScope_WhenSameKey()
    {
        ILogger logger = _provider.CreateLogger("test");
        using var scope = logger.BeginScope(
            new Dictionary<string, object?> { ["outcome"] = "from-scope" });

        using var activity = new Activity("test-op");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        activity.SetTag("outcome", "from-activity");

        var result = Builder.Build();

        result["outcome"].Should().Be("from-activity");
    }
}
