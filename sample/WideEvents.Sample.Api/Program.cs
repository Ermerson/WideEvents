using System.Diagnostics;
using Serilog;
using Serilog.Events;
using WideEvents.AspNetCore;
using WideEvents.Core.Context;
using WideEvents.Sample.Api;

// Populate Activity.Current so the wide event picks up trace_id/span_id
// without wiring a full OpenTelemetry pipeline in this sample.
ActivitySource.AddActivityListener(new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = static (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
});

var builder = WebApplication.CreateBuilder(args);

// Use AddSerilog (not UseSerilog) so Microsoft's ILoggerFactory remains in control.
// UseSerilog replaces the factory entirely: Serilog then manages scopes via its own
// LogContext, bypassing IExternalScopeProvider. That prevents WideEventBuilder from
// reading BeginScope() values. AddSerilog keeps the default factory, which calls
// SetScopeProvider() on all ILoggerProvider implementations including WideEventLoggerProvider.
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .WriteTo.Console(new PrettyJsonFormatter())
    .CreateLogger(), dispose: true);

builder.Services.AddWideEvents(options =>
{
    options
        .UseAuthEnricher()
        .UseTraceEnricher();
    options.MinimumCaptureLevel = LogLevel.Information;
});

var app = builder.Build();

app.UseWideEvents();

app.MapGet("/", () => "WideEvents sample - try GET /checkout/user_456 or GET /boom");

app.MapGet("/checkout/{userId}", (string userId, ILogger<Program> logger) =>
{
    WideEvent.Add("user.subscription", "premium");
    WideEvent.Add("cart.id", "cart_xyz");
    WideEvent.Add("cart.total_cents", 15999);

    // This warning is emitted mid-request while the request scope is active.
    // Scope values from WideEventMiddleware (http.method, http.path) enrich this log line.
    logger.LogWarning("Cart value above fraud review threshold");

    WideEvent.Add("payment.method", "card");
    WideEvent.Add("payment.provider", "stripe");
    WideEvent.Add("outcome", "ok");

    return Results.Ok(new { status = "checked_out", user = userId });
});

// Demonstrates two complementary patterns:
//   BeginScope → enriches intermediate log lines while the scope is alive (e.g. the warning below).
//               Scope values that are disposed before the middleware's finally block runs do NOT
//               appear in the final wide event — the scope closes when the handler returns,
//               but Build() only executes afterward in the middleware's finally.
//   WideEvent.Add → goes to the AsyncLocal buffer; always appears in the final wide event.
// Long-lived scopes created by middleware (http.method, http.path) and by ASP.NET Core's
// hosting layer (RequestId, SpanId, etc.) DO appear in the wide event because they outlive
// the handler invocation.
app.MapGet("/checkout/scope", (ILogger<Program> logger) =>
{
    using (logger.BeginScope(new Dictionary<string, object?>
    {
        ["correlationId"] = Guid.NewGuid(),
        ["cart.operationId"] = "xx-1p-2026"
    }))
    {
        WideEvent.Add("user.subscription", "premium");
        WideEvent.Add("cart.id", "cart_xyz");
        WideEvent.Add("cart.total_cents", 15999);

        // correlationId and cart.error enrich this log line via BeginScope above.
        // They will NOT appear in the final wide event (scope closes when handler returns).
        logger.LogWarning("Cart value above fraud review threshold");

        WideEvent.Add("payment.method", "card");
        WideEvent.Add("payment.provider", "stripe");
        WideEvent.Add("outcome", "ok");
    }

    return Results.Ok(new { status = "checked_out", user = "test_scope" });
});

app.MapGet("/boom", () =>
{
    WideEvent.Add("payment.provider", "stripe");

    throw new InvalidOperationException("payment provider timeout");
});

app.Run("http://localhost:5080");
