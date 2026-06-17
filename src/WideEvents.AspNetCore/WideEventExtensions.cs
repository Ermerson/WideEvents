using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WideEvents.Abstractions;
using WideEvents.AspNetCore.Enrichers;
using WideEvents.Core.Builder;
using WideEvents.Core.Context;
using WideEvents.Core.Enrichers;
using WideEvents.Core.Exporters;
using WideEvents.Core.Logging;

namespace WideEvents.AspNetCore;

/// <summary>
/// Extension methods for registering WideEvents services in the ASP.NET Core DI container
/// and adding the middleware to the HTTP request pipeline.
/// </summary>
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

        // Register the provider as a singleton, then also as ILoggerProvider so the logging
        // infrastructure calls SetScopeProvider() on it, sharing the external scope.
        services.AddSingleton<WideEventLoggerProvider>();
        services.AddSingleton<ILoggerProvider>(sp =>
            sp.GetRequiredService<WideEventLoggerProvider>());

        if (options.TraceEnricher)
            services.AddSingleton<IWideEventEnricher, TraceActivityEnricher>();

        // The builder merges scopes, Activity tags, and the AsyncLocal buffer, then applies enrichers.
        services.AddSingleton<IWideEventBuilder>(sp =>
            new WideEventBuilder(
                sp.GetRequiredService<WideEventLoggerProvider>(),
                sp.GetServices<IWideEventEnricher>()));

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
