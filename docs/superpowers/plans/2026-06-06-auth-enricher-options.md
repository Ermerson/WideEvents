# AuthEnricher via WideEventsOptions — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mover `AuthEnricher` para `WideEvents.AspNetCore` e expô-lo como opção configurável em `AddWideEvents`, eliminando o registro manual no sample.

**Architecture:** Três novos tipos são criados em `WideEvents.AspNetCore` (`AuthEnricherOptions`, `WideEventsOptions`, `AuthEnricher`). `AddWideEvents` passa a aceitar `Action<WideEventsOptions>?` e registra `AuthEnricher` como Singleton apenas quando `UseAuthEnricher()` é chamado nas opções. O sample é atualizado para usar a nova API.

**Tech Stack:** .NET 10, xUnit, FluentAssertions, Moq, `Microsoft.AspNetCore.Http`

---

## File Map

| Ação | Arquivo |
|------|---------|
| Criar | `src/WideEvents.AspNetCore/AuthEnricherOptions.cs` |
| Criar | `src/WideEvents.AspNetCore/WideEventsOptions.cs` |
| Criar | `src/WideEvents.AspNetCore/AuthEnricher.cs` |
| Modificar | `src/WideEvents.AspNetCore/WideEventExtensions.cs` |
| Criar | `tests/WideEvents.AspNetCore.Tests/AuthEnricherTests.cs` |
| Criar | `tests/WideEvents.AspNetCore.Tests/WideEventsOptionsTests.cs` |
| Criar | `tests/WideEvents.AspNetCore.Tests/WideEventExtensionsTests.cs` |
| Modificar | `sample/WideEvents.Sample.Api/Program.cs` |
| Deletar | `sample/WideEvents.Sample.Api/Enricher/AuthEnricher.cs` |

---

## Task 1: AuthEnricherOptions e WideEventsOptions

**Files:**
- Create: `src/WideEvents.AspNetCore/AuthEnricherOptions.cs`
- Create: `src/WideEvents.AspNetCore/WideEventsOptions.cs`
- Create: `tests/WideEvents.AspNetCore.Tests/WideEventsOptionsTests.cs`

- [ ] **Step 1: Escrever os testes que vão falhar**

Crie `tests/WideEvents.AspNetCore.Tests/WideEventsOptionsTests.cs`:

```csharp
using System.Security.Claims;
using FluentAssertions;
using Xunit;

namespace WideEvents.AspNetCore.Tests;

public sealed class WideEventsOptionsTests
{
    [Fact]
    public void WithoutUseAuthEnricher_AuthEnricherIsNull()
    {
        var options = new WideEventsOptions();

        options.AuthEnricher.Should().BeNull();
    }

    [Fact]
    public void UseAuthEnricher_WithNoArgs_SetsDefaults()
    {
        var options = new WideEventsOptions();

        options.UseAuthEnricher();

        options.AuthEnricher.Should().NotBeNull();
        options.AuthEnricher!.ClaimType.Should().Be(ClaimTypes.NameIdentifier);
        options.AuthEnricher!.FieldName.Should().Be("user.id");
    }

    [Fact]
    public void UseAuthEnricher_WithCustomConfig_OverridesDefaults()
    {
        var options = new WideEventsOptions();

        options.UseAuthEnricher(o =>
        {
            o.ClaimType = "sub";
            o.FieldName = "auth.subject";
        });

        options.AuthEnricher!.ClaimType.Should().Be("sub");
        options.AuthEnricher!.FieldName.Should().Be("auth.subject");
    }

    [Fact]
    public void UseAuthEnricher_ReturnsItself_ForChaining()
    {
        var options = new WideEventsOptions();

        var result = options.UseAuthEnricher();

        result.Should().BeSameAs(options);
    }
}
```

- [ ] **Step 2: Rodar os testes para confirmar que falham**

```
dotnet test WideEvents.slnx --filter "FullyQualifiedName~WideEventsOptionsTests" -v m
```

Esperado: erro de compilação — `WideEventsOptions` e `AuthEnricherOptions` não existem.

- [ ] **Step 3: Criar AuthEnricherOptions**

Crie `src/WideEvents.AspNetCore/AuthEnricherOptions.cs`:

```csharp
using System.Security.Claims;

namespace WideEvents.AspNetCore;

public sealed class AuthEnricherOptions
{
    public string ClaimType { get; set; } = ClaimTypes.NameIdentifier;
    public string FieldName { get; set; } = "user.id";
}
```

- [ ] **Step 4: Criar WideEventsOptions**

Crie `src/WideEvents.AspNetCore/WideEventsOptions.cs`:

```csharp
namespace WideEvents.AspNetCore;

public sealed class WideEventsOptions
{
    internal AuthEnricherOptions? AuthEnricher { get; private set; }

    public WideEventsOptions UseAuthEnricher(Action<AuthEnricherOptions>? configure = null)
    {
        AuthEnricher = new AuthEnricherOptions();
        configure?.Invoke(AuthEnricher);
        return this;
    }
}
```

- [ ] **Step 5: Rodar os testes para confirmar que passam**

```
dotnet test WideEvents.slnx --filter "FullyQualifiedName~WideEventsOptionsTests" -v m
```

Esperado: 4 testes passando.

- [ ] **Step 6: Commit**

```
git add src/WideEvents.AspNetCore/AuthEnricherOptions.cs
git add src/WideEvents.AspNetCore/WideEventsOptions.cs
git add tests/WideEvents.AspNetCore.Tests/WideEventsOptionsTests.cs
git commit -m "feat(aspnetcore): add AuthEnricherOptions and WideEventsOptions"
```

---

## Task 2: AuthEnricher na biblioteca

**Files:**
- Create: `src/WideEvents.AspNetCore/AuthEnricher.cs`
- Create: `tests/WideEvents.AspNetCore.Tests/AuthEnricherTests.cs`

- [ ] **Step 1: Escrever os testes que vão falhar**

Crie `tests/WideEvents.AspNetCore.Tests/AuthEnricherTests.cs`:

```csharp
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using WideEvents.Core.Context;
using Xunit;

namespace WideEvents.AspNetCore.Tests;

public sealed class AuthEnricherTests
{
    [Fact]
    public void EnrichRequest_AddsFieldWithClaimValue()
    {
        var enricher = new AuthEnricher(new AuthEnricherOptions());
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user_123")]));
        var wideEvent = new WideEventContext(enrichers: []);

        enricher.EnrichRequest(ctx, wideEvent);

        var built = wideEvent.Build();
        var user = built["user"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        user["id"].Should().Be("user_123");
    }

    [Fact]
    public void EnrichRequest_WhenClaimMissing_IgnoresNullValue()
    {
        // WideEventContext.Add ignora null — chave não deve aparecer no evento
        var enricher = new AuthEnricher(new AuthEnricherOptions());
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity());
        var wideEvent = new WideEventContext(enrichers: []);

        enricher.EnrichRequest(ctx, wideEvent);

        wideEvent.Build().Should().NotContainKey("user");
    }

    [Fact]
    public void EnrichRequest_UsesCustomClaimTypeAndFieldName()
    {
        var options = new AuthEnricherOptions { ClaimType = "sub", FieldName = "auth.subject" };
        var enricher = new AuthEnricher(options);
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("sub", "subject_abc")]));
        var wideEvent = new WideEventContext(enrichers: []);

        enricher.EnrichRequest(ctx, wideEvent);

        var built = wideEvent.Build();
        var auth = built["auth"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        auth["subject"].Should().Be("subject_abc");
    }

    [Fact]
    public void EnrichResponse_DoesNotAddAnyField()
    {
        var enricher = new AuthEnricher(new AuthEnricherOptions());
        var wideEvent = new WideEventContext(enrichers: []);

        enricher.EnrichResponse(new DefaultHttpContext(), wideEvent);

        wideEvent.Build().Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Rodar os testes para confirmar que falham**

```
dotnet test WideEvents.slnx --filter "FullyQualifiedName~AuthEnricherTests" -v m
```

Esperado: erro de compilação — `AuthEnricher` não existe na biblioteca.

- [ ] **Step 3: Criar AuthEnricher na biblioteca**

Crie `src/WideEvents.AspNetCore/AuthEnricher.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using WideEvents.Abstractions;

namespace WideEvents.AspNetCore;

public sealed class AuthEnricher(AuthEnricherOptions options) : IHttpWideEventEnricher
{
    public void EnrichRequest(HttpContext context, IWideEventContext wideEvent)
    {
        var value = context.User?.FindFirst(options.ClaimType)?.Value;
        wideEvent.Add(options.FieldName, value);
    }

    public void EnrichResponse(HttpContext context, IWideEventContext wideEvent) { }
}
```

- [ ] **Step 4: Rodar os testes para confirmar que passam**

```
dotnet test WideEvents.slnx --filter "FullyQualifiedName~AuthEnricherTests" -v m
```

Esperado: 4 testes passando.

- [ ] **Step 5: Commit**

```
git add src/WideEvents.AspNetCore/AuthEnricher.cs
git add tests/WideEvents.AspNetCore.Tests/AuthEnricherTests.cs
git commit -m "feat(aspnetcore): add AuthEnricher as built-in enricher"
```

---

## Task 3: Modificar AddWideEvents para aceitar opções

**Files:**
- Modify: `src/WideEvents.AspNetCore/WideEventExtensions.cs`
- Create: `tests/WideEvents.AspNetCore.Tests/WideEventExtensionsTests.cs`

- [ ] **Step 1: Escrever os testes que vão falhar**

Crie `tests/WideEvents.AspNetCore.Tests/WideEventExtensionsTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WideEvents.AspNetCore.Tests;

public sealed class WideEventExtensionsTests
{
    [Fact]
    public void AddWideEvents_WithoutOptions_DoesNotRegisterAuthEnricher()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddWideEvents();

        var provider = services.BuildServiceProvider();
        var enrichers = provider.GetServices<IHttpWideEventEnricher>();
        enrichers.Should().NotContain(e => e is AuthEnricher);
    }

    [Fact]
    public void AddWideEvents_WithUseAuthEnricher_RegistersAuthEnricher()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddWideEvents(o => o.UseAuthEnricher());

        var provider = services.BuildServiceProvider();
        var enrichers = provider.GetServices<IHttpWideEventEnricher>();
        enrichers.Should().Contain(e => e is AuthEnricher);
    }

    [Fact]
    public void AddWideEvents_AlwaysRegistersDefaultHttpEnricher()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddWideEvents();

        var provider = services.BuildServiceProvider();
        var enrichers = provider.GetServices<IHttpWideEventEnricher>();
        enrichers.Should().Contain(e => e is DefaultHttpEnricher);
    }
}
```

- [ ] **Step 2: Rodar os testes para confirmar que falham**

```
dotnet test WideEvents.slnx --filter "FullyQualifiedName~WideEventExtensionsTests" -v m
```

Esperado: `AddWideEvents_WithUseAuthEnricher_RegistersAuthEnricher` falha — `AuthEnricher` não é registrado.
Os outros dois devem compilar e talvez passar ou falhar; o importante é confirmar o estado inicial.

- [ ] **Step 3: Modificar WideEventExtensions.cs**

Substitua o conteúdo de `src/WideEvents.AspNetCore/WideEventExtensions.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WideEvents.Abstractions;
using WideEvents.Core.Context;

namespace WideEvents.AspNetCore;

public static class WideEventExtensions
{
    public static IServiceCollection AddWideEvents(
        this IServiceCollection services,
        Action<WideEventsOptions>? configure = null)
    {
        var options = new WideEventsOptions();
        configure?.Invoke(options);

        services.AddScoped<IWideEventContext>(_ => WideEvent.Current);
        services.AddSingleton<IHttpWideEventEnricher, DefaultHttpEnricher>();
        services.AddSingleton<IWideEventExporter, LoggerWideEventExporter>();

        if (options.AuthEnricher is { } authOpts)
            services.AddSingleton<IHttpWideEventEnricher>(_ => new AuthEnricher(authOpts));

        return services;
    }

    public static IApplicationBuilder UseWideEvents(this IApplicationBuilder app)
        => app.UseMiddleware<WideEventMiddleware>();
}
```

- [ ] **Step 4: Rodar todos os testes**

```
dotnet test WideEvents.slnx -v m
```

Esperado: todos os testes passando (incluindo os existentes de `WideEventMiddlewareTests`, `DefaultHttpEnricherTests`, `LoggerWideEventExporterTests`).

- [ ] **Step 5: Commit**

```
git add src/WideEvents.AspNetCore/WideEventExtensions.cs
git add tests/WideEvents.AspNetCore.Tests/WideEventExtensionsTests.cs
git commit -m "feat(aspnetcore): AddWideEvents accepts WideEventsOptions for built-in enrichers"
```

---

## Task 4: Atualizar o sample

**Files:**
- Modify: `sample/WideEvents.Sample.Api/Program.cs`
- Delete: `sample/WideEvents.Sample.Api/Enricher/AuthEnricher.cs`

- [ ] **Step 1: Remover o AuthEnricher do sample**

Delete o arquivo `sample/WideEvents.Sample.Api/Enricher/AuthEnricher.cs`.

- [ ] **Step 2: Atualizar Program.cs**

Substitua as linhas de registro em `sample/WideEvents.Sample.Api/Program.cs`:

```csharp
// Remover estas duas linhas:
builder.Services.AddSingleton<IHttpWideEventEnricher, AuthEnricher>();
builder.Services.AddWideEvents();

// Substituir por:
builder.Services.AddWideEvents(options => options.UseAuthEnricher());
```

Remova também o `using WideEvents.Sample.Api.Enricher;` do topo do arquivo, pois a classe não existe mais.

O `Program.cs` final deve ficar assim:

```csharp
using System.Diagnostics;
using Serilog;
using Serilog.Events;
using WideEvents.AspNetCore;
using WideEvents.Core.Context;
using WideEvents.Sample.Api;

// Populate Activity.Current so the wide event picks up trace_id/span_id
// without wiring a full OpenTelemetry pipeline in this sample.
ActivitySource.AddActivityListener(new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = static (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
});

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, logging) =>
    logging
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .WriteTo.Console(new PrettyJsonFormatter()));

builder.Services.AddWideEvents(options => options.UseAuthEnricher());

var app = builder.Build();

app.UseWideEvents();

app.MapGet("/", () => "WideEvents sample - try GET /checkout/user_456 or GET /boom");

app.MapGet("/checkout/{userId}", (string userId) =>
{
    WideEvent.Add("user.id", userId);
    WideEvent.Add("user.subscription", "premium");
    WideEvent.Add("cart.id", "cart_xyz");
    WideEvent.Add("cart.total_cents", 15999);
    WideEvent.Add("payment.method", "card");
    WideEvent.Add("payment.provider", "stripe");
    WideEvent.Add("outcome", "ok");

    return Results.Ok(new { status = "checked_out", user = userId });
});

app.MapGet("/boom", () =>
{
    WideEvent.Add("user.id", "user_456");
    WideEvent.Add("payment.provider", "stripe");

    throw new InvalidOperationException("payment provider timeout");
});

app.Run("http://localhost:5080");
```

- [ ] **Step 3: Build para confirmar que compila**

```
dotnet build WideEvents.slnx
```

Esperado: sem erros, apenas avisos de `GeneratePackageOnBuild` (normais).

- [ ] **Step 4: Rodar todos os testes**

```
dotnet test WideEvents.slnx -v m
```

Esperado: todos os testes passando.

- [ ] **Step 5: Commit**

```
git add sample/WideEvents.Sample.Api/Program.cs
git rm sample/WideEvents.Sample.Api/Enricher/AuthEnricher.cs
git commit -m "feat(sample): migrate to AddWideEvents(options => options.UseAuthEnricher())"
```
