using System.Threading.Channels;
using TelegramNotifier.Models;

namespace TelegramNotifier.Services;

public class TelegramNotifierQueue
{
    private readonly Channel<TelegramMessage> _queue = Channel.CreateUnbounded<TelegramMessage>();

    public async Task EnqueueAsync(TelegramMessage message)
        => await _queue.Writer.WriteAsync(message);

    public IAsyncEnumerable<TelegramMessage> DequeueAsync(CancellationToken ct)
        => _queue.Reader.ReadAllAsync(ct);
}