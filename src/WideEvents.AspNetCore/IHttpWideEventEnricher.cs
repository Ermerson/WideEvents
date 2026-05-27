using Microsoft.AspNetCore.Http;
using WideEvents.Abstractions;

namespace WideEvents.AspNetCore;

/// <summary>
/// Enriquece um wide event com atributos derivados da requisição/resposta HTTP.
/// Implemente e registre via DI para adicionar novos atributos sem modificar o middleware.
/// </summary>
public interface IHttpWideEventEnricher
{
    /// <summary>Chamado antes de passar para o próximo middleware.</summary>
    void EnrichRequest(HttpContext context, IWideEventContext wideEvent);

    /// <summary>Chamado após retorno do próximo middleware (sucesso).</summary>
    void EnrichResponse(HttpContext context, IWideEventContext wideEvent);
}