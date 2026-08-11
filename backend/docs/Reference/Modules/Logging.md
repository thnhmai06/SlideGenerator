# Logging Module

The **SlideGenerator.Logging** module provides a unified logging infrastructure based on **Serilog**.

## Responsibility

- High-performance asynchronous logging.
- Per-workflow log isolation via dedicated file sinks.
- Structured log formatting.

## Log Streams

1. **System Log**: Global app events, written to `stderr` and `latest.log`. Created once at startup via
   `SystemLoggerBootstrapper.Initialize(...)`.
2. **Workflow Logs**: Per-workflow logs isolated to individual files in `{TempFolder}/TaskLogs/`. One file per workflow
   instance.

## Key Abstractions

### `IFileLoggerFactory`

Creates file-backed `ILoggerFactory` instances. Each factory owns a dedicated Serilog sink writing to a single file,
optionally alongside a second in-memory sink that forwards every event to a callback in real time.

```csharp
ILoggerFactory CreateFile(
    string filePath, IReadOnlyList<string>? scopePropertyNames = null, Action<LogNotification>? onLogEvent = null);
```

- `filePath` — log file path; directory is auto-created.
- `scopePropertyNames` — ordered list of ambient property names (pushed via `Serilog.Context.LogContext.PushProperty`)
  to join into each event's scope path (e.g. `["RequestId", "JobId", "RowIndex"]` → `"req-1/job-2/3"`). This module
  has **no** notion of what a scope means — it just joins whichever names the caller supplies; the caller (e.g.
  `SlideGenerator.Generator`'s `Middleware.cs`) owns the actual property names, keeping Foundation-module Logging
  free of any downstream module's business concepts (`dep-inward-only`). `null`/empty → every event's scope path is
  empty.
- `onLogEvent` — optional callback, invoked once per log line via a second Serilog sink (`ScopeNotifyingSink`)
  running alongside the file sink, carrying a `LogNotification` (`Timestamp`, `Path` — the built scope path, `Level`
  — `Serilog.Events.LogEventLevel`, `Info` — the rendered message). Used by callers that need to forward log lines
  to a frontend in real time without a second persistence store.

The returned `ILoggerFactory` is standard MEL — callers use `CreateLogger(categoryName)` to get named `ILogger`
instances that all write to the same file.

### `ISystemLogger`

Process-wide logger. Initialized before DI via `SystemLoggerBootstrapper`. Not injected through `IFileLoggerFactory`.

## Log Format

```
[yyyy-MM-dd HH:mm:ss.fff zzz] [Category/ScopePath] LVL: Message
```

- **Category**: `SourceContext`/`LoggerName` set by MEL adapter (e.g. `GenerateJobStep`, `InspectUrlsStep`).
- **ScopePath**: built per-event from whichever `scopePropertyNames` are present on `Serilog.Context.LogContext` at
  the point of logging (e.g. `req-1/job-2/3`) — **not** a static label set once at `ILoggerFactory` creation time.
  Callers push scope onto `LogContext` for the duration of a step/row (`using (LogContext.PushProperty(...))`), so
  every log line written during that scope carries the right path automatically.
- For `Warning` with exception: one summary line appended.
- For `Error`/`Fatal`: full exception chain with indented stack trace.

## Usage Pattern (Generator)

`Middleware` (in `SlideGenerator.Generator`) creates one `ILoggerFactory` per job and stores it in
`JobContext.Transient.LoggerFactory`, passing the module's own Request/Job/Row scope property names and a callback
that forwards every line to `ILogNotifier` (see the Generator module doc for how that reaches the frontend). Each
step obtains its own named `ILogger`:

```csharp
// Middleware (once per job, survives persistence resume)
data.Transient.LoggerFactory ??= fileLoggerFactory.CreateFile(
    data.Persist.LogPath,
    ["RequestId", "JobId", "RowIndex"],
    notification => logNotifier.Publish(new LogEntry { /* map LogNotification → Generator's own LogEntry */ }));

// Each step, wrapped in a LogContext scope for its own RequestId/JobId (and RowIndex, per-row)
using (LogContext.PushProperty("RequestId", requestId))
using (LogContext.PushProperty("JobId", jobId))
{
    var logger = data.Transient.LoggerFactory.CreateLogger(nameof(GenerateJobStep));
    logger.LogInformation("...");
}
```

`ILoggerFactory` is disposed with the rest of the job's transient state once the job workflow completes.
