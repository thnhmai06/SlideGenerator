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

Registered in `AddGeneratorServices` — `Middleware` (`Infrastructure/Middleware/Middleware.cs`) is the only step
middleware. It lazily initializes `JobContext.Transient.LoggerFactory` before each step, supplying the module's
Request/Job/Row scope property names and a callback that forwards every log line to `ILogNotifier`. There is no
separate progress-publishing middleware — step-level "progress" is no longer a concept; see **Progress** below.

## Progress

Progress is 3 separate scoped records (`RequestProgress`/`JobProgress`/`RowProgress`, in
`Domain/Models/Data/Progress.cs`), each published via a matching `IEventBus.Publish` overload:

- `RequestProgress` (`RequestId`, `Phase: RequestPhase`) — `PreparationStarted` published by `Service.CreateAsync`;
  `ProcessingStarted`/`Completed` inferred by the Stdio module's `ProgressCoalescer` from the `JobProgress` stream.
- `JobProgress` (`RequestId`, `JobId`, `Status`) — published on spawn (`Pending`), by `Service.HandleLifeCycleEvent`
  (`Running`/`Complete`/`Error`), and by `Service.FanOutAsync` (Pause/Stop/Resume).
- `RowProgress` (`RequestId`, `JobId`, `RowIndex`, `Status: RowStatus`, `Stage: RowStage`, `Note`) — published
  exclusively from `GenerateJobStep`'s per-row loop via `StepProgress` (`Application/StepProgress.cs`), a
  per-step-execution helper exposing `ReportRow(...)`/`SeedRows(...)`.

`IEventBus` (`Application/Abstractions/IEventBus.cs`) is implemented by the Stdio module's `GeneratingEventBus`
(`dep-interface-ownership`: Generator owns the interface as publisher, Stdio owns the implementation as the module
that actually forwards to JSON-RPC). `IStudioRepository`/`StudioRepository` persist current-state Progress
(UPSERT) to a `Studio.db` SQLite database, so `Service.ListActiveAsync`/`ListCompletedAsync` can answer with full
current Row/Phase/Status even for a client that only just started polling. `Summary`/`JobSummary`/`RowSummary`
(`Domain/Models/Data/Summary.cs`) also each carry a `Logs: IReadOnlyList<LogEntry>`, populated on every call by
`ILogFileReader`/`LogFileReader` reading the job's `.log` file straight off disk and filtering by scope path
(deliberately not RAM-cached — see the Logging module doc).

## Enums

Defined in `Domain/Models/Enum/`:

- `Status` — job/request execution status (`Pending`/`Running`/`Complete`/`Paused`/`Cancelled`/`Error`).
- `RowStatus` — `Waiting`/`Processing`/`Done`/`Error`.
- `RowStage` — `None`/`Downloading`/`CroppingImage`/`SavingOutput`.

`RequestPhase` is defined alongside `RequestProgress` in `Domain/Models/Data/Progress.cs`.
