using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WideEvents.Abstractions;
using WideEvents.Core.Context;
using WideEvents.Core.Exporters;

namespace WideEvents.AspNetCore;

public static class WideEventExtensions
{

    public static IServiceCollection AddWideEvents(this IServiceCollection services)
    {
        services.AddSingleton<IHttpWideEventEnricher, DefaultHttpEnricher>();
        services.AddScoped<IWideEventContext, WideEventContext>();
        services.AddSingleton<IWideEventExporter, LoggerWideEventExporter>();
        return services;
    }
    
    public static IApplicationBuilder UseWideEvents(this IApplicationBuilder app)
        => app.UseMiddleware<WideEventMiddleware>();
}