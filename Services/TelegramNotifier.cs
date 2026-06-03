using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TelegramNotifier.Models;

namespace TelegramNotifier.Services;

public class TelegramNotifier : ITelegramNotifier
{
    private readonly TelegramNotifierOptions _options;
    private readonly TelegramNotifierQueue _queue;

    private const int FILE_THRESHOLD = 2000;

    public TelegramNotifier(
        IOptions<TelegramNotifierOptions> options,
        TelegramNotifierQueue queue)
    {
        _options = options.Value;
        _queue = queue;
    }

    public async Task SendExceptionAsync(Exception ex, HttpContext? context = null)
    {
        if (!_options.Enabled) return;

        var caption = BuildExceptionCaption(ex, context);
        var fileContent = BuildExceptionFile(ex, context);

        await ProcessAsync(fileContent, caption);
    }

    public async Task SendMessageAsync(string message)
    {
        if (!_options.Enabled) return;
        if (string.IsNullOrWhiteSpace(message)) return;

        await ProcessAsync(message, null);
    }

    private async Task ProcessAsync(string content, string? caption)
    {
        if (!_options.Enabled) return;
        if (string.IsNullOrWhiteSpace(content)) return;

        if (content.Length > FILE_THRESHOLD)
        {
            await _queue.EnqueueAsync(new TelegramMessage
            {
                IsFile = true,
                Caption = caption,
                FileContent = content
            });
        }
        else
        {
            await _queue.EnqueueAsync(new TelegramMessage
            {
                IsFile = false,
                Text = content
            });
        }
    }
    
    
    private static string BuildExceptionCaption(Exception ex, HttpContext? context)
    {
        var request = context != null
            ? $" | {context.Request.Method} {context.Request.Path}"
            : string.Empty;

        return $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}{request}";
    }

    private static string BuildExceptionFile(Exception ex, HttpContext? context)
    {
        var sb = new System.Text.StringBuilder();

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