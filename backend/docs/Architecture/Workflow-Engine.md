# Workflow Engine: Generation Pipeline

SlideGenerator uses **WorkflowCore** to orchestrate the complex process of generating PowerPoint presentations. This
document explains the execution model, phase boundaries, and state persistence.

## Execution Model: Phase-Sequential, Item-Parallel

The generation process is divided into preparation steps followed by three distinct phases. While items within a phase
can process in parallel via `.ForEach()`, all items must complete their current phase before the workflow progresses to
the next one.

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
- **Iterators**: `.ForEach(data => data.ValidWorksheets.Values)` for extraction, then
  `.ForEach(data => data.ImageContexts)` for image work.
- **Key Steps**:
    - `ExtractData`: Reads Excel rows and maps them to `SlideContext` and `ImageContext`.
    - `CollectImage`: Resolves and fetches assets via `ICloudResolver` + `ICloudClient`.
    - `EditImage`: Performs ROI-based cropping and resizing through MagickImage.
- **Asset Deduplication**: During Phase B the asset-deduplication path in the Coordinator ensures that identical source
  images with the same edit parameters are only processed once. Secondary requests create **hard links** (via
  `SlideGenerator.Utilities/HardLink`) to the primary result, saving significant CPU and I/O.
- **Throttling**: All resource-heavy steps acquire a gate from the `GateLocker` — `DownloadImage` for `CollectImage`,
  `EditImage` for `EditImage`.

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
- **Resilience**: If the sidecar process crashes, the workflow engine can reload the context and resume execution from
  the last successful step boundary.

### [JsonIgnore] Strategy

Not all data can be serialized (e.g., file handles, loggers, large byte arrays).

- **Transient Fields**: Marked with `[Newtonsoft.Json.JsonIgnore]`.
- **Lazy Re-opening**: Utilities like `GetOrOpenWorkbook()` / `GetOrOpenPresentation()` / `GetOrOpenOutput()` (in
  `Application/Utilities.cs`) check if a handle is null and reopen it using identifiers stored in the context, allowing
  seamless resumption after a crash.

---

## Progress Observation

Progress is scoped to 3 levels, each its own record published through `IEventBus`:

- **`RequestProgress`** (`RequestId`, `Phase`): `PreparationStarted` (published by `Service.CreateAsync` right before
  it spawns jobs) → `ProcessingStarted` → `Completed` (both inferred by `ProgressCoalescer` once every job of the
  request has left `Pending` / reached a terminal status).
- **`JobProgress`** (`RequestId`, `JobId`, `Status`): published on spawn (`Pending`), on WorkflowCore lifecycle events
  (`Running`/`Complete`/`Error`, via `Service.HandleLifeCycleEvent`), and on Pause/Stop/Resume
  (`Service.FanOutAsync`).
- **`RowProgress`** (`RequestId`, `JobId`, `RowIndex`, `Status`, `Stage`, `Note`): published exclusively from inside
  `GenerateJobStep`'s per-row loop via the `StepProgress` helper — the only step-level Progress producer left; there
  is no generic "step completed" event anymore.

**Coalescing & delivery**: `ProgressCoalescer` (in `SlideGenerator.Stdio`) buffers dirty Request/Job/Row state
(last-write-wins per key) and log lines (append-only, never dropped) separately, then every ~1s upserts the dirty
Progress into a `Studio.db` SQLite database (so a late-attaching client — or `generator.active.list` itself — sees
full current state, not just future events) and forwards each non-empty batch as a JSON-RPC notification:
`progress/request`, `progress/jobs`, `progress/rows`, `log/entries`.

Log lines still land in the same per-request `.log` file as before, just with a parseable Request/Job/Row scope path
on every line (via `Serilog.Context.LogContext.PushProperty`), so `Summary` can read the file back and filter by
scope on demand instead of needing a second log store.
