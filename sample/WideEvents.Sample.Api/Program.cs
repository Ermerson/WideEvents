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

builder.Host.UseSerilog((_, logging) =>
    logging
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .WriteTo.Console(new PrettyJsonFormatter()));

builder.Services.AddWideEvents(options => options.UseAuthEnricher());

var app = builder.Build();

app.UseWideEvents();

app.MapGet("/", () => "WideEvents sample - try GET /checkout/user_456 or GET /boom");

app.MapGet("/checkout/{userId}", (string userId) =>
{
    WideEvent.Add("user.id", userId);
    WideEvent.Add("user.subscription", "premium");
    WideEvent.Add("cart.id", "cart_xyz");
    WideEvent.Add("cart.total_cents", 15999);
    WideEvent.Add("payment.method", "card");
    WideEvent.Add("payment.provider", "stripe");
    WideEvent.Add("outcome", "ok");

    return Results.Ok(new { status = "checked_out", user = userId });
});

app.MapGet("/boom", () =>
{
    WideEvent.Add("user.id", "user_456");
    WideEvent.Add("payment.provider", "stripe");

    throw new InvalidOperationException("payment provider timeout");
});

app.Run("http://localhost:5080");
