using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WideEvents.Abstractions;
using Xunit;

namespace WideEvents.AspNetCore.Tests;

public sealed class WideEventMiddlewareTests
{
    private static WideEventMiddleware BuildMiddleware(
        RequestDelegate? next = null,
        IEnumerable<IHttpWideEventEnricher>? enrichers = null,
        IWideEventExporter? exporter = null,
        ILogger<WideEventMiddleware>? logger = null)
    {
        var mockExporter = new Mock<IWideEventExporter>();
        mockExporter
            .Setup(e => e.ExportAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new WideEventMiddleware(
            next ?? (_ => Task.CompletedTask),
            enrichers ?? [],
            exporter ?? mockExporter.Object,
            logger ?? NullLogger<WideEventMiddleware>.Instance);
    }

    private static (Mock<IWideEventContext> mock, IWideEventContext context) MockContext()
    {
        var mock = new Mock<IWideEventContext>();
        mock.Setup(c => c.Build()).Returns(new Dictionary<string, object?>());
        return (mock, mock.Object);
    }

    // ── Enrichers ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_CallsEnrichRequest_BeforeNext()
    {
        var order = new List<string>();
        var enricher = new Mock<IHttpWideEventEnricher>();
        enricher
            .Setup(e => e.EnrichRequest(It.IsAny<HttpContext>(), It.IsAny<IWideEventContext>()))
            .Callback(() => order.Add("enrich"));

        RequestDelegate next = _ =>
        {
            order.Add("next");
            return Task.CompletedTask;
        };

        var (_, ctx) = MockContext();
        var middleware = BuildMiddleware(next: next, enrichers: [enricher.Object]);

        await middleware.Invoke(new DefaultHttpContext(), ctx);

        order.Should().Equal("enrich", "next");
    }

    [Fact]
    public async Task Invoke_CallsEnrichResponse_AfterNext()
    {
        var order = new List<string>();
        var enricher = new Mock<IHttpWideEventEnricher>();
        enricher
            .Setup(e => e.EnrichResponse(It.IsAny<HttpContext>(), It.IsAny<IWideEventContext>()))
            .Callback(() => order.Add("response"));

        RequestDelegate next = _ =>
        {
            order.Add("next");
            return Task.CompletedTask;
        };

        var (_, ctx) = MockContext();
        var middleware = BuildMiddleware(next: next, enrichers: [enricher.Object]);

        await middleware.Invoke(new DefaultHttpContext(), ctx);

        order.Should().Equal("next", "response");
    }

    [Fact]
    public async Task Invoke_PassesCorrectContextToEnrichers()
    {
        var httpContext = new DefaultHttpContext();
        var enricher = new Mock<IHttpWideEventEnricher>();
        var (mock, ctx) = MockContext();

        var middleware = BuildMiddleware(enrichers: [enricher.Object]);
        await middleware.Invoke(httpContext, ctx);

        enricher.Verify(e => e.EnrichRequest(httpContext, ctx), Times.Once);
        enricher.Verify(e => e.EnrichResponse(httpContext, ctx), Times.Once);
    }

    // ── Exporter ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_ExportsBuiltEvent()
    {
        var builtEvent = new Dictionary<string, object?> { ["outcome"] = "ok" };
        var (mock, ctx) = MockContext();
        mock.Setup(c => c.Build()).Returns(builtEvent);

        var exporter = new Mock<IWideEventExporter>();
        exporter
            .Setup(e => e.ExportAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var middleware = BuildMiddleware(exporter: exporter.Object);
        await middleware.Invoke(new DefaultHttpContext(), ctx);

        exporter.Verify(
            e => e.ExportAsync(builtEvent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_ExportsEvenOnException()
    {
        var (mock, ctx) = MockContext();
        var exporter = new Mock<IWideEventExporter>();
        exporter
            .Setup(e => e.ExportAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var middleware = BuildMiddleware(
            next: _ => throw new InvalidOperationException("boom"),
            exporter: exporter.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.Invoke(new DefaultHttpContext(), ctx));

        exporter.Verify(
            e => e.ExportAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Atributos de erro ──────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_OnException_AddsErrorTypeAndMessage()
    {
        var (mock, ctx) = MockContext();
        var middleware = BuildMiddleware(
            next: _ => throw new InvalidOperationException("algo deu errado"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.Invoke(new DefaultHttpContext(), ctx));

        mock.Verify(c => c.Add("error.type", "InvalidOperationException"), Times.Once);
        mock.Verify(c => c.Add("error.message", "algo deu errado"), Times.Once);
    }

    [Fact]
    public async Task Invoke_OnException_Rethrows()
    {
        var (_, ctx) = MockContext();
        var original = new ArgumentNullException("param");
        var middleware = BuildMiddleware(next: _ => throw original);

        var thrown = await Assert.ThrowsAsync<ArgumentNullException>(
            () => middleware.Invoke(new DefaultHttpContext(), ctx));

        thrown.Should().BeSameAs(original);
    }

    [Fact]
    public async Task Invoke_OnSuccess_DoesNotAddErrorAttributes()
    {
        var (mock, ctx) = MockContext();
        var middleware = BuildMiddleware();

        await middleware.Invoke(new DefaultHttpContext(), ctx);

        mock.Verify(c => c.Add("error.type", It.IsAny<object?>()), Times.Never);
        mock.Verify(c => c.Add("error.message", It.IsAny<object?>()), Times.Never);
    }

    // ── duration_ms ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_AddsDurationMs_AsDouble()
    {
        var (mock, ctx) = MockContext();
        var middleware = BuildMiddleware();

        await middleware.Invoke(new DefaultHttpContext(), ctx);

        mock.Verify(
            c => c.Add("duration_ms", It.Is<object?>(v => v != null && (double)v >= 0)),
            Times.Once);
    }

    // ── Logger scope ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_OpensScope_WithWideEventContext()
    {
        var (_, ctx) = MockContext();
        var logger = new Mock<ILogger<WideEventMiddleware>>();
        logger
            .Setup(l => l.BeginScope(It.IsAny<IWideEventContext>()))
            .Returns(Mock.Of<IDisposable>());

        var middleware = BuildMiddleware(logger: logger.Object);
        await middleware.Invoke(new DefaultHttpContext(), ctx);

        logger.Verify(l => l.BeginScope(ctx), Times.Once);
    }

    [Fact]
    public async Task Invoke_DisposesScope_WhenRequestCompletes()
    {
        var (_, ctx) = MockContext();
        var disposable = new Mock<IDisposable>();
        var logger = new Mock<ILogger<WideEventMiddleware>>();
        logger
            .Setup(l => l.BeginScope(It.IsAny<IWideEventContext>()))
            .Returns(disposable.Object);

        var middleware = BuildMiddleware(logger: logger.Object);
        await middleware.Invoke(new DefaultHttpContext(), ctx);

        disposable.Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Invoke_DisposesScope_WhenRequestThrows()
    {
        var (_, ctx) = MockContext();
        var disposable = new Mock<IDisposable>();
        var logger = new Mock<ILogger<WideEventMiddleware>>();
        logger
            .Setup(l => l.BeginScope(It.IsAny<IWideEventContext>()))
            .Returns(disposable.Object);

        var middleware = BuildMiddleware(
            next: _ => throw new InvalidOperationException("boom"),
            logger: logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.Invoke(new DefaultHttpContext(), ctx));

        disposable.Verify(d => d.Dispose(), Times.Once);
    }
}
