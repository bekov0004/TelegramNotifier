using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TelegramNotifier.Services;

public interface ITelegramNotifier
{
    Task SendExceptionAsync(Exception ex, HttpContext? context = null);
    Task SendMessageAsync(string message, LogLevel level = LogLevel.Information);
}