# Configuration

## Table of contents

1. [Config source](#config-source)
2. [Runtime behavior](#runtime-behavior)
3. [Safety rules](#safety-rules)

## Config source

Configuration is loaded on startup and persisted on disk.

Relevant code:

- `SlideGenerator.Application/Configs/ConfigHolder.cs`
- `SlideGenerator.Infrastructure/Configs/ConfigLoader.cs`
- `SlideGenerator.Presentation/Hubs/ConfigHub.cs`

## Runtime behavior

- Server host/port and debug mode are read from config.
- Hangfire uses a SQLite storage, using the configured path.
- Worker count is controlled by the configured `MaxConcurrentJobs`.

## Safety rules

Config changes are blocked while jobs are active:

- `ConfigHub` checks `IJobManager.Active.HasActiveJobs`.

See also: [Job system](job-system.md)
