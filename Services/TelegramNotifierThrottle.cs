using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TelegramNotifier.Models;

namespace TelegramNotifier.Services;

public class TelegramNotifierThrottle
{
    private readonly TelegramNotifierOptions _options;
    private readonly ConcurrentDictionary<string, DateTime> _lastSent = new();

    public TelegramNotifierThrottle(IOptions<TelegramNotifierOptions> options)
    {
        _options = options.Value;
    }

    public bool IsDuplicate(Exception ex)
    {
        if (_options.DuplicateThrottleWindow <= TimeSpan.Zero)
            return false;

        var key = ex.GetType().FullName ?? ex.GetType().Name;
        var now = DateTime.UtcNow;

        if (_lastSent.TryGetValue(key, out var last) && now - last < _options.DuplicateThrottleWindow)
            return true;

        _lastSent[key] = now;
        return false;
    }
}
