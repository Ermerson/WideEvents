# ASP.NET Core integration

[← Back to index](README.md) · [Português](../pt-BR/aspnetcore.md)

`WideEvents.AspNetCore` provides a middleware that emits exactly **one wide event
per HTTP request**, automatically capturing request/response metadata and
correlating it with the active trace.

## Setup

Call `AddWideEvents()` in your service registration, then `UseWideEvents()` in
the request pipeline:

```csharp
using WideEvents.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWideEvents(); // required — registers middleware dependencies

var app = builder.Build();

app.UseWideEvents(); // early in the pipeline so it wraps all handlers
```

`AddWideEvents()` registers:

- `IWideEventExporter` → `LoggerWideEventExporter` (default emitter)
- `IWideEventBuilder` → `WideEventBuilder` (3-source merge pipeline)
- `WideEventLoggerProvider` as `ILoggerProvider` (scope integration)
- `DefaultHttpEnricher` as `IHttpWideEventEnricher`

## What it captures automatically

| Attribute | Source | When |
| --- | --- | --- |
| `http.method` | `DefaultHttpEnricher` | Always. |
| `http.path` | `DefaultHttpEnricher` | Always. |
| `http.status_code` | `DefaultHttpEnricher` | On success (after the pipeline completes). |
| `error.type` | Middleware | When the pipeline throws — the exception's type name. |
| `error.message` | Middleware | When the pipeline throws — the exception's message. |
| `duration_ms` | Middleware | Always (measured with `Stopwatch`). |
| `trace_id`, `span_id`, `trace_flags` | `WideEventBuilder` | When an `Activity` is active. |

> On an unhandled exception, the middleware records `error.*`, emits the event,
> and **re-throws** — so `http.status_code` is not present on the error path.

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
event. The context is cleared when the middleware finishes (after `Build()` is
called), so contexts do not leak between requests.

## Enrichers

`IHttpWideEventEnricher` is the extension point for adding HTTP-derived fields
without modifying the middleware. Implement the interface and register it in DI:

```csharp
public class TenantEnricher : IHttpWideEventEnricher
{
    public void EnrichRequest(HttpContext context, IWideEventContext wideEvent)
    {
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        wideEvent.Add("tenant.id", tenantId);
    }

    public void EnrichResponse(HttpContext context, IWideEventContext wideEvent) { }
}

// in Program.cs:
builder.Services.AddSingleton<IHttpWideEventEnricher, TenantEnricher>();
```

### Enricher lifecycle contract

> **Note:** `EnrichResponse` is called only on the **success path** — when the
> pipeline completes without throwing. If an unhandled exception propagates,
> `EnrichResponse` is **not invoked**; the middleware captures `error.type` and
> `error.message` directly and re-throws.
>
> If your enricher needs to add data regardless of the request outcome, do it in
> `EnrichRequest`, or read the required state defensively (for example, avoid
> accessing `context.Response.StatusCode` inside `EnrichResponse` assuming it is
> always set).

### Built-in enrichers

**`DefaultHttpEnricher`** is registered automatically by `AddWideEvents()`. It
captures `http.method`, `http.path`, and `http.status_code`.

**`AuthEnricher`** is optional. Enable it via `WideEventsOptions`:

```csharp
builder.Services.AddWideEvents(options =>
    options.UseAuthEnricher()); // reads ClaimTypes.NameIdentifier → "user.id"

// custom claim and field name:
builder.Services.AddWideEvents(options =>
    options.UseAuthEnricher(o =>
    {
        o.ClaimType = "sub";
        o.FieldName = "auth.subject";
    }));
```

`AuthEnricher` is a no-op when the request is unauthenticated or the claim is
absent.

## How the event is exported

After the pipeline completes (in the middleware's `finally` block), the middleware
calls `IWideEventBuilder.Build()` to produce the merged event dictionary, then
`IWideEventExporter.ExportAsync()` to emit it.

The default exporter — `LoggerWideEventExporter` — writes via `ILogger`:

```csharp
_logger.LogInformation("WideEvent: {@WideEvent}", wideEvent);
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

### Custom exporter

Register your own `IWideEventExporter` after `AddWideEvents()` to override the
default:

```csharp
builder.Services.AddWideEvents();
builder.Services.AddSingleton<IWideEventExporter, MyExporter>();
```
