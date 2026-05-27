# Logging Module

The **SlideGenerator.Logging** module provides a unified logging infrastructure based on **Serilog**.

## Responsibility
- High-performance asynchronous logging.
- Per-workflow log isolation.
- Structured log formatting.

## Log Streams
1. **System Log**: Global app events, written to `stderr` and `latest.log`.
2. **Task Logs**: Granular logs for a specific generation job, stored in the `{TempFolder}/TaskLogs/` directory.

## Scoped Logging
Uses `IAppLogger` to support log contexts. When a workflow step logs an error, it automatically carries context like the current Excel Row Index or Sheet Name.
