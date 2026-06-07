# IExternalScopeProvider Interop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Push `WideEventContext` as an `ILogger` scope on every request so that Serilog, OpenTelemetry, and any `IExternalScopeProvider`-aware provider automatically see WideEvent fields in all log entries during the request — without any extra adapter or configuration.

**Architecture:** `WideEventContext` implements `IEnumerable<KeyValuePair<string, object?>>` so providers can extract structured key-value pairs from it (the standard scope enrichment contract). `WideEventMiddleware` injects `ILogger<WideEventMiddleware>` and calls `_logger.BeginScope(wideEvent)` at the start of each request; the scope remains open through the `finally` block (including `ExportAsync`) and is disposed automatically via `using var`. The existing `AsyncLocal` is untouched — it handles `WideEvent.Current` O(1) lookup; the scope is additive.

**Tech Stack:** xUnit, FluentAssertions, Moq, `Microsoft.Extensions.Logging.Abstractions` (`NullLogger<T>`)

---

## File Map

| Action | File | What changes |
|--------|------|-------------|
| Modify | `src/WideEvents.Core/Context/WideEventContext.cs` | Implement `IEnumerable<KeyValuePair<string, object?>>` |
| Modify | `src/WideEvents.AspNetCore/WideEventMiddleware.cs` | Add `ILogger<WideEventMiddleware>` field + ctor param; open scope in `Invoke` |
| Modify | `tests/WideEvents.Core.Tests/WideEventContextTests.cs` | Add enumeration tests |
| Modify | `tests/WideEvents.AspNetCore.Tests/WideEventMiddlewareTests.cs` | Add scope tests; update `BuildMiddleware` helper |

---

## Task 1: `WideEventContext` implements `IEnumerable<KeyValuePair<string, object?>>`

**Files:**
- Modify: `tests/WideEvents.Core.Tests/WideEventContextTests.cs`
- Modify: `src/WideEvents.Core/Context/WideEventContext.cs`

- [ ] **Step 1.1: Write three failing tests**

  Append the following region to `tests/WideEvents.Core.Tests/WideEventContextTests.cs`, inside `public sealed class WideEventContextTests`:

  ```csharp
  // ── IEnumerable<KeyValuePair<string, object?>> ────────────────────────────

  [Fact]
  public void Enumeration_ReturnsStoredAttributes()
  {
      var context = new WideEventContext(enrichers: []);
      context.Add("user.id", "u-1");
      context.Add("http.method", "GET");

      var pairs = context.ToList();

      pairs.Should().ContainSingle(p => p.Key == "user.id" && (string?)p.Value == "u-1");
      pairs.Should().ContainSingle(p => p.Key == "http.method" && (string?)p.Value == "GET");
  }

  [Fact]
  public void Enumeration_ReflectsLaterMutations()
  {
      var context = new WideEventContext(enrichers: []);
      context.Add("a", 1);
      context.Add("b", 2);

      context.Select(p => p.Key).Should().BeEquivalentTo(["a", "b"]);
  }

  [Fact]
  public void Enumeration_DoesNotYieldNullValues()
  {
      // Add() rejects null — the invariant is enforced at the gate; verify enumeration reflects it.
      var context = new WideEventContext(enrichers: []);
      context.Add("key", "value");

      context.Should().AllSatisfy(p => p.Value.Should().NotBeNull());
  }
  ```

- [ ] **Step 1.2: Add stub to `WideEventContext` so tests compile**

  Replace the class declaration line in `src/WideEvents.Core/Context/WideEventContext.cs`:

  ```csharp
  // Before:
  public sealed class WideEventContext : IWideEventContext

  // After:
  public sealed class WideEventContext : IWideEventContext, IEnumerable<KeyValuePair<string, object?>>
  ```

  Then add stub methods at the bottom of the class (before the closing `}`):

  ```csharp
  public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
      => throw new NotImplementedException();

  IEnumerator IEnumerable.GetEnumerator()
      => throw new NotImplementedException();
  ```

  Add the missing using at the top of the file if not already present (global usings in .NET 10 cover `System.Collections.Generic`, so no explicit `using` is needed).

- [ ] **Step 1.3: Run tests and verify the 3 new tests fail**

  ```
  dotnet test tests/WideEvents.Core.Tests/WideEvents.Core.Tests.csproj
  ```

  Expected: all pre-existing tests pass; the 3 new tests fail with `NotImplementedException`.

- [ ] **Step 1.4: Replace stubs with real implementation**

  Replace the two stub methods added in Step 1.2 with:

  ```csharp
  public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
      => _attributes.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  ```

- [ ] **Step 1.5: Run tests and verify all pass**

  ```
  dotnet test tests/WideEvents.Core.Tests/WideEvents.Core.Tests.csproj
  ```

  Expected: all tests pass, including the 3 new enumeration tests.

- [ ] **Step 1.6: Commit**

  ```
  git add src/WideEvents.Core/Context/WideEventContext.cs tests/WideEvents.Core.Tests/WideEventContextTests.cs
  git commit -m "feat(core): WideEventContext implements IEnumerable<KeyValuePair<string, object?>>"
  ```

---

## Task 2: `WideEventMiddleware` injects logger and opens scope per request

**Files:**
- Modify: `tests/WideEvents.AspNetCore.Tests/WideEventMiddlewareTests.cs`
- Modify: `src/WideEvents.AspNetCore/WideEventMiddleware.cs`

- [ ] **Step 2.1: Write two failing tests and update `BuildMiddleware`**

  In `tests/WideEvents.AspNetCore.Tests/WideEventMiddlewareTests.cs`:

  1. Add `using Microsoft.Extensions.Logging;` and `using Microsoft.Extensions.Logging.Abstractions;` at the top.

  2. Replace the `BuildMiddleware` helper with this updated version that accepts an optional logger:

     ```csharp
     private static WideEventMiddleware BuildMiddleware(
         RequestDelegate? next = null,
         IEnumerable<IHttpWideEventEnricher>? enrichers = null,
         IWideEventExporter? exporter = null,
         ILogger<WideEventMiddleware>? logger = null)
     {
         var mockExporter = new Mock<IWideEventExporter>();
         mockExporter
             .Setup(e => e.ExportAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

         return new WideEventMiddleware(
             next ?? (_ => Task.CompletedTask),
             enrichers ?? [],
             exporter ?? mockExporter.Object,
             logger ?? NullLogger<WideEventMiddleware>.Instance);
     }
     ```

  3. Append the following region inside `public sealed class WideEventMiddlewareTests`:

     ```csharp
     // ── Logger scope ───────────────────────────────────────────────────────────

     [Fact]
     public async Task Invoke_OpensScope_WithWideEventContext()
     {
         var (_, ctx) = MockContext();
         var logger = new Mock<ILogger<WideEventMiddleware>>();
         logger
             .Setup(l => l.BeginScope(It.IsAny<IWideEventContext>()))
             .Returns(Mock.Of<IDisposable>());

         var middleware = BuildMiddleware(logger: logger.Object);
         await middleware.Invoke(new DefaultHttpContext(), ctx);

         logger.Verify(l => l.BeginScope(ctx), Times.Once);
     }

     [Fact]
     public async Task Invoke_DisposesScope_WhenRequestCompletes()
     {
         var (_, ctx) = MockContext();
         var disposable = new Mock<IDisposable>();
         var logger = new Mock<ILogger<WideEventMiddleware>>();
         logger
             .Setup(l => l.BeginScope(It.IsAny<IWideEventContext>()))
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
             .Setup(l => l.BeginScope(It.IsAny<IWideEventContext>()))
             .Returns(disposable.Object);

         var middleware = BuildMiddleware(
             next: _ => throw new InvalidOperationException("boom"),
             logger: logger.Object);

         await Assert.ThrowsAsync<InvalidOperationException>(
             () => middleware.Invoke(new DefaultHttpContext(), ctx));

         disposable.Verify(d => d.Dispose(), Times.Once);
     }
     ```

- [ ] **Step 2.2: Add logger field and constructor param to `WideEventMiddleware`** (do NOT call `BeginScope` yet)

  Replace the contents of `src/WideEvents.AspNetCore/WideEventMiddleware.cs` with:

  ```csharp
  using System.Diagnostics;
  using Microsoft.AspNetCore.Http;
  using Microsoft.Extensions.Logging;
  using WideEvents.Abstractions;
  using WideEvents.Core.Context;

  namespace WideEvents.AspNetCore;

  public sealed class WideEventMiddleware
  {
      private readonly RequestDelegate _next;
      private readonly IEnumerable<IHttpWideEventEnricher> _enrichers;
      private readonly IWideEventExporter _exporter;
      private readonly ILogger<WideEventMiddleware> _logger;

      public WideEventMiddleware(
          RequestDelegate next,
          IEnumerable<IHttpWideEventEnricher> enrichers,
          IWideEventExporter exporter,
          ILogger<WideEventMiddleware> logger)
      {
          _next = next;
          _enrichers = enrichers;
          _exporter = exporter;
          _logger = logger;
      }

      public async Task Invoke(HttpContext context, IWideEventContext wideEvent)
      {
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
              await _exporter.ExportAsync(wideEvent.Build(), context.RequestAborted);
              WideEvent.Reset();
          }
      }
  }
  ```

- [ ] **Step 2.3: Run tests and verify new scope tests fail**

  ```
  dotnet test tests/WideEvents.AspNetCore.Tests/WideEvents.AspNetCore.Tests.csproj
  ```

  Expected: all pre-existing tests pass; the 3 new scope tests fail (`BeginScope` is never called).

- [ ] **Step 2.4: Add `BeginScope` call to `Invoke`**

  In `src/WideEvents.AspNetCore/WideEventMiddleware.cs`, replace the `Invoke` method with:

  ```csharp
  public async Task Invoke(HttpContext context, IWideEventContext wideEvent)
  {
      using var scope = _logger.BeginScope(wideEvent);
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
          await _exporter.ExportAsync(wideEvent.Build(), context.RequestAborted);
          WideEvent.Reset();
      }
  }
  ```

  The `using var scope` is placed before the `try` so it covers the entire try-catch-finally block and is disposed only after `ExportAsync` and `Reset()` complete.

- [ ] **Step 2.5: Run the full solution and verify all tests pass**

  ```
  dotnet test WideEvents.slnx
  ```

  Expected: all tests pass across all test projects.

- [ ] **Step 2.6: Commit**

  ```
  git add src/WideEvents.AspNetCore/WideEventMiddleware.cs tests/WideEvents.AspNetCore.Tests/WideEventMiddlewareTests.cs
  git commit -m "feat(aspnetcore): push WideEventContext as ILogger scope for OTel/Serilog interop"
  ```
