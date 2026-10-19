# AGENTS.md

Guidance for AI coding agents (Claude Code, Codex, etc.) working in this repository.

## Basic Rules

### 1. Think Before Coding

**Don't assume it. Don't hide confusion. Surface tradeoffs.**

Before implementing:

- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them – don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines, and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:

- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it – don't delete it.

When your changes create orphans:

- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should be traced directly to the user's request.

### 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:

- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multistep tasks, state a brief plan:

```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

## Build & Test Commands

```bash
# Build
dotnet build SlideGenerator.slnx

# Build release
dotnet build SlideGenerator.slnx -c Release

# Clean
dotnet clean SlideGenerator.slnx

# Run the desktop app
dotnet run --project src/SlideGenerator.Desktop/SlideGenerator.Desktop.csproj

# Run all tests
dotnet test SlideGenerator.slnx

# Run tests for one project
dotnet test tests/SlideGenerator.Workflows.Tests/SlideGenerator.Workflows.Tests.csproj
```

- SDK: .NET 10.0 (`global.json` pins `10.0.100`, `rollForward: latestMajor`, prerelease allowed) and the
  `Microsoft.Testing.Platform` test runner. The solution uses the XML-based `SlideGenerator.slnx` (no `.sln`).
- CI (`.github/workflows/ci.yml`) restores and builds the solution, then runs `dotnet test` **per test project**
  (`--report-trx`) on 6 OS/arch runners.
- `Taskfile.yml`'s `run` task still points at the removed `SlideGenerator.Stdio` project — use the `dotnet run` command
  above instead.
- **Syncfusion license**: resolved at **build time**, not runtime. The `GenerateLicenseKey` MSBuild target in
  `SlideGenerator.Document.csproj` reads `SYNCFUSION_LICENSE_KEY` from the repo-root `.env` (copy `.env.example`; takes
  precedence) or from the environment variable, and bakes it (XOR-obfuscated) into a generated `SyncfusionLicense`
  class. `AddDocumentServices()` registers it at startup. Changing the key therefore needs a rebuild of `Document`. A
  missing key becomes `empty` → no license is registered (evaluation behavior; tests that need it skip).
- **GitHub Packages**: `SlideGenerator.Image` depends on per-platform `SlideGenerator.OpenCvSharp4.runtime.*` packages
  hosted at `nuget.pkg.github.com/thnhmai06`. `nuget.config` reads credentials from `%GITHUB_USERNAME%` and
  `%GITHUB_TOKEN%` — set both before restoring.

## Solution Layout

```
├── src/                                — 11 modules (all tracked in SlideGenerator.slnx)
│   ├── SlideGenerator.Utilities/        — Sha256, Naming, TextNormalization, generic Pool<T>, FileAccessHelper
│   ├── SlideGenerator.Settings/         — Immutable/ (NameAndPaths), Mutable/ (Setting + SettingManager), Database/ (DbUp)
│   ├── SlideGenerator.Cloud/            — CloudClient + Resolvers/ (Google Drive only)
│   ├── SlideGenerator.Logging/          — FileLogging/, Formats/ (file + console formatters, ScopeNotifyingSink)
│   ├── SlideGenerator.Document/         — Workbooks/, Presentations/, Template/ (Syncfusion + Mustache)
│   ├── SlideGenerator.Image/            — FaceDetection/, Cropping/, Loading/
│   ├── SlideGenerator.Workflows/        — Engine/ (generic Workflow Engine, zero project references)
│   ├── SlideGenerator.Recipe/           — Models/, Services/ (repository + export/import), Formats/
│   ├── SlideGenerator.Summarizer/       — Workbooks/, Presentations/, SummarizationService
│   ├── SlideGenerator.Generator/        — Workflows/ (workflow level), Jobs/ (job level), Persistence/, Progress/, root Service + DTOs
│   └── SlideGenerator.Desktop/          — Avalonia host (MVVM, Features/, Services/, Shell/, Bootstrap/)
├── tests/                              — one test project per module (mirrors src, standalone; no Summarizer.Tests)
│   └── fixtures/                       — shared test data
├── scripts/ApplyCopyright/             — build-time tool (in the slnx, not a module)
├── plans/                              — backend-remediation.md (open plan) + domain-reference.md (V2 domain reference); not source of truth for code
└── .github/                            — workflows (ci, codeql, qodana, sponsors), codeql config, CODEOWNERS
```

The host is `SlideGenerator.Desktop`, a single in-process Avalonia application. **There is no IPC layer**: the former
`SlideGenerator.Stdio` JSON-RPC sidecar (and the Tauri frontend it served) no longer exist. ViewModels depend on
`SlideGenerator.Generator.IService` and friends directly.

Modules removed in the past and not to be reintroduced by accident: WorkflowCore (workflow execution is plain `Task`-based
code, on our own engine), `Cryptography` (→ `Utilities/Sha256.cs`), `Coordinator` (its gates and performance calibration were deleted;
`Pool<T>` → `Utilities/Pool.cs`), `Acquisition`, `Collector`, and the Recipe `Node`/`Edge` graph model.
`tests/SlideGenerator.Acquisition.Tests/` may exist on disk as an orphan (no source, not in the slnx) — leave it alone
unless asked.

## Architecture: Modular Monolith, In-Process

SlideGenerator automates PowerPoint generation from Excel data and templates. Each request runs as one `Workflow`
(a plain in-process `Task`, managed by a generic Workflow Engine) that fans out into concurrent `Job`s, driven from the
Avalonia UI through the `IService` facade.

### Module Map

```
Foundation
├── Utilities     - string normalization, Sha256, Pool<T>, helpers
├── Cloud         - URI inspect/download + provider resolvers (Google Drive only; OneDrive/SharePoint not implemented)
├── Logging       - Serilog file/console formatters, IFileLoggerFactory, scope-notifying sink
├── Document      - Syncfusion Excel/PowerPoint abstractions + Mustache text templating
├── Image         - NetVips loading, ROI cropping, OpenCV YuNet face detection
└── Workflows     - Generic Workflow Engine: runs any IWorkflowRunner<TState> — pause/resume/stop and
                    crash-resume per workflow — with no knowledge of what a workflow does or contains

Domain
├── Settings      - JSON user settings (ISettingProvider/ISettingManager) + the shared SQLite schema migrator
└── Recipe        - Recipe CRUD (SQLite) + *.recipe export/import packages; a Recipe is a flat list of Mappings

Application
├── Generator     - IService facade, WorkflowController (adapter over the Workflow Engine),
│                   SlideGenerationWorkflow (runs a request's jobs), JobExecutor (one job's 4 phases),
│                   Workflows/Jobs persistence, progress + log contracts
└── Summarizer    - Workbook/presentation metadata scanner (used by the recipe editor)

Host
└── Desktop       - Avalonia UI host: DI wiring, ViewModels, progress hub, localization, theming
```

### Dependency Rules

- Dependencies flow downward only (Foundation → Domain → Application → Host) — no circular references.
- `SlideGenerator.Workflows` has zero project references.
- Each module has a root `Registration.cs` as its DI entry point.
- `SlideGenerator.Desktop` is the executable that wires all modules (`App.axaml.cs`).

### DI Registration Methods

| Module        | Extension Method                                                                                           |
|---------------|------------------------------------------------------------------------------------------------------------|
| Logging       | `AddLoggingServices()`                                                                                     |
| Settings      | `AddSettingsServices()`                                                                                    |
| Cloud         | `AddCloudServices()`                                                                                       |
| Document      | `AddDocumentServices()` (also registers the Syncfusion license)                                            |
| Image         | `AddImageServices()`                                                                                       |
| Recipe        | `AddRecipeServices()`                                                                                      |
| Summarizer    | `AddSummarizationServices()`                                                                               |
| Workflows     | `AddWorkflowEngine<TKey,TState>()` — generic, called from `AddGeneratorServices()`, not from the host directly  |
| Generator     | `AddGeneratorServices()`                                                                                   |
| Desktop       | `AddDesktopServices()` (event bus, progress hub, localization, theme, dialogs, ViewModels)                 |

`Registration.cs` in Cloud, Generator, Image and Logging uses C# 14 **extension member syntax**:

```csharp
extension(IServiceCollection services)
{
    public IServiceCollection AddFooServices() { ... }
}
```

Desktop, Document, Workflows, Recipe, Settings and Summarizer still use a classic `this IServiceCollection` extension
method — match the file you are editing.

### Startup / shutdown sequence

`Program.Main` (`SlideGenerator.Desktop/Program.cs`):

1. `VelopackApp.Build().Run()` (install/update hooks, may exit the process).
2. Build bootstrap configuration (`appsettings.json`, env vars, command line).
3. Single-instance guard (`SingleInstanceLock`).
4. `BootstrapSystemLogger` — Serilog file + **stderr** console sink, sets static `Log.Logger`. Not passed through DI.
5. `DatabaseMigrator.Migrate(...)` (after creating `DataFolder.FolderPath`).
6. Register process-wide exception handlers, then start Avalonia's classic desktop lifetime.

`App.InitializeAsync` then: `NameAndPaths.InitializeDirectories()` → **resolve `IProgressHub` first** → load settings →
apply theme/language → `IService.InitializeAsync()` (crash-resume) → fire-and-forget update check.
`IProgressHub` must be constructed (subscribed) **before** `IService.InitializeAsync()`: crash-resumed workflows are scheduled
immediately and a late subscriber would miss their first events.
On shutdown (`ShutdownRequested`), `IService.ShutdownAsync()` runs, then settings are saved.

## Request → Workflow → Job flow

One **Request** (what the user submits in the Run dialog) is executed by one **Workflow**, which contains one **Job**
per worksheet/presentation pair — a Job produces exactly one output `.pptx`.

`IService` (`Generator/Service.cs`) is the only surface the UI uses:

- `PreviewAsync(request)` — computes the job list and per-job `ConflictKind` without spawning anything.
- `CreateAsync(request)` — reads the recipe, flattens it to jobs, validates output paths, persists the
  `WorkflowRecord` (status `Pending`), then makes a **single** `IWorkflowController.StartWorkflowAsync` call that
  persists one `Pending` job row per job and starts the workflow. Returns immediately; not throttled by
  `MaxConcurrentJobs`.
- `StopAsync` / `PauseAsync` / `ResumeAsync(workflowId)` — act on the **whole workflow** (one controller call, never
  one per job) when its current status makes it eligible, returning `PartialResult(1,0)` or `PartialResult(0,1)`
  (skipped). `StopAllAsync` / `PauseAllAsync` are bulk variants. There is no per-job control.
- `ListActiveAsync` / `ListCompletedAsync(includeLogs)` — `IReadOnlyDictionary<string, Summary>` keyed by workflow id.
  Pass `includeLogs: false` for list views (reading/parsing the `.log` file is the expensive part).
- `DeleteAsync` (stops the workflow first if still active) / `DeleteAllCompletedAsync`.

**Identity**: `workflowId` is a GUID minted in `CreateAsync` (it is also the id that groups the workflow's jobs);
`JobId` is a plain 0-based `int` ordinal within the workflow.

**Building jobs** (`Service.BuildJobs`): one `JobSpecification` per (mapping × worksheet source) pair, every value
already resolved (workbook path, sheet, template presentation + slide index, instructions, `UsedColumns`, `RowFilter`).
Output path = `{SaveFolder}/{workbook name}/{sanitized sheet name}{ext}`. A job needs nothing else to run or resume — no
recipe or workbook lookup — so editing/deleting a recipe never affects existing workflows.

**Conflict guard**: no recipe-level lock; multiple active workflows for the same recipe are allowed. Instead
`CreateAsync` throws if two jobs of the request share an output path (`FindDuplicateOutputPath`) or if a job's output
path equals one claimed by an already Pending/Running/Paused job of another workflow (`GetActiveOutputPathsAsync`).

**Status** is two-level. A workflow stores its own lifecycle status (`Workflows.Status`: Pending / Running / Paused /
Complete / Cancelled / Error — the `JobStatus` enum is reused), assigned only by `GeneratorWorkflowObserver`. A job's
status (`Jobs.Status`) is assigned only by `SlideGenerationWorkflow`. The status shown for a request is **derived**
(`Service.DeriveStatus`): any Running/Pending job → `Running`; else any Paused → `Paused`; else all Cancelled →
`Cancelled`; else `Complete`; and a workflow stored as `Paused` whose jobs still derive as running is `Paused` (pause acts
on the workflow without rewriting job rows). A request whose jobs ended in `Error` therefore reports `Complete`; error
status exists per job (`JobSummary.JobStatus`). `Summary.Phase` (`WorkflowPhase`) is recomputed on every list call
(`DeriveWorkflowPhase`).

## Workflow Engine (`SlideGenerator.Workflows`)

Fully generic and domain-free: no `Job*`/`Workflow*` Generator types, Syncfusion, `Settings` or SQLite references. It runs
one unit — a *workflow* — and knows nothing about jobs.

- `IWorkflowEngine<TKey,TState>` / `WorkflowEngine<TKey,TState>` — `InitializeAsync(resumeSource)`, `ShutdownAsync`,
  `StartWorkflowAsync`, `PauseAsync`, `ResumeAsync`, `StopAsync`. Each workflow runs on its own `Task.Run`, tracked in an
  in-memory `ConcurrentDictionary`. Starting a duplicate key throws. The engine imposes **no** concurrency limit.
- `IWorkflowRunner<TState>.RunAsync(initial, IWorkflowContext<TState>, ct)` — the domain half. `IWorkflowContext` offers
  `IsResume`, `ReportAsync(state, durable, ct)` and `CheckpointAsync(ct)`.
- `IWorkflowObserver<TKey,TState>` — `OnProgressAsync`, `OnPausedAsync`, `OnResumedAsync`, `OnTerminalAsync`. The engine
  never inspects `TState`; every transition is forwarded here. The "starting execution" tick is published before the
  runner starts.
- `IWorkflowResumeSource<TKey,TState>` → `PendingWorkflow` list for crash recovery.

**Pause/stop** use `PauseGate` (one per running workflow, engine-owned): `Pause()`/`Resume()` toggle a swappable
`TaskCompletionSource`; the runner calls `CheckpointAsync` before each row, so pausing is cooperative and happens
**between rows**, never mid-row. Every job of a workflow shares the workflow's gate. `StopAsync` cancels the workflow's CTS
*and* resumes the gate so a paused workflow observes the cancellation immediately. `ShutdownAsync` does the same for every
workflow, then awaits them all.

**State ownership**: `RunAsync` is the only place that creates a new `TState`. On normal completion its *return value*
is the terminal state; on cancel/fault the terminal state is the last one passed to `ReportAsync` (cached per running
workflow). Outcome mapping: `Completed` / `Cancelled` (`OperationCanceledException` while cancellation was requested) /
`Faulted` (anything else, logged).

## Slide generation (`SlideGenerator.Generator`)

**Concurrency (`MaxConcurrentJobs`)** is the *only* concurrency/RAM control (default 5, `Setting.PerformanceSetting`).
It caps **jobs across all workflows**: `JobGate` (`Jobs/JobGate.cs`) wraps a `SemaphoreSlim`. On every `AcquireAsync` it
re-reads the limit (values below 1 are treated as 1) and swaps in a **new** semaphore only when the value changed;
each lease releases the exact instance it acquired from, so a swap mid-job cannot cause `SemaphoreFullException`. A paused
workflow keeps its running jobs' slots (they hold their workbook/presentation open). There are no per-operation gates
(download/edit/save). A settings change applies to jobs that start afterwards.

`WorkflowController` (`Workflows/WorkflowController.cs`, `IWorkflowController`) is a thin adapter: `StartWorkflowAsync`
persists one `Pending` job row per job and **flushes it immediately** (so conflict checks see them right away), wraps
`SlideGenerationWorkflow` in a `LoggingWorkflowRunner`, and hands it to the engine. `ShutdownAsync` shuts the engine down
*then* flushes the jobs repository.

`SlideGenerationWorkflow : IWorkflowRunner<WorkflowSnapshot>` (`Workflows/`) loads the workflow's unfinished jobs
(Pending/Running/Paused rows) and runs them concurrently through `JobExecutor`, each throttled by `JobGate` and each
checkpointing through the workflow's gate (also once right after acquiring a slot, so a job queued while paused does not
start). It is the only place a job's `JobStatus` is assigned: reports → `Running`, finish → `Complete`, an exception →
`Error` (the siblings keep going), cancellation → `Cancelled` (rethrown so the engine reports the workflow cancelled).
It also deletes the job's temp download folder when the job finishes.

`JobExecutor` (`Jobs/JobExecutor.cs`) runs one job's four phases, each with `ct.ThrowIfCancellationRequested()` +
`context.CheckpointAsync(ct)` before every row and `output.Save()` after it:

```
CreatingOutput → CreatingSlides → FillingText → FillingImages (→ Done)
```

- **A — output**: copy the template to the output path and strip its slides (or reopen the existing output on resume).
  `PreflightCleanup` runs only when `!context.IsResume`.
- **B — slides**: append one cloned template slide per data row.
- **C — text**: `BuildRowTextValues` + `TextComposer` fill placeholders; for each `TextInstruction` the first non-empty
  cell across its columns wins.
- **D — images**: inspect every distinct image source once up front (`ICloudClient.InspectAsync`, with retry/backoff from
  `Network.Retry`); then per row resolve a local file, or download (per-job cache, `MaxDownloadBytes` cap) and crop with
  `ISmartCropper`, falling back to `FallbackImagePath`. A failed crop is logged and skipped, not fatal.

The template presentation handle must stay open for the whole run (Syncfusion's `ISlide.Clone()` still resolves
layout/master through its source). `IJobContext` (internal, in `JobExecutor.cs`) is what a job sees of its workflow:
`IsResume`, a logger, `ReportAsync(JobSnapshot, durable)` and `CheckpointAsync`.

`JobPhase` has 5 values (`Done` is the terminal stamp, never "run"). `JobSnapshot` = `JobSpecification` + `JobStatus` /
`Phase` / `CurrentIndex` / `Timestamp` / `TotalRows` (nullable; known once the job starts, drives the UI's determinate
progress bar). `WorkflowSnapshot` = `WorkflowId` + `Status` + `Timestamp` (the engine's `TState`).

**`GeneratorWorkflowObserver`** maps engine callbacks onto the workflow's own status: `OnProgress` → `Running`,
`OnPaused` → `Paused`, `OnResumed` → `Running`, `OnTerminal` → `Complete`/`Cancelled`/`Error`; each persists via
`IWorkflowsRepository.UpdateStatusAsync` then `eventBus.Publish(WorkflowSnapshot)`.

**Crash-resume**: `GeneratorResumeSource` loads every Pending/Running/Paused workflow row
(`IWorkflowsRepository.GetNonTerminalAsync`) and rebuilds a `LoggingWorkflowRunner` using the persisted `LogPath` (so the
resumed run keeps appending to the same `.log`). When it runs, `SlideGenerationWorkflow` re-reads the workflow's
unfinished job rows: `Phase` + `CurrentIndex` say where each job continues; the embedded `JobSpecification` says what to
do. A workflow that was `Paused` before the crash resumes as plain `Running`.

Any exception during a job's rows propagates and fails **that job only** (→ `Error`); rows are not individually try/caught.

## Progress, logs and persistence

**Event flow** (all in-process; there is no JSON-RPC notification layer any more):

```
SlideGenerationWorkflow ─Publish(JobSnapshot)──► IEventBus ─┐
JobExecutor ────────────ReportRow──────────────► IEventBus ─┼─► GeneratingEventBus (Desktop) ─► ProgressHub ─► ViewModels
GeneratorWorkflowObserver ─Publish(WorkflowSnapshot)► IEventBus ┘
LoggingWorkflowRunner ─► ILogNotifier ─► LogNotifier (Desktop) ─────────────────────────────────► ProgressHub
```

- Contracts (`IEventBus`, `ILogNotifier`, `RowProgress`, `RowStatus`, `RowStage`, `WorkflowPhase`) live in
  `Generator/Progress/`; Generator depends only on these interfaces. The concrete `GeneratingEventBus` / `LogNotifier`
  live in `Desktop/Services/Progress/` and are registered both as concrete types and as the interfaces (so `ProgressHub`
  can subscribe to the C# events).
- `ProgressHub` (`IProgressHub`) drains every **250 ms** on the UI `DispatcherTimer`: workflows coalesce by
  `WorkflowId`, jobs by `(WorkflowId, JobId)`, rows by `(WorkflowId, JobId, RowIndex)` (last write wins); logs are
  append-only (never dropped). `WorkflowRunViewModel` keeps the workflow-level pause flag from `Workflows` snapshots and
  derives its display status from that plus its jobs.
- `RowProgress` is **never persisted** — live only.

**Logging scopes**: log lines go to one `.log` file per workflow (`WorkflowRecord.LogPath`, under `Logs/Workflows/`,
shared by all of its jobs). `LoggingWorkflowRunner` opens **one** `IFileLoggerFactory.CreateFile(...)` scope per workflow
with ordered property names `["WorkflowId", "JobId", "RowIndex"]` and pushes `WorkflowId` on `LogContext`;
`SlideGenerationWorkflow` pushes `JobId` per job and the phases push `RowIndex` per row. `FileLogFormatter` writes the
scope path into each line; `Service.ToSummaryAsync` reads it back via `ILogFileReader`/`LogFileReader` (regex parse) and
filters by scope-path prefix — logs are deliberately not cached in RAM. `Logging` itself has no notion of what a scope
means.

**Persistence** — a **single** SQLite database `Data/Data.db` shared by every module (tables `Recipes`, `Workflows`,
`Jobs`). Schema is created centrally by `Settings.Database.DatabaseMigrator.Migrate` (DbUp, embedded scripts in
`Settings/Database/Scripts/`; currently the single consolidated `001_2.0.0.sql`). No repository creates its own tables.
Each repository opens/closes a short-lived connection per operation. **Pre-release, schema changes are merged into
`001`** (an existing dev `Data.db` must be deleted); once a release ships, add numbered scripts instead.

- `Workflows`: one row per workflow — `WorkflowId` (PK), the originating `Request` fields (`RecipeId`, `Name`,
  `OutputType`, `SaveFolder`, `AllowLocalPaths`), `LogPath`, `CreatedAt`, and `Status`. Written once and only `Status`
  changes afterwards, so `WorkflowsRepository` does not buffer. `RecipeId` is informational only (not used on resume).
- `Jobs`: one explicit column per `JobSpecification` field, except `UsedColumnsJson` / `TextInstructionsJson` /
  `ImageInstructionsJson` (JSON text, serialized with `JobSpecificationJson` options). `RowFilter` →
  `RowFilterType` + 4 nullable scalars. Plus `Status`, `Phase`, `CurrentIndex`, `TotalRows`, `Timestamp`. Composite PK
  `(WorkflowId, JobId)`.
- `JobsRepository` extends `BufferedRepository<TKey,TValue>`: `Enqueue` is coalesced last-write-wins, a ~1 s
  `PeriodicTimer` drains the dirty set and upserts one batch per tick, then raises `Flushed`; `FlushAsync` forces it.
- `Service.DeleteAsync` / `DeleteAllCompletedAsync` delete from both `Jobs` and `Workflows`.

## Recipes

`SlideGenerator.Recipe`'s `Recipe` is a **flat list of `Mapping`s** — no graph, `Node`, `Edge` or id lookup:

```csharp
public sealed record Recipe(IReadOnlyList<Mapping> Mappings);

public sealed record Mapping(
    IReadOnlyList<WorksheetSource> Sources,
    PresentationSource Template,                 // Presentation + Slide identifiers
    IReadOnlyList<TextInstruction> TextInstructions,
    IReadOnlyList<ImageInstruction> ImageInstructions);

public sealed record WorksheetSource(
    WorkbookIdentifier Workbook,
    WorksheetIdentifier Worksheet,
    IReadOnlySet<ColumnIdentifier>? UsedColumns = null,
    RowFilter? RowFilter = null);
```

One `Mapping` = one template slide + its instructions, fed by one or more worksheets that share them. `RowFilter` has a
small closed set of shapes (`AllRowFilter` / `IndexRangeFilter` / `PartitionBlockFilter`). `ImageInstruction` carries
target shapes, source columns, `ImageEditInstruction` (ordered `RoiOption`s) and an optional `FallbackImagePath`.
`RecipeRepository` stores the recipe as JSON; export/import (`RecipePackageService`, partial class split into
`.Export.cs` / `.Import.cs`) works on `*.recipe` zip packages. The import path must normalize a deserialized `Recipe`
whose `Mappings` is `null` (e.g. `recipe.json` of `"{}"`, or a crafted archive) to `[]` — see
`imported with { Mappings = imported.Mappings ?? [] }`.

`SummarizationService` (`SlideGenerator.Summarizer`, synchronous) produces `WorkbookSummary` / `PresentationSummary`
metadata for the Desktop recipe editor (via `ISummaryCache`); it is unrelated to `Service.BuildJobs`.

## Image Processing

`SlideGenerator.Image` uses **NetVips** (`IImage`, implemented by `VipsImage`), loaded via
`IImageLoader.Open(path | byte[])`. The crop pipeline is fully in-memory and the result goes straight into
`IShape.ImageData` — never to a disk file.

- `ISmartCropper.CropAsync(IImage, Size targetSize, params RoiOption[] options)` → `IImage?` — tries each `RoiOption` in
  order (`AnchorOption` via `IAnchorCropper`, `InterestOption` via `IInterestCropper`) and returns the first that
  succeeds; `null` if none do or `targetSize` is invalid.
- `IImage.ToPng()` → `byte[]` is the only place a `byte[]` re-appears, right before `IShape.ImageData = ...`.
- Face detection: `IFaceDetector.DetectAsync` (OpenCV YuNet), pooled via `YuNetPool` (generic `Pool<T>`, bounded by
  `Environment.ProcessorCount` — CPU-bound native work; unrelated to `MaxConcurrentJobs`).
- Always `using` / `await using` for `IImage`.

## Desktop host (Avalonia)

MVVM with CommunityToolkit.Mvvm (`[ObservableProperty]` / `[RelayCommand]`), Semi.Avalonia theme, Velopack updates.
Folders are created on demand when a feature is built — no empty placeholder folders.

```
SlideGenerator.Desktop/
├── App.axaml(.cs)        — builds the Host/DI container, startup/shutdown sequence
├── Program.cs            — entry point (see Startup sequence)
├── Bootstrap/            — SingleInstanceLock, Metadata, UpdateChecker (used only from Program/App startup)
├── Shell/                — MainWindow, ShellViewModel, SplashViewModel, navigation destinations
├── Features/             — About, RecipeEditor, Recipes, Runs, Settings; each has Views/ ViewModels/ (+ Models/, Services/)
├── Components/           — reusable cross-feature controls
├── Converters/           — Avalonia value converters
├── Services/             — Progress/ (GeneratingEventBus, LogNotifier, ProgressHub), Localization/ (.resx, vi),
│                           Theme/, Dialogs/
├── Resources/            — Tokens / Primitives / Semantic / Controls .axaml (design system)
└── Assets/
```

Notes: UI strings go through localization (`Resources.resx` + `Resources.vi.resx`, `TrExtension`). Design tokens are
gated by `DesignSystemTests` (e.g. contrast ratios) — change tokens deliberately and keep that test green. Per-run
`RunDialogViewModel`, `RecipeEditorViewModel` and `TemplatePickerViewModel` are transient; page ViewModels are
singletons.

## Testing

### Packages (all test projects)

`Microsoft.NET.Test.Sdk`, `xunit.v3` (**not** `xunit` v2), `xunit.runner.visualstudio`,
`Microsoft.Testing.Extensions.TrxReport`, `FluentAssertions` 8.x, `NSubstitute` where mocking is needed. Match the
versions already used in the sibling test projects. `Directory.Build.props` adds `coverlet.collector` and
`StyleCop.Analyzers` globally — test `.csproj`s `Remove` them as needed (copy an existing test project's `.csproj`).

### Conventions

- Test naming: `[Method]_[Scenario]_[ExpectedResult]` — e.g. `Pause_BlocksAtNextCheckpoint_ButNotMidStep`.
- All test classes and methods carry English XML `<summary>` documentation.
- Tests are split into `Unit/` and `Integration/` folders in the larger projects.
- **InternalsVisibleTo**: when a test needs `internal` types, add `<InternalsVisibleTo Include="SlideGenerator.XYZ.Tests" />`
  to the **source** project's `.csproj`.
- **NuGet transitivity pitfall**: `Directory.Build.props` sets `PrivateAssets="all"` on every `ProjectReference`, so NuGet
  packages from referenced projects do **not** flow into test projects. Add an explicit `PackageReference` for any
  package the test project uses directly (e.g. `Generator.Tests` references `Microsoft.Data.Sqlite` itself).

### What NOT to unit test

`JobExecutor`'s phase bodies need a Syncfusion license plus real `.xlsx`/`.pptx` files and real
NetVips/OpenCV work — they belong in integration tests (`Generator.Tests/Integration`), which skip when the license or
fixtures are missing. Do not write unit stubs that bypass that behavior. Safe to unit test directly:

- `PreflightCleanup` (plain file I/O) and pure helpers such as `BuildRowTextValues`.
- `BufferedRepository<TKey,TValue>` with a fake `UpsertBatchAsync`.
- `JobsRepository` / `WorkflowsRepository` row mapping/schema against a real SQLite DB.
- `Service` logic (`DeriveStatus`, `BuildJobs`, workflow-wide pause/stop/resume, `CreateAsync`) against mocked
  `IWorkflowController` / repositories, and `JobGate` (slot sharing, swap-not-resize).
- `WorkflowEngine<TKey,TState>` with a fake runner/observer (`Workflows.Tests/WorkflowEngineTests.cs`):
  pause/checkpoint cooperation, stop unblocking a paused workflow, non-blocking `InitializeAsync`, terminal state
  ownership.
- Desktop ViewModels/services (`Desktop.Tests`), including headless Avalonia view-construction smoke tests that share one
  `HeadlessTestSession.Instance` per assembly (several sessions racing on `Application.Current` fail under parallel xUnit).

## Development Patterns

### Folder structure — feature folders

Library modules are organized by business feature/concept, not by technical layer. A feature folder may mix interfaces,
implementations, models and several small related classes in one file (the old "1 file = 1 class" rule does not apply
within a feature folder). Folder names are plural when the singular would collide with a type inside it (e.g.
`Recipe/Models/` containing `Recipe.cs`; CS0118). Loose root files are fine for cross-feature helpers (e.g.
`Logging/Utilities.cs`). **Namespace mirrors the physical folder path 1:1** (e.g. `Workflows/Engine/WorkflowEngine.cs` →
`SlideGenerator.Workflows.Engine`, `Generator/Workflows/Runners/SlideGenerationRunner.cs` →
`SlideGenerator.Generator.Workflows.Runners`).

### Coding style

- `record` for DTOs/value objects; `sealed class` for services; `internal` unless it needs to be public.
- File-scoped namespaces.
- `ConfigureAwait(false)` in all library/module async code (the Desktop UI layer deliberately resumes on the UI thread
  where it needs to).
- Primary constructors for services: `public sealed class Foo(IBar bar) : IFoo`.
- Extension members (C# 14) where the surrounding file already uses them (see DI Registration Methods).
- Class names: max three words.
- `#region` / `#endregion` to delimit logical sections within a file — never plain `//` comments as separators.
- All public APIs have XML documentation comments.
- New source files carry the repo's copyright header (`scripts/ApplyCopyright` applies it).

## Security Patterns (CodeQL)

CodeQL config lives at `.github/codeql/codeql-config.yml` and excludes `tests/**`.

### Path injection (`cs/path-injection`)

`Path.GetFullPath()` is CodeQL's recognized sanitizer. Apply it at every entry point that receives a user-supplied path:

```csharp
filePath = Path.GetFullPath(filePath); // method entry — breaks the taint chain
```

`NameAndPaths.UserPath` resolves to `%LOCALAPPDATA%\SlideGenerator` normally, or to `BasePath` (the executable
directory) when running portable. Both branches are wrapped in `Path.GetFullPath`, so all derived paths inherit the
sanitization. **Do not remove those wrappers.** `NameAndPaths.IsPortable` is checked at each property access (no
caching).

`NameAndPaths` lives in `SlideGenerator.Settings/Immutable/NameAndPaths.cs`. Layout under `UserPath`:

```
UserPath/
├── Instance.pid           — AppLocker
├── Logs/System/           — LogsFolder.SystemPath (system logs, one file per run)
├── Logs/Workflows/        — LogsFolder.WorkflowPath (per-workflow .log files; name predates the WorkflowCore removal)
├── Imported/Workbooks/    — ImportedFolder.WorkbooksPath
├── Imported/Presentations/— ImportedFolder.PresentationsPath
└── Data/
    ├── Data.db            — DataFolder.DataFile (shared SQLite: Recipes / Workflows / Jobs)
    └── UserSettings.*     — DataFolder.SettingsFile (user settings)

TempFolder.RootPath (%TEMP%\SlideGenerator) — per-job download cache outside UserPath:
{RootPath}/{workflowId}/{jobId}/{hash(url)}{ext}  (JobTempFolder.GetPath). Deleted wholesale by
SlideGenerationWorkflow when that job finishes; there is no shared cross-job cache.
```

### Resource injection (`cs/resource-injection`)

SQLite connection strings must use `SqliteConnectionStringBuilder`, never string interpolation:

```csharp
// ✅
new SqliteConnectionStringBuilder { DataSource = filePath }.ConnectionString
// ❌ — trips cs/resource-injection even with a sanitized filePath
$"Data Source={filePath}"
```

### Log forging (`cs/log-forging`)

Strip line endings from path values before logging. `SettingManager` has a `private static string L(string? s)` helper
for this; replicate the pattern in any new service that logs file paths from external input.

## Known gaps (verified in code — don't "fix" silently, raise with the user)

- `Request.AllowLocalPaths` is persisted and shown in the Run dialog but **not enforced**: `ResolveShapeImageAsync`
  always accepts a `File.Exists(source)` local path.
- A request's derived status never reports `Error` or "completed with errors" (see **Status**); a request whose jobs
  failed rolls up to `Complete`.
- `Taskfile.yml` still references the removed `SlideGenerator.Stdio` project.

## Invariants Checklist

- [ ] Each module has a root `Registration.cs` with its DI setup
- [ ] Module dependencies flow downward only; no circular references
- [ ] `SlideGenerator.Workflows` (`Engine/`) stays domain-free — no reference to `WorkflowSnapshot` / `JobStatus` /
  Syncfusion / `Settings` / SQLite, no knowledge of jobs, and no project references to any other module
- [ ] `JobExecutor` phase bodies call `context.CheckpointAsync` + `ct.ThrowIfCancellationRequested()` before every row,
  never mid-row
- [ ] A workflow's status is assigned only in `GeneratorWorkflowObserver`; a job's status only in
  `SlideGenerationWorkflow`
- [ ] Async library code uses `ConfigureAwait(false)`
- [ ] `record` for data, `sealed` for logic by default
- [ ] Image handling uses `IImage` / NetVips; byte arrays only at boundaries
- [ ] All public APIs have XML documentation comments
- [ ] Serilog never writes to stdout — stderr only
- [ ] User-supplied file paths go through `Path.GetFullPath()` at method entry
- [ ] SQLite connection strings use `SqliteConnectionStringBuilder`, not string interpolation
- [ ] Schema lives in the DbUp scripts of the single shared `Data.db` (pre-release: merged into `001`; after a release:
  a new numbered script) — never a new per-purpose file or an ad-hoc `CREATE TABLE` in a repository
- [ ] Deserializing external/untrusted data (recipe imports, etc.) normalizes possibly-`null` collection fields to empty
  rather than letting a `NullReferenceException` escape
