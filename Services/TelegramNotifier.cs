using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TelegramNotifier.Models;

namespace TelegramNotifier.Services;

public class TelegramNotifier : ITelegramNotifier
{
    private readonly HttpClient _httpClient;
    private readonly TelegramNotifierOptions _options;
    private readonly TelegramNotifierQueue _queue;

    private const int FILE_THRESHOLD = 2000;

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

        var caption = $"🚨 {ex.GetType().Name}: {ex.Message}";
        var fileContent = BuildExceptionFile(ex, context);

        await ProcessAsync($"{caption}\n\n{fileContent}");
    }

    public async Task SendMessageAsync(string message)
    {
        if (!_options.Enabled) return;
        if (string.IsNullOrWhiteSpace(message)) return;

        await _queue.EnqueueAsync(message);
    }

    public async Task ProcessAsync(string message)
    {
        if (!_options.Enabled) return;
        if (string.IsNullOrWhiteSpace(message)) return;

        if (message.Length > FILE_THRESHOLD)
        {
            await SendAsFileAsync("⚠️ Large Message", message);
        }
        else
        {
            await _queue.EnqueueAsync(message);
        }
    }

    private async Task SendAsFileAsync(string caption, string content)
    {
        try
        {
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendDocument";

            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(_options.ChatId), "chat_id");

            if (_options.MessageThreadId.HasValue)
            {
                form.Add(new StringContent(_options.MessageThreadId.Value.ToString()), "message_thread_id");
            }

            form.Add(new StringContent(caption), "caption");

            var bytes = Encoding.UTF8.GetBytes(content);

            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

            form.Add(file, "document", $"log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt");

            await _httpClient.PostAsync(url, form);
        }
        catch
        {
            // ignored
        }
    }

    private string BuildExceptionFile(Exception ex, HttpContext? context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("========== EXCEPTION REPORT ==========");
        sb.AppendLine($"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        sb.AppendLine("---- SUMMARY ----");
        sb.AppendLine($"{ex.GetType().Name}: {ex.Message}");
        sb.AppendLine();

        if (context != null)
        {
            sb.AppendLine("---- REQUEST ----");
            sb.AppendLine($"Path: {context.Request.Path}");
            sb.AppendLine($"Method: {context.Request.Method}");
            sb.AppendLine();
        }

        sb.AppendLine("---- STACKTRACE ----");
        sb.AppendLine(ex.StackTrace);

        if (ex.InnerException != null)
        {
            sb.AppendLine();
            sb.AppendLine("---- INNER EXCEPTION ----");
            sb.AppendLine(ex.InnerException.ToString());
        }

        sb.AppendLine();
        sb.AppendLine("========== END ==========");

        return sb.ToString();
    }
}