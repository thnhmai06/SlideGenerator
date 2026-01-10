# Job system

Vietnamese version: [Vietnamese](../vi/job-system.md)

## Concepts

- Job types:
  - Group job: one workbook + one template + output folder.
  - Sheet job: one worksheet output file.
- Internal model:
  - `JobGroup` (composite root)
  - `JobSheet` (leaf)
- Job states:
  - Pending → Processing → Done
  - Paused, Cancelled, Error

## Collections

- Active collection: in-memory concurrent dictionaries for Pending/Processing/Paused jobs.
- Completed collection: Done/Failed/Cancelled groups and sheets.
- JobManager queries across both collections.

## Lifecycle

- Create: `JobCreate` maps to `IActiveJobCollection.CreateGroup`.
- Start: auto-start is enabled by default; can be paused/resumed later.
- Control: Pause/Resume/Cancel (Stop is treated as Cancel), Remove deletes backend state.
- Completion: when all sheets finish, the group moves to Completed.

## Concurrency

- `job.maxConcurrentJobs` limits running sheet jobs.
- Resume uses available slots first; others remain paused or pending.

## Persistence and recovery

- State is persisted in Hangfire SQLite (`HangfireJobStateStore`).
- Stored state includes paths, status, progress, and text/image configs.
- On startup, Pending/Processing tasks are forced to Paused.
- Payload JSON is not stored; it is reconstructed from persisted state when requested.

Next: [SignalR API](signalr.md)
