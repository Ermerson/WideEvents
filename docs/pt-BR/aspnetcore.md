# Integração com ASP.NET Core

[← Voltar ao índice](README.md) · [English](../en-US/aspnetcore.md)

`WideEvents.AspNetCore` fornece um middleware que emite exatamente **um wide event
por requisição HTTP**, capturando automaticamente metadados de requisição/resposta
e correlacionando-os com o trace ativo.

## Configuração

Chame `AddWideEvents()` no registro de serviços e `UseWideEvents()` no pipeline
de requisições:

```csharp
using WideEvents.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWideEvents(); // obrigatório — registra dependências do middleware

var app = builder.Build();

app.UseWideEvents(); // cedo no pipeline para envolver todos os handlers
```

`AddWideEvents()` registra:

- `IWideEventExporter` → `LoggerWideEventExporter` (emissor padrão)
- `IWideEventBuilder` → `WideEventBuilder` (pipeline de merge de 3 fontes)
- `WideEventLoggerProvider` como `ILoggerProvider` (integração de scope)
- `DefaultHttpEnricher` como `IHttpWideEventEnricher`

## O que ele captura automaticamente

| Atributo | Origem | Quando |
| --- | --- | --- |
| `http.method` | `DefaultHttpEnricher` | Sempre. |
| `http.path` | `DefaultHttpEnricher` | Sempre. |
| `http.status_code` | `DefaultHttpEnricher` | Em caso de sucesso (após o pipeline concluir). |
| `error.type` | Middleware | Quando o pipeline lança exceção — o nome do tipo da exceção. |
| `error.message` | Middleware | Quando o pipeline lança exceção — a mensagem da exceção. |
| `duration_ms` | Middleware | Sempre (medido com `Stopwatch`). |
| `trace_id`, `span_id`, `trace_flags` | `WideEventBuilder` | Quando há um `Activity` ativo. |

> Em uma exceção não tratada, o middleware registra `error.*`, emite o evento e
> **relança** — portanto `http.status_code` não está presente no caminho de erro.

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
O contexto é limpo quando o middleware termina (após `Build()` ser chamado), então
os contextos não vazam entre requisições.

## Enrichers

`IHttpWideEventEnricher` é o ponto de extensão para adicionar campos derivados do
HTTP sem modificar o middleware. Implemente a interface e registre-a no DI:

```csharp
public class TenantEnricher : IHttpWideEventEnricher
{
    public void EnrichRequest(HttpContext context, IWideEventContext wideEvent)
    {
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        wideEvent.Add("tenant.id", tenantId);
    }

    public void EnrichResponse(HttpContext context, IWideEventContext wideEvent) { }
}

// em Program.cs:
builder.Services.AddSingleton<IHttpWideEventEnricher, TenantEnricher>();
```

### Contrato de ciclo de vida dos enrichers

> **Atenção:** `EnrichResponse` é chamado apenas no **caminho de sucesso** — quando
> o pipeline conclui sem lançar exceção. Em caso de exceção não tratada,
> `EnrichResponse` **não é invocado**; o middleware captura `error.type` e
> `error.message` diretamente e relança.
>
> Se seu enricher precisa adicionar dados independentemente do desfecho da
> requisição, faça isso em `EnrichRequest` ou leia o estado necessário de forma
> defensiva (por exemplo, evite acessar `context.Response.StatusCode` em
> `EnrichResponse` assumindo que sempre estará disponível).

### Enrichers embutidos

**`DefaultHttpEnricher`** é registrado automaticamente por `AddWideEvents()`. Ele
captura `http.method`, `http.path` e `http.status_code`.

**`AuthEnricher`** é opcional. Habilite-o via `WideEventsOptions`:

```csharp
builder.Services.AddWideEvents(options =>
    options.UseAuthEnricher()); // lê ClaimTypes.NameIdentifier → "user.id"

// claim e field name personalizados:
builder.Services.AddWideEvents(options =>
    options.UseAuthEnricher(o =>
    {
        o.ClaimType = "sub";
        o.FieldName = "auth.subject";
    }));
```

`AuthEnricher` é um no-op quando a requisição não está autenticada ou a claim
está ausente.

## Como o evento é exportado

Após o pipeline concluir (no bloco `finally` do middleware), o middleware chama
`IWideEventBuilder.Build()` para produzir o dicionário de evento mesclado, e
então `IWideEventExporter.ExportAsync()` para emiti-lo.

O exporter padrão — `LoggerWideEventExporter` — escreve via `ILogger`:

```csharp
_logger.LogInformation("WideEvent: {@WideEvent}", wideEvent);
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

### Exporter personalizado

Registre sua própria implementação de `IWideEventExporter` após `AddWideEvents()`
para substituir o padrão:

```csharp
builder.Services.AddWideEvents();
builder.Services.AddSingleton<IWideEventExporter, MyExporter>();
```
