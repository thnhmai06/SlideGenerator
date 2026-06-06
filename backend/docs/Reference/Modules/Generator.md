# Generator Module

The **SlideGenerator.Generator** module is the central orchestration engine. It uses **WorkflowCore** to execute the
generation pipeline.

## Responsibility

- Coordinates the multi-stage generating workflow.
- Manages state persistence via SQLite (`Workflows.db`).
- Handles error resilience and partial success.

## Pipeline Stages

`GeneratingWorkflow.Build()` chains the following steps:

```
LoadRecipeSummary → PreflightCleanup
    ↓
Phase A: ValidateRequest → CreateTemplate            (.ForEach(ValidationItems))
    ↓
Phase B: ExtractData                                 (.ForEach(ValidWorksheets))
         CollectImage → EditImage                    (.ForEach(ImageContexts))
    ↓
Phase C: ReplaceSlideData                            (.ForEach(SlideContexts))
         CloseAllHandles
```

Phase boundaries are enforced with `ExecutionResult.Next()` barriers so all items in one phase finish before the next
begins.

### Preparation steps

- `LoadRecipeSummary`: Loads the active `RecipeSummary` (from `SlideGenerator.Summarization`) into the workflow context.
- `PreflightCleanup`: Removes any leftover temp folders or output files from a previous run of the same workflow.

### Phase A — Validation & Setup

- `ValidateRequest`: Opens each workbook via Syncfusion to confirm the requested sheets exist.
- `CreateTemplate`: Copies the template `.pptx` to the output path and isolates the requested slides.

### Phase B — Resource Preparation

- `ExtractData`: Reads Excel cells and maps them to `SlideContext` and `ImageContext`.
- `CollectImage`: Resolves the source URI via `ICloudResolver` and downloads via `ICloudClient` (gated by
  `GateType.DownloadImage`).
- `EditImage`: Performs ROI computation (`RoiResolver`) and MagickImage crop/resize (gated by `GateType.EditImage`).

### Phase C — Assembly & Cleanup

- `ReplaceSlideData`: Injects text and processed images into each output slide shape.
- `CloseAllHandles`: Releases all Syncfusion `IWorkbook` / `IPresentation` handles.

## State Management

The workflow state is the `GeneratingContext` class.

- **Persistence**: WorkflowCore persists it to `Workflows.db` (SQLite) via Newtonsoft.Json.
- **Transient fields**: File handles, `IAppLogger`, etc. carry `[Newtonsoft.Json.JsonIgnore]` and are lazily reopened
  after resume via `GetOrOpenWorkbook`/`GetOrOpenPresentation`/`GetOrOpenOutput` extensions in
  `Application/Utilities.cs`.
- **Error capture**: Each context class has a `ConcurrentDictionary<string, Exception> Errors`. Steps catch exceptions
  and record them rather than aborting the workflow, enabling partial success.

## Middleware

Registered in `AddGeneratorServices`:

- **`GeneratingLoggerMiddleware`**: Lazily initializes `GeneratingContext.Logger` before each step using
  `WorkflowLogPath`/`WorkflowScope` (survives persistence resume).
- **`GeneratingProgressMiddleware`**: Publishes `GeneratingEvent.StepCompleted` with the resolved `GeneratingPhase`
  after each step.

## Events

`GeneratingService` subscribes to `IWorkflowHost.OnLifeCycleEvent` and republishes lifecycle events (
`WorkflowCompleted`, `WorkflowError`, `WorkflowStarted`, `WorkflowSuspended`, `WorkflowResumed`, `WorkflowTerminated`)
through `IGeneratingEventBus`. The Ipc sidecar's `WorkflowProgressObserver` forwards these events to the frontend as
`workflow/progress` JSON-RPC notifications.

## Enums

Defined in the file where each concept lives:

- `GeneratingPhase` — in `Application/Workflows/GeneratingWorkflow.cs`
- `GeneratingEvent` — in `Application/Abstractions/IGeneratingEventBus.cs`
- `GeneratingStatus` — in `Domain/Models/GeneratingStatus.cs`
