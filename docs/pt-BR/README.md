# Documentação do WideEvents

[English](../en-US/README.md) | **Português (Brasil)**

WideEvents é uma biblioteca de logging estruturado para .NET construída em torno
de **wide events** (também conhecidos como *canonical log lines*). Em vez de
espalhar várias linhas de log ao longo de uma requisição, você acumula contexto
em um único evento estruturado e rico, e o emite uma única vez.

> **Status:** estágio inicial. Esta documentação descreve o que está
> **atualmente implementado**. O [README](../../README.md) raiz descreve a visão
> mais ampla e o roadmap (exporters, sampling, mascaramento de PII, schemas
> gerados por source generator), grande parte ainda não construída.

## Projetos

| Projeto | Descrição |
| --- | --- |
| `WideEvents.Abstractions` | Contratos: `IWideEventContext` e `IWideEventExporter`. |
| `WideEvents.Core` | O contexto do wide event e o acumulador estático `WideEvent`. |
| `WideEvents.AspNetCore` | Middleware que emite um wide event por requisição HTTP. |

## Requisitos

- .NET 10 SDK — todos os projetos têm como alvo `net10.0`.

## Instalação

Os pacotes **ainda não estão publicados no NuGet**. Referencie os projetos
diretamente. Referenciar `WideEvents.AspNetCore` traz, de forma transitiva,
`WideEvents.Core` e `WideEvents.Abstractions`:

```xml
<ItemGroup>
  <ProjectReference Include="../WideEvents/src/WideEvents.AspNetCore/WideEvents.AspNetCore.csproj" />
</ItemGroup>
```

Como alternativa, `dotnet pack` gera arquivos locais `WideEvents.*.1.0.0.nupkg`
que você pode consumir a partir de um feed local.

## Início rápido (ASP.NET Core)

```csharp
using WideEvents.AspNetCore;
using WideEvents.Core.Context;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Emite um wide event por requisição.
app.UseWideEvents();

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

> O middleware registra com `ILogger` usando `{@WideEvent}`. Para renderizar o
> evento como JSON aninhado você precisa de um logger estruturado que suporte
> *destructuring* (por exemplo, Serilog). O logger de console padrão apenas chama
> `ToString()` no dicionário. Veja [Integração com ASP.NET Core](aspnetcore.md).

## Tópicos

- [Conceitos principais](core-concepts.md) — a API `WideEvent`, chaves aninhadas,
  correlação de trace e as abstrações.
- [Integração com ASP.NET Core](aspnetcore.md) — o middleware e o que ele captura.

## Sample

Um exemplo executável está em [`sample/WideEvents.Sample.Api`](../../sample/WideEvents.Sample.Api).
Rode-o e acesse os endpoints:

```bash
dotnet run --project sample/WideEvents.Sample.Api
# depois:
curl http://localhost:5080/checkout/user_456
curl http://localhost:5080/boom   # caminho de erro
```
