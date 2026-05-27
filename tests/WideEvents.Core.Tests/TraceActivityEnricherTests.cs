using System.Diagnostics;
using FluentAssertions;
using WideEvents.Core.Enrichers;
using Xunit;

namespace WideEvents.Core.Tests;

public sealed class TraceActivityEnricherTests
{
    [Fact]
    public void Enrich_WithActiveActivity_AddsTraceFields()
    {
        using var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var root = new Dictionary<string, object?>();
        new TraceActivityEnricher().Enrich(root);

        root.Should().ContainKey("trace_id");
        root.Should().ContainKey("span_id");
        root.Should().ContainKey("trace_flags");
        root["trace_id"].Should().Be(activity.TraceId.ToString());
        root["span_id"].Should().Be(activity.SpanId.ToString());
    }

    [Fact]
    public void Enrich_WithoutActivity_DoesNotModifyDictionary()
    {
        Activity.Current = null;
        var root = new Dictionary<string, object?>();

        new TraceActivityEnricher().Enrich(root);

        root.Should().BeEmpty();
    }

    [Fact]
    public void Enrich_CalledTwiceWithSameActivity_OverwritesExistingTraceFields()
    {
        using var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var root = new Dictionary<string, object?>();
        var enricher = new TraceActivityEnricher();

        enricher.Enrich(root);
        enricher.Enrich(root); // segunda chamada

        root.Should().HaveCount(3); // trace_id, span_id, trace_flags — sem duplicatas
    }
}
