using Microsoft.AspNetCore.Http;

namespace TelegramNotifier.Services;

public interface ITelegramNotifier
{
    Task SendExceptionAsync(Exception ex, HttpContext? context = null);
    Task SendMessageAsync(string message);
    Task SendToTelegramAsync(string message);
}