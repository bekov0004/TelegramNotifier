using Microsoft.AspNetCore.Builder;
using TelegramNotifier.Middleware;

namespace TelegramNotifier.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTelegramNotifier(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TelegramExceptionMiddleware>();
    }
}