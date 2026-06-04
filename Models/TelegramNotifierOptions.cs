namespace TelegramNotifier.Models;

public class TelegramNotifierOptions
{
    public string BotToken { get; set; } = null!;
    public string ChatId { get; set; } = null!;
    public int? MessageThreadId { get; set; }
    public bool Enabled { get; set; } = true;
    public int MaxRetryCount { get; set; } = 3;

    public ICollection<Type> ExcludedExceptionTypes { get; set; } = new List<Type>();
    public TimeSpan DuplicateThrottleWindow { get; set; } = TimeSpan.Zero;

    public bool IsExceptionExcluded(Exception ex)
        => ExcludedExceptionTypes.Any(t => t.IsAssignableFrom(ex.GetType()));
}