# Workflow Engine: Generation Pipeline

SlideGenerator uses **WorkflowCore** to orchestrate the complex process of generating PowerPoint presentations. This document explains the execution model, phase boundaries, and state persistence.

## Execution Model: Phase-Sequential, Item-Parallel

The generation process is divided into preparation steps followed by three distinct phases. While items within a phase can process in parallel via `.ForEach()`, all items must complete their current phase before the workflow progresses to the next one.

### Preparation
Two preparatory steps run before Phase A:
- `LoadRecipeSummary`: Loads the active `RecipeSummary` (from `SlideGenerator.Summarization`) into the workflow context.
- `PreflightCleanup`: Removes leftover temp folders or partial output from a previous run of the same workflow.

### Phase A: Validation & Template Setup
- **Goal**: Ensure the request is valid and prepare the output file.
- **Iterator**: `.ForEach(data => data.ValidationItems)`.
- **Key Steps**:
  - `ValidateRequest`: Opens the source workbook through Syncfusion and confirms the required sheets exist.
  - `CreateTemplate`: Copies the template `.pptx` to the output path and isolates the requested slide(s).
- **Barrier**: The workflow waits here until the output file is ready for further processing.

### Phase B: Resource Preparation
- **Goal**: Gather all data and process external assets.
- **Iterators**: `.ForEach(data => data.ValidWorksheets.Values)` for extraction, then `.ForEach(data => data.ImageContexts)` for image work.
- **Key Steps**:
  - `ExtractData`: Reads Excel rows and maps them to `SlideContext` and `ImageContext`.
  - `CollectImage`: Resolves and fetches assets via `ICloudResolver` + `ICloudClient`.
  - `EditImage`: Performs ROI-based cropping and resizing through MagickImage.
- **Asset Deduplication**: During Phase B the asset-deduplication path in the Coordinator ensures that identical source images with the same edit parameters are only processed once. Secondary requests create **hard links** (via `SlideGenerator.Utilities/HardLink`) to the primary result, saving significant CPU and I/O.
- **Throttling**: All resource-heavy steps acquire a gate from the `GateLocker` — `DownloadImage` for `CollectImage`, `EditImage` for `EditImage`.

### Phase C: Assembly & Cleanup
- **Goal**: Finalize the document and release resources.
- **Iterator**: `.ForEach(data => data.SlideContexts)` followed by a final `CloseAllHandles`.
- **Key Steps**:
  - `ReplaceSlideData`: Injects text and processed images into the slide shapes.
  - `CloseAllHandles`: Ensures all Syncfusion `IWorkbook` / `IPresentation` handles are properly disposed.

---

## State Persistence

Workflow state is managed through the `GeneratingContext` class.

### SQLite Persistence
- **Storage**: State is serialized to a local SQLite database (`Workflows.db` under `%LOCALAPPDATA%/SlideGenerator/`).
- **Resilience**: If the sidecar process crashes, the workflow engine can reload the context and resume execution from the last successful step boundary.

### [JsonIgnore] Strategy
Not all data can be serialized (e.g., file handles, loggers, large byte arrays).
- **Transient Fields**: Marked with `[Newtonsoft.Json.JsonIgnore]`.
- **Lazy Re-opening**: Utilities like `GetOrOpenWorkbook()` / `GetOrOpenPresentation()` / `GetOrOpenOutput()` (in `Application/Utilities.cs`) check if a handle is null and reopen it using identifiers stored in the context, allowing seamless resumption after a crash.

---

## Progress Observation

The workflow host publishes lifecycle events (e.g., `WorkflowStarted`, `WorkflowCompleted`, `WorkflowError`).
- **Step Events**: `GeneratingProgressMiddleware` publishes `GeneratingEvent.StepCompleted` after every step.
- **Event Bus**: `GeneratingEventBus` aggregates step events plus lifecycle events forwarded by `GeneratingService`.
- **IPC Notification**: The `WorkflowProgressObserver` in `SlideGenerator.Ipc` forwards these events as JSON-RPC `workflow/progress` notifications to the frontend, providing real-time UI feedback.
