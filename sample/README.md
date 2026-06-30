# WideEvents Sample API

A minimal ASP.NET Core app that demonstrates WideEvents in action with Serilog
rendering each wide event as a single pretty-printed JSON object.

## Running

```bash
dotnet run --project sample/WideEvents.Sample.Api
```

Listens on `http://localhost:5080`.

## Endpoints

### `GET /`

Returns a plain-text welcome message listing the available endpoints.

---

### `GET /checkout/{userId}`

**Success path.** Accumulates user, cart, and payment fields via `WideEvent.Add()`,
logs a mid-request warning, and returns HTTP 200.

```bash
curl http://localhost:5080/checkout/user_456
```

The emitted wide event includes:

| Field | Value |
| --- | --- |
| `http.method` | `GET` |
| `http.path` | `/checkout/user_456` |
| `http.status_code` | `200` |
| `user.id` | `user_456` *(from `AuthEnricher`)* |
| `user.subscription` | `premium` |
| `cart.id` | `cart_xyz` |
| `cart.total_cents` | `15999` |
| `payment.method` | `card` |
| `payment.provider` | `stripe` |
| `outcome` | `ok` |
| `duration_ms` | *(measured)* |
| `trace_id`, `span_id`, `trace_flags` | *(from `Activity`)* |

---

### `GET /checkout/scope`

**`BeginScope` vs `WideEvent.Add()` contrast.** Demonstrates that scope
key-value pairs opened inside the request handler — and disposed before the
middleware's `finally` block runs — do **not** appear in the final wide event.

```bash
curl http://localhost:5080/checkout/scope
```

`correlationId` and `cart.operationId` are pushed via `logger.BeginScope()`.
They enrich the mid-request `LogWarning` line but are gone by the time
`Build()` runs, so they are absent from the wide event. The fields added via
`WideEvent.Add()` always appear.

> This contrasts with long-lived scopes created by middleware (e.g. `http.method`,
> `http.path`) which outlive the handler and **do** show up in the wide event.

---

### `GET /boom`

**Error path.** Throws `InvalidOperationException` after adding one field, so
the middleware captures `error.*` and re-throws (HTTP 500 from the default
exception handler).

```bash
curl -i http://localhost:5080/boom
```

The emitted wide event includes `error.type`, `error.message`, and
`payment.provider`, but **not** `http.status_code` (which is only captured on
the success path).

## What to observe

Each request produces **one** JSON log entry (the wide event) followed by the
normal log lines emitted during the request. The wide event is rendered by
`PrettyJsonFormatter` — a compact Serilog sink included in this sample — as a
nested JSON structure with all dotted keys expanded.
