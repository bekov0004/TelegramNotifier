namespace TelegramNotifier.Models;

public class TelegramMessage
{
    public string Text { get; set; }
    public bool IsFile { get; set; }
    public string FileContent { get; set; }
    public string Caption { get; set; }
}