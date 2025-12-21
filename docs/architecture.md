# Architecture

## Layers and modules

- **Presentation**: ASP.NET Core host, SignalR hubs (`/hubs/sheet`, `/hubs/slide`, `/hubs/config`).
- **Infrastructure**: Hangfire scheduling/persistence, SignalR publisher, framework adapters, filesystem/download implementations.
- **Application**: request/response DTOs, job contracts, slide/sheet abstractions.
- **Domain**: job entities, value objects, and repository/publisher abstractions.

## Job model

- **GroupJob**: one workbook + one template + one output folder. Stable `GroupJobId`.
- **SheetJob**: one worksheet -> one output presentation. Stable `SheetJobId`.
- **Progress**: per-sheet progress is row-based; group progress is the average of sheets.
- **Errors**: per-row image failures increment `ErrorCount` but do not fail the job.

## Pause/resume semantics

- Pause is cooperative and checkpoint-based.
- Checkpoints occur before/after: row processing, cloud resolve, download, image processing, slide update, and state persistence.
- Resume continues from `NextRowIndex` (the next record after the last persisted row).

## SignalR contract

- Subscribe with `SubscribeGroup(groupJobId)` or `SubscribeSheet(sheetJobId)`.
- Notifications are scoped to the subscribed group/sheet only (no global broadcasts).
- `ReceiveNotification` payloads include job id, timestamp, level, message, and structured fields.

## Hangfire dashboard

- Dashboard is available at `/hangfire`.
- Read-only mode is enforced.
- Server binds to `localhost` / `127.0.0.1` only.
