using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TelegramNotifier.Models;

namespace TelegramNotifier.Services;

public class TelegramNotifier : ITelegramNotifier
{
    private readonly HttpClient _httpClient;
    private readonly TelegramNotifierOptions _options;
    private readonly TelegramNotifierQueue _queue;

    public TelegramNotifier(
        HttpClient httpClient,
        IOptions<TelegramNotifierOptions> options,
        TelegramNotifierQueue queue)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _queue = queue;
    }

    public async Task SendExceptionAsync(Exception ex, HttpContext? context = null)
    {
        if (!_options.Enabled) return;

        var message = $@"
🚨 ERROR

📌 Message:
{ex.Message}

📍 Path:
{context?.Request?.Path}

🕒 Time:
{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}

📚 StackTrace:
{ex.StackTrace}
";

        await SendMessageAsync(message);
    }

    public async Task SendMessageAsync(string message)
    {
        if (!_options.Enabled) return;
        
        if (string.IsNullOrWhiteSpace(message)) return;

        await _queue.EnqueueAsync(message);
    }

    public async Task SendToTelegramAsync(string message)
    {
        if (!_options.Enabled) return;
        
        if (string.IsNullOrWhiteSpace(message)) return;

        try
        {
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";

            var payload = new
            {
                chat_id = _options.ChatId,
                text = message,
                message_thread_id = _options.MessageThreadId
            };

            await _httpClient.PostAsJsonAsync(url, payload);
        }
        catch
        {
        }
    }
}