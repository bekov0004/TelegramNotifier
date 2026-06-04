# TelegramNotifier

A lightweight .NET library for sending logs and exceptions to Telegram with support for queuing, retries, and background processing.

---

## Key Features

- Sending messages to Telegram with log level prefixes
- Automatic exception reporting via middleware
- Long messages sent as `.txt` file attachments (> 2000 chars)
- Support for Telegram forum topics (`MessageThreadId`)
- Asynchronous queue processing (Channel + BackgroundService)
- Resilient delivery: exponential backoff + rate-limit handling
- Exception type filtering
- Duplicate throttling — suppresses repeated exceptions within a time window
- App name and environment included in notifications automatically
- Custom exception formatter

---

## Supported Platforms

- .NET 6
- .NET 7
- .NET 8
- .NET 9

---

## Installation

```bash
dotnet add package TelegramNotifier
```

> See [CHANGELOG.md](https://github.com/bekov0004/TelegramNotifier/blob/main/CHANGELOG.md) for version history.

---

## Registration

Three ways to register — pick the one that fits your setup.

### 1. From code only

```csharp
builder.Services.AddTelegramNotifier(options =>
{
    options.BotToken = "YOUR_BOT_TOKEN";
    options.ChatId = "-100XXXXXXXXXX";
    options.Enabled = true;
});
```

### 2. From `appsettings.json`

```json
{
  "TelegramNotifier": {
    "Enabled": true,
    "BotToken": "YOUR_BOT_TOKEN",
    "ChatId": "-100XXXXXXXXXX",
    "MessageThreadId": 6
  }
}
```

```csharp
builder.Services.AddTelegramNotifier(
    builder.Configuration.GetSection("TelegramNotifier"));
```

### 3. From `appsettings.json` + override from code

```csharp
builder.Services.AddTelegramNotifier(
    builder.Configuration.GetSection("TelegramNotifier"),
    options =>
    {
        options.DuplicateThrottleWindow = TimeSpan.FromMinutes(5);
        options.ExcludedExceptionTypes.Add(typeof(OperationCanceledException));
    });
```

---

## Configuration options

| Parameter                | Type              | Default      | Description                                              |
|--------------------------|-------------------|--------------|----------------------------------------------------------|
| `BotToken`               | `string`          | —            | Telegram bot token                                       |
| `ChatId`                 | `string`          | —            | Target chat or group ID                                  |
| `Enabled`                | `bool`            | `true`       | Master switch — set to `false` to silence all sending    |
| `MessageThreadId`        | `int?`            | `null`       | Topic ID for forum groups                                |
| `MaxRetryCount`          | `int`             | `3`          | Max retry attempts on HTTP failure                       |
| `ApplicationName`        | `string?`         | auto         | App name shown in notifications (auto-filled from host)  |
| `EnvironmentName`        | `string?`         | auto         | Environment shown in notifications (auto-filled from host)|
| `DuplicateThrottleWindow`| `TimeSpan`        | `Zero`       | Suppress duplicate exception types within this window    |
| `ExcludedExceptionTypes` | `ICollection<Type>`| `[]`        | Exception types (and subclasses) to never send           |
| `ExceptionFormatter`     | `Func<Exception, HttpContext?, string>?` | `null` | Custom formatter for exception body    |

---

## Middleware — automatic exception reporting

Add to capture all unhandled exceptions automatically:

```csharp
app.UseTelegramNotifier();
```

---

## Usage

### Send a message with log level

```csharp
await _notifier.SendMessageAsync("Server started");                              // 🟢 [INFO]
await _notifier.SendMessageAsync("Queue is 80% full", LogLevel.Warning);         // 🟡 [WARN]
await _notifier.SendMessageAsync("Database unavailable", LogLevel.Error);        // 🔴 [ERROR]
await _notifier.SendMessageAsync("Service crashed", LogLevel.Critical);          // 🚨 [CRIT]
```

### Send an exception manually

```csharp
try
{
    // ...
}
catch (Exception ex)
{
    await _notifier.SendExceptionAsync(ex);
}
```

### Inject into a controller

```csharp
public class OrdersController : ControllerBase
{
    private readonly ITelegramNotifier _notifier;

    public OrdersController(ITelegramNotifier notifier)
    {
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<IActionResult> Create(OrderDto dto)
    {
        await _notifier.SendMessageAsync($"New order: {dto.Id}", LogLevel.Information);
        return Ok();
    }
}
```

---

## Exception filtering

Suppress specific exception types (including subclasses):

```csharp
options.ExcludedExceptionTypes.Add(typeof(OperationCanceledException));
options.ExcludedExceptionTypes.Add(typeof(BadHttpRequestException));
```

---

## Duplicate throttling

Prevent the same exception type from flooding Telegram:

```csharp
options.DuplicateThrottleWindow = TimeSpan.FromMinutes(5);
```

One notification per exception type per 5 minutes.

---

## Custom exception formatter

Override the default exception body format:

```csharp
options.ExceptionFormatter = (ex, ctx) =>
    $"Error: {ex.Message}\nPath: {ctx?.Request.Path}\nTime: {DateTime.UtcNow}";
```

If the result exceeds 2000 characters it is sent as a `.txt` file — the caption stays auto-generated.

---

## Security

- Never store `BotToken` in source code
- Use `appsettings.json`, environment variables, or a secrets manager

---

## How to get ChatId

### Private chat with the bot

1. Send any message to your bot
2. Open: `https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates`
3. Find `"chat": { "id": 123456789 }` — that is your `ChatId`

### Group

1. Add the bot to the group and send a message
2. Open the same `getUpdates` URL
3. Find `"chat": { "id": -1001234567890 }` — group IDs are negative

### Forum group (topics)

- `ChatId` — the group ID
- `MessageThreadId` — the topic ID (visible in the URL when you open a topic)

> If `getUpdates` returns nothing, make sure no webhook is set:
> `https://api.telegram.org/bot<YOUR_BOT_TOKEN>/deleteWebhook`
