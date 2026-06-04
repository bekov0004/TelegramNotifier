# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.0.0] - 2026-06-04

### Added
- Three `AddTelegramNotifier` overloads: code-only, `IConfigurationSection`, and section + code override
- Exception type filtering via `ExcludedExceptionTypes` — supports subclass matching with `IsAssignableFrom`
- Duplicate throttling via `DuplicateThrottleWindow` — one notification per exception type per window
- App name and environment in notifications — auto-populated from `IHostEnvironment`, overridable manually
- `LogLevel` parameter in `SendMessageAsync` with emoji prefixes (🟢 INFO, 🟡 WARN, 🔴 ERROR, 🚨 CRIT)
- Custom exception formatter via `ExceptionFormatter` (`Func<Exception, HttpContext?, string>?`)
- Nullable reference types enabled project-wide (`<Nullable>enable</Nullable>`)

### Fixed
- `SendTextAsync` was always serializing `message_thread_id: null` — now conditionally added like `SendFileAsync`
- Exception caption could exceed Telegram's 1024-char limit — replaced with compact one-liner
- Markdown code fences (` ``` `) removed from `.txt` file content
- `ProcessAsync` was `public` but absent from `ITelegramNotifier` — made `private`
- `TelegramMessage` string properties lacked nullable annotations

### Changed
- `AddTelegramNotifier` now accepts `IConfigurationSection` instead of `IConfiguration` — no more hardcoded `"TelegramNotifier"` section name inside the library

### Breaking Changes
- `AddTelegramNotifier(IConfiguration, Action<TelegramNotifierOptions>?)` removed — replace with:
  ```csharp
  // Before
  services.AddTelegramNotifier(builder.Configuration);

  // After
  services.AddTelegramNotifier(builder.Configuration.GetSection("TelegramNotifier"));
  ```

---

## [1.0.1] - 2026-05-01

### Added
- Multi-target support: net6.0, net7.0, net8.0, net9.0
- Resilient message delivery: exponential backoff on HTTP failures
- Rate-limit handling — respects Telegram `Retry-After` header
- `MaxRetryCount` option (default: 3)

### Fixed
- Background worker double-registration in DI
- NuGet dependency versions made explicit

---

## [1.0.0] - 2026-04-01

### Added
- Initial release
- Send exceptions and log messages to Telegram via Bot API
- Messages > 2000 chars sent as `.txt` file attachments via `sendDocument`
- Asynchronous queue via `System.Threading.Channels` + `BackgroundService`
- Markdown formatting for all messages
- Forum topic support via `MessageThreadId`
- `Enabled` flag to silence all sending without removing registration
- `UseTelegramNotifier()` middleware for automatic unhandled exception capture
- `AddTelegramNotifier(IConfiguration)` DI extension
