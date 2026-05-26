# ASP.NET Core integration

[← Back to index](README.md) · [Português](../pt-BR/aspnetcore.md)

`WideEvents.AspNetCore` provides a middleware that emits exactly **one wide event
per HTTP request**, automatically capturing request/response metadata and
correlating it with the active trace.

## Enabling the middleware

```csharp
using WideEvents.AspNetCore;

var app = builder.Build();

app.UseWideEvents();
```

Register it early in the pipeline so it wraps the rest of your request handling.
`UseWideEvents()` is a thin extension over `UseMiddleware<WideEventMiddleware>()`.

## What it captures automatically

| Attribute | When |
| --- | --- |
| `http.method` | Always. |
| `http.path` | Always. |
| `http.status_code` | On success (after the pipeline completes). |
| `error.type` | When the pipeline throws — the exception's type name. |
| `error.message` | When the pipeline throws — the exception's message. |
| `duration_ms` | Always (measured with `Stopwatch`). |
| `trace_id`, `span_id`, `trace_flags` | When an `Activity` is active (added by `Build()`). |

> On an unhandled exception, the middleware records `error.*`, emits the event,
> and **re-throws** — so `http.status_code` is not present on the error path
> (the response status had not been written yet).

## Adding your own context

Anywhere downstream of the middleware (controllers, minimal-API handlers,
services) you enrich the same event via the static API:

```csharp
app.MapGet("/checkout/{userId}", (string userId) =>
{
    WideEvent.Add("user.id", userId);
    WideEvent.Add("user.subscription", "premium");
    WideEvent.Add("cart.total_cents", 15999);
    return Results.Ok();
});
```

Because the context is `AsyncLocal`, these calls land on the current request's
event. The middleware calls `WideEvent.Reset()` after emitting, so contexts do
not leak between requests.

## How the event is emitted (and rendering it)

The middleware writes the event through `ILogger`:

```csharp
_logger.LogInformation("WideEvent: {@WideEvent}", WideEvent.Current.Build());
```

The `@` in `{@WideEvent}` is a **destructuring** hint. To see the event as nested
JSON you need a structured logger that honors it:

- **Serilog** (recommended) renders the nested dictionary as structured JSON.
- The **default** `Microsoft.Extensions.Logging` console provider does *not*
  destructure — it logs `dictionary.ToString()`, which is not useful. Even
  `AddJsonConsole` serializes the value via `ToString()`.

### Serilog example

```csharp
using Serilog;
using Serilog.Formatting.Compact;

builder.Host.UseSerilog((_, logging) =>
    logging.WriteTo.Console(new CompactJsonFormatter()));
```

This is exactly the setup used by the
[sample](../../sample/WideEvents.Sample.Api). Run it:

```bash
dotnet run --project sample/WideEvents.Sample.Api
curl http://localhost:5080/checkout/user_456   # success path
curl http://localhost:5080/boom                # error path
```

The success path logs a single event containing `http`, `user`, `payment`,
`duration_ms`, and the trace fields; the error path logs the same shape with an
`error` object and an HTTP 500 response.
