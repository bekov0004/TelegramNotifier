using System.Net;
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

    private Task SendTextAsync(TelegramMessage msg, CancellationToken ct)
    {
        var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";

        return SendWithRetryAsync("sendMessage", innerCt =>
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(_options.ChatId), "chat_id");
            form.Add(new StringContent(msg.Text ?? ""), "text");
            form.Add(new StringContent("Markdown"), "parse_mode");

            if (_options.MessageThreadId.HasValue)
                form.Add(new StringContent(_options.MessageThreadId.Value.ToString()), "message_thread_id");

            return _httpClient.PostAsync(url, form, innerCt);
        }, ct);
    }

    private Task SendFileAsync(TelegramMessage msg, CancellationToken ct)
    {
        var url = $"https://api.telegram.org/bot{_options.BotToken}/sendDocument";
        var bytes = System.Text.Encoding.UTF8.GetBytes(msg.FileContent ?? "");
        var filename = $"log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";

        return SendWithRetryAsync("sendDocument", async innerCt =>
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(_options.ChatId), "chat_id");
            form.Add(new StringContent("Markdown"), "parse_mode");

            if (_options.MessageThreadId.HasValue)
                form.Add(new StringContent(_options.MessageThreadId.Value.ToString()), "message_thread_id");

            form.Add(new StringContent(msg.Caption ?? "Log"), "caption");

            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("text/plain");
            form.Add(file, "document", filename);

            return await _httpClient.PostAsync(url, form, innerCt);
        }, ct);
    }

    private async Task SendWithRetryAsync(
        string operation,
        Func<CancellationToken, Task<HttpResponseMessage>> send,
        CancellationToken ct)
    {
        var maxAttempts = _options.MaxRetryCount + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var response = await send(ct);

                if (response.IsSuccessStatusCode)
                    return;

                var statusCode = (int)response.StatusCode;
                var isRetryable = statusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests;

                if (!isRetryable || attempt == maxAttempts)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("{Operation} failed with {StatusCode}: {Body}", operation, statusCode, body);
                    return;
                }

                var delay = GetRetryDelay(response, attempt);
                _logger.LogWarning("{Operation} returned {StatusCode}, retrying in {Delay}s ({Attempt}/{Max})",
                    operation, statusCode, delay.TotalSeconds, attempt, maxAttempts);

                await Task.Delay(delay, ct);
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger.LogWarning(ex, "{Operation} request failed, retrying in {Delay}s ({Attempt}/{Max})",
                    operation, delay.TotalSeconds, attempt, maxAttempts);
                await Task.Delay(delay, ct);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "{Operation} failed after {Max} attempt(s)", operation, maxAttempts);
            }
        }
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests
            && response.Headers.RetryAfter?.Delta is { } delta)
            return delta;

        return TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)); // 1s, 2s, 4s, 8s...
    }
}
