# Logging Module

The **SlideGenerator.Logging** module provides a unified logging infrastructure based on **Serilog**.

## Responsibility
- High-performance asynchronous logging.
- Per-workflow log isolation via dedicated file sinks.
- Structured log formatting.

## Log Streams
1. **System Log**: Global app events, written to `stderr` and `latest.log`. Created once at startup via `SystemLoggerBootstrapper.Initialize(...)`.
2. **Workflow Logs**: Per-workflow logs isolated to individual files in `{TempFolder}/TaskLogs/`. One file per workflow instance.

## Key Abstractions

### `IFileLoggerFactory`
Creates file-backed `ILoggerFactory` instances. Each factory owns a dedicated Serilog sink writing to a single file.

```csharp
ILoggerFactory CreateFile(string filePath, string? scope = null);
```

- `filePath` — log file path; directory is auto-created.
- `scope` — static label enriched onto every log event (e.g. `"Workflow/abc123"`). Appears as `Scope` in the formatter.

The returned `ILoggerFactory` is standard MEL — callers use `CreateLogger(categoryName)` to get named `ILogger` instances that all write to the same file.

### `ISystemLogger`
Process-wide logger. Initialized before DI via `SystemLoggerBootstrapper`. Not injected through `IFileLoggerFactory`.

## Log Format

```
[yyyy-MM-dd HH:mm:ss.fff zzz] [Category/Scope] LVL: Message
```

- **Category**: `SourceContext` set by MEL adapter (e.g. `ValidateRequest`, `ExtractData`).
- **Scope**: static label set at `ILoggerFactory` creation time (e.g. `Workflow/abc123`).
- For `Warning` with exception: one summary line appended.
- For `Error`/`Fatal`: full exception chain with indented stack trace.

## Usage Pattern (Generator)

`GeneratingMiddleware` creates one `ILoggerFactory` per workflow and stores it in `GeneratingContext.LoggerFactory`. Each step obtains its own named `ILogger`:

```csharp
// Middleware (once per workflow, survives resume)
data.LoggerFactory ??= fileLoggerFactory.CreateFile(
    data.WorkflowLogPath,
    scope: $"Workflow/{data.WorkflowScope}");

// Each step
var logger = data.LoggerFactory.CreateLogger(nameof(ValidateRequest));
logger.LogInformation("...");
```

`ILoggerFactory` is disposed in `GeneratingContext.Dispose()` (called by `CloseAllHandles`).
