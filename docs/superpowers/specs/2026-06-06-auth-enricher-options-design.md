# Design: AuthEnricher via WideEventsOptions

**Date:** 2026-06-06  
**Scope:** `WideEvents.AspNetCore`

## Problem

`AuthEnricher` vive no sample e é registrado manualmente pelo consumidor antes de `AddWideEvents()`. Não existe forma de ativá-lo como um comportamento opcional da biblioteca sem copiar a classe.

## Goal

Mover `AuthEnricher` para `WideEvents.AspNetCore` e expô-lo como opção configurável em `AddWideEvents`, eliminando o registro manual.

## API final

```csharp
// mínimo — defaults: ClaimTypes.NameIdentifier → "user.id"
builder.Services.AddWideEvents(options =>
    options.UseAuthEnricher());

// configurado
builder.Services.AddWideEvents(options =>
    options.UseAuthEnricher(o =>
    {
        o.ClaimType = "sub";
        o.FieldName = "auth.user_id";
    }));

// sem AuthEnricher (comportamento atual — sem breaking change)
builder.Services.AddWideEvents();
```

## Architecture

```
AddWideEvents(Action<WideEventsOptions>? configure)
     │
     ▼
WideEventsOptions                    ← coleta intenções de registro
  └─ AuthEnricher: AuthEnricherOptions?   (null = não registrado)

AddWideEvents registra:
  • sempre: DefaultHttpEnricher, LoggerWideEventExporter, IWideEventContext
  • se AuthEnricher != null → AuthEnricher como Singleton com options injetadas
```

## New types

### `AuthEnricherOptions`

```csharp
public sealed class AuthEnricherOptions
{
    public string ClaimType { get; set; } = ClaimTypes.NameIdentifier;
    public string FieldName { get; set; } = "user.id";
}
```

### `WideEventsOptions`

```csharp
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

### `AuthEnricher` (movido do sample)

```csharp
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

## Modified types

### `WideEventExtensions.AddWideEvents`

```csharp
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
```

## Files changed

| Ação | Arquivo |
|------|---------|
| Criar | `src/WideEvents.AspNetCore/AuthEnricherOptions.cs` |
| Criar | `src/WideEvents.AspNetCore/WideEventsOptions.cs` |
| Criar | `src/WideEvents.AspNetCore/AuthEnricher.cs` |
| Modificar | `src/WideEvents.AspNetCore/WideEventExtensions.cs` |
| Modificar | `sample/WideEvents.Sample.Api/Program.cs` |
| Deletar | `sample/WideEvents.Sample.Api/Enricher/AuthEnricher.cs` |

## Non-goals

- Suporte a múltiplos `AuthEnricher` simultâneos.
- Configuração via `appsettings.json` / `IOptions<T>`.
- Outros enrichers built-in neste ciclo.

## Breaking changes

Nenhum. `AddWideEvents()` sem argumento continua funcionando identicamente.
