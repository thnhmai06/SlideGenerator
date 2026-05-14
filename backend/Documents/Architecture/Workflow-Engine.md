# Workflow Engine: 3-Phase Generation Pipeline

SlideGenerator utilizes **WorkflowCore** to orchestrate the complex process of generating PowerPoint presentations. This document explains the execution model, phase boundaries, and state persistence.

## Execution Model: Phase-Sequential, Item-Parallel

The generation process is divided into three distinct phases. While items within a phase can process in parallel, all items must complete their current phase before the workflow progresses to the next one.

### Phase A: Validation & Template Setup
- **Goal**: Ensure the request is valid and prepare the output file.
- **Key Steps**: 
  - `ValidateRequest`: Checks file existence and basic sheet structure.
  - `CreateTemplate`: Copies the template PPTX to the output path and isolates the requested slide.
- **Barrier**: The workflow waits here until the output file is ready for cloning.

### Phase B: Resource Preparation
- **Goal**: Gather all data and process external assets.
- **Key Steps**:
  - `ExtractData`: Reads Excel rows and maps them to `SlideContext` and `ImageContext`.
  - `DownloadImage`: Fetches assets from cloud/web.
  - `EditImage`: Performs ROI-based cropping and resizing.
- **Asset Deduplication**: During Phase B, the `AssetCoordinator` ensures that identical source images with the same edit parameters are only processed once. Secondary steps create **hard links** to the primary result, saving significant CPU and I/O.
- **Parallelism**: Uses WorkflowCore's `.ForEach()` iterator. Multiple rows and images are processed concurrently, throttled by the `GateLocker`.

### Phase C: Assembly & Cleanup
- **Goal**: Finalize the document and release resources.
- **Key Steps**:
  - `ReplaceSlideData`: Injects text and processed images into the slide shapes.
  - `CloseAllHandles`: Ensures all file streams (Excel/PowerPoint) are properly disposed of.

---

## State Persistence

Workflow state is managed through the `GeneratingContext` class.

### SQLite Persistence
- **Storage**: State is serialized to a local SQLite database (`Workflows.db`).
- **Resilience**: If the sidecar process crashes, the workflow engine can reload the context and resume execution from the last successful step boundary.

### [JsonIgnore] Strategy
Not all data can be serialized (e.g., file handles, loggers, large byte arrays).
- **Transient Fields**: These are marked with `[Newtonsoft.Json.JsonIgnore]`.
- **Lazy Re-opening**: Utilities like `GetOrOpenWorkbook()` check if a handle is null and reopen it using the identifiers stored in the context, allowing seamless resumption after a crash.

---

## Progress Observation

The workflow host publishes lifecycle events (e.g., `WorkflowStarted`, `StepCompleted`).
- **Event Bus**: The `GeneratingEventBus` captures these events.
- **IPC Notification**: The `WorkflowProgressObserver` forwards these events as JSON-RPC notifications to the frontend, providing real-time UI feedback.
