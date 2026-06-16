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

## `IWideEventBuilder` and the merge pipeline

`WideEvents.Core.Builder.IWideEventBuilder` is the interface for building the
final event dictionary. The default implementation `WideEventBuilder` merges
**three data sources** in ascending precedence order:

| Source | Precedence | How it gets there |
| --- | --- | --- |
| `ILogger.BeginScope(...)` values | Lowest | Via `IExternalScopeProvider` (shared by `WideEventLoggerProvider`) |
| `Activity.Current` trace IDs and tags | Middle | Read at build time from `System.Diagnostics.Activity.Current` |
| `WideEvent.Add(...)` values | Highest | AsyncLocal buffer, drained and cleared on `Build()` |

When keys overlap, the higher-precedence source wins. The merged flat dictionary
is then expanded by `WideEventStructureBuilder` into a nested hierarchy.

In the ASP.NET Core integration, `IWideEventBuilder` is registered as a singleton
and injected into the middleware. The middleware pushes request metadata
(`http.method`, `http.path`) as an `ILogger` scope so they are captured through
the scope provider pipeline even before any application code runs.

> For non-web or console use, prefer `WideEvent.Current.Build()` directly. It
> produces the event from only the AsyncLocal buffer without the scope/Activity
> merge, which is sufficient for simple scenarios.

## `IWideEventExporter`

`WideEvents.Abstractions` defines the export contract:

```csharp
public interface IWideEventExporter
{
    Task ExportAsync(
        IReadOnlyDictionary<string, object?> wideEvent,
        CancellationToken cancellationToken = default);
}
```

`AddWideEvents()` registers `LoggerWideEventExporter` as the default implementation.
It emits the event via `ILogger` using the `{@WideEvent}` destructuring hint:

```csharp
_logger.LogInformation("WideEvent: {@WideEvent}", wideEvent);
```

Register your own implementation after `AddWideEvents()` to send events to a
custom destination (OTLP, Kafka, stdout, …). Because it is registered after the
default, it overrides the default exporter in DI resolution.

> The interface is defined in `WideEvents.Abstractions` so exporter packages can
> depend on the contract without referencing `WideEvents.Core`.
