# WideEvents Documentation

**English** | [Português (Brasil)](../pt-BR/README.md)

WideEvents is a structured-logging library for .NET built around **wide events**
(also known as *canonical log lines*). Instead of scattering many log lines
across a request, you accumulate context into a single rich, structured event
and emit it once.

> **Learning project.** This library is developed primarily for learning and
> experimentation. See the [root README](../../README.md) for the full disclaimer
> and a pointer to a production-ready alternative.

> **Status:** early stage. This documentation describes what is **currently
> implemented**.

## Projects

| Project | Description |
| --- | --- |
| `WideEvents.Abstractions` | Contracts: `IWideEventContext` and `IWideEventExporter`. |
| `WideEvents.Core` | Wide-event context, static `WideEvent` accumulator, and `WideEventBuilder`. |
| `WideEvents.AspNetCore` | Middleware, enrichers, and the default `LoggerWideEventExporter`. |

## Requirements

- .NET 8 SDK or later — packages target `net8.0` and `net10.0`.

## Installation

```bash
# ASP.NET Core apps (brings in Core and Abstractions transitively)
dotnet add package WideEvents.AspNetCore

# Non-web or console apps
dotnet add package WideEvents.Core
```

## Quick start (ASP.NET Core)

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

A request to `/checkout/user_456` produces a single event:

```json
{
  "http": { "method": "GET", "path": "/checkout/user_456", "status_code": 200 },
  "user": { "id": "user_456" },
  "payment": { "method": "card", "provider": "stripe" },
  "duration_ms": 29.52,
  "trace_id": "23ca1dc84f7b4cc4b44b7717ca231c2b",
  "span_id": "2454b21b523f02f4",
  "trace_flags": "None"
}
```

> The default exporter emits via `ILogger` using `{@WideEvent}`. To render the
> event as nested JSON you need a structured logger that supports destructuring
> (e.g. Serilog). The default console logger will only call `ToString()` on the
> dictionary. See [ASP.NET Core integration](aspnetcore.md).

## Topics

- [Core concepts](core-concepts.md) — the `WideEvent` API, nested keys, trace
  correlation, the builder pipeline, and the abstractions.
- [ASP.NET Core integration](aspnetcore.md) — the middleware, enrichers, and how
  the event is exported.

## Sample

A runnable sample lives in [`sample/WideEvents.Sample.Api`](../../sample/WideEvents.Sample.Api).
Run it and hit the endpoints:

```bash
dotnet run --project sample/WideEvents.Sample.Api
# then:
curl http://localhost:5080/checkout/user_456
curl http://localhost:5080/boom   # error path
```
