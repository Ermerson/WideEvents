# Core concepts

[← Back to index](README.md) · [Português](../pt-BR/core-concepts.md)

## Wide events

A **wide event** is a single structured record that describes one unit of work
(typically one request) with many attributes: operational (`duration_ms`,
`http.status_code`), technical (`trace_id`), and business (`user.id`,
`cart.total_cents`). One event, full context — instead of correlating dozens of
narrow log lines after the fact.

## The `WideEvent` static API

`WideEvents.Core.Context.WideEvent` is the entry point. It holds the current
context in an `AsyncLocal`, so each asynchronous flow (e.g. each HTTP request)
gets its own isolated context.

```csharp
using WideEvents.Core.Context;

WideEvent.Add("user.id", "user_456");      // accumulate attributes
WideEvent.Add("cart.total_cents", 15999);

IWideEventContext ctx = WideEvent.Current; // the context for this flow
IReadOnlyDictionary<string, object?> evt = ctx.Build();

WideEvent.Reset();                          // clear the context for this flow
```

| Member | Description |
| --- | --- |
| `WideEvent.Add(string key, object? value)` | Adds/overwrites an attribute on the current context. |
| `WideEvent.Current` | The current `IWideEventContext` (created lazily, never null). |
| `WideEvent.Reset()` | Drops the current context so the next access starts fresh. |

## `WideEventContext` and `IWideEventContext`

`WideEventContext` (in `WideEvents.Core`) implements `IWideEventContext` (in
`WideEvents.Abstractions`):

```csharp
public interface IWideEventContext
{
    void Add(string name, object? value);
    IReadOnlyDictionary<string, object?> Build();
}
```

### `Add` semantics

- **Overwrite:** adding the same key twice keeps the last value (it does not throw).
- **Null values are ignored:** `Add("user.id", null)` is a no-op, so absent data
  never shows up as an empty key.
- **Key validation:** a `null`, empty, or whitespace key throws `ArgumentException`.

### Nested keys

Dotted keys are expanded into nested objects when the event is built:

```csharp
WideEvent.Add("payment.method", "card");
WideEvent.Add("payment.provider", "stripe");
```

```json
{ "payment": { "method": "card", "provider": "stripe" } }
```

Keys that share a prefix are merged into the same object. If a path collides with
a scalar already set at an intermediate segment, the structured (nested) value
wins.

### `Build` and trace correlation

`Build()` is non-mutating — it materializes a fresh dictionary from the
accumulated attributes and, when an `Activity` is active, adds correlation fields
from `System.Diagnostics.Activity.Current`:

| Field | Source |
| --- | --- |
| `trace_id` | `Activity.Current.TraceId` |
| `span_id` | `Activity.Current.SpanId` |
| `trace_flags` | `Activity.Current.ActivityTraceFlags` |

If there is no active `Activity`, these fields are simply omitted. In ASP.NET Core
an activity is created per request when there is a listener (e.g. OpenTelemetry,
or any registered `ActivityListener`).

## Exporting: `IWideEventExporter`

`WideEvents.Abstractions` also defines the export contract:

```csharp
public interface IWideEventExporter
{
    Task ExportAsync(
        IReadOnlyDictionary<string, object?> wideEvent,
        CancellationToken cancellationToken = default);
}
```

This is the extension point for sending built events to a downstream destination
(OTLP, Kafka, stdout, …). It lets exporter packages depend on contracts without
referencing `WideEvents.Core`.

> **Not wired up yet.** There is currently no pipeline that resolves and invokes
> `IWideEventExporter`. Today, events are emitted through `ILogger` by the
> ASP.NET Core middleware (see [ASP.NET Core integration](aspnetcore.md)). The
> interface exists so exporters can be built against a stable contract.
