# Design: IExternalScopeProvider Interop

**Date:** 2026-06-07  
**Status:** Approved  
**Scope:** `WideEvents.Core`, `WideEvents.AspNetCore`

## Problem

WideEvents accumulates context in an `AsyncLocal<IWideEventContext>` and emits a single structured log at the end of each request. This means:

- Intermediate `_logger.LogXxx()` calls during the request carry none of the WideEvent fields.
- Serilog and OpenTelemetry, which consume ambient state via `IExternalScopeProvider`, cannot see WideEvent data without extra adapters.

## Goal

Make WideEvent data visible to Serilog, OTel, and any `IExternalScopeProvider`-aware provider — automatically, with no configuration on their side — by pushing the context as a standard `ILogger` scope at the start of each request.

## Non-Goals

- Removing the `AsyncLocal` from the runtime (the scope provider itself uses `AsyncLocal<Scope>` internally).
- Changing the `WideEvent` static facade API.
- Changing `IWideEventContext` (Abstractions interface).
- Emitting nested/built structure to intermediate logs (flat keys only in scope; `Build()` runs at end of request as before).

## Approach

**Dual-track ambient state**: `AsyncLocal` stays for O(1) lookup via `WideEvent.Current`; the same object is also pushed as an `ILogger` scope so providers can enumerate it.

The scope exposes the raw flat `_attributes` dictionary (keys like `"user.id"`, not nested objects). Each log provider that reads the scope chain sees the fields accumulated up to that moment.

## Changes

### 1. `WideEventContext` — implement `IEnumerable<KeyValuePair<string, object?>>`

Providers check for this interface to extract structured key-value pairs from a scope. Without it, they fall back to `ToString()`, which logs only the type name.

```csharp
public sealed class WideEventContext : IWideEventContext, IEnumerable<KeyValuePair<string, object?>>
{
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        => _attributes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

Enumeration is live (not a snapshot): each consumer sees whatever fields have been added at the moment of iteration.

### 2. `WideEventMiddleware` — inject logger and open scope

```csharp
public WideEventMiddleware(
    RequestDelegate next,
    IEnumerable<IHttpWideEventEnricher> enrichers,
    IWideEventExporter exporter,
    ILogger<WideEventMiddleware> logger)

public async Task Invoke(HttpContext context, IWideEventContext wideEvent)
{
    using var scope = _logger.BeginScope(wideEvent);
    var start = Stopwatch.GetTimestamp();
    try { ... }
    finally
    {
        wideEvent.Add("duration_ms", ...);
        await _exporter.ExportAsync(wideEvent.Build(), ...);
        WideEvent.Reset();
    }
}
```

`using var scope` is placed **outside** the `try` block so the scope remains open through the `finally`, including the `ExportAsync` call that emits the canonical final log entry.

### 3. No changes to

- `IWideEventContext` (Abstractions)
- `WideEvent` static facade
- `WideEventExtensions` / `AddWideEvents`
- `IWideEventExporter` / `LoggerWideEventExporter`

## Provider Compatibility

| Provider | Behavior |
|---|---|
| Serilog | Checks `IEnumerable<KeyValuePair<string, object?>>` on scope; adds each pair as a log property |
| OTel ILogger bridge | Same pattern; adds pairs as Activity attributes |
| Built-in console/JSON | Same pattern; includes scope in formatted output |
| Any unknown provider | Worst case falls back to `ToString()` — no regression vs. today |

## Testing

### Unit — `WideEvents.Core.Tests`

- `WideEventContext` enumerates `_attributes` correctly after `Add()` calls.
- Enumeration reflects mutations added after initial `Add()` calls (live, not snapshot).
- `null` values are not stored (existing invariant), so they never appear in enumeration.

### Integration — `WideEvents.AspNetCore.Tests` (new project or added to existing)

- A `_logger.LogWarning()` emitted inside a request handler includes WideEvent fields (e.g., `"user.id"`) as scope properties on the captured log event.
- Uses Serilog `InMemory` sink (or a test sink) to capture intermediate log entries.

## Invariants Preserved

- `WideEvent.Add()` overwrites on duplicate keys; ignores `null` values; throws on empty/whitespace keys.
- `Build()` produces nested structure from dotted keys — unchanged, called only at end of request.
- `WideEvent.Reset()` clears the `AsyncLocal` at end of request — unchanged.
- The scope is disposed after `Reset()`, so no scope leakage across requests.
