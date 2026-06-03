using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramNotifier.Models;

namespace TelegramNotifier.Services;

public class TelegramBackgroundWorker : BackgroundService
{
    private readonly TelegramNotifierOptions _options;
    private readonly HttpClient _httpClient;
    private readonly TelegramNotifierQueue _queue;
    private readonly ILogger<TelegramBackgroundWorker> _logger;

    public TelegramBackgroundWorker(
        TelegramNotifierQueue queue,
        IOptions<TelegramNotifierOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramBackgroundWorker> logger)
    {
        _queue = queue;
        _options = options.Value;
        _httpClient = httpClientFactory.CreateClient(nameof(TelegramBackgroundWorker));
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var msg in _queue.DequeueAsync(stoppingToken))
        {
            try
            {
                if (msg.IsFile)
                    await SendFileAsync(msg, stoppingToken);
                else
                    await SendTextAsync(msg, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram message");
            }
        }
    }

    private async Task SendTextAsync(TelegramMessage msg, CancellationToken ct)
    {
        var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";

        var payload = new
        {
            chat_id = _options.ChatId,
            text = msg.Text,
            message_thread_id = _options.MessageThreadId,
            parse_mode = "Markdown"
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Telegram sendMessage failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
        }
    }

    private async Task SendFileAsync(TelegramMessage msg, CancellationToken ct)
    {
        var url = $"https://api.telegram.org/bot{_options.BotToken}/sendDocument";

        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(_options.ChatId), "chat_id");
        form.Add(new StringContent("Markdown"), "parse_mode");

        if (_options.MessageThreadId.HasValue)
            form.Add(new StringContent(_options.MessageThreadId.Value.ToString()), "message_thread_id");

        form.Add(new StringContent(msg.Caption ?? "Log"), "caption");

        var bytes = System.Text.Encoding.UTF8.GetBytes(msg.FileContent ?? "");
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("text/plain");
        form.Add(file, "document", $"log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt");

        var response = await _httpClient.PostAsync(url, form, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Telegram sendDocument failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
        }
    }
}