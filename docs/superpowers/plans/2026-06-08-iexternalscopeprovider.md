# IExternalScopeProvider Integration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce `IExternalScopeProvider` as a first-class data source alongside `Activity.Current` and `AsyncLocal`, merging all three in a new `WideEventBuilder` with precedence Local > Activity > Scope.

**Architecture:** A new `WideEventLoggerProvider` participates in the shared `IExternalScopeProvider` so that any `ILogger.BeginScope()` call during a request is visible to `WideEventBuilder.Build()`. `WideEventBuilder` merges scope data (lowest), Activity tags + trace IDs (middle), and `WideEvent.Drain()` AsyncLocal data (highest). The middleware switches from calling `wideEvent.Build()` to `_builder.Build()`.

**Tech Stack:** .NET 8/10 multi-target; xUnit + FluentAssertions + Moq; `Microsoft.Extensions.Logging.Abstractions`; `System.Diagnostics.Activity`.

---

## File Structure

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `src/WideEvents.Core/Builder/WideEventStructureBuilder.cs` | Dot-notation flat dict → nested dict |
| Create | `src/WideEvents.Core/Builder/IWideEventBuilder.cs` | Public contract for building the final event |
| Create | `src/WideEvents.Core/Builder/WideEventBuilder.cs` | Merges scope + Activity + AsyncLocal |
| Create | `src/WideEvents.Core/Logging/WideEventLoggerProvider.cs` | `ILoggerProvider` + `ISupportExternalScope` |
| Create | `src/WideEvents.Core/Logging/WideEventLogger.cs` | Lightweight logger (scope propagation only) |
| Modify | `src/WideEvents.Core/Context/WideEventContext.cs` | Add `internal Drain()`, delegate `SetNested` to builder |
| Modify | `src/WideEvents.Core/Context/WideEvent.cs` | Add `internal static Drain()` |
| Modify | `src/WideEvents.Core/WideEvents.Core.csproj` | Add `InternalsVisibleTo` for test project |
| Modify | `src/WideEvents.AspNetCore/WideEventMiddleware.cs` | Inject `IWideEventBuilder`; push request scope; call `_builder.Build()` |
| Modify | `src/WideEvents.AspNetCore/WideEventExtensions.cs` | Register `WideEventLoggerProvider` and `IWideEventBuilder` |
| Create | `tests/WideEvents.Core.Tests/Builder/WideEventStructureBuilderTests.cs` | Unit tests for dot-notation expansion |
| Create | `tests/WideEvents.Core.Tests/Builder/WideEventBuilderTests.cs` | Tests for merge + precedence |

---

## Task 1: Extract WideEventStructureBuilder

**Files:**
- Create: `src/WideEvents.Core/Builder/WideEventStructureBuilder.cs`
- Modify: `src/WideEvents.Core/Context/WideEventContext.cs`
- Create: `tests/WideEvents.Core.Tests/Builder/WideEventStructureBuilderTests.cs`

- [ ] **Step 1.1: Write failing tests for WideEventStructureBuilder**

Create `tests/WideEvents.Core.Tests/Builder/WideEventStructureBuilderTests.cs`:

```csharp
using FluentAssertions;
using WideEvents.Core.Builder;
using Xunit;

namespace WideEvents.Core.Tests.Builder;

public sealed class WideEventStructureBuilderTests
{
    [Fact]
    public void Build_FlatKey_ReturnsSingleLevel()
    {
        var flat = new Dictionary<string, object?> { ["status"] = 200 };

        var result = WideEventStructureBuilder.Build(flat);

        result["status"].Should().Be(200);
    }

    [Fact]
    public void Build_DottedKey_ReturnsNestedDictionary()
    {
        var flat = new Dictionary<string, object?>
        {
            ["user.id"] = "u-1",
            ["user.name"] = "alice"
        };

        var result = WideEventStructureBuilder.Build(flat);

        var user = result["user"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        user["id"].Should().Be("u-1");
        user["name"].Should().Be("alice");
    }

    [Fact]
    public void Build_DeepNesting_ReturnsMultipleLevels()
    {
        var flat = new Dictionary<string, object?> { ["a.b.c"] = "deep" };

        var result = WideEventStructureBuilder.Build(flat);

        var a = result["a"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        var b = a["b"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        b["c"].Should().Be("deep");
    }

    [Fact]
    public void Build_SiblingKeys_ShareParentDictionary()
    {
        var flat = new Dictionary<string, object?>
        {
            ["payment.method"] = "card",
            ["payment.provider"] = "stripe"
        };

        var result = WideEventStructureBuilder.Build(flat);

        var payment = result["payment"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        payment.Should().HaveCount(2);
    }

    [Fact]
    public void Build_EmptyInput_ReturnsEmptyDictionary()
    {
        var result = WideEventStructureBuilder.Build(new Dictionary<string, object?>());

        result.Should().BeEmpty();
    }
}
```

- [ ] **Step 1.2: Run test to verify it fails**

Run: `dotnet test tests/WideEvents.Core.Tests/WideEvents.Core.Tests.csproj --filter "FullyQualifiedName~WideEventStructureBuilderTests" -v minimal`

Expected: compile error — `WideEventStructureBuilder` not found.

- [ ] **Step 1.3: Create WideEventStructureBuilder**

Create `src/WideEvents.Core/Builder/WideEventStructureBuilder.cs`:

```csharp
namespace WideEvents.Core.Builder;

/// <summary>
/// Converts a flat key-value dictionary with dotted keys into a nested dictionary hierarchy.
/// Keys like <c>user.id</c> become <c>{ "user": { "id": ... } }</c>.
/// </summary>
internal static class WideEventStructureBuilder
{
    /// <summary>
    /// Builds a nested dictionary from <paramref name="flat"/>.
    /// Dotted keys are expanded into nested <see cref="Dictionary{TKey,TValue}"/> objects.
    /// </summary>
    internal static Dictionary<string, object?> Build(IReadOnlyDictionary<string, object?> flat)
    {
        var root = new Dictionary<string, object?>();
        foreach (var (key, value) in flat)
            SetNested(root, key, value);
        return root;
    }

    internal static void SetNested(Dictionary<string, object?> root, string key, object? value)
    {
        var segments = key.Split('.');
        var current = root;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (current.TryGetValue(segments[i], out var existing)
                && existing is Dictionary<string, object?> child)
            {
                current = child;
            }
            else
            {
                var created = new Dictionary<string, object?>();
                current[segments[i]] = created;
                current = created;
            }
        }

        current[segments[^1]] = value;
    }
}
```

- [ ] **Step 1.4: Update WideEventContext.Build() to delegate to WideEventStructureBuilder**

In `src/WideEvents.Core/Context/WideEventContext.cs`, replace `SetNested` and `Build()`:

```csharp
using System.Collections;
using WideEvents.Abstractions;
using WideEvents.Core.Builder;
using WideEvents.Core.Enrichers;

namespace WideEvents.Core.Context;

/// <summary>
/// Default <see cref="IWideEventContext"/> implementation. Accumulates flat key-value
/// attributes and materializes them into a nested dictionary on <see cref="Build"/>.
/// Also implements <see cref="IEnumerable{T}"/> so the raw attribute bag can be pushed
/// as an <c>ILogger</c> scope.
/// </summary>
public sealed class WideEventContext : IWideEventContext, IEnumerable<KeyValuePair<string, object?>>
{
    private readonly Dictionary<string, object?> _attributes = new();
    private readonly IReadOnlyList<IWideEventEnricher> _enrichers;

    /// <summary>
    /// Creates a new context, optionally supplying a custom set of enrichers.
    /// When <paramref name="enrichers"/> is <see langword="null"/>, defaults to
    /// <see cref="TraceActivityEnricher"/>.
    /// </summary>
    public WideEventContext(IEnumerable<IWideEventEnricher>? enrichers = null)
        => _enrichers = enrichers?.ToList()
            ?? new List<IWideEventEnricher> { new TraceActivityEnricher() };

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    public void Add(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (value is not null)
            _attributes[name] = value;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Build()
    {
        var root = WideEventStructureBuilder.Build(_attributes);
        foreach (var enricher in _enrichers)
            enricher.Enrich(root);
        return root;
    }

    /// <summary>
    /// Returns a snapshot of the current raw attributes without nesting or enrichment.
    /// Called by <see cref="WideEvent.Drain"/> to extract data for <c>WideEventBuilder</c>.
    /// </summary>
    internal IReadOnlyDictionary<string, object?> Drain()
        => new Dictionary<string, object?>(_attributes);

    /// <summary>Iterates the raw (flat) attribute bag.</summary>
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        => _attributes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

- [ ] **Step 1.5: Run all tests to verify nothing regressed**

Run: `dotnet test WideEvents.slnx -v minimal`

Expected: all previously passing tests still pass.

- [ ] **Step 1.6: Commit**

```bash
git add src/WideEvents.Core/Builder/WideEventStructureBuilder.cs \
        src/WideEvents.Core/Context/WideEventContext.cs \
        tests/WideEvents.Core.Tests/Builder/WideEventStructureBuilderTests.cs
git commit -m "refactor(core): extract WideEventStructureBuilder from WideEventContext"
```

---

## Task 2: Add Drain() to WideEvent + InternalsVisibleTo

**Files:**
- Modify: `src/WideEvents.Core/Context/WideEvent.cs`
- Modify: `src/WideEvents.Core/WideEvents.Core.csproj`

- [ ] **Step 2.1: Add InternalsVisibleTo to WideEvents.Core.csproj**

In `src/WideEvents.Core/WideEvents.Core.csproj`, inside the first `<ItemGroup>`:

```xml
<ItemGroup>
  <ProjectReference Include="..\WideEvents.Abstractions\WideEvents.Abstractions.csproj" />
  <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
    <_Parameter1>WideEvents.Core.Tests</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```

- [ ] **Step 2.2: Write failing test for WideEvent.Drain()**

Add to `tests/WideEvents.Core.Tests/WideEventTests.cs` (inside the `WideEventTests` class):

```csharp
// ── Drain ──────────────────────────────────────────────────────────────────

[Fact]
public void Drain_ReturnsAddedValues()
{
    WideEvent.Add("user.id", "u-1");
    WideEvent.Add("outcome", "ok");

    var data = WideEvent.Drain();

    data.Should().ContainKey("user.id").WhoseValue.Should().Be("u-1");
    data.Should().ContainKey("outcome").WhoseValue.Should().Be("ok");
}

[Fact]
public void Drain_ClearsContext_SubsequentCurrentIsEmpty()
{
    WideEvent.Add("outcome", "ok");

    WideEvent.Drain();

    WideEvent.Current.Build().Should().NotContainKey("outcome");
}

[Fact]
public void Drain_WhenEmpty_ReturnsEmptyDictionary()
{
    var data = WideEvent.Drain();

    data.Should().BeEmpty();
}
```

- [ ] **Step 2.3: Run test to verify it fails**

Run: `dotnet test tests/WideEvents.Core.Tests/WideEvents.Core.Tests.csproj --filter "FullyQualifiedName~WideEventTests" -v minimal`

Expected: compile error — `WideEvent.Drain` not found.

- [ ] **Step 2.4: Add Drain() to WideEvent**

Replace the content of `src/WideEvents.Core/Context/WideEvent.cs`:

```csharp
using WideEvents.Abstractions;

namespace WideEvents.Core.Context;

/// <summary>
/// Static facade that provides ambient access to the <see cref="IWideEventContext"/>
/// for the current async flow via <see cref="AsyncLocal{T}"/>.
/// <para>
/// Application code accumulates data with <see cref="Add"/>.
/// At the end of a request <see cref="WideEvents.Core.Builder.WideEventBuilder"/> calls
/// <see cref="Drain"/> to collect that data, then resets the context.
/// </para>
/// </summary>
public static class WideEvent
{
    private static readonly AsyncLocal<IWideEventContext?> CurrentContext = new();

    /// <summary>Gets (or lazily creates) the context for the current async flow.</summary>
    public static IWideEventContext Current
        => CurrentContext.Value ??= WideEventContextFactory.Create();

    /// <summary>Adds or overwrites an attribute on the current context.</summary>
    public static void Add(string key, object? value)
        => Current.Add(key, value);

    /// <summary>Clears the context for the current async flow.</summary>
    public static void Reset()
        => CurrentContext.Value = null;

    /// <summary>
    /// Returns the flat attributes accumulated in the current context and then clears it.
    /// Called by <see cref="WideEvents.Core.Builder.WideEventBuilder"/> at build time.
    /// </summary>
    internal static IReadOnlyDictionary<string, object?> Drain()
    {
        var data = CurrentContext.Value is WideEventContext ctx
            ? ctx.Drain()
            : new Dictionary<string, object?>();
        CurrentContext.Value = null;
        return data;
    }

    /// <summary>
    /// Replaces the factory used to create new contexts.
    /// Useful in tests to substitute a custom <see cref="IWideEventContext"/> implementation.
    /// </summary>
    public static void SetFactory(Func<IWideEventContext> factory)
        => WideEventContextFactory.SetFactory(factory);
}
```

- [ ] **Step 2.5: Run tests to verify they pass**

Run: `dotnet test tests/WideEvents.Core.Tests/WideEvents.Core.Tests.csproj --filter "FullyQualifiedName~WideEventTests" -v minimal`

Expected: all `WideEventTests` tests pass, including the three new `Drain_*` tests.

- [ ] **Step 2.6: Commit**

```bash
git add src/WideEvents.Core/Context/WideEvent.cs \
        src/WideEvents.Core/WideEvents.Core.csproj \
        tests/WideEvents.Core.Tests/WideEventTests.cs
git commit -m "feat(core): add internal WideEvent.Drain() that returns and clears AsyncLocal data"
```

---

## Task 3: Create WideEventLoggerProvider and WideEventLogger

**Files:**
- Create: `src/WideEvents.Core/Logging/WideEventLoggerProvider.cs`
- Create: `src/WideEvents.Core/Logging/WideEventLogger.cs`

- [ ] **Step 3.1: Create WideEventLogger**

Create `src/WideEvents.Core/Logging/WideEventLogger.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace WideEvents.Core.Logging;

/// <summary>
/// Lightweight <see cref="ILogger"/> whose only purpose is scope propagation.
/// All <c>Log</c> calls are no-ops; <see cref="BeginScope{TState}"/> pushes state
/// to the shared <see cref="IExternalScopeProvider"/> so that
/// <c>WideEventBuilder</c> can later read it via <c>ForEachScope</c>.
/// </summary>
internal sealed class WideEventLogger : ILogger
{
    private readonly IExternalScopeProvider _scopeProvider;

    internal WideEventLogger(IExternalScopeProvider scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _scopeProvider.Push(state);

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => false;

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) { }
}
```

- [ ] **Step 3.2: Create WideEventLoggerProvider**

Create `src/WideEvents.Core/Logging/WideEventLoggerProvider.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace WideEvents.Core.Logging;

/// <summary>
/// <see cref="ILoggerProvider"/> that participates in the shared
/// <see cref="IExternalScopeProvider"/> infrastructure. When any logger in the
/// application calls <c>BeginScope()</c>, the scope data is also visible to
/// <c>WideEventBuilder</c> via <see cref="ScopeProvider"/>.
/// </summary>
public sealed class WideEventLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    /// <inheritdoc/>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        => _scopeProvider = scopeProvider;

    /// <summary>
    /// The active scope provider. Exposed internally so that
    /// <c>WideEventBuilder</c> can enumerate active scopes at build time.
    /// </summary>
    internal IExternalScopeProvider ScopeProvider => _scopeProvider;

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
        => new WideEventLogger(_scopeProvider);

    /// <inheritdoc/>
    public void Dispose() { }
}
```

- [ ] **Step 3.3: Build to verify no compile errors**

Run: `dotnet build src/WideEvents.Core/WideEvents.Core.csproj -v minimal`

Expected: Build succeeded, 0 Error(s).

- [ ] **Step 3.4: Commit**

```bash
git add src/WideEvents.Core/Logging/WideEventLoggerProvider.cs \
        src/WideEvents.Core/Logging/WideEventLogger.cs
git commit -m "feat(core): add WideEventLoggerProvider + WideEventLogger for IExternalScopeProvider integration"
```

---

## Task 4: Create IWideEventBuilder and WideEventBuilder

**Files:**
- Create: `src/WideEvents.Core/Builder/IWideEventBuilder.cs`
- Create: `src/WideEvents.Core/Builder/WideEventBuilder.cs`
- Create: `tests/WideEvents.Core.Tests/Builder/WideEventBuilderTests.cs`

- [ ] **Step 4.1: Write failing tests for WideEventBuilder**

Create `tests/WideEvents.Core.Tests/Builder/WideEventBuilderTests.cs`:

```csharp
using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using WideEvents.Core.Builder;
using WideEvents.Core.Context;
using WideEvents.Core.Logging;
using Xunit;

namespace WideEvents.Core.Tests.Builder;

public sealed class WideEventBuilderTests : IDisposable
{
    private readonly WideEventLoggerProvider _provider = new();
    private WideEventBuilder Builder => new(_provider);

    public WideEventBuilderTests()
    {
        WideEvent.SetFactory(static () => new WideEventContext());
        WideEvent.Reset();
    }

    public void Dispose()
    {
        WideEvent.SetFactory(static () => new WideEventContext());
        WideEvent.Reset();
    }

    // ── AsyncLocal data ────────────────────────────────────────────────────────

    [Fact]
    public void Build_WideEventAddValues_AppearInResult()
    {
        WideEvent.Add("user.id", "u-1");

        var result = Builder.Build();

        var user = result["user"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        user["id"].Should().Be("u-1");
    }

    [Fact]
    public void Build_DrainsClearsContext_AfterBuild()
    {
        WideEvent.Add("outcome", "ok");
        Builder.Build();

        // After Build, Drain was called — context is empty
        WideEvent.Current.Build().Should().NotContainKey("outcome");
    }

    // ── Scope data ─────────────────────────────────────────────────────────────

    [Fact]
    public void Build_ScopeValues_AppearInResult()
    {
        using var logger = _provider.CreateLogger("test");
        using var scope = logger.BeginScope(
            new Dictionary<string, object?> { ["tenant"] = "acme" });

        var result = Builder.Build();

        result.Should().ContainKey("tenant").WhoseValue.Should().Be("acme");
    }

    [Fact]
    public void Build_AfterScopeDisposed_ScopeValuesAbsent()
    {
        ILogger logger = _provider.CreateLogger("test");
        var scope = logger.BeginScope(
            new Dictionary<string, object?> { ["tenant"] = "acme" });
        scope!.Dispose();

        var result = Builder.Build();

        result.Should().NotContainKey("tenant");
    }

    // ── Activity data ──────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithActivity_IncludesTraceFields()
    {
        using var activity = new Activity("test-op");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var result = Builder.Build();

        result.Should().ContainKey("trace_id");
        result.Should().ContainKey("span_id");
        result.Should().ContainKey("trace_flags");
        result["trace_id"].Should().Be(activity.TraceId.ToString());
    }

    [Fact]
    public void Build_WithoutActivity_OmitsTraceFields()
    {
        Activity.Current = null;

        var result = Builder.Build();

        result.Should().NotContainKey("trace_id");
        result.Should().NotContainKey("span_id");
    }

    [Fact]
    public void Build_ActivityTags_AppearInResult()
    {
        using var activity = new Activity("test-op");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        activity.SetTag("http.method", "GET");

        var result = Builder.Build();

        result.Should().ContainKey("http");
        var http = result["http"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        http["method"].Should().Be("GET");
    }

    // ── Merge precedence ───────────────────────────────────────────────────────

    [Fact]
    public void Build_LocalWinsOverScope_WhenSameKey()
    {
        using var logger = _provider.CreateLogger("test");
        using var scope = logger.BeginScope(
            new Dictionary<string, object?> { ["outcome"] = "from-scope" });

        WideEvent.Add("outcome", "from-local");

        var result = Builder.Build();

        result["outcome"].Should().Be("from-local");
    }

    [Fact]
    public void Build_LocalWinsOverActivity_WhenSameKey()
    {
        using var activity = new Activity("test-op");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        activity.SetTag("outcome", "from-activity");

        WideEvent.Add("outcome", "from-local");

        var result = Builder.Build();

        result["outcome"].Should().Be("from-local");
    }

    [Fact]
    public void Build_ActivityWinsOverScope_WhenSameKey()
    {
        using var logger = _provider.CreateLogger("test");
        using var scope = logger.BeginScope(
            new Dictionary<string, object?> { ["outcome"] = "from-scope" });

        using var activity = new Activity("test-op");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        activity.SetTag("outcome", "from-activity");

        var result = Builder.Build();

        result["outcome"].Should().Be("from-activity");
    }
}
```

- [ ] **Step 4.2: Run test to verify it fails**

Run: `dotnet test tests/WideEvents.Core.Tests/WideEvents.Core.Tests.csproj --filter "FullyQualifiedName~WideEventBuilderTests" -v minimal`

Expected: compile error — `IWideEventBuilder`, `WideEventBuilder` not found.

- [ ] **Step 4.3: Create IWideEventBuilder**

Create `src/WideEvents.Core/Builder/IWideEventBuilder.cs`:

```csharp
namespace WideEvents.Core.Builder;

/// <summary>
/// Builds the final wide event by merging all context sources:
/// <see cref="System.Diagnostics.Activity.Current"/> trace data, active
/// <c>ILogger</c> scopes, and the <c>WideEvent</c> AsyncLocal buffer.
/// </summary>
public interface IWideEventBuilder
{
    /// <summary>
    /// Produces a fully-merged, nested wide event dictionary.
    /// Merge precedence (highest wins): <c>WideEvent.Add()</c> values
    /// &gt; Activity tags &gt; scope values.
    /// </summary>
    IReadOnlyDictionary<string, object?> Build();
}
```

- [ ] **Step 4.4: Create WideEventBuilder**

Create `src/WideEvents.Core/Builder/WideEventBuilder.cs`:

```csharp
using System.Diagnostics;
using WideEvents.Core.Context;
using WideEvents.Core.Logging;

namespace WideEvents.Core.Builder;

/// <summary>
/// Default <see cref="IWideEventBuilder"/> implementation.
/// Merges three data sources in ascending precedence order:
/// <list type="number">
///   <item>Scope values from <c>IExternalScopeProvider.ForEachScope()</c></item>
///   <item><see cref="Activity.Current"/> trace IDs and tags</item>
///   <item><see cref="WideEvent.Drain()"/> AsyncLocal buffer (highest priority)</item>
/// </list>
/// The merged flat dictionary is then expanded by <see cref="WideEventStructureBuilder"/>
/// into a nested hierarchy.
/// </summary>
public sealed class WideEventBuilder : IWideEventBuilder
{
    private readonly WideEventLoggerProvider _provider;

    /// <summary>Initializes the builder with the provider that holds the active scope provider.</summary>
    public WideEventBuilder(WideEventLoggerProvider provider)
        => _provider = provider;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Build()
    {
        var flat = new Dictionary<string, object?>();

        // 1. Scope values — lowest precedence
        _provider.ScopeProvider.ForEachScope(
            (scope, state) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> kvps)
                    foreach (var kvp in kvps)
                        if (kvp.Value is not null)
                            state[kvp.Key] = kvp.Value;
            },
            flat);

        // 2. Activity data — middle precedence
        var activity = Activity.Current;
        if (activity is not null)
        {
            flat["trace_id"] = activity.TraceId.ToString();
            flat["span_id"] = activity.SpanId.ToString();
            flat["trace_flags"] = activity.ActivityTraceFlags.ToString();
            foreach (var tag in activity.TagObjects)
                if (tag.Value is not null)
                    flat[tag.Key] = tag.Value;
        }

        // 3. WideEvent AsyncLocal buffer — highest precedence; also clears the context
        foreach (var (key, value) in WideEvent.Drain())
            flat[key] = value;

        return WideEventStructureBuilder.Build(flat);
    }
}
```

- [ ] **Step 4.5: Run tests to verify they pass**

Run: `dotnet test tests/WideEvents.Core.Tests/WideEvents.Core.Tests.csproj --filter "FullyQualifiedName~WideEventBuilderTests" -v minimal`

Expected: all `WideEventBuilderTests` pass.

- [ ] **Step 4.6: Run full test suite to check for regressions**

Run: `dotnet test WideEvents.slnx -v minimal`

Expected: all tests pass.

- [ ] **Step 4.7: Commit**

```bash
git add src/WideEvents.Core/Builder/IWideEventBuilder.cs \
        src/WideEvents.Core/Builder/WideEventBuilder.cs \
        tests/WideEvents.Core.Tests/Builder/WideEventBuilderTests.cs
git commit -m "feat(core): add IWideEventBuilder + WideEventBuilder merging Activity, scopes, and AsyncLocal"
```

---

## Task 5: Refactor WideEventMiddleware

**Files:**
- Modify: `src/WideEvents.AspNetCore/WideEventMiddleware.cs`
- Modify: `tests/WideEvents.AspNetCore.Tests/WideEventMiddlewareTests.cs`

- [ ] **Step 5.1: Update WideEventMiddlewareTests to expect IWideEventBuilder**

In `tests/WideEvents.AspNetCore.Tests/WideEventMiddlewareTests.cs`, update `BuildMiddleware` to accept a builder:

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WideEvents.Abstractions;
using WideEvents.Core.Builder;
using Xunit;

namespace WideEvents.AspNetCore.Tests;

public sealed class WideEventMiddlewareTests
{
    private static WideEventMiddleware BuildMiddleware(
        RequestDelegate? next = null,
        IEnumerable<IHttpWideEventEnricher>? enrichers = null,
        IWideEventExporter? exporter = null,
        ILogger<WideEventMiddleware>? logger = null,
        IWideEventBuilder? builder = null)
    {
        var mockExporter = new Mock<IWideEventExporter>();
        mockExporter
            .Setup(e => e.ExportAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockBuilder = new Mock<IWideEventBuilder>();
        mockBuilder
            .Setup(b => b.Build())
            .Returns(new Dictionary<string, object?>());

        return new WideEventMiddleware(
            next ?? (_ => Task.CompletedTask),
            enrichers ?? [],
            exporter ?? mockExporter.Object,
            logger ?? NullLogger<WideEventMiddleware>.Instance,
            builder ?? mockBuilder.Object);
    }

    private static (Mock<IWideEventContext> mock, IWideEventContext context) MockContext()
    {
        var mock = new Mock<IWideEventContext>();
        mock.Setup(c => c.Build()).Returns(new Dictionary<string, object?>());
        return (mock, mock.Object);
    }

    // ── Enrichers ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_CallsEnrichRequest_BeforeNext()
    {
        var order = new List<string>();
        var enricher = new Mock<IHttpWideEventEnricher>();
        enricher
            .Setup(e => e.EnrichRequest(It.IsAny<HttpContext>(), It.IsAny<IWideEventContext>()))
            .Callback(() => order.Add("enrich"));

        RequestDelegate next = _ =>
        {
            order.Add("next");
            return Task.CompletedTask;
        };

        var (_, ctx) = MockContext();
        var middleware = BuildMiddleware(next: next, enrichers: [enricher.Object]);

        await middleware.Invoke(new DefaultHttpContext(), ctx);

        order.Should().Equal("enrich", "next");
    }

    [Fact]
    public async Task Invoke_CallsEnrichResponse_AfterNext()
    {
        var order = new List<string>();
        var enricher = new Mock<IHttpWideEventEnricher>();
        enricher
            .Setup(e => e.EnrichResponse(It.IsAny<HttpContext>(), It.IsAny<IWideEventContext>()))
            .Callback(() => order.Add("response"));

        RequestDelegate next = _ =>
        {
            order.Add("next");
            return Task.CompletedTask;
        };

        var (_, ctx) = MockContext();
        var middleware = BuildMiddleware(next: next, enrichers: [enricher.Object]);

        await middleware.Invoke(new DefaultHttpContext(), ctx);

        order.Should().Equal("next", "response");
    }

    [Fact]
    public async Task Invoke_PassesCorrectContextToEnrichers()
    {
        var httpContext = new DefaultHttpContext();
        var enricher = new Mock<IHttpWideEventEnricher>();
        var (mock, ctx) = MockContext();

        var middleware = BuildMiddleware(enrichers: [enricher.Object]);
        await middleware.Invoke(httpContext, ctx);

        enricher.Verify(e => e.EnrichRequest(httpContext, ctx), Times.Once);
        enricher.Verify(e => e.EnrichResponse(httpContext, ctx), Times.Once);
    }

    // ── Builder ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_CallsBuilderBuild_AtCompletion()
    {
        var (_, ctx) = MockContext();
        var builder = new Mock<IWideEventBuilder>();
        builder.Setup(b => b.Build()).Returns(new Dictionary<string, object?>());

        var middleware = BuildMiddleware(builder: builder.Object);
        await middleware.Invoke(new DefaultHttpContext(), ctx);

        builder.Verify(b => b.Build(), Times.Once);
    }

    // ── Exporter ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_ExportsBuiltEvent()
    {
        var builtEvent = new Dictionary<string, object?> { ["outcome"] = "ok" };
        var (_, ctx) = MockContext();

        var builder = new Mock<IWideEventBuilder>();
        builder.Setup(b => b.Build()).Returns(builtEvent);

        var exporter = new Mock<IWideEventExporter>();
        exporter
            .Setup(e => e.ExportAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var middleware = BuildMiddleware(builder: builder.Object, exporter: exporter.Object);
        await middleware.Invoke(new DefaultHttpContext(), ctx);

        exporter.Verify(
            e => e.ExportAsync(builtEvent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_ExportsEvenOnException()
    {
        var (_, ctx) = MockContext();
        var builder = new Mock<IWideEventBuilder>();
        builder.Setup(b => b.Build()).Returns(new Dictionary<string, object?>());

        var exporter = new Mock<IWideEventExporter>();
        exporter
            .Setup(e => e.ExportAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var middleware = BuildMiddleware(
            next: _ => throw new InvalidOperationException("boom"),
            builder: builder.Object,
            exporter: exporter.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.Invoke(new DefaultHttpContext(), ctx));

        exporter.Verify(
            e => e.ExportAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Atributos de erro ──────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_OnException_AddsErrorTypeAndMessage()
    {
        var (mock, ctx) = MockContext();
        var middleware = BuildMiddleware(
            next: _ => throw new InvalidOperationException("algo deu errado"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.Invoke(new DefaultHttpContext(), ctx));

        mock.Verify(c => c.Add("error.type", "InvalidOperationException"), Times.Once);
        mock.Verify(c => c.Add("error.message", "algo deu errado"), Times.Once);
    }

    [Fact]
    public async Task Invoke_OnException_Rethrows()
    {
        var (_, ctx) = MockContext();
        var original = new ArgumentNullException("param");
        var middleware = BuildMiddleware(next: _ => throw original);

        var thrown = await Assert.ThrowsAsync<ArgumentNullException>(
            () => middleware.Invoke(new DefaultHttpContext(), ctx));

        thrown.Should().BeSameAs(original);
    }

    [Fact]
    public async Task Invoke_OnSuccess_DoesNotAddErrorAttributes()
    {
        var (mock, ctx) = MockContext();
        var middleware = BuildMiddleware();

        await middleware.Invoke(new DefaultHttpContext(), ctx);

        mock.Verify(c => c.Add("error.type", It.IsAny<object?>()), Times.Never);
        mock.Verify(c => c.Add("error.message", It.IsAny<object?>()), Times.Never);
    }

    // ── duration_ms ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_AddsDurationMs_AsDouble()
    {
        var (mock, ctx) = MockContext();
        var middleware = BuildMiddleware();

        await middleware.Invoke(new DefaultHttpContext(), ctx);

        mock.Verify(
            c => c.Add("duration_ms", It.Is<object?>(v => v != null && (double)v >= 0)),
            Times.Once);
    }

    // ── Logger scope ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_OpensScope_WithRequestDictionary()
    {
        var (_, ctx) = MockContext();
        var logger = new Mock<ILogger<WideEventMiddleware>>();
        logger
            .Setup(l => l.BeginScope(It.IsAny<Dictionary<string, object?>>()))
            .Returns(Mock.Of<IDisposable>());

        var middleware = BuildMiddleware(logger: logger.Object);
        await middleware.Invoke(new DefaultHttpContext(), ctx);

        logger.Verify(
            l => l.BeginScope(It.IsAny<Dictionary<string, object?>>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_DisposesScope_WhenRequestCompletes()
    {
        var (_, ctx) = MockContext();
        var disposable = new Mock<IDisposable>();
        var logger = new Mock<ILogger<WideEventMiddleware>>();
        logger
            .Setup(l => l.BeginScope(It.IsAny<Dictionary<string, object?>>()))
            .Returns(disposable.Object);

        var middleware = BuildMiddleware(logger: logger.Object);
        await middleware.Invoke(new DefaultHttpContext(), ctx);

        disposable.Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Invoke_DisposesScope_WhenRequestThrows()
    {
        var (_, ctx) = MockContext();
        var disposable = new Mock<IDisposable>();
        var logger = new Mock<ILogger<WideEventMiddleware>>();
        logger
            .Setup(l => l.BeginScope(It.IsAny<Dictionary<string, object?>>()))
            .Returns(disposable.Object);

        var middleware = BuildMiddleware(
            next: _ => throw new InvalidOperationException("boom"),
            logger: logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.Invoke(new DefaultHttpContext(), ctx));

        disposable.Verify(d => d.Dispose(), Times.Once);
    }
}
```

- [ ] **Step 5.2: Run tests to verify they fail** (middleware not yet updated)

Run: `dotnet test tests/WideEvents.AspNetCore.Tests/WideEvents.AspNetCore.Tests.csproj --filter "FullyQualifiedName~WideEventMiddlewareTests" -v minimal`

Expected: compile error — `WideEventMiddleware` constructor doesn't accept `IWideEventBuilder`.

- [ ] **Step 5.3: Rewrite WideEventMiddleware**

Replace `src/WideEvents.AspNetCore/WideEventMiddleware.cs`:

```csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WideEvents.Abstractions;
using WideEvents.Core.Builder;
using WideEvents.Core.Context;

namespace WideEvents.AspNetCore;

/// <summary>
/// ASP.NET Core middleware that wraps each HTTP request in a wide-event context,
/// runs registered <see cref="IHttpWideEventEnricher"/> implementations, and exports
/// the built event after the response is complete.
/// <para>
/// Request metadata (method, path) is pushed as an <c>ILogger</c> scope so that
/// <see cref="IWideEventBuilder"/> can read it via <c>IExternalScopeProvider</c>.
/// Enrichers and application code add further data through <c>WideEvent.Add()</c>.
/// </para>
/// </summary>
public sealed class WideEventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnumerable<IHttpWideEventEnricher> _enrichers;
    private readonly IWideEventExporter _exporter;
    private readonly ILogger<WideEventMiddleware> _logger;
    private readonly IWideEventBuilder _builder;

    /// <summary>Initializes the middleware with its pipeline dependencies.</summary>
    public WideEventMiddleware(
        RequestDelegate next,
        IEnumerable<IHttpWideEventEnricher> enrichers,
        IWideEventExporter exporter,
        ILogger<WideEventMiddleware> logger,
        IWideEventBuilder builder)
    {
        _next = next;
        _enrichers = enrichers;
        _exporter = exporter;
        _logger = logger;
        _builder = builder;
    }

    /// <summary>
    /// Processes the request: opens a scope with request metadata, enriches on entry,
    /// invokes the pipeline, captures <c>error.*</c> fields on exception, and exports
    /// the built event on completion via <see cref="IWideEventBuilder.Build"/>.
    /// </summary>
    public async Task Invoke(HttpContext context, IWideEventContext wideEvent)
    {
        var requestScope = new Dictionary<string, object?>
        {
            ["http.method"] = context.Request.Method,
            ["http.path"] = context.Request.Path.Value,
        };
        using var scope = _logger.BeginScope(requestScope);
        var start = Stopwatch.GetTimestamp();

        try
        {
            foreach (var enricher in _enrichers)
                enricher.EnrichRequest(context, wideEvent);

            await _next(context);

            foreach (var enricher in _enrichers)
                enricher.EnrichResponse(context, wideEvent);
        }
        catch (Exception ex)
        {
            wideEvent.Add("error.type", ex.GetType().Name);
            wideEvent.Add("error.message", ex.Message);
            throw;
        }
        finally
        {
            wideEvent.Add("duration_ms", Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            var built = _builder.Build();
            await _exporter.ExportAsync(built, context.RequestAborted);
        }
    }
}
```

- [ ] **Step 5.4: Run middleware tests to verify they pass**

Run: `dotnet test tests/WideEvents.AspNetCore.Tests/WideEvents.AspNetCore.Tests.csproj --filter "FullyQualifiedName~WideEventMiddlewareTests" -v minimal`

Expected: all `WideEventMiddlewareTests` pass.

- [ ] **Step 5.5: Run full test suite**

Run: `dotnet test WideEvents.slnx -v minimal`

Expected: all tests pass.

- [ ] **Step 5.6: Commit**

```bash
git add src/WideEvents.AspNetCore/WideEventMiddleware.cs \
        tests/WideEvents.AspNetCore.Tests/WideEventMiddlewareTests.cs
git commit -m "refactor(aspnetcore): inject IWideEventBuilder into middleware; push request metadata as ILogger scope"
```

---

## Task 6: Update DI Registration

**Files:**
- Modify: `src/WideEvents.AspNetCore/WideEventExtensions.cs`
- Modify: `tests/WideEvents.AspNetCore.Tests/WideEventExtensionsTests.cs`

- [ ] **Step 6.1: Read current WideEventExtensionsTests**

Read `tests/WideEvents.AspNetCore.Tests/WideEventExtensionsTests.cs` to understand existing assertions before modifying registration.

- [ ] **Step 6.2: Update AddWideEvents registration**

Replace `src/WideEvents.AspNetCore/WideEventExtensions.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WideEvents.Abstractions;
using WideEvents.AspNetCore.Enrichers;
using WideEvents.Core.Builder;
using WideEvents.Core.Context;
using WideEvents.Core.Logging;

namespace WideEvents.AspNetCore;

public static class WideEventExtensions
{
    /// <summary>
    /// Registers the services required by WideEvents in the DI container.
    /// Must be called before <see cref="UseWideEvents"/>.
    /// </summary>
    public static IServiceCollection AddWideEvents(
        this IServiceCollection services,
        Action<WideEventsOptions>? configure = null)
    {
        var options = new WideEventsOptions();
        configure?.Invoke(options);

        // Returns WideEvent.Current so the context injected into the middleware is the same
        // instance the handler code reaches via WideEvent.Add().
        services.AddScoped<IWideEventContext>(_ => WideEvent.Current);

        // Register the provider as a singleton, then register it as ILoggerProvider so the
        // logging infrastructure calls SetScopeProvider() on it, sharing the external scope.
        services.AddSingleton<WideEventLoggerProvider>();
        services.AddSingleton<ILoggerProvider>(sp =>
            sp.GetRequiredService<WideEventLoggerProvider>());

        // The builder reads the scope provider from WideEventLoggerProvider.
        services.AddSingleton<IWideEventBuilder>(sp =>
            new WideEventBuilder(sp.GetRequiredService<WideEventLoggerProvider>()));

        services.AddSingleton<IHttpWideEventEnricher, DefaultHttpEnricher>();
        services.AddSingleton<IWideEventExporter, LoggerWideEventExporter>();

        if (options.AuthEnricher is { } authOpts)
            services.AddSingleton<IHttpWideEventEnricher>(_ => new AuthEnricher(authOpts));

        return services;
    }

    /// <summary>Adds <see cref="WideEventMiddleware"/> to the request pipeline.</summary>
    public static IApplicationBuilder UseWideEvents(this IApplicationBuilder app)
        => app.UseMiddleware<WideEventMiddleware>();
}
```

- [ ] **Step 6.3: Run WideEventExtensionsTests to see what needs updating**

Run: `dotnet test tests/WideEvents.AspNetCore.Tests/WideEvents.AspNetCore.Tests.csproj --filter "FullyQualifiedName~WideEventExtensionsTests" -v minimal`

If tests fail, update `WideEventExtensionsTests.cs` to also assert:
- `services.BuildServiceProvider().GetService<WideEventLoggerProvider>()` is not null
- `services.BuildServiceProvider().GetService<IWideEventBuilder>()` is not null

- [ ] **Step 6.4: Run full test suite**

Run: `dotnet test WideEvents.slnx -v minimal`

Expected: all tests pass.

- [ ] **Step 6.5: Commit**

```bash
git add src/WideEvents.AspNetCore/WideEventExtensions.cs \
        tests/WideEvents.AspNetCore.Tests/WideEventExtensionsTests.cs
git commit -m "feat(aspnetcore): register WideEventLoggerProvider and IWideEventBuilder in AddWideEvents()"
```

---

## Task 7: XML Documentation

**Files:**
- All new/modified public types

- [ ] **Step 7.1: Verify XML docs on all public types**

Ensure the following types have meaningful `<summary>` XML docs (the templates in Tasks 1–6 already include them). Run a build to check for documentation warnings:

Run: `dotnet build WideEvents.slnx -v minimal`

If any `CS1591` (missing XML doc) warnings appear, add the missing docs.

Types requiring docs (already covered in plan code):
- `WideEventStructureBuilder` — internal, no public doc needed
- `IWideEventBuilder` — ✓ doc in Task 4.3
- `WideEventBuilder` — ✓ doc in Task 4.4
- `WideEventLoggerProvider` — ✓ doc in Task 3.2
- `WideEventLogger` — internal, no public doc needed
- `WideEventContext.Drain()` — internal, ✓ doc in Task 1.4
- `WideEvent.Drain()` — internal, ✓ doc in Task 2.4
- `WideEventMiddleware` — ✓ doc in Task 5.3

- [ ] **Step 7.2: Commit if any doc changes were needed**

```bash
git add -p  # stage only doc changes
git commit -m "docs(core,aspnetcore): add XML documentation to new public types"
```

---

## Task 8: Structured Builder Tests + Final Verification

**Files:**
- `tests/WideEvents.Core.Tests/Builder/WideEventStructureBuilderTests.cs` — already done in Task 1
- `tests/WideEvents.Core.Tests/Builder/WideEventBuilderTests.cs` — already done in Task 4

- [ ] **Step 8.1: Run the complete test suite one final time**

Run: `dotnet test WideEvents.slnx -v normal`

Expected: all tests pass across both test projects.

- [ ] **Step 8.2: Build the solution in Release to verify no packaging issues**

Run: `dotnet build WideEvents.slnx -c Release -v minimal`

Expected: Build succeeded, 0 Error(s), 0 Warning(s).

- [ ] **Step 8.3: Final commit (if any stragglers)**

```bash
git status
# stage and commit any remaining changes
git commit -m "chore: final cleanup after IExternalScopeProvider integration"
```

---

## Self-Review Against Spec

| Spec Requirement | Covered in Task |
|-----------------|----------------|
| `WideEventLoggerProvider` : `ILoggerProvider` + `ISupportExternalScope` | Task 3 |
| Default to `LoggerExternalScopeProvider` | Task 3 |
| `WideEventLogger.BeginScope()` pushes to scope provider | Task 3 |
| `IWideEventBuilder` + `WideEventBuilder` | Task 4 |
| `Build()` reads `Activity.Current` (trace_id, span_id, trace_flags, tags) | Task 4 |
| `Build()` reads scopes via `scopeProvider.ForEachScope()` | Task 4 |
| `Build()` reads AsyncLocal via `WideEvent.Drain()` | Task 4 |
| Merge precedence: Local > Activity > Scope | Task 4 |
| `WideEvent.Add()` kept | Task 2 |
| `WideEvent.Drain()` added (internal) | Task 2 |
| `Drain()` clears AsyncLocal | Task 2 |
| `WideEventStructureBuilder` with arbitrary nesting | Task 1 |
| Middleware uses `logger.BeginScope()` for request scope | Task 5 |
| Middleware builds via `WideEventBuilder` | Task 5 |
| `services.AddWideEvents()` registers all new services | Task 6 |
| XML docs on `WideEvent`, `WideEventBuilder`, `WideEventLoggerProvider`, `WideEventMiddleware`, `IWideEventBuilder` | Task 7 |
| Tests: Activity values in final event | Task 4 (`Build_WithActivity_IncludesTraceFields`) |
| Tests: Scope values in final event | Task 4 (`Build_ScopeValues_AppearInResult`) |
| Tests: WideEvent.Add values in final event | Task 4 (`Build_WideEventAddValues_AppearInResult`) |
| Tests: Merge precedence Local>Activity>Scope | Task 4 (three precedence tests) |
| Tests: Drain clears AsyncLocal | Task 2 + Task 4 (`Build_DrainsClearsContext_AfterBuild`) |
| Tests: Structured builder creates nested JSON | Task 1 (`WideEventStructureBuilderTests`) |
| Tests: Middleware emits single canonical event | Task 5 (`Invoke_CallsBuilderBuild_AtCompletion`) |

All spec requirements are covered. No gaps found.
