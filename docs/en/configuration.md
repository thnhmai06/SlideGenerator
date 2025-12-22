# Configuration

Vietnamese version: [Vietnamese](../vi/configuration.md)

## Table of contents

1. [Config source](#config-source)
2. [Runtime behavior](#runtime-behavior)
3. [Safety rules](#safety-rules)
4. [Image config](#image-config)

## Config source

Configuration is loaded on startup and persisted on disk.

Relevant code:

- `SlideGenerator.Application/Configs/ConfigHolder.cs`
- `SlideGenerator.Infrastructure/Configs/ConfigLoader.cs`
- `SlideGenerator.Presentation/Hubs/ConfigHub.cs`

## Runtime behavior

- Server host/port and debug mode are read from config.
- Hangfire uses a SQLite storage at `jobs.db` next to the running executable.
- Worker count is controlled by the configured `MaxConcurrentJobs`.
- Image processing defaults (face/saliency padding) are read from config.

## Safety rules

Config changes are blocked while jobs are running or pending:

- `ConfigHub` checks for `GroupStatus.Pending` or `GroupStatus.Running`.

Paused jobs are allowed to keep configuration editable.

See also: [Job system](job-system.md)

## Image config

`image` config is split into `face` and `saliency` sections:

- `face.confidence`: minimum face detection confidence (0-1).
- `face.padding_*`: padding ratios around detected faces (0-1).
- `face.union_all`: union all detected faces into one ROI.
- `saliency.padding_*`: padding ratios around saliency ROI (0-1).

