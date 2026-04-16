using Microsoft.Extensions.Hosting;

namespace TelegramNotifier.Services;

public class TelegramBackgroundWorker : BackgroundService
{
    private readonly TelegramNotifierQueue _queue;
    private readonly ITelegramNotifier _notifier;

    public TelegramBackgroundWorker(
        TelegramNotifierQueue queue,
        ITelegramNotifier notifier)
    {
        _queue = queue;
        _notifier = notifier;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.DequeueAsync(stoppingToken))
        {
            await _notifier.SendToTelegramAsync(message);
        }
    }
}