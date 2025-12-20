# Job system

## Table of contents

1. [Concepts](#concepts)
2. [Composite model](#composite-model)
3. [Active vs completed collections](#active-vs-completed-collections)
4. [Lifecycle operations](#lifecycle-operations)
5. [Pause/resume behavior](#pauseresume-behavior)
6. [Cancel and clear](#cancel-and-clear)

## Concepts

- **Group**: a batch created from one workbook and one template.
- **Sheet**: a single worksheet job that generates one output file.
- **Active jobs**: pending/running/paused.
- **Completed jobs**: completed/failed/cancelled.

## Composite model

The domain uses a composite pattern:

- `IJobGroup` is the composite root.
- `IJobSheet` is the leaf.
- `IJobGroup.Sheets` exposes children.

## Active vs completed collections

`IJobManager` splits tracking into two collections:

- `IJobManager.Active` (`IActiveJobCollection`)
  - Creates groups.
  - Controls running jobs (start/pause/resume/cancel).
- `IJobManager.Completed` (`ICompletedJobCollection`)
  - Stores finished groups.
  - Supports removal and clearing.

### Automatic move

When all sheets in a group finish (Completed/Failed/Cancelled), the group is moved from Active to Completed.

## Lifecycle operations

### Create and start

- Create: `IActiveJobCollection.CreateGroup(request)`
- Start: `IActiveJobCollection.StartGroup(groupId)`

### Group control

- Pause: `PauseGroup(groupId)`
- Resume: `ResumeGroup(groupId)`
- Cancel: `CancelGroup(groupId)`

### Sheet control

- Pause: `PauseSheet(sheetId)`
- Resume: `ResumeSheet(sheetId)`
- Cancel: `CancelSheet(sheetId)`

## Pause/resume behavior

Sheet execution uses an event-based wait:

- When paused, a sheet blocks immediately.
- When resumed, it continues immediately (no polling delay).

Implementation notes:

- Domain: `JobSheet.WaitIfPaused(token)`
- Executor: calls `WaitIfPaused` inside the processing loop.

## Cancel and clear

- Cancel active:
  - Per group: `IActiveJobCollection.CancelGroup`
  - All: `IActiveJobCollection.CancelAll`
- Clear completed:
  - Per group: `ICompletedJobCollection.RemoveGroup`
  - All: `ICompletedJobCollection.ClearAll`

Next: [SignalR API](signalr.md)
