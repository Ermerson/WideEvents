# Conceitos principais

[← Voltar ao índice](README.md) · [English](../en-US/core-concepts.md)

## Wide events

Um **wide event** é um único registro estruturado que descreve uma unidade de
trabalho (tipicamente uma requisição) com muitos atributos: operacionais
(`duration_ms`, `http.status_code`), técnicos (`trace_id`) e de negócio
(`user.id`, `cart.total_cents`). Um evento, contexto completo — em vez de
correlacionar dezenas de linhas de log isoladas depois do fato.

## A API estática `WideEvent`

`WideEvents.Core.Context.WideEvent` é o ponto de entrada. Ele mantém o contexto
atual em um `AsyncLocal`, de modo que cada fluxo assíncrono (por exemplo, cada
requisição HTTP) tem seu próprio contexto isolado.

```csharp
using WideEvents.Core.Context;

WideEvent.Add("user.id", "user_456");      // acumula atributos
WideEvent.Add("cart.total_cents", 15999);

IWideEventContext ctx = WideEvent.Current; // o contexto deste fluxo
IReadOnlyDictionary<string, object?> evt = ctx.Build();

WideEvent.Reset();                          // limpa o contexto deste fluxo
```

| Membro | Descrição |
| --- | --- |
| `WideEvent.Add(string key, object? value)` | Adiciona/sobrescreve um atributo no contexto atual. |
| `WideEvent.Current` | O `IWideEventContext` atual (criado sob demanda, nunca nulo). |
| `WideEvent.Reset()` | Descarta o contexto atual; o próximo acesso começa do zero. |

## `WideEventContext` e `IWideEventContext`

`WideEventContext` (em `WideEvents.Core`) implementa `IWideEventContext` (em
`WideEvents.Abstractions`):

```csharp
public interface IWideEventContext
{
    void Add(string name, object? value);
    IReadOnlyDictionary<string, object?> Build();
}
```

### Semântica de `Add`

- **Sobrescrita:** adicionar a mesma chave duas vezes mantém o último valor (não
  lança exceção).
- **Valores nulos são ignorados:** `Add("user.id", null)` é um no-op, então dados
  ausentes nunca aparecem como uma chave vazia.
- **Validação de chave:** uma chave `null`, vazia ou só com espaços lança
  `ArgumentException`.

### Chaves aninhadas

Chaves com ponto são expandidas em objetos aninhados quando o evento é construído:

```csharp
WideEvent.Add("payment.method", "card");
WideEvent.Add("payment.provider", "stripe");
```

```json
{ "payment": { "method": "card", "provider": "stripe" } }
```

Chaves que compartilham um prefixo são mescladas no mesmo objeto. Se um caminho
colidir com um valor escalar já definido em um segmento intermediário, o valor
estruturado (aninhado) prevalece.

### `Build` e correlação de trace

`Build()` não muta o estado — ele materializa um novo dicionário a partir dos
atributos acumulados e, quando há um `Activity` ativo, adiciona campos de
correlação de `System.Diagnostics.Activity.Current`:

| Campo | Origem |
| --- | --- |
| `trace_id` | `Activity.Current.TraceId` |
| `span_id` | `Activity.Current.SpanId` |
| `trace_flags` | `Activity.Current.ActivityTraceFlags` |

Se não houver `Activity` ativo, esses campos são simplesmente omitidos. No
ASP.NET Core, um activity é criado por requisição quando há um listener (por
exemplo, OpenTelemetry, ou qualquer `ActivityListener` registrado).

## Exportação: `IWideEventExporter`

`WideEvents.Abstractions` também define o contrato de exportação:

```csharp
public interface IWideEventExporter
{
    Task ExportAsync(
        IReadOnlyDictionary<string, object?> wideEvent,
        CancellationToken cancellationToken = default);
}
```

Este é o ponto de extensão para enviar eventos construídos a um destino downstream
(OTLP, Kafka, stdout, …). Ele permite que pacotes de exporter dependam dos
contratos sem referenciar `WideEvents.Core`.

> **Ainda não conectado.** Atualmente não existe um pipeline que resolva e invoque
> `IWideEventExporter`. Hoje, os eventos são emitidos via `ILogger` pelo middleware
> do ASP.NET Core (veja [Integração com ASP.NET Core](aspnetcore.md)). A interface
> existe para que exporters possam ser construídos sobre um contrato estável.
