# TelegramNotifier

A lightweight .NET library for sending logs and exceptions to Telegram with support for queuing and background processing.

---

## Key Features

* Sending messages to Telegram
* Automatic exception reporting (`Exception`)
* Sending long logs as `.txt` files
* Support for Telegram forum topics (`message_thread_id`)
* Asynchronous processing via queue (Channel + BackgroundWorker)
* Formatting logs into a readable view (code blocks)

---

## Supported Platforms

* .NET 6
* .NET 7
* .NET 8
* .NET 9

---

## Installation

```bash
dotnet add package TelegramNotifier
```

---

## Configuration

Add settings to `appsettings.json`:

```json
{
  "TelegramNotifier": {
    "Enabled": true,
    "BotToken": "YOUR_BOT_TOKEN",
    "ChatId": "-100XXXXXXXXXX"
  }
}
```

### Explanations

| Parameter       | Description                                                   |
| --------------- | ------------------------------------------------------------- |
| Enabled         | Enables or disables sending to Telegram                       |
| BotToken        | Your Telegram bot token                                       |
| ChatId          | ID of the chat or group where messages will be sent           |
| MessageThreadId | (optional) ID of the group topic, if using forum chats        |

---

## 🧠 Important

* `MessageThreadId` is only needed if you have a **group with topics**
* If it is a regular chat — you can leave it blank
* Nothing will be sent without `Enabled = true`
---

## Registration

```csharp
builder.Services.AddTelegramNotifier(builder.Configuration);
```

---

## Usage

### Automatic Error Reporting

If you plug in the middleware, all unhandled errors will be automatically sent to Telegram:
```csharp
app.UseTelegramNotifier();
```

---

### Sending a Regular Message Manually
If you need to send a message yourself:
```csharp
public class TestController : ControllerBase
{
    private readonly ITelegramNotifier _notifier;

    public TestController(ITelegramNotifier notifier)
    {
        _notifier = notifier;
    }

    [HttpGet("send")]
    public async Task<IActionResult> Send()
    {
        await _notifier.SendMessageAsync("Test message");
        return Ok();
    }
}
```

---

### Sending an Exception Manually
If you want to send an exception manually:
```csharp
try
{
    throw new Exception("Test exception");
}
catch (Exception ex)
{
    await _notifier.SendExceptionAsync(ex);
}
```

---

## Security

* Do not store `BotToken` in open source code
* Use `appsettings` or environment secrets

---

## 📍 How to get ChatId

To send messages to Telegram, you need to find out the `ChatId`.

---

### 🤖 1. For a private chat with the bot

1. Send any message to your bot (e.g., `hi`)
2. Open in your browser:

```
https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates
```

3. Find the field:

```json
"chat": {
  "id": 123456789
}
```

👉 this is your `ChatId`

---

### 👥 2. For a group

1. Add the bot to the group
2. Send any message to the group
3. Open:

```
https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates
```

4. Find:

```json
"chat": {
  "id": -1001234567890
}
```

👉 this is the group's `ChatId`

---

### 🧵 3. For forum groups (Topics)

If you have a group with topics:

* `ChatId` — is the ID of the entire group
* `MessageThreadId` — is the ID of the specific topic

Example:

```json
"message_thread_id": 6
```

---

### ⚠️ Important

* Without a message in the chat, `getUpdates` may show nothing
* If you are using a webhook — disable it first:

```
https://api.telegram.org/bot<YOUR_BOT_TOKEN>/deleteWebhook
```

---