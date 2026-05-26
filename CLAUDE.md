# CLAUDE.md

Project context for Claude Code sessions. Complementary to
[AGENTS.md](AGENTS.md) (which already has build/test commands, code style,
security guidance, and commit conventions) — skim both at the start of a
session.

## What this project is

WideEvents is a .NET library for **wide events** / canonical log lines:
accumulate context across a unit of work and emit a single rich, structured
event at the end. The root [README](README.md) is the vision/marketing
document and describes a broad roadmap. **Treat the README as aspirational.**
The accurate, up-to-date description of what is actually implemented lives
in [docs/en-US/](docs/en-US/README.md) and [docs/pt-BR/](docs/pt-BR/README.md).

## Repository layout

```
src/
  WideEvents.Abstractions/   IWideEventContext, IWideEventExporter
  WideEvents.Core/           WideEvent (static facade), WideEventContext
  WideEvents.AspNetCore/     WideEventMiddleware + UseWideEvents
tests/
  WideEvents.Core.Tests/     xUnit + FluentAssertions
sample/
  WideEvents.Sample.Api/     Minimal API + Serilog (PrettyJsonFormatter)
docs/
  en-US/  pt-BR/             implemented behavior, not the roadmap
WideEvents.slnx              solution file (XML .slnx format — no .sln)
Directory.Build.props        net10.0, TreatWarningsAsErrors, etc.
Directory.Packages.props     central package management
```

The files under `build/` (`Directory.Build.props`, `Directory.Packages.props`)
are dead duplicates — MSBuild ignores them because no projects live under
`build/`.

## Implementation state

Built and tested today:

- `WideEvent.Add/Current/Reset` with an `AsyncLocal` context.
- `WideEventContext.Build()` — expands dotted keys into nested objects,
  injects `trace_id`/`span_id`/`trace_flags` from `Activity.Current`,
  non-mutating.
- `IWideEventContext` (Core implements it).
- `IWideEventExporter` — **contract only; not consumed by any pipeline yet.**
  Intentional: exists so exporter packages can build against a stable
  contract. Today the middleware emits via `ILogger`.
- ASP.NET Core middleware: captures `http.method`, `http.path`,
  `http.status_code` (on success), `error.type`/`error.message` (on throw),
  `duration_ms`; logs with `{@WideEvent}`.

Not built (despite README claims): OTLP/Kafka exporters, adaptive sampling,
PII masking, source-generated schemas, AOT optimizations.

## Build, test, run

- Use the `.slnx` file: `dotnet build WideEvents.slnx`,
  `dotnet test WideEvents.slnx`. **There is no `.sln`.**
- Root `Directory.Build.props` sets `GeneratePackageOnBuild=true`. Any
  test or sample project must override `IsPackable=false` and
  `GeneratePackageOnBuild=false` to avoid being packed.
- Sample: `dotnet run --project sample/WideEvents.Sample.Api` — listens on
  `http://localhost:5080`. Endpoints: `/`, `/checkout/{userId}` (success
  path), `/boom` (error path with `error.*` and HTTP 500).

## Gotchas

- **File lock when rebuilding the sample.** If the sample is running,
  `dotnet build sample/...` fails copying the AspNetCore DLL. Kill the
  process first (PowerShell: stop any `dotnet.exe` whose `CommandLine`
  contains `WideEvents.Sample.Api`).
- **`http.path` must be `Request.Path.Value`, not `Request.Path`.**
  `PathString` is a struct and structured loggers destructure it into
  `{Value, HasValue, $type}`. This was a real bug; do not regress.
- **Default `ILogger` console does not destructure `{@WideEvent}`** — even
  `AddJsonConsole` serializes the dictionary via `ToString()`. Nested JSON
  rendering needs Serilog (or another structured logger that honors `@`).
- **`Activity.Current` is null unless something listens.** ASP.NET Core
  creates a request `Activity` only when an `ActivityListener` is
  registered. The sample registers a sample-everything listener so
  `trace_id`/`span_id` show up; production apps usually rely on OpenTelemetry
  for this.
- **The slnx is XML, not the old `.sln` format.** Edit it as XML; do not
  attempt `dotnet sln` commands against it.

## Conventions

- Conventional Commits (see AGENTS.md). Scopes used so far: `core`,
  `abstractions`, `aspnetcore`, `sample`, `docs`.
- Do not commit `.idea/`, `*.DotSettings.user`, `bin/`, `obj/`.
- Documentation is bilingual (`en-US` + `pt-BR`) — keep both in sync.
- `WideEvent.Add` overwrites on duplicate keys and ignores `null` values;
  empty/whitespace keys throw — preserve this semantics when changing Core.
