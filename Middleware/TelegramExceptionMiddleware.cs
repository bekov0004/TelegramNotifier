using Microsoft.AspNetCore.Http;
using TelegramNotifier.Services;

namespace TelegramNotifier.Middleware;

public class TelegramExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public TelegramExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ITelegramNotifier notifier)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await notifier.SendExceptionAsync(ex, context);
            throw;
        }
    }
}