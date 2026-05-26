using System.Diagnostics;
using FluentAssertions;
using WideEvents.Core.Context;
using Xunit;

namespace WideEvents.Core.Tests;

public sealed class WideEventContextTests
{
    [Fact]
    public void Add_StoresValue_AndBuildReturnsIt()
    {
        var context = new WideEventContext();

        context.Add("http.method", "GET");

        var built = context.Build();
        var http = built["http"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        http["method"].Should().Be("GET");
    }

    [Fact]
    public void Add_NullValue_IsSkipped()
    {
        var context = new WideEventContext();

        context.Add("user.id", null);

        context.Build().Should().NotContainKey("user");
    }

    [Fact]
    public void Add_SameKeyTwice_Overwrites()
    {
        var context = new WideEventContext();

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
        var context = new WideEventContext();

        var act = () => context.Add(key!, "value");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Build_ExpandsDottedKeys_IntoNestedDictionaries()
    {
        var context = new WideEventContext();

        context.Add("payment.method", "card");
        context.Add("payment.provider", "stripe");

        var built = context.Build();
        var payment = built["payment"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        payment.Should().HaveCount(2);
        payment["method"].Should().Be("card");
        payment["provider"].Should().Be("stripe");
    }

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
}
