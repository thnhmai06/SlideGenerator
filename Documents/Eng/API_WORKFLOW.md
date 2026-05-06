# API & Workflow Documentation

## IPC Endpoints
Operating as an IPC Sidecar, the backend exposes exactly **9 JSON-RPC 2.0 methods** to the Frontend via standard input (`stdin`). These are registered using `AddLocalRpcMethod` in `SlideGenerator.Ipc`.

| Method Name | Handler | Description |
|---|---|---|
| `workflow.start` | `WorkflowHandler.StartAsync` | Initiates the generation pipeline. Requires a specific Data Transfer Object (DTO) parameter. |
| `workflow.cancel` | `WorkflowHandler.CancelAsync` | Gracefully cancels an active running workflow. |
| `workflow.pause` | `WorkflowHandler.PauseAsync` | Pauses the currently running workflow. |
| `workflow.resume` | `WorkflowHandler.ResumeAsync` | Resumes a previously paused workflow. |
| `scanning.scanWorkbook` | `ScanningHandler.ScanWorkbookAsync` | Scans an Excel file to return structured metadata (`WorkbookSummary`) for frontend preview. |
| `scanning.scanPresentation`| `ScanningHandler.ScanPresentationAsync`| Scans a PowerPoint template to extract its structure (`PresentationSummary`) and available Mustache variables. |
| `settings.get` | `SettingsHandler.GetAsync` | Retrieves all current system settings. Requires no parameters (only `CancellationToken`). |
| `settings.update` | `SettingsHandler.UpdateAsync` | Updates system configuration with the provided payload. |
| `settings.resetToDefaults` | `SettingsHandler.ResetToDefaultsAsync` | Resets all system configurations back to their factory default states. |

## System Notifications
In addition to Request/Response methods, the backend actively pushes continuous updates to the frontend via standard output (`stdout`):

- **`workflow/progress`**: A continuous notification stream broadcasted by `WorkflowProgressObserver`. Sends real-time progress events during long-running tasks.

## Workflow System
The core engine (`WorkflowCore`) orchestrates the pipeline using a **Phase-Sequential, Item-Parallel** model:

1. **Phase A (Validation & Setup):** Reads incoming data, validates templates, and creates the `GeneratingTask` state.
2. **Phase B (Resource Prep):** Iterates over assets (strictly using WorkflowCore's native `.ForEach()`). Steps include `DownloadImage` and `EditImage`.
3. **Phase C (Assembly & Cleanup):** Injects mapped variables (`TextInstruction`) and images (`ImageInstruction`) into slides (`ReplaceSlideData`), followed by graceful file handle closure.
