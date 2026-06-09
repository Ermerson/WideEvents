using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WideEvents.Abstractions;
using WideEvents.AspNetCore.Enrichers;
using WideEvents.Core.Context;

namespace WideEvents.AspNetCore;

public static class WideEventExtensions
{
    /// <summary>
    /// Registra os serviços necessários para o WideEvents no contêiner de DI.
    /// Deve ser chamado antes de <see cref="UseWideEvents"/>.
    /// </summary>
    public static IServiceCollection AddWideEvents(
        this IServiceCollection services,
        Action<WideEventsOptions>? configure = null)
    {
        var options = new WideEventsOptions();
        configure?.Invoke(options);

        // Retorna WideEvent.Current para que o contexto injetado no middleware seja o mesmo
        // que o código do handler acessa via WideEvent.Add().
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
