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

## `IWideEventBuilder` e o pipeline de merge

`WideEvents.Core.Builder.IWideEventBuilder` é a interface para construir o
dicionário final do evento. A implementação padrão `WideEventBuilder` mescla
**três fontes de dados** em ordem crescente de precedência:

| Fonte | Precedência | Como chega lá |
| --- | --- | --- |
| Valores de `ILogger.BeginScope(...)` | Menor | Via `IExternalScopeProvider` (compartilhado por `WideEventLoggerProvider`) |
| IDs de trace e tags de `Activity.Current` | Média | Lido no momento do build de `System.Diagnostics.Activity.Current` |
| Valores de `WideEvent.Add(...)` | Maior | Buffer AsyncLocal, drenado e limpo no `Build()` |

Quando chaves se sobrepõem, a fonte de maior precedência vence. O dicionário flat
mesclado é então expandido pelo `WideEventStructureBuilder` em uma hierarquia
aninhada.

Na integração com ASP.NET Core, o `IWideEventBuilder` é registrado como singleton
e injetado no middleware. O middleware empurra os metadados do request
(`http.method`, `http.path`) como um scope do `ILogger`, capturando-os via
pipeline de scope provider mesmo antes do código da aplicação rodar.

> Para uso sem web ou em console, prefira `WideEvent.Current.Build()` diretamente.
> Ele produz o evento apenas a partir do buffer AsyncLocal, sem o merge de
> scope/Activity, o que é suficiente para cenários simples.

## `IWideEventExporter`

`WideEvents.Abstractions` define o contrato de exportação:

```csharp
public interface IWideEventExporter
{
    Task ExportAsync(
        IReadOnlyDictionary<string, object?> wideEvent,
        CancellationToken cancellationToken = default);
}
```

`AddWideEvents()` registra `LoggerWideEventExporter` como implementação padrão.
Ela emite o evento via `ILogger` usando a dica de destructuring `{@WideEvent}`:

```csharp
_logger.LogInformation("WideEvent: {@WideEvent}", wideEvent);
```

Registre sua própria implementação após `AddWideEvents()` para enviar eventos a
um destino personalizado (OTLP, Kafka, stdout, …). Por ser registrada depois da
padrão, ela sobrescreve o exporter padrão na resolução do DI.

> A interface é definida em `WideEvents.Abstractions` para que pacotes de exporter
> possam depender do contrato sem referenciar `WideEvents.Core`.
