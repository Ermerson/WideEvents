using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WideEvents.Abstractions;
using WideEvents.AspNetCore.Enrichers;
using WideEvents.Core.Context;

namespace WideEvents.AspNetCore;

public static class WideEventExtensions
{
    /// <summary>
    /// Registers the services required by WideEvents in the DI container.
    /// Must be called before <see cref="UseWideEvents"/>.
    /// </summary>
    public static IServiceCollection AddWideEvents(
        this IServiceCollection services,
        Action<WideEventsOptions>? configure = null)
    {
        var options = new WideEventsOptions();
        configure?.Invoke(options);

        // Returns WideEvent.Current so the context injected into the middleware is the same
        // instance the handler code reaches via WideEvent.Add().
        services.AddScoped<IWideEventContext>(_ => WideEvent.Current);
        services.AddSingleton<IHttpWideEventEnricher, DefaultHttpEnricher>();
        services.AddSingleton<IWideEventExporter, LoggerWideEventExporter>();

        if (options.AuthEnricher is { } authOpts)
            services.AddSingleton<IHttpWideEventEnricher>(_ => new AuthEnricher(authOpts));

        return services;
    }

    /// <summary>Adds <see cref="WideEventMiddleware"/> to the request pipeline.</summary>
    public static IApplicationBuilder UseWideEvents(this IApplicationBuilder app)
        => app.UseMiddleware<WideEventMiddleware>();
}
