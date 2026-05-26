using Microsoft.AspNetCore.Builder;

namespace WideEvents.AspNetCore;

public static class WideEventExtensions
{
    public static IApplicationBuilder UseWideEvents(this IApplicationBuilder app)
        => app.UseMiddleware<WideEventMiddleware>();
}