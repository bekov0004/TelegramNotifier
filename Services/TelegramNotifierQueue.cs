using System.Threading.Channels;

namespace TelegramNotifier.Services;

public class TelegramNotifierQueue
{
    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>();

    public async Task EnqueueAsync(string message)
    {
        await _queue.Writer.WriteAsync(message);
    }

    public IAsyncEnumerable<string> DequeueAsync(CancellationToken ct)
    {
        return _queue.Reader.ReadAllAsync(ct);
    }
}