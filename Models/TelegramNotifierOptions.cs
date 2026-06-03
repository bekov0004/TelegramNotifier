namespace TelegramNotifier.Models;

public class TelegramNotifierOptions
{
    public string BotToken { get; set; } = null!;
    public string ChatId { get; set; } = null!;
    public int? MessageThreadId { get; set; }
    public bool Enabled { get; set; } = true;
    public int MaxRetryCount { get; set; } = 3;
}