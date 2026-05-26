# WideEvents

WideEvents is a high-performance structured logging and observability framework for .NET focused on **Wide Events** and **Canonical Log Lines**.

Instead of producing dozens of fragmented logs during a request lifecycle, WideEvents captures a single rich, structured event containing operational, technical, and business context.

The goal is simple:

> Make production systems easier to understand, debug, query, and observe.

---

# Documentation

Full documentation lives in [`docs/`](docs/), available in English and Portuguese:

- [English](docs/en-US/README.md)
- [Português (Brasil)](docs/pt-BR/README.md)

---

# Why WideEvents Exists

Traditional logging was designed for monoliths and local debugging.

Modern distributed systems generate:

* fragmented logs
* duplicated context
* expensive correlation
* noisy observability pipelines

A single HTTP request may produce:

* controller logs
* service logs
* database logs
* retry logs
* integration logs
* exception logs

Reconstructing what happened becomes operational archaeology.

WideEvents changes the model:

Instead of logging every step, emit a single canonical event describing the complete outcome of the request.

---

# Example

Instead of:

```text
Request started
Loading cart
Calling payment provider
Retrying payment
Payment failed
Request finished
```

WideEvents produces:

```json
{
  "duration_ms": 1247,
  "status_code": 500,
  "outcome": "error",

  "payment": {
    "method": "card",
    "provider": "stripe"
  },

  "error": {
    "type": "PaymentError",
    "code": "card_declined"
  },

  "cart": {
    "id": "cart_xyz",
    "total_cents": 15999
  },

  "user": {
    "id": "user_456",
    "subscription": "premium"
  },

  "trace_id": "7d3b9c5f7b",
  "request_id": "req_8bf7ec2d"
}
```

One event. Full context.

---

# Features

* Structured wide events
* Canonical log lines
* OpenTelemetry integration
* Automatic trace/span correlation
* Structured nested JSON output
* Adaptive sampling
* PII masking/sanitization
* Async exporter pipeline
* Source-generated schemas
* Minimal allocations
* Extensible exporters
* ASP.NET Core middleware
* Kafka-ready architecture
* High-throughput serialization options

---

# Architecture

```text
HTTP Request
     ↓
Middleware
     ↓
Scopes + Activity Context
     ↓
Wide Event Builder
     ↓
Structured Event
     ↓
Export Pipeline
```

WideEvents integrates with:

* ILogger
* OpenTelemetry
* Serilog
* OTLP collectors
* Kafka pipelines
* Structured JSON logging platforms

---

# Installation

## Core

```bash
dotnet add package WideEvents.Core
```

## ASP.NET Core Integration

```bash
dotnet add package WideEvents.AspNetCore
```

## OpenTelemetry Integration

```bash
dotnet add package WideEvents.OpenTelemetry
```

---

# Quick Start

## Register services

```csharp
builder.Services.AddWideEvents(options =>
{
    options.EnableOpenTelemetry = true;
});
```

## Enable middleware

```csharp
app.UseWideEvents();
```

## Add contextual data

```csharp
WideEvent.Add("user.id", user.Id);
WideEvent.Add("cart.total_cents", cart.TotalCents);
```

---

# Structured Events

WideEvents supports hierarchical structured events.

```csharp
WideEvent.Add("payment.method", "card");
WideEvent.Add("payment.provider", "stripe");
```

Produces:

```json
{
  "payment": {
    "method": "card",
    "provider": "stripe"
  }
}
```

---

# OpenTelemetry

WideEvents automatically integrates with OpenTelemetry Activities.

Captured automatically:

* trace_id
* span_id
* activity tags

Example:

```csharp
builder.Services.AddWideEvents(options =>
{
    options.EnableOpenTelemetry = true;
});
```

---

# Source Generated Schemas

Define strongly typed event schemas.

```csharp
[WideEventSchema]
public partial record CheckoutEvent
{
    public string UserId { get; init; }
    public decimal Total { get; init; }
}
```

Generated automatically:

```csharp
event.Enrich();
```

No reflection required.

---

# Exporters

WideEvents supports asynchronous exporters.

Current architecture supports:

* OpenTelemetry / OTLP
* Kafka
* Custom exporters

Example:

```csharp
public class MyExporter : IWideEventExporter
{
    public Task ExportAsync(
        IReadOnlyDictionary<string, object?> evt,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
```

---

# Performance

WideEvents was designed with performance as a first-class concern.

Available serialization modes:

* Source-generated `System.Text.Json`
* Span-based pooled JSON writer
* Minimal allocation pipelines

The framework avoids:

* runtime reflection
* blocking I/O
* synchronous exporters
* unnecessary allocations

---

# Sampling

WideEvents supports adaptive sampling.

Example policy:

* always keep errors
* always keep slow requests
* probabilistic sampling for successful requests

---

# Data Governance

WideEvents includes sanitization hooks for:

* PII masking
* sensitive field filtering
* compliance strategies

These capabilities are essential for modern observability pipelines where logs may contain business, customer, or regulated data.

Typical use cases include:

* GDPR compliance
* PCI DSS environments
* internal security policies
* audit and governance requirements
* multi-tenant SaaS platforms

WideEvents allows organizations to centralize sanitization policies before events reach external systems such as Kafka, OTLP collectors, SIEMs, or analytics platforms.

Example:

```json
{
  "user": {
    "email": "***"
  }
}
```

---

# Philosophy

WideEvents is not "just logging".

It treats observability as structured operational data.

The framework is heavily inspired by:

* Canonical Log Lines
* Wide Events
* Modern distributed observability practices
* High-cardinality event pipelines
* [https://loggingsucks.com/](https://loggingsucks.com/)
* [https://stripe.com/blog/canonical-log-lines](https://stripe.com/blog/canonical-log-lines)

---

# Roadmap

WideEvents is being designed as a long-term observability platform for modern .NET systems.

Planned evolutions include:

## Observability

* ClickHouse exporter
* Loki integration
* Metrics integration
* Dashboard templates
* Native OpenTelemetry Logs support

## Performance

* Native AOT optimizations
* Zero-allocation serialization pipelines
* Advanced pooling strategies
* Benchmark suite

## Governance

* Dynamic sampling
* Schema registry
* Centralized masking policies
* Compliance-aware exporters

## Developer Experience

* Roslyn analyzers
* `dotnet new` templates
* Visual Studio tooling
* Event schema validation

---

# Contributing

Contributions, ideas, discussions, and experiments are welcome.

This project is focused on building practical observability tooling for modern .NET systems.

---

# License

Licensed under the Apache License 2.0.
