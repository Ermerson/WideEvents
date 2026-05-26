# Integração com ASP.NET Core

[← Voltar ao índice](README.md) · [English](../en-US/aspnetcore.md)

`WideEvents.AspNetCore` fornece um middleware que emite exatamente **um wide event
por requisição HTTP**, capturando automaticamente metadados de requisição/resposta
e correlacionando-os com o trace ativo.

## Habilitando o middleware

```csharp
using WideEvents.AspNetCore;

var app = builder.Build();

app.UseWideEvents();
```

Registre-o cedo no pipeline, para que ele envolva o restante do processamento da
requisição. `UseWideEvents()` é uma extensão fina sobre
`UseMiddleware<WideEventMiddleware>()`.

## O que ele captura automaticamente

| Atributo | Quando |
| --- | --- |
| `http.method` | Sempre. |
| `http.path` | Sempre. |
| `http.status_code` | Em caso de sucesso (após o pipeline concluir). |
| `error.type` | Quando o pipeline lança exceção — o nome do tipo da exceção. |
| `error.message` | Quando o pipeline lança exceção — a mensagem da exceção. |
| `duration_ms` | Sempre (medido com `Stopwatch`). |
| `trace_id`, `span_id`, `trace_flags` | Quando há um `Activity` ativo (adicionado por `Build()`). |

> Em uma exceção não tratada, o middleware registra `error.*`, emite o evento e
> **relança** — portanto `http.status_code` não está presente no caminho de erro
> (o status da resposta ainda não havia sido escrito).

## Adicionando seu próprio contexto

Em qualquer ponto downstream do middleware (controllers, handlers de minimal API,
serviços) você enriquece o mesmo evento via API estática:

```csharp
app.MapGet("/checkout/{userId}", (string userId) =>
{
    WideEvent.Add("user.id", userId);
    WideEvent.Add("user.subscription", "premium");
    WideEvent.Add("cart.total_cents", 15999);
    return Results.Ok();
});
```

Como o contexto é `AsyncLocal`, essas chamadas caem no evento da requisição atual.
O middleware chama `WideEvent.Reset()` após emitir, então os contextos não vazam
entre requisições.

## Como o evento é emitido (e como renderizá-lo)

O middleware escreve o evento via `ILogger`:

```csharp
_logger.LogInformation("WideEvent: {@WideEvent}", WideEvent.Current.Build());
```

O `@` em `{@WideEvent}` é uma dica de **destructuring**. Para ver o evento como
JSON aninhado você precisa de um logger estruturado que a respeite:

- **Serilog** (recomendado) renderiza o dicionário aninhado como JSON estruturado.
- O provider de console **padrão** do `Microsoft.Extensions.Logging` *não* faz
  destructuring — ele registra `dictionary.ToString()`, o que não é útil. Até
  mesmo `AddJsonConsole` serializa o valor via `ToString()`.

### Exemplo com Serilog

```csharp
using Serilog;
using Serilog.Formatting.Compact;

builder.Host.UseSerilog((_, logging) =>
    logging.WriteTo.Console(new CompactJsonFormatter()));
```

Essa é exatamente a configuração usada pelo
[sample](../../sample/WideEvents.Sample.Api). Rode-o:

```bash
dotnet run --project sample/WideEvents.Sample.Api
curl http://localhost:5080/checkout/user_456   # caminho de sucesso
curl http://localhost:5080/boom                # caminho de erro
```

O caminho de sucesso registra um único evento contendo `http`, `user`, `payment`,
`duration_ms` e os campos de trace; o caminho de erro registra o mesmo formato com
um objeto `error` e uma resposta HTTP 500.
