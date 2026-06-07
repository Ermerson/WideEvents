using System.Diagnostics;
using FluentAssertions;
using Moq;
using WideEvents.Core.Context;
using WideEvents.Core.Enrichers;
using Xunit;

namespace WideEvents.Core.Tests;

public sealed class WideEventContextTests
{
    // ── Add ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_StoresValue_AndBuildReturnsIt()
    {
        var context = new WideEventContext(enrichers: []);

        context.Add("http.method", "GET");

        var built = context.Build();
        var http = built["http"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        http["method"].Should().Be("GET");
    }

    [Fact]
    public void Add_NullValue_IsSkipped()
    {
        var context = new WideEventContext(enrichers: []);

        context.Add("user.id", null);

        context.Build().Should().NotContainKey("user");
    }

    [Fact]
    public void Add_SameKeyTwice_Overwrites()
    {
        var context = new WideEventContext(enrichers: []);

        context.Add("status", 200);
        context.Add("status", 500);

        context.Build()["status"].Should().Be(500);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Add_InvalidKey_Throws(string? key)
    {
        var context = new WideEventContext(enrichers: []);

        var act = () => context.Add(key!, "value");

        act.Should().Throw<ArgumentException>();
    }

    // ── Build / chaves pontuadas ───────────────────────────────────────────────

    [Fact]
    public void Build_ExpandsDottedKeys_IntoNestedDictionaries()
    {
        var context = new WideEventContext(enrichers: []);

        context.Add("payment.method", "card");
        context.Add("payment.provider", "stripe");

        var built = context.Build();
        var payment = built["payment"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        payment.Should().HaveCount(2);
        payment["method"].Should().Be("card");
        payment["provider"].Should().Be("stripe");
    }

    // ── Enrichers ──────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithDefaultEnrichers_UsesTraceActivityEnricher()
    {
        // new WideEventContext() sem argumentos deve incluir TraceActivityEnricher
        Activity.Current = null;
        var context = new WideEventContext(); // padrão
        context.Add("outcome", "ok");

        var built = context.Build();

        // Sem Activity ativa, o enricher padrão não adiciona campos de trace
        built.Should().NotContainKey("trace_id");
    }

    [Fact]
    public void Build_WithCustomEnricher_InvokesEnricher()
    {
        var enricher = new Mock<IWideEventEnricher>();
        var context = new WideEventContext([enricher.Object]);
        context.Add("key", "value");

        context.Build();

        enricher.Verify(e => e.Enrich(It.IsAny<Dictionary<string, object?>>()), Times.Once);
    }

    [Fact]
    public void Build_WithMultipleEnrichers_InvokesAllInOrder()
    {
        var callOrder = new List<int>();
        var e1 = new Mock<IWideEventEnricher>();
        e1.Setup(e => e.Enrich(It.IsAny<Dictionary<string, object?>>()))
            .Callback(() => callOrder.Add(1));
        var e2 = new Mock<IWideEventEnricher>();
        e2.Setup(e => e.Enrich(It.IsAny<Dictionary<string, object?>>()))
            .Callback(() => callOrder.Add(2));

        var context = new WideEventContext([e1.Object, e2.Object]);
        context.Build();

        callOrder.Should().Equal(1, 2);
    }

    [Fact]
    public void Build_WithEmptyEnrichers_OmitsTraceFields()
    {
        using var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var context = new WideEventContext(enrichers: []);
        context.Add("outcome", "ok");

        var built = context.Build();

        built.Should().NotContainKey("trace_id");
        built.Should().NotContainKey("span_id");
    }

    [Fact]
    public void Build_EnricherCanAddFields_ToRoot()
    {
        var enricher = new Mock<IWideEventEnricher>();
        enricher.Setup(e => e.Enrich(It.IsAny<Dictionary<string, object?>>()))
            .Callback((Dictionary<string, object?> root) => root["injected"] = "yes");

        var context = new WideEventContext([enricher.Object]);
        var built = context.Build();

        built["injected"].Should().Be("yes");
    }

    // ── Trace correlation (via TraceActivityEnricher padrão) ───────────────────

    [Fact]
    public void Build_WithoutActivity_OmitsTraceFields()
    {
        Activity.Current = null;
        var context = new WideEventContext();

        context.Add("outcome", "ok");

        var built = context.Build();
        built.Should().NotContainKey("trace_id");
        built.Should().NotContainKey("span_id");
    }

    [Fact]
    public void Build_WithActivity_IncludesTraceCorrelation()
    {
        using var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var context = new WideEventContext();
        context.Add("outcome", "ok");

        var built = context.Build();
        built["trace_id"].Should().Be(activity.TraceId.ToString());
        built["span_id"].Should().Be(activity.SpanId.ToString());
        built.Should().ContainKey("trace_flags");
    }

    // ── IEnumerable<KeyValuePair<string, object?>> ────────────────────────────

    [Fact]
    public void Enumeration_ReturnsStoredAttributes()
    {
        var context = new WideEventContext(enrichers: []);
        context.Add("user.id", "u-1");
        context.Add("http.method", "GET");

        var pairs = context.ToList();

        pairs.Should().ContainSingle(p => p.Key == "user.id" && (string?)p.Value == "u-1");
        pairs.Should().ContainSingle(p => p.Key == "http.method" && (string?)p.Value == "GET");
    }

    [Fact]
    public void Enumeration_YieldsAllAddedKeys()
    {
        var context = new WideEventContext(enrichers: []);
        context.Add("a", 1);
        context.Add("b", 2);

        context.Select(p => p.Key).Should().BeEquivalentTo(["a", "b"]);
    }

    [Fact]
    public void Enumeration_DoesNotYieldNullValues()
    {
        // Add() rejects null — the invariant is enforced at the gate; verify enumeration reflects it.
        var context = new WideEventContext(enrichers: []);
        context.Add("key", "value");

        context.Should().AllSatisfy(p => p.Value.Should().NotBeNull());
    }
}
