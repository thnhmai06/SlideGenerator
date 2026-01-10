# Architecture

Vietnamese version: [Vietnamese](../vi/architecture.md)

## Overview

The backend follows Clean Architecture with feature-based slices. SignalR hubs expose a minimal task API, while job execution runs in background workers and persists state for crash recovery.

## Layers

- Presentation: ASP.NET Core host and SignalR hubs (Task/Sheet/Config).
- Application: contracts, DTOs, and feature orchestration.
- Domain: core job entities, states, and invariants.
- Infrastructure: Hangfire + SQLite state store, IO, logging, and background execution.

## Key runtime components

- TaskHub: validates requests and maps them to task operations.
- JobManager: owns Active and Completed collections.
- ActiveJobCollection: in-memory concurrent dictionaries for active tasks.
- JobExecutor: processes rows, checkpoints, and persists state.
- HangfireJobStateStore: persists group/sheet state in SQLite.
- JobNotifier: pushes scoped notifications to subscribers.

## Data flow

1. Client sends JSON to TaskHub (`ProcessRequest`).
2. TaskHub creates a group or sheet task via JobManager.Active.
3. ActiveJobCollection persists state and queues Hangfire jobs (if auto-start).
4. JobExecutor processes rows and updates state/notifications.
5. When all sheets finish, the group moves to Completed.

Next: [SignalR API](signalr.md)
