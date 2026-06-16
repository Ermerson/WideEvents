# WideEvents

WideEvents is a .NET library for **wide events** (also known as *canonical log lines*).

Instead of scattering many log lines across a request, you accumulate context into a single rich, structured event and emit it once at the end.

> Heavily inspired by [loggingsucks.com](https://loggingsucks.com/) and [Stripe's canonical log lines](https://stripe.com/blog/canonical-log-lines).

---

## 📚 Learning Project

This project is primarily a **learning and experimentation** project. A significant portion of the codebase, architecture exploration, and implementation process is being developed with the assistance of AI-powered development tools and agents.

Because of that, you may encounter incomplete features, experimental implementations, architectural changes over time, and limited guarantees regarding stability, performance, or production readiness.

Contributions, feedback, and [discussions](https://github.com/Ermerson/WideEvents/discussions) are highly appreciated.

### Looking for a production-ready solution?

Evaluate **[Waystone Widelog Events](https://draekien-industries.wpei.me/waystone.widelogevents)** — a more complete, battle-tested implementation.

---

## The problem

A single HTTP request typically produces:

```text
Request started
Loading cart for user_456
Calling payment provider stripe
Retrying payment (attempt 2)
Payment failed: card_declined
Request finished with 500
```

Six lines. Fragmented context. Expensive to correlate.

## The solution

WideEvents accumulates all context and emits one structured event:

```json
{
  "http":     { "method": "GET", "path": "/checkout/user_456", "status_code": 200 },
  "user":     { "id": "user_456" },
  "payment":  { "method": "card", "provider": "stripe" },
  "duration_ms": 29.52,
  "trace_id": "23ca1dc84f7b4cc4b44b7717ca231c2b",
  "span_id":  "2454b21b523f02f4",
  "trace_flags": "None"
}
```

One event. Full context.

---

## Packages

| Package | Description |
|---|---|
| `WideEvents.Abstractions` | `IWideEventContext` and `IWideEventExporter` contracts. |
| `WideEvents.Core` | `WideEvent` static accumulator, `WideEventContext`, and `WideEventBuilder`. |
| `WideEvents.AspNetCore` | Middleware, enrichers, and `LoggerWideEventExporter`. |

Targets `net8.0` and `net10.0`.

---

## Installation

```bash
# ASP.NET Core apps (brings in Core and Abstractions transitively)
dotnet add package WideEvents.AspNetCore

# Non-web or console apps
dotnet add package WideEvents.Core
```

---

## Quick start (ASP.NET Core)

Register the services, add the middleware, and call `WideEvent.Add` anywhere in the request pipeline:

```csharp
using WideEvents.AspNetCore;
using WideEvents.Core.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWideEvents(); // register services

var app = builder.Build();

app.UseWideEvents(); // emit one wide event per request

app.MapGet("/checkout/{userId}", (string userId) =>
{
    WideEvent.Add("user.id", userId);
    WideEvent.Add("payment.method", "card");
    WideEvent.Add("payment.provider", "stripe");
    return Results.Ok();
});

app.Run();
```

The middleware automatically adds `http.method`, `http.path`, `http.status_code`, `duration_ms`, and — when `Activity.Current` is set — `trace_id`, `span_id`, `trace_flags`.

On unhandled exceptions it captures `error.type` and `error.message` instead of `http.status_code`.

---

## Dotted keys → nested JSON

Dotted key names are expanded into nested objects when the event is built:

```csharp
WideEvent.Add("payment.method", "card");
WideEvent.Add("payment.provider", "stripe");
```

```json
{
  "payment": {
    "method": "card",
    "provider": "stripe"
  }
}
```

Duplicate keys overwrite the previous value. `null` values are ignored. Empty or whitespace keys throw `ArgumentException`.

---

## Using without ASP.NET Core

```csharp
using WideEvents.Core.Context;

WideEvent.Add("job.name", "invoice-sync");
WideEvent.Add("job.records", 142);

var evt = WideEvent.Current.Build(); // IReadOnlyDictionary<string, object?>
// ... send evt to your logger or exporter

WideEvent.Reset();
```

---

## Structured log output

The default exporter logs via `ILogger` using `{@WideEvent}`. The default .NET console logger serializes dictionaries as `ToString()` — to get proper nested JSON you need a structured logger that supports destructuring, such as **Serilog**:

```csharp
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(new PrettyJsonFormatter()));
```

A runnable sample with Serilog lives in [`sample/WideEvents.Sample.Api`](sample/WideEvents.Sample.Api).

---

## Enrichers

`IHttpWideEventEnricher` lets you add fields from the HTTP context without modifying the middleware. `DefaultHttpEnricher` (registered automatically) captures `http.method`, `http.path`, and `http.status_code`. An optional `AuthEnricher` reads a configured claim from the authenticated user:

```csharp
builder.Services.AddWideEvents(options =>
    options.UseAuthEnricher()); // writes user.id from ClaimTypes.NameIdentifier by default
```

Implement `IHttpWideEventEnricher` and register it with DI to add custom enrichers.

---

## Custom exporters

`IWideEventExporter` is the contract for custom destinations. The default `LoggerWideEventExporter` (registered by `AddWideEvents()`) emits via `ILogger`. Replace it by registering your own after calling `AddWideEvents()`:

```csharp
public class MyExporter : IWideEventExporter
{
    public Task ExportAsync(
        IReadOnlyDictionary<string, object?> wideEvent,
        CancellationToken cancellationToken = default)
    {
        // send to OTLP, Kafka, ClickHouse, stdout, ...
        return Task.CompletedTask;
    }
}
```

---

## Documentation

Full documentation in [`docs/`](docs/):

- [English](docs/en-US/README.md)
- [Português (Brasil)](docs/pt-BR/README.md)

---

## License

Licensed under the [MIT License](https://opensource.org/licenses/MIT).
