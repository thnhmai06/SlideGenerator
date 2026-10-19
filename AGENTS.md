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
dotnet test tests/SlideGenerator.Jobs.Tests/SlideGenerator.Jobs.Tests.csproj
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
│   ├── SlideGenerator.Utilities/        — Sha256, Naming, TextNormalization, generic Pool<T>, HardLink, FileAccessHelper
│   ├── SlideGenerator.Settings/         — Immutable/ (NameAndPaths), Mutable/ (Setting + SettingManager), Database/ (DbUp)
│   ├── SlideGenerator.Cloud/            — CloudClient + Resolvers/ (Google Drive only)
│   ├── SlideGenerator.Logging/          — FileLogging/, Formats/ (file + console formatters, ScopeNotifyingSink)
│   ├── SlideGenerator.Document/         — Workbooks/, Presentations/, Template/ (Syncfusion + Mustache)
│   ├── SlideGenerator.Image/            — FaceDetection/, Cropping/, Loading/
│   ├── SlideGenerator.Jobs/             — Engine/ (generic Job Engine, zero project references)
│   ├── SlideGenerator.Recipe/           — Models/, Services/ (repository + export/import), Formats/
│   ├── SlideGenerator.Summarizer/       — Workbooks/, Presentations/, SummarizationService
│   ├── SlideGenerator.Generator/        — Jobs/, Persistence/, Progress/, root Service + DTOs
│   └── SlideGenerator.Desktop/          — Avalonia host (MVVM, Features/, Services/, Shell/, Bootstrap/)
├── tests/                              — one test project per module (mirrors src, standalone; no Summarizer.Tests)
│   └── fixtures/                       — shared test data
├── scripts/ApplyCopyright/             — build-time tool (in the slnx, not a module)
├── plans/                              — design notes / UI mockups, not source of truth for code
└── .github/                            — workflows (ci, codeql, qodana, sponsors), codeql config, CODEOWNERS
```

The host is `SlideGenerator.Desktop`, a single in-process Avalonia application. **There is no IPC layer**: the former
`SlideGenerator.Stdio` JSON-RPC sidecar (and the Tauri frontend it served) no longer exist. ViewModels depend on
`SlideGenerator.Generator.IService` and friends directly.

Modules removed in the past and not to be reintroduced by accident: WorkflowCore (job execution is plain `Task`-based
code), `Cryptography` (→ `Utilities/Sha256.cs`), `Coordinator` (its gates and performance calibration were deleted;
`Pool<T>` → `Utilities/Pool.cs`), `Acquisition`, `Collector`, and the Recipe `Node`/`Edge` graph model.
`tests/SlideGenerator.Acquisition.Tests/` may exist on disk as an orphan (no source, not in the slnx) — leave it alone
unless asked.

## Architecture: Modular Monolith, In-Process

SlideGenerator automates PowerPoint generation from Excel data and templates. Each `Job` runs as a plain in-process
`Task`, managed by a generic Job Engine and driven from the Avalonia UI through the `IService` facade.

### Module Map

```
Foundation
├── Utilities     - string normalization, Sha256, Pool<T>, helpers
├── Cloud         - URI inspect/download + provider resolvers (Google Drive only; OneDrive/SharePoint not implemented)
├── Logging       - Serilog file/console formatters, IFileLoggerFactory, scope-notifying sink
├── Document      - Syncfusion Excel/PowerPoint abstractions + Mustache text templating
├── Image         - NetVips loading, ROI cropping, OpenCV YuNet face detection
└── Jobs          - Generic Job Engine: runs any IJobWorkload<TState> — concurrency, pause/resume/stop,
                    crash-resume — with no knowledge of what a job does

Domain
├── Settings      - JSON user settings (ISettingProvider/ISettingManager) + the shared SQLite schema migrator
├── Recipe        - Recipe CRUD (SQLite) + *.recipe export/import packages; a Recipe is a flat list of Mappings
└── Summarizer    - Workbook/presentation metadata scanner (used by the recipe editor)

Application
├── Generator     - IService facade, JobRunner (adapter over the Job Engine), SlideGenerationWorkload,
│                   Requests/Jobs persistence, progress + log contracts
└── Desktop       - Avalonia UI host: DI wiring, ViewModels, progress hub, localization, theming
```

### Dependency Rules

- Dependencies flow downward only — no circular references.
- `SlideGenerator.Jobs` has zero project references.
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
| Jobs          | `AddJobEngine<TKey,TState>()` — generic, called from `AddGeneratorServices()`, not from the host directly  |
| Generator     | `AddGeneratorServices()`                                                                                   |
| Desktop       | `AddDesktopServices()` (event bus, progress hub, localization, theme, dialogs, ViewModels)                 |

`Registration.cs` in Cloud, Generator, Image and Logging uses C# 14 **extension member syntax**:

```csharp
extension(IServiceCollection services)
{
    public IServiceCollection AddFooServices() { ... }
}
```

Desktop, Document, Jobs, Recipe, Settings and Summarizer still use a classic `this IServiceCollection` extension
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
`IProgressHub` must be constructed (subscribed) **before** `IService.InitializeAsync()`: crash-resumed jobs are scheduled
immediately and a late subscriber would miss their first events.
On shutdown (`ShutdownRequested`), `IService.ShutdownAsync()` runs, then settings are saved.

## Request → Job flow

`IService` (`Generator/Service.cs`) is the only surface the UI uses:

- `PreviewAsync(request)` — computes the job list and per-job `ConflictKind` without spawning anything.
- `CreateAsync(request)` — reads the recipe, flattens it to jobs, validates output paths, persists the
  `RequestRecord`, publishes `RequestProgress(PreparationStarted)` + `AnnounceExpectedJobCount`, then calls
  `IJobRunner.StartJobAsync` once per job. Returns immediately; it is **not** throttled by `MaxConcurrentJobs`.
- `StopAsync` / `PauseAsync` / `ResumeAsync(requestId)` — request-scoped, best-effort fan-out over the request's jobs,
  returning `PartialResult(Succeeded, Skipped)`. `StopAllAsync` / `PauseAllAsync` are bulk variants. There is no per-job
  variant on `IService`.
- `ListActiveAsync` / `ListCompletedAsync(includeLogs)` — `IReadOnlyDictionary<string, Summary>` keyed by request id.
  Pass `includeLogs: false` for list views (reading/parsing the `.log` file is the expensive part).
- `DeleteAsync` (stops the request first if still active) / `DeleteAllCompletedAsync`.

**Identity**: `requestId` is a GUID minted in `CreateAsync`; `JobId` is a plain 0-based `int` ordinal within the request;
`JobKey` is a `global using` alias for `(string RequestId, int JobId)`.

**Building jobs** (`Service.BuildJobs`): one `JobSpecification` per (mapping × worksheet source) pair, every value
already resolved (workbook path, sheet, template presentation + slide index, instructions, `UsedColumns`, `RowFilter`).
Output path = `{SaveFolder}/{workbook name}/{sanitized sheet name}{ext}`. A job needs nothing else to run or resume — no
recipe or workbook lookup — so editing/deleting a recipe never affects existing jobs.

**Conflict guard**: there is no recipe-level lock; multiple active requests for the same recipe are allowed. Instead
`CreateAsync` throws if two jobs of the request share an output path (`FindDuplicateOutputPath`) or if a job's output
path equals one claimed by an already Pending/Running/Paused job of another request (`GetActiveOutputPathsAsync`).

**Request status** is derived, never stored (`Service.DeriveStatus`): any Running/Pending → `Running`; else any Paused →
`Paused`; else all Cancelled → `Cancelled`; else `Complete`. Note a request whose jobs ended in `Error` therefore reports
`Complete` at request level — error status exists only per job (`JobSummary.JobStatus`). `Summary.Phase`
(`RequestPhase`) is likewise recomputed from current job statuses on every list call (`DeriveRequestPhase`).

## Job Engine (`SlideGenerator.Jobs`)

Fully generic and domain-free: no `JobSnapshot`, `JobStatus`, Syncfusion, `Settings` or SQLite references.

- `IJobEngine<TKey,TState>` / `JobEngine<TKey,TState>` — `InitializeAsync(resumeSource)`, `ShutdownAsync`,
  `StartJobAsync`, `PauseAsync`, `ResumeAsync`, `StopAsync`. Each job runs on its own `Task.Run`, tracked in an in-memory
  `ConcurrentDictionary`. Starting a duplicate key throws.
- `IJobWorkload<TState>.RunAsync(initial, IJobContext<TState>, ct)` — the domain half. `IJobContext` offers
  `IsResume`, `ReportAsync(state, durable, ct)` and `CheckpointAsync(ct)`.
- `IJobObserver<TKey,TState>` — `OnProgressAsync(key, state, durable, ct)`, `OnPausedAsync`, `OnResumedAsync`,
  `OnTerminalAsync(key, JobTerminalResult)`. The engine never inspects `TState`; every transition is forwarded here.
- `IJobResumeSource<TKey,TState>` → `PendingJob` list for crash recovery; `IJobConcurrencyProvider.MaxConcurrentJobs`.

**Concurrency (`MaxConcurrentJobs`)** is the *only* concurrency/RAM control in the pipeline (default 5,
`Setting.PerformanceSetting`, the only field left on it). There are no per-operation gates (download/edit/save) and no
performance calibration. The engine uses a `SemaphoreSlim`:

- `ApplyConcurrencyLimit()` runs in `InitializeAsync` and at the start of every `StartJobAsync`; it swaps in a **new**
  semaphore only when the value actually changed (tracked in `_currentLimit`). Swapping unconditionally would give each
  new job its own private semaphore and defeat the limit.
- `RunJobAsync` captures `_semaphore` into a local once and uses that same instance for `WaitAsync` and `Release`, so a
  swap mid-job can't cause a `SemaphoreFullException`.
- The limit only throttles *execution*: the "starting execution" progress tick is published before the slot is awaited,
  so a queued job still shows as running.
- `SettingConcurrencyProvider` (Generator) re-reads `settingProvider.Current.Performance.MaxConcurrentJobs` on every
  access, so a settings change applies to the next started job; running jobs are unaffected.

**Pause/stop** use `PauseGate` (one per running job, engine-owned): `Pause()`/`Resume()` toggle a swappable
`TaskCompletionSource`; the workload calls `CheckpointAsync` before each row, so pausing is cooperative and happens
**between rows**, never mid-row. `StopAsync` cancels the job's CTS *and* resumes the gate so a paused job observes the
cancellation immediately. `ShutdownAsync` does the same for every job, then awaits them all.

**State ownership**: `RunAsync` is the only place that creates a new `TState`. On normal completion its *return value*
is the terminal state; on cancel/fault the terminal state is the last one passed to `ReportAsync` (cached per running
job by the engine). Outcome mapping: `Completed` / `Cancelled` (`OperationCanceledException` while cancellation was
requested) / `Faulted` (anything else, logged).

## Slide-generation workload (`SlideGenerator.Generator`)

`JobRunner` (`Jobs/JobRunner.cs`, `IJobRunner`) is a thin adapter: `StartJobAsync` persists the initial `Pending`
`JobSnapshot` and **flushes it immediately** (so conflict checks see it right away), wraps `SlideGenerationWorkload` in a
`LoggingWorkload`, and hands it to the engine. `ShutdownAsync` shuts the engine down *then* flushes the repository.

`SlideGenerationWorkload : IJobWorkload<JobSnapshot>` (`Jobs/Workloads/`) runs four phases in order, each with
`ct.ThrowIfCancellationRequested()` + `context.CheckpointAsync(ct)` before every row and `output.Save()` after it:

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
layout/master through its source).

Any other exception during a row propagates and fails the whole job (→ `JobStatus.Error`); rows are not individually
try/caught.

`JobPhase` has 5 values (`Done` is the terminal stamp, never "run"). `JobSnapshot` = `JobSpecification` + `JobStatus` /
`Phase` / `CurrentIndex` / `Timestamp` / `TotalRows` (nullable; known once the workload starts, drives the UI's
determinate progress bar).

**`GeneratorJobObserver`** is the only place `JobStatus` values are assigned (`OnProgress` → `Running`, `OnPaused` →
`Paused`, `OnResumed` → `Running`, `OnTerminal` → `Complete`/`Cancelled`/`Error`). Each transition does
`jobsRepository.Enqueue(state)` (+ immediate `FlushAsync` when `durable`) then `eventBus.Publish(state)`. On terminal it
also deletes the job's temp download folder.

**Crash-resume**: `GeneratorResumeSource` loads every Pending/Running/Paused row (`GetNonTerminalAsync`) — by definition
crash leftovers — and rebuilds a `LoggingWorkload` using the owning request's persisted `LogPath` (so the resumed run
keeps appending to the same `.log`). `Phase` + `CurrentIndex` say where to continue; the embedded `JobSpecification` says
what to do. A job that was `Paused` resumes as `Running` (no persisted "why").

## Progress, logs and persistence

**Event flow** (all in-process; there is no JSON-RPC notification layer any more):

```
SlideGenerationWorkload ──ReportRow──► IEventBus ─┐
GeneratorJobObserver ───Publish(JobSnapshot)────► IEventBus ─┼─► GeneratingEventBus (Desktop) ─► ProgressHub ─► ViewModels
Service ──RequestProgress / AnnounceExpectedJobCount──────────┘
LoggingWorkload ─► ILogNotifier ─► LogNotifier (Desktop) ─────────────► ProgressHub
```

- Contracts (`IEventBus`, `ILogNotifier`, `RequestProgress`, `RowProgress`, `RowStatus`, `RowStage`) live in
  `Generator/Progress/`; Generator depends only on these interfaces. The concrete `GeneratingEventBus` / `LogNotifier`
  live in `Desktop/Services/Progress/` and are registered both as concrete types and as the interfaces (so `ProgressHub`
  can subscribe to the C# events).
- `ProgressHub` (`IProgressHub`) drains every **250 ms** on the UI `DispatcherTimer`: jobs coalesce by
  `(RequestId, JobId)`, rows by `(RequestId, JobId, RowIndex)` (last write wins), logs are append-only (never dropped).
- `RowProgress` is **never persisted** — live only. `RequestProgress` is not persisted either.
- `RequestPhase`: `PreparationStarted` | `ProcessingStarted` | `Completed`.

**Logging scopes**: log lines go to one `.log` file per request (`RequestRecord.LogPath`, under
`Logs/Workflows/`, shared by all of the request's jobs). `LoggingWorkload` opens an `IFileLoggerFactory.CreateFile(...)`
scope with ordered property names `["RequestId", "JobId", "RowIndex"]`, pushes `RequestId`/`JobId` on `LogContext`, and
the phases push `RowIndex` per row. `FileLogFormatter` writes the scope path into each line; `Service.ToSummaryAsync`
reads it back via `ILogFileReader`/`LogFileReader` (regex parse) and filters by scope-path prefix — logs are
deliberately not cached in RAM. `Logging` itself has no notion of what a scope means.

**Persistence** — a **single** SQLite database `Data/Data.db` shared by every module (tables `Recipes`, `Requests`,
`Jobs`). Schema is created centrally by `Settings.Database.DatabaseMigrator.Migrate` (DbUp, embedded scripts in
`Settings/Database/Scripts/`: `001_2.0.0.sql`, `002_add-total-rows-to-jobs.sql`; new scripts are picked up by the
`.csproj` wildcard — just add the file with the next number). No repository creates its own tables. Each repository
opens/closes a short-lived connection per operation.

- `Jobs`: one explicit column per `JobSpecification` field, except `UsedColumnsJson` / `TextInstructionsJson` /
  `ImageInstructionsJson` (JSON text, serialized with `JobSpecificationJson` options). `RowFilter` →
  `RowFilterType` + 4 nullable scalars. Composite PK `(RequestId, JobId)`.
- `Requests`: one column per `Request` field plus `LogPath` and `CreatedAt`; written once, never updated.
  `RecipeId` is informational only (not used on resume).
- `JobsRepository` extends `BufferedRepository<TKey,TValue>`: `Enqueue` is coalesced last-write-wins, a ~1 s
  `PeriodicTimer` drains the dirty set and upserts one batch per tick, then raises `Flushed`; `FlushAsync` forces it.
  `RequestsRepository` deliberately does **not** buffer.
- `Service.DeleteAsync` / `DeleteAllCompletedAsync` delete from both `Jobs` and `Requests`.

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

`SlideGenerationWorkload`'s phase bodies need a Syncfusion license plus real `.xlsx`/`.pptx` files and real
NetVips/OpenCV work — they belong in integration tests (`Generator.Tests/Integration`), which skip when the license or
fixtures are missing. Do not write unit stubs that bypass that behavior. Safe to unit test directly:

- `PreflightCleanup` (plain file I/O) and pure helpers such as `BuildRowTextValues`.
- `BufferedRepository<TKey,TValue>` with a fake `UpsertBatchAsync`.
- `JobsRepository` row mapping/schema against a real temp-file SQLite DB.
- `Service` aggregation logic (`DeriveStatus`, `BuildJobs`, fan-out) against mocked `IJobRunner` / repositories.
- `JobEngine<TKey,TState>` with a fake workload/observer (`Jobs.Tests/JobEngineTests.cs`): pause/checkpoint cooperation,
  stop unblocking a paused job, concurrency-cap sharing and swap-not-resize, non-blocking `InitializeAsync`, terminal
  state ownership.
- Desktop ViewModels/services (`Desktop.Tests`), including headless Avalonia view-construction smoke tests that share one
  `HeadlessTestSession.Instance` per assembly (several sessions racing on `Application.Current` fail under parallel xUnit).

## Development Patterns

### Folder structure — feature folders

Library modules are organized by business feature/concept, not by technical layer. A feature folder may mix interfaces,
implementations, models and several small related classes in one file (the old "1 file = 1 class" rule does not apply
within a feature folder). Folder names are plural when the singular would collide with a type inside it (e.g.
`Recipe/Models/` containing `Recipe.cs`; CS0118). Loose root files are fine for cross-feature helpers (e.g.
`Logging/Utilities.cs`). **Namespace mirrors the physical folder path 1:1** (e.g. `Jobs/Engine/JobEngine.cs` →
`SlideGenerator.Jobs.Engine`, `Generator/Jobs/Workloads/SlideGenerationWorkload.cs` →
`SlideGenerator.Generator.Jobs.Workloads`).

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
├── Logs/System/           — LogsFolder.SystemPath (system logs, plus a latest.log hard link)
├── Logs/Workflows/        — LogsFolder.WorkflowPath (per-request .log files; name predates the WorkflowCore removal)
├── Imported/Workbooks/    — ImportedFolder.WorkbooksPath
├── Imported/Presentations/— ImportedFolder.PresentationsPath
└── Data/
    ├── Data.db            — DataFolder.DataFile (shared SQLite: Recipes / Requests / Jobs)
    └── UserSettings.*     — DataFolder.SettingsFile (user settings)

TempFolder.RootPath (%TEMP%\SlideGenerator) — per-job download cache outside UserPath:
{RootPath}/{requestId}/{jobId}/{hash(url)}{ext}  (JobTempFolder.GetPath). Deleted wholesale by
GeneratorJobObserver.OnTerminalAsync when that job ends; there is no shared cross-job cache.
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
- Request-level status never reports `Error` (see **Request status**); errored jobs roll up to `Complete`.
- `Taskfile.yml` still references the removed `SlideGenerator.Stdio` project.

## Invariants Checklist

- [ ] Each module has a root `Registration.cs` with its DI setup
- [ ] Module dependencies flow downward only; no circular references
- [ ] `SlideGenerator.Jobs` (`Engine/`) stays domain-free — no reference to `JobSnapshot` / `JobStatus` / Syncfusion /
  `Settings` / SQLite, and no project references to any other module
- [ ] `SlideGenerationWorkload` phase bodies call `context.CheckpointAsync` + `ct.ThrowIfCancellationRequested()` before
  every row, never mid-row
- [ ] `JobStatus` values are assigned only in `GeneratorJobObserver`
- [ ] Async library code uses `ConfigureAwait(false)`
- [ ] `record` for data, `sealed` for logic by default
- [ ] Image handling uses `IImage` / NetVips; byte arrays only at boundaries
- [ ] All public APIs have XML documentation comments
- [ ] Serilog never writes to stdout — stderr only
- [ ] User-supplied file paths go through `Path.GetFullPath()` at method entry
- [ ] SQLite connection strings use `SqliteConnectionStringBuilder`, not string interpolation
- [ ] New SQLite tables/columns arrive as a new numbered DbUp script in the single shared `Data.db`, not a new
  per-purpose file and not an ad-hoc `CREATE TABLE` in a repository
- [ ] Deserializing external/untrusted data (recipe imports, etc.) normalizes possibly-`null` collection fields to empty
  rather than letting a `NullReferenceException` escape
