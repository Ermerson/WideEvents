# Documentação do WideEvents

[English](../en-US/README.md) | **Português (Brasil)**

WideEvents é uma biblioteca de logging estruturado para .NET construída em torno
de **wide events** (também conhecidos como *canonical log lines*). Em vez de
espalhar várias linhas de log ao longo de uma requisição, você acumula contexto
em um único evento estruturado e rico, e o emite uma única vez.

> **Projeto de aprendizado.** Esta biblioteca é desenvolvida principalmente para
> aprendizado e experimentação. Veja o [README raiz](../../README.md) para o
> aviso completo e uma indicação de alternativa para produção.

> **Status:** estágio inicial. Esta documentação descreve o que está
> **atualmente implementado**.

## Projetos

| Projeto | Descrição |
| --- | --- |
| `WideEvents.Abstractions` | Contratos: `IWideEventContext` e `IWideEventExporter`. |
| `WideEvents.Core` | Contexto do wide event, acumulador estático `WideEvent` e `WideEventBuilder`. |
| `WideEvents.AspNetCore` | Middleware, enrichers e o `LoggerWideEventExporter` padrão. |

## Requisitos

- .NET 8 SDK ou superior — os pacotes têm como alvo `net8.0` e `net10.0`.

## Instalação

```bash
# Apps ASP.NET Core (traz Core e Abstractions de forma transitiva)
dotnet add package WideEvents.AspNetCore

# Apps sem web ou console
dotnet add package WideEvents.Core
```

## Início rápido (ASP.NET Core)

```csharp
using WideEvents.AspNetCore;
using WideEvents.Core.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWideEvents(); // registra os serviços

var app = builder.Build();

app.UseWideEvents(); // emite um wide event por requisição

app.MapGet("/checkout/{userId}", (string userId) =>
{
    WideEvent.Add("user.id", userId);
    WideEvent.Add("payment.method", "card");
    WideEvent.Add("payment.provider", "stripe");
    return Results.Ok();
});

app.Run();
```

Uma requisição para `/checkout/user_456` produz um único evento:

```json
{
  "http": { "method": "GET", "path": "/checkout/user_456", "status_code": 200 },
  "user": { "id": "user_456" },
  "payment": { "method": "card", "provider": "stripe" },
  "duration_ms": 29.52,
  "trace_id": "23ca1dc84f7b4cc4b44b7717ca231c2b",
  "span_id": "2454b21b523f02f4",
  "trace_flags": "None"
}
```

> O exporter padrão emite via `ILogger` usando `{@WideEvent}`. Para renderizar o
> evento como JSON aninhado você precisa de um logger estruturado que suporte
> *destructuring* (por exemplo, Serilog). O logger de console padrão apenas chama
> `ToString()` no dicionário. Veja [Integração com ASP.NET Core](aspnetcore.md).

## Tópicos

- [Conceitos principais](core-concepts.md) — a API `WideEvent`, chaves aninhadas,
  correlação de trace, o pipeline de merge e as abstrações.
- [Integração com ASP.NET Core](aspnetcore.md) — o middleware, enrichers e como
  o evento é exportado.

## Sample

Um exemplo executável está em [`sample/WideEvents.Sample.Api`](../../sample/WideEvents.Sample.Api).
Rode-o e acesse os endpoints:

```bash
dotnet run --project sample/WideEvents.Sample.Api
# depois:
curl http://localhost:5080/checkout/user_456
curl http://localhost:5080/boom   # caminho de erro
```
