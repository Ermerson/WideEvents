# WideEvents Documentation

**English** | [Português (Brasil)](../pt-BR/README.md)

WideEvents is a structured-logging library for .NET built around **wide events**
(also known as *canonical log lines*). Instead of scattering many log lines
across a request, you accumulate context into a single rich, structured event
and emit it once.

> **Status:** early stage. This documentation describes what is **currently
> implemented**. The root [README](../../README.md) describes the broader vision
> and roadmap (exporters, sampling, PII masking, source-generated schemas), much
> of which is not built yet.

## Projects

| Project | Description |
| --- | --- |
| `WideEvents.Abstractions` | Contracts: `IWideEventContext` and `IWideEventExporter`. |
| `WideEvents.Core` | The wide-event context and the static `WideEvent` accumulator. |
| `WideEvents.AspNetCore` | Middleware that emits one wide event per HTTP request. |

## Requirements

- .NET 10 SDK — every project targets `net10.0`.

## Installation

The packages are **not published to NuGet yet**. Reference the projects directly.
Referencing `WideEvents.AspNetCore` transitively brings in `WideEvents.Core` and
`WideEvents.Abstractions`:

```xml
<ItemGroup>
  <ProjectReference Include="../WideEvents/src/WideEvents.AspNetCore/WideEvents.AspNetCore.csproj" />
</ItemGroup>
```

Alternatively, `dotnet pack` produces local `WideEvents.*.1.0.0.nupkg` files you
can consume from a local feed.

## Quick start (ASP.NET Core)

```csharp
using WideEvents.AspNetCore;
using WideEvents.Core.Context;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Emits one wide event per request.
app.UseWideEvents();

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

> The middleware logs with `ILogger` using `{@WideEvent}`. To render the event as
> nested JSON you need a structured logger that supports destructuring (e.g.
> Serilog). The default console logger will only call `ToString()` on the
> dictionary. See [ASP.NET Core integration](aspnetcore.md).

## Topics

- [Core concepts](core-concepts.md) — the `WideEvent` API, nested keys, trace
  correlation, and the abstractions.
- [ASP.NET Core integration](aspnetcore.md) — the middleware and what it captures.

## Sample

A runnable sample lives in [`sample/WideEvents.Sample.Api`](../../sample/WideEvents.Sample.Api).
Run it and hit the endpoints:

```bash
dotnet run --project sample/WideEvents.Sample.Api
# then:
curl http://localhost:5080/checkout/user_456
curl http://localhost:5080/boom   # error path
```
