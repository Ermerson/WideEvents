using FluentAssertions;
using Microsoft.AspNetCore.Http;
using WideEvents.AspNetCore.Enrichers;
using WideEvents.Core.Context;
using WideEvents.Core.Enrichers;
using Xunit;

namespace WideEvents.AspNetCore.Tests;

public sealed class DefaultHttpEnricherTests
{
    private readonly DefaultHttpEnricher _enricher = new();

    [Fact]
    public void EnrichRequest_AddsHttpMethodAndPath()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/orders";
        var wideEvent = new WideEventContext(enrichers: []);

        _enricher.EnrichRequest(httpContext, wideEvent);

        var built = wideEvent.Build();
        var http = built["http"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        http["method"].Should().Be("POST");
        http["path"].Should().Be("/api/orders");
    }

    [Fact]
    public void EnrichResponse_AddsStatusCode()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.StatusCode = 201;
        var wideEvent = new WideEventContext(enrichers: []);

        _enricher.EnrichResponse(httpContext, wideEvent);

        var built = wideEvent.Build();
        var http = built["http"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        http["status_code"].Should().Be(201);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public void EnrichRequest_PreservesHttpMethod(string method)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = "/";
        var wideEvent = new WideEventContext(enrichers: []);

        _enricher.EnrichRequest(httpContext, wideEvent);

        var http = wideEvent.Build()["http"].Should()
            .BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        http["method"].Should().Be(method);
    }
}
