using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TelegramNotifier.Models;

namespace TelegramNotifier.Services;

public class TelegramBackgroundWorker : BackgroundService
{
    private readonly TelegramNotifierOptions _options;
    private readonly HttpClient _httpClient;
    private readonly TelegramNotifierQueue _queue;

    public TelegramBackgroundWorker(
        TelegramNotifierQueue queue,
        IOptions<TelegramNotifierOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _queue = queue;
        _options = options.Value;
        _httpClient = httpClientFactory.CreateClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var msg in _queue.DequeueAsync(stoppingToken))
        {
            if (msg.IsFile)
            {
                await SendFileAsync(msg);
            }
            else
            {
                await SendTextAsync(msg);
            }
        }
    }

    // =========================
    // TEXT MESSAGE
    // =========================
    private async Task SendTextAsync(TelegramMessage msg)
    {
        var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";

        var payload = new
        {
            chat_id = _options.ChatId,
            text = msg.Text,
            message_thread_id = _options.MessageThreadId,
            parse_mode = "Markdown"
        };

        await _httpClient.PostAsJsonAsync(url, payload);
    }

    // =========================
    // FILE MESSAGE
    // =========================
    private async Task SendFileAsync(TelegramMessage msg)
    {
        var url = $"https://api.telegram.org/bot{_options.BotToken}/sendDocument";

        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(_options.ChatId), "chat_id");
        form.Add(new StringContent("Markdown"), "parse_mode");

        if (_options.MessageThreadId.HasValue)
        {
            form.Add(new StringContent(_options.MessageThreadId.Value.ToString()), "message_thread_id");
        }

        form.Add(new StringContent(msg.Caption ?? "Log"), "caption");

        var bytes = System.Text.Encoding.UTF8.GetBytes(msg.FileContent ?? "");

        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType =
            System.Net.Http.Headers.MediaTypeHeaderValue.Parse("text/plain");

        form.Add(file, "document", $"log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt");

        await _httpClient.PostAsync(url, form);
    }
}