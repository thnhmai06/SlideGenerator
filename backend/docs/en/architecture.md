# Architecture

Vietnamese version: [Vietnamese](../vi/architecture.md)

## Table of contents

1. [High-level overview](#high-level-overview)
2. [Projects and responsibilities](#projects-and-responsibilities)
3. [Key runtime components](#key-runtime-components)
4. [Data flow](#data-flow)

## High-level overview

The backend follows a layered architecture:

- `SlideGenerator.Presentation`: ASP.NET Core host, SignalR hubs.
- `SlideGenerator.Infrastructure`: implementations (Hangfire, file IO, notifications).
- `SlideGenerator.Application`: public contracts (services, DTOs, requests/responses).
- `SlideGenerator.Domain`: core business entities and domain interfaces.

## Projects and responsibilities

### `SlideGenerator.Presentation`

- Exposes SignalR endpoints.
- Validates/dispatches incoming requests.
- Returns typed success/error responses.

See also: [SignalR API](signalr.md)

### `SlideGenerator.Infrastructure`

- Implements job execution using Hangfire.
- Provides concrete job manager and collections.
- Publishes notifications via SignalR.

See also: [Job system](job-system.md)

### `SlideGenerator.Application`

- Defines contracts used by presentation/infrastructure.
- Defines DTOs for requests, responses, and notifications.

### `SlideGenerator.Domain`

- Defines composite job model: group as composite root, sheet as leaf.
- Defines statuses, progress calculation rules, and entity invariants.

## Key runtime components

- SignalR hubs: handle UI traffic and scoped job subscriptions.
- Hangfire server + HangfireSQLite: schedules sheet execution and persists job state.
- Job manager: tracks active vs completed jobs and restores unfinished work on startup.

## Data flow

1. Client sends request to `SlideHub`.
2. Hub uses `IJobManager.Active` to create a group and start it.
3. Hangfire enqueues one job per sheet (identified by stable sheet IDs).
4. `IJobExecutor` processes rows, checkpoints for pause/resume, and persists state.
5. `IJobNotifier` publishes updates to subscribed group/sheet listeners only.
6. When a group finishes, it is moved from Active to Completed.

Next: [Job system](job-system.md)
