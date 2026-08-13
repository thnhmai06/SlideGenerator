# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

# Run all tests
dotnet test SlideGenerator.slnx

# Run tests for one project
dotnet test tests/SlideGenerator.Settings.Tests/SlideGenerator.Settings.Tests.csproj

# Run a single test by name filter
dotnet test --filter "FullyQualifiedName~Load_SettingsFileNotFound_ReturnsFalse"
```

SDK: .NET 10.0 (`global.json` pins to `latestMajor`, allows prerelease). The solution uses the XML-based
`SlideGenerator.slnx` (no `.sln`). A Syncfusion license is required at runtime: copy `.env.example` to `.env` and fill
`SYNCFUSION_LICENSE_KEY` before running the Stdio sidecar.

**GitHub Packages**: `SlideGenerator.Image` depends on per-platform `SlideGenerator.OpenCvSharp4.runtime.*` packages
hosted at `nuget.pkg.github.com/thnhmai06`. `backend/nuget.config` reads credentials from `%GITHUB_USERNAME%` and
`%GITHUB_TOKEN%` env vars — set these before restoring.

## Solution Layout

```
backend/
├── src/                                — 10 source modules (slnx-tracked)
│   ├── SlideGenerator.Utilities/        — loose files, no subfolders
│   ├── SlideGenerator.Settings/         — Config/, Rules/, Database/ (NameAndPaths stays put — see below)
│   ├── SlideGenerator.Cloud/            — Resolvers/, root CloudClient.cs
│   ├── SlideGenerator.Document/         — Workbooks/, Slides/, Template/
│   ├── SlideGenerator.Logging/          — FileLogging/, root formatters/helpers
│   ├── SlideGenerator.Image/            — FaceDetection/, Cropping/, Loading/
│   ├── SlideGenerator.Summarization/    — Workbook/, Slide/, root service
│   ├── SlideGenerator.Recipe/           — Mappings/, root RecipeRepository+entry+package rules
│   ├── SlideGenerator.Generator/        — Job/, Persistence/, Progress/, root Service+DTOs
│   └── SlideGenerator.Stdio/            — Handlers/, Implementations/ (already 1 feature = 1 IPC method group)
└── tests/                              — 9 test projects (mirrors src, standalone)
    ├── SlideGenerator.Utilities.Tests/
    ├── SlideGenerator.Cloud.Tests/
    ├── SlideGenerator.Settings.Tests/
    ├── SlideGenerator.Document.Tests/
    ├── SlideGenerator.Logging.Tests/
    ├── SlideGenerator.Image.Tests/
    ├── SlideGenerator.Recipe.Tests/
    ├── SlideGenerator.Generator.Tests/
    └── SlideGenerator.Stdio.Tests/
```

All 9 non-host modules now use a **feature-folder** layout — see **Development Patterns → Folder structure**
below for the convention and rationale. `SlideGenerator.Recipe` and `SlideGenerator.Generator` were reorganized
last (after the WorkflowCore-removal rewrite settled their file contents, to avoid reorganizing then immediately
rewriting most of the same files). `SlideGenerator.Stdio`'s `Handlers/`/`Implementations/` split was already
feature-shaped (one handler class per IPC method group) from the start, so it never needed a separate reorg pass.

**WorkflowCore has been removed entirely** (package, all `Steps/`/`Workflows/`/`Middleware/` folders, the
`Workflows.db` SQLite store) — job execution is now plain `Task`-based C# in `SlideGenerator.Generator`'s
`JobRunner` (see **Job Execution (JobRunner)** below, which replaces the old **Workflow System (WorkflowCore)**
section). `SlideGenerator.Recipe`'s `Node`/`Edge` graph model has also been removed — a recipe is now a flat
`Recipe(Mappings)` list (see **Job Execution (JobRunner) → Input mapping** below).

`SlideGenerator.Cryptography` module has been removed (its `Sha256` helper moved into
`SlideGenerator.Utilities/Sha256.cs`). `SlideGenerator.Coordinator` has also been removed — its 3 `GateType`
concurrency gates (`DownloadImage`/`EditImage`/`EditPresentation`) and the whole performance-calibration system
(`SettingProbe`/`SettingTuner`/`SettingCalibrator`) were deleted; RAM/concurrency control is now solely
`MaxConcurrentJobs` (see **Concurrency: MaxConcurrentJobs** below). `Coordinator`'s generic `Pool<T>` (the only
still-needed piece, backing `FaceDetectorPool`) moved to `SlideGenerator.Utilities/Pool.cs`.
`src/SlideGenerator.Acquisition/` and `src/SlideGenerator.Collector/` are gone
entirely (no longer even present on disk). `tests/SlideGenerator.Acquisition.Tests/` still exists on disk (only
`bin`/`obj`, no source) but has no matching src project and is **not** in `SlideGenerator.slnx` — an orphan, leave
alone unless asked to clean it up. `scripts/ApplyCopyright/ApplyCopyright.csproj` is also in the slnx (a build-time
tool project, not a module).

`Summarization` has no dedicated test project.

## Architecture: Modular Monolith + IPC Sidecar

SlideGenerator automates PowerPoint generation from Excel data and templates. It is a **Modular Monolith** —
each `Job` runs as a plain in-process `Task` managed by `JobRunner` (no external workflow engine) — exposed to a
Tauri frontend through a JSON-RPC 2.0 IPC sidecar.

### Module Map

```
Foundation Modules
├── SlideGenerator.Utilities     - Shared utilities (string normalization, Sha256, generic Pool<T>, helpers)
├── SlideGenerator.Cloud         - Multi-cloud URI resolver (Google Drive; OneDrive/SharePoint modules not yet
│                                   implemented — only Resolver/GoogleDriveModule.cs exists on disk)
├── SlideGenerator.Logging       - Serilog: IFileLoggerFactory (FileLogging/), ConsoleLogFormatter
├── SlideGenerator.Document      - Syncfusion Excel/PowerPoint abstractions (Workbook/, Slide/) + Mustache
│                                   template engine (Template/)
└── SlideGenerator.Image         - NetVips-based image loading (Loading/); ROI cropping (Cropping/) + face
                                    detection via OpenCV YuNet (FaceDetection/)

Domain Modules
├── SlideGenerator.Settings      - YAML-based configuration; ISettingProvider (Config/)
├── SlideGenerator.Summarization - Workbook/presentation metadata scanner
└── SlideGenerator.Recipe        - Recipe CRUD (SQLite) + export/import (*.recipe zip packages); a Recipe is a
                                    flat list of Mappings — no graph/Node/Edge (see Job Execution below)

Application
└── SlideGenerator.Generator     - JobRunner: runs each Job's 4-phase pipeline directly on Task.Run, no external
                                    workflow engine; spawn phase (Service.CreateAsync) is plain code too

Host
└── SlideGenerator.Stdio         - JSON-RPC 2.0 IPC sidecar (StreamJsonRpc over stdin/stdout)
```

### Dependency Rules

- Dependencies flow downward only — no circular references.
- Each module has a root `Registration.cs` as DI entry point.
- `SlideGenerator.Stdio` is the executable that wires all modules.

## DI Registration Methods

| Module                | Extension Method                                                                                       |
|-----------------------|--------------------------------------------------------------------------------------------------------|
| Settings              | `AddSettingsServices()`                                                                                |
| Cloud                 | `AddCloudServices()`                                                                                   |
| Document              | `AddDocumentServices()`                                                                                |
| Image                 | `AddImageServices()`                                                                                   |
| Logging               | `AddLoggingServices()`                                                                                 |
| Summarization         | `AddSummarizationServices()`                                                                           |
| Recipe                | `AddRecipeServices()`                                                                                  |
| Generator             | `AddGeneratorServices()`                                                                               |
| Stdio                 | `AddIpcServices()`                                                                                     |

The system logger is bootstrapped up-front by a private `BootstrapSystemLogger(IConfiguration)` method inline in
`src/SlideGenerator.Stdio/Program.cs` (file Serilog sink → `stderr` only), which sets the static `Log.Logger`. It is
**not** passed through DI or into `AddDocumentServices`.

`Registration.cs` files use C# 14 **extension member syntax**:

```csharp
extension(IServiceCollection services)
{
    public IServiceCollection AddFooServices() { ... }
}
```

## IPC Layer (SlideGenerator.Stdio)

JSON-RPC 2.0 over stdin/stdout using **StreamJsonRpc** with NDJSON framing (`NewLineDelimitedMessageHandler`) and STJ
serialization (`SystemTextJsonFormatter`).

### Stream ownership

| Stream | Owner         | Purpose                                  |
|--------|---------------|------------------------------------------|
| stdin  | StreamJsonRpc | Incoming JSON-RPC requests               |
| stdout | StreamJsonRpc | Outgoing responses **and** notifications |
| stderr | Serilog       | System logs only                         |

### JsonRpc setup

`JsonRpc` is created **after** the DI container is built (raw stream access). Not registered in DI. Methods wired via
`AddLocalRpcMethod`:

```csharp
// DTO param → UseSingleObjectParameterDeserialization = true (via local Attr() helper)
jsonRpc.AddLocalRpcMethod(method, handler, Attr("workflow.start"));

// No DTO param
jsonRpc.AddLocalRpcMethod(method, handler, new JsonRpcMethodAttribute("settings.get"));
```

### Progress notifications

Progress is scoped to 3 levels — Request/Job/Row (see **Progress model** under **Job Execution (JobRunner)** below).
Only the `Jobs` table is actually persisted and buffered now — `Requests` is write-once (inserted directly by
`Service.CreateAsync`, never updated) and `Rows`/per-row progress is **never persisted at all**, only forwarded live.
`ProgressCoalescer` (`Implementations/ProgressCoalescer.cs`) does not own its own buffer for Jobs — `IJobsRepository`
(`SlideGenerator.Generator`) already buffers/flushes internally via `BufferedRepository<TKey,TValue>` (coalesced,
last-write-wins, ~1s `PeriodicTimer` tick, see **Job Execution (JobRunner) → Persistence** below); `ProgressCoalescer`
just subscribes to `IJobsRepository.Flushed` and relays each batch as `progress/jobs`. `RequestProgress`/`RowProgress`
are forwarded immediately, un-buffered, straight off `GeneratingEventBus`. Log lines remain buffered here in a
`ConcurrentQueue` (append-only, never coalesced — every line matters) with the coalescer's own 1s flush loop, since
logs are never persisted through a repository (they still go to the per-request `.log` file — see **Logging scope
notifications** below). Every notification is sent via `JsonRpc.NotifyAsync(method, payload)` — **not**
`NotifyWithParameterObjectAsync`, which marshals a `List<T>` argument by reflecting over its public properties
(named-parameter convention) and would serialize an empty `{}` instead of the array; `NotifyAsync` binds it
positionally instead (`"params": [[...]]`). Bound at runtime via `JsonRpcBootstrap.AttachProgressCoalescer(services,
jsonRpc)` → `coalescer.Attach(bus, logNotifier, jsonRpc)` — not DI injected, since `JsonRpc` doesn't exist until
after the DI container is built. `DetachAsync()` (called during shutdown in `Program.Startup.cs`) flushes the log
queue one last time before cancelling the timer, so the final <1s window of buffered logs isn't dropped (`Jobs`
flushes itself independently via `JobRunner.ShutdownAsync` → `IJobsRepository.FlushAsync`/`DisposeAsync`).

`GeneratingEventBus` (in `Implementations/GeneratingEventBus.cs`) is registered as both `GeneratingEventBus` (concrete)
and `IEventBus` (interface, `SlideGenerator.Generator.Abstractions.IEventBus`) in the Stdio
`Registration.cs` so that `ProgressCoalescer.Attach` can receive the concrete type. `LogNotifier`/`ILogNotifier`
mirror the same pattern for log lines (see **Logging scope notifications** below).

### STJ adapters

`Implementations/Adapters/` contains custom STJ converters registered in `BuildJsonSerializerOptions()`
(`JsonRpcBootstrap.cs`):

- `RoiOptionJsonAdapter` — polymorphic `RoiOption` discriminated by `"type"` (`"Center"` | `"RuleOfThirds"`)
- `RectangleFJsonAdapter` — `RectangleF` as `{"x", "y", "width", "height"}`
- `Vector2JsonConverter` (in `Implementations/Adapters/`)

There is no longer a `NodeJsonConverter` — `SlideGenerator.Recipe`'s `Recipe`/`Mapping`/`WorksheetSource` types are
plain non-polymorphic records now, so the wire shape of `recipe.add`/`recipe.update`/`recipe.query` serializes with
System.Text.Json's default reflection-based behavior. `JobSpecificationJson` (`SlideGenerator.Generator.Adapters`) is
a separate, small `JsonSerializerOptions` used only by `JobsRepository` to serialize the `*Json` columns
(`UsedColumns`/`TextInstructions`/`ImageInstructions`) — **not** shared with the IPC layer's options, since Generator
must not depend on `SlideGenerator.Stdio` and the equivalent options in `SlideGenerator.Recipe` are `internal`.

`JsonStringEnumConverter` is registered globally — all enums serialize as strings automatically.

### Registered methods

| Method                           | Handler                                           |
|----------------------------------|---------------------------------------------------|
| `generator.active.create`        | `GeneratingActiveHandler.CreateAsync`             |
| `generator.active.stop`          | `GeneratingActiveHandler.StopAsync`               |
| `generator.active.pause`         | `GeneratingActiveHandler.PauseAsync`              |
| `generator.active.resume`        | `GeneratingActiveHandler.ResumeAsync`             |
| `generator.active.stopAll`       | `GeneratingActiveHandler.StopAllAsync`            |
| `generator.active.pauseAll`      | `GeneratingActiveHandler.PauseAllAsync`           |
| `generator.active.list`          | `GeneratingActiveHandler.ListAsync`               |
| `generator.completed.list`       | `GeneratingCompletedHandler.ListAsync`            |
| `generator.completed.delete`     | `GeneratingCompletedHandler.DeleteAsync`          |
| `generator.completed.deleteAll`  | `GeneratingCompletedHandler.DeleteAllAsync`       |
| `recipe.list`                    | `RecipeHandler.ListAsync`                         |
| `recipe.query`                   | `RecipeHandler.QueryAsync`                        |
| `recipe.add`                     | `RecipeHandler.AddAsync`                          |
| `recipe.update`                  | `RecipeHandler.UpdateAsync`                       |
| `recipe.delete`                  | `RecipeHandler.DeleteAsync`                       |
| `recipe.export`                  | `RecipeHandler.ExportAsync`                       |
| `recipe.import`                  | `RecipeHandler.ImportAsync`                       |
| `summarization.workbook`         | `SummarizationHandler.SummarizeWorkbookAsync`     |
| `summarization.presentation`     | `SummarizationHandler.SummarizePresentationAsync` |
| `settings.get`                   | `SettingsHandler.GetAsync`                        |
| `settings.update`                | `SettingsHandler.UpdateAsync`                     |
| `settings.reset`                 | `SettingsHandler.ResetAsync`                      |
| `settings.performance.get`       | `SettingsHandler.GetPerformanceAsync`             |
| `settings.performance.update`    | `SettingsHandler.UpdatePerformanceAsync`          |
| `settings.performance.reset`     | `SettingsHandler.ResetPerformanceAsync`           |
| `settings.network.get`           | `SettingsHandler.GetNetworkAsync`                 |
| `settings.network.update`        | `SettingsHandler.UpdateNetworkAsync`              |
| `settings.network.reset`         | `SettingsHandler.ResetNetworkAsync`               |

Notifications emitted by the sidecar: `progress/request`, `progress/jobs`, `progress/rows`, `log/entries` — `progress/jobs`
is batched at up to 1/s (piggybacking on `IJobsRepository`'s flush tick), `log/entries` is separately batched at up to
1/s; `progress/request`/`progress/rows` are sent immediately, one notification per event (never buffered). Every
payload is a single positional array argument (`params[0]`), a list of `RequestProgress`/`JobRecord`/`RowProgress`/
`LogEntry` respectively — not a named-object param. There is no separate `JobProgress` DTO — `JobRecord` (a job's
full current-state row) doubles as the job-scoped progress payload (see **Job Execution (JobRunner) → Progress
model** below).

## Concurrency: MaxConcurrentJobs

There is no per-operation concurrency gate anywhere in the pipeline (downloading, image editing, and presentation
saving run uncontended within a job). The **sole** concurrency/RAM control is at the job level, now owned entirely
by `JobRunner` (`SlideGenerator.Generator/Job/JobRunner.cs`) via a plain `SemaphoreSlim` field — no external
workflow engine involved.

`JobRunner.ApplyMaxConcurrentJobs()` (private) does `_semaphore = new SemaphoreSlim(value, value)` — it **swaps in a
new semaphore instance** rather than mutating the old one, so jobs already waiting on the previous instance are
unaffected by a resize; only newly-queued waits (i.e. jobs started after the resize) see the new limit. Called once
in `InitializeAsync` (startup) and again at the start of every `StartJobAsync` call, reading
`settingProvider.Current.Performance.MaxConcurrentJobs` fresh each time — so a `settings.performance.update` takes
effect for the *next* job spawned, no restart needed (existing running jobs are unaffected either way, since they
already acquired their semaphore slot). `Setting.PerformanceSetting.MaxConcurrentJobs` (default 5) is the **only**
field left on `PerformanceSetting` — the old `MaxParallelDownloadImage`/`MaxParallelEditImage`/
`MaxParallelEditPresentation`/`MaxParallelReadWorkbook`/`MaxParallelReadPresentation` fields and the whole
hardware/network probing system that calibrated them (`SettingProbe`, `SettingTuner`, `SettingCalibrator`,
`ISettingCalibrator`, the `settings.performance.calibrate` IPC method) were deleted along with the old gates — there
is nothing left to calibrate.

This cap only throttles job *execution* (`RunJobAsync` awaits `_semaphore.WaitAsync` after publishing `Status.Running`
but before doing any real work) — it never delays *accepting* a new generation request, since `Service.CreateAsync`'s
spawn phase (recipe read, job-list computation, `StartJobAsync` calls) is plain C# code that returns immediately per
job, not itself gated (see **Job Execution (JobRunner)** below).

`SlideGenerator.Image`'s `FaceDetectorPool` (a separate concern — pools actual `IFaceDetector`/OpenCV instances, not a
throughput gate) is unrelated to `MaxConcurrentJobs`; it is bounded by a static `Environment.ProcessorCount` limit
(CPU-bound native work) via the generic `Pool<T>` living in `SlideGenerator.Utilities/Pool.cs`.

## Image Processing

`SlideGenerator.Image` (`Loading/`) uses **NetVips** (`IImage`, implemented by `VipsImage`) as the primary in-memory
image type, loaded via `IImageLoader.Open(path|byte[])`. Convert to/from `byte[]` only at the system boundary — the
crop pipeline is fully in-memory end to end and the result is written straight into `IShape.ImageData`, never to a
disk file (see `JobRunner.Phases.cs` → `CropToPngAsync`).

- `IImageLoader.Open(string path)` / `Open(byte[] data)` → `IImage`
- `ISmartCropper.CropAsync(IImage, Size targetSize, IReadOnlyList<RoiOption> roiOptions)` → `IImage?` — tries each
  `RoiOption` in order (anchor-based via `IAnchorCropper`, content-aware via `IInterestCropper`), returns the first
  that succeeds
- `IImage.ToPng()` → `byte[]` (only place a `byte[]` re-appears, right before `IShape.ImageData = imageData`)
- Face detection (`FaceDetection/`): `IFaceDetector.DetectAsync` (OpenCV YuNet, `YuNet.cs`), pooled via
  `FaceDetectorPool` (bounded by `Environment.ProcessorCount`, see **Concurrency** above)
- Always use `using`/`await using` for `IImage` disposal.

## Job Execution (JobRunner)

**One `Job` = one in-process `Task`, tracked in an in-memory registry.** `JobRunner`
(`SlideGenerator.Generator/Job/JobRunner.cs`, implements `IJobRunner`) replaces WorkflowCore entirely — there is
no external workflow engine, no separate persistence engine for job state. Each job runs its 4 phases sequentially,
in order, inside one `Task.Run`:

```
CreatingOutput → CreatingSlides → FillingText → FillingImages (→ Done)
```

`JobPhase` (`Job/JobPhase.cs`) has these 5 values (`Done` is the terminal value stamped onto the final
`JobRecord`, never actually "run"). Phase bodies live in `JobRunner.Phases.cs` (a `partial class` split of
`JobRunner`), each in its own `#region`:

- **Phase A — output** (`OpenOutputAsync`/`CreateOutputAsync`/`LoadTemplateSlideAsync`): creates the output
  `.pptx` (copies the template file, strips its slides) if it doesn't exist yet, or reopens it as-is on resume.
- **Phase B — slides** (`RunCreatingSlidesAsync`): appends one cloned template slide per data row, `output.Save()`
  after each.
- **Phase C — text** (`RunFillingTextAsync`, `BuildRowTextValues`): fills placeholder text into each slide via
  `ITextComposer.Compose`, `output.Save()` after each row.
- **Phase D — images** (`InspectSourcesAsync`, `RunFillingImagesAsync`, `ResolveShapeImageAsync`,
  `EnsureDownloadedAsync`, `CropToPngAsync`): inspects every image source once up front (URL → `ContentInfo`,
  via `ICloudClient.InspectAsync`, deduped per job), then per row: downloads (if not already cached, see
  **Per-job download cache** below), crops via `ISmartCropper`, and assigns `IShape.ImageData`, `output.Save()`
  after each row.

There is no `ForEach`/barrier orchestration and no separate "spawn phase workflow" concept — `Service.CreateAsync`
(`Service.cs`) reads the recipe, computes the job list (`Service.BuildJobs`, internal static — a plain
`Recipe.Mappings`-to-`List<JobSpecification>` flattening, see **Input mapping** below), and loops
`jobRunner.StartJobAsync(requestId, jobId, spec, logPath, ct)` once per job — all plain async C# code.
`StartJobAsync` runs `PreflightCleanup` synchronously, persists the job's initial `Pending` `JobRecord` (flushed
immediately, not on the next 1s tick — see **Persistence** below), registers it in the in-memory `_running`
dictionary, then fires `Task.Run(RunJobAsync)` and returns without waiting. Multiple active requests for the
**same recipe** are allowed to run concurrently — there is no recipe-level guard (deleting/updating a recipe
definition doesn't need one either: every `JobRecord` already carries its own fully-resolved `JobSpecification`,
snapshotted from the recipe at spawn time — see **Input mapping**). Instead, `Service.CreateAsync` guards at the
**output-path** level via the private `FindConflictingOutputPathAsync` (not exposed on `IService`): after computing
the new request's job list, it checks every already-active (running/pending/paused) job across all requests for an
`OutputPath` collision and throws if one is found.

**Pause/cancel checkpointing**: `PauseGate` (a small class in `JobRunner.cs`, not DI-registered — one instance per
`RunningJob`) wraps a swappable `TaskCompletionSource<bool>`. `Pause()`/`Resume()` toggle the signal; every phase
loop body calls `await running.Gate.CheckpointAsync(ct)` **before each row**, plus phase transitions naturally fall
between checkpoints too — so pause/cancel granularity is "between rows," never mid-row. `StopJobAsync` cancels the
job's own `CancellationTokenSource` and also calls `Gate.Resume()` so a paused job unblocks immediately to observe
the cancellation rather than sitting blocked on the pause signal forever.

**Data model** (`Job/`): there is no `JobContext`/`TransientContext` split anymore — `JobSpecification`
(fully resolved: `WorkbookPath`, `WorksheetName`, `UsedColumns`, `RowFilter`, `TemplatePresentationPath`,
`TemplateSlideIndex`, `TextInstructions`, `ImageInstructions`, `OutputPath`) plus 4 scalars
(`Status`/`Phase`/`CurrentIndex`/`Timestamp`) fully describe a job's state in one record, `JobRecord`
(`Job/JobRecord.cs`). A `JobRecord` needs nothing else to run or resume — no recipe/workbook lookup, no
transient-only fields to reconstruct after a restart.

**Persistence**: `IJobsRepository`/`JobsRepository` (`Persistence/IJobsRepository.cs`,
`Persistence/JobsRepository.cs`) persist `JobRecord`s to the shared `Data.db` (see **Data.db** below), buffered via
`BufferedRepository<TKey,TValue>` (`Persistence/BufferedRepository.cs`) — a small generic base class: callers
`Enqueue(key, value)` (coalesced, last-write-wins per key), a background `PeriodicTimer` (~1s) atomically drains the
dirty dictionary (`Interlocked.Exchange`) and calls the abstract `UpsertBatchAsync` once per tick in one transaction,
then raises `Flushed` with the batch. `JobsRepository` is the only subclass today; `IRequestsRepository`/
`RequestsRepository` deliberately does **not** inherit it (a request row is written once at creation and never
updated, so buffering would add nothing — see **Data.db** below).

**Crash-resume**: `JobRunner.InitializeAsync` (called once at startup, before the JSON-RPC connection opens) queries
`IJobsRepository.GetNonTerminalAsync()` — any row still `Pending`/`Running`/`Paused` when the process starts is by
definition a crash leftover — and resumes each one directly from its stored `JobRecord` (`Phase`+`CurrentIndex` say
exactly where to pick up; `JobSpecification` says exactly what to do — no recipe/workbook lookup needed at all,
unlike the old WorkflowCore design). A job that was `Paused` before the crash resumes as plain `Running` — there is
no persisted concept of "why it was paused" to restore, and closing/reopening file handles on pause was never
implemented (see the "known limitation" remark on `IJobRunner`) so there's nothing to reopen either; the client can
re-pause it if it wants. `PreflightCleanup` is **not** re-run on resume (it only runs once, from `StartJobAsync`, on
a genuinely new job) — resuming mid-phase never deletes the in-progress output file.

**Request/job identity**: a client-facing `requestId` (`Guid.NewGuid().ToString()`, minted once in
`Service.CreateAsync`) groups N `JobRecord`s; `JobId` is a **plain `int`, 0-based ordinal position within the
request** (assigned by the `for` loop in `Service.CreateAsync`) — not a GUID, not self-generated by anything. There
is no dedicated "request" row/type beyond `RequestRecord` (see **Data.db**) — `Service.ListGroupsAsync` (internal)
groups `IJobsRepository.GetAllAsync()`'s flat result by `RequestId` on every call; `Summary`
(`Summary.cs`) itself carries no `RequestId` field — `IService.ListActiveAsync`/`ListCompletedAsync`
return `IReadOnlyDictionary<string, Summary>` keyed by `RequestId` instead, so the id lives only as the dictionary
key. `Service.DeriveStatus` (internal static) aggregates a group's `JobRecord.Status` values into one request-level
`Status`: any `Running`/`Pending` → `Running`; else any `Paused` → `Paused`; else all `Cancelled` → `Cancelled`;
else → `Complete`. `Summary` is two-level: request-level fields (`Request`, aggregate `Status`, `Phase`
(`RequestPhase?`, computed — see **Progress model** below), `CreatedAt`/`CompletedAt`, request-scoped `Logs`) plus
`Jobs` — an `IReadOnlyDictionary<int, JobSummary>` keyed by job id. `JobSummary` (`Status`, `Phase`, `CurrentIndex`,
`OutputPath`, `CompletedAt`, `Logs`) has **no `Rows` field** — per-row history is not persisted at all (see
**Progress model** below), so historical row-level detail simply doesn't exist past the moment it happens; only
`progress/rows` (live) carries it.
`IService.StopAsync`/`PauseAsync`/`ResumeAsync` (request-scoped — take a `requestId`) fan out best-effort over a
request's job list via `Service.FanOutAsync`, returning `PartialResult(Succeeded, Skipped)` — jobs already in a
terminal/non-eligible state count as skipped, not failed. There is no per-job variant of these on `IService` — its
only surface is request/recipe-scoped: `CreateAsync`, `StopAsync`/`PauseAsync`/`ResumeAsync` (+ `StopAllAsync`/
`PauseAllAsync` bulk variants), `ListActiveAsync`/`ListCompletedAsync`, `DeleteAsync` (stops the request first if
still active), and `DeleteAllCompletedAsync`. There is no single-request query method — a client looks up one
request by indexing the `ListActiveAsync`/`ListCompletedAsync` result dictionary by `requestId`.

**Progress model** (`Progress/Progress.cs`) is 2 records now (`RequestProgress`, `RowProgress`) — **there is no
separate `JobProgress` DTO**; `JobRecord` itself (a job's full current-state row, see **Data model** above) doubles
as the job-scoped progress payload published via `IEventBus.Publish(JobRecord)`, since a job's current state *is*
its progress:

- `RequestProgress` — `RequestId`, `Phase` (`RequestPhase`: `PreparationStarted` | `ProcessingStarted` | `Completed`,
  monotonically increasing), `Timestamp`. Published by `Service.CreateAsync` (`PreparationStarted`, right before the
  spawn loop) and inferred by `ProgressCoalescer` (`ProcessingStarted`/`Completed`, see below) — **never persisted**
  (see **Data.db** below), purely a live notification.
- `RowProgress` — `RequestId`, `JobId`, `RowIndex` (1-based), `Status` (`RowStatus`: `Waiting`/`Processing`/`Done`/
  `Error`), `Stage` (`RowStage`: `None`/`Downloading`/`CroppingImage`/`SavingOutput`), `Note` (free text — e.g. the
  URL being downloaded, or a row's failure message), `Timestamp`. Published exclusively via `JobRunner.ReportRow`
  (a private helper in `JobRunner.Phases.cs`, one call per row/image, like a logger call) — also **never
  persisted**, forwarded live only. The per-row loop bodies don't wrap individual rows in try/catch to report
  `RowStatus.Error` and continue — any exception during a row propagates up through `RunPhasesAsync` and fails the
  whole job (caught in `JobRunner.RunJobAsync`'s outer try/catch, which publishes `Status.Error` on the job).

`Service.CreateAsync` publishes the job's initial `Pending` `JobRecord` indirectly (via `StartJobAsync` →
`Enqueue`/immediate `FlushAsync`, not through `IEventBus`) — the transitions actually published via `IEventBus` are:
`JobRunner.RunJobAsync` (`Running` right before acquiring the semaphore, then `Complete`/`Cancelled`/`Error` on
exit), and `JobRunner.PauseJobAsync`/`ResumeJobAsync` (`Paused`/`Running`).

**`RequestPhase` aggregation** lives entirely in `ProgressCoalescer` (Stdio), not `Service` — a per-request
`RequestAggregateState` (`ExpectedJobCount`/`KnownJobs`/`StartedJobs`/`TerminalJobs`) tracks every `JobRecord` it
sees via `TrackRequestAggregate` (which also does double duty: it's the handler that `Enqueue`s the job into
`IJobsRepository` for persistence). `ExpectedJobCount` comes from `IEventBus.AnnounceExpectedJobCount(requestId,
jobs.Count)`, called by `Service.CreateAsync` right before its spawn loop — using it (rather than however many jobs
have been observed so far) as the denominator avoids a race where job 0 is already `Running` while job 1 hasn't even
been spawned yet, since the spawn loop `await`s each `StartJobAsync` sequentially. `ProcessingStarted` fires once
every announced job has left `Pending`; `Completed` fires once every announced job has reached a terminal `Status`.
This aggregate state is in-memory only, purely for live notification timing — `RequestPhase` itself is **never**
persisted; `Summary.Phase` is instead recomputed on every `ListActiveAsync`/`ListCompletedAsync` call by
`Service`'s own `DeriveRequestPhase` (a much simpler, stateless function operating on the current `JobRecord.Status`
values already fetched from `Data.db` — no in-memory dependency on the coalescer's transition history).

### Data.db — the shared SQLite database

There is a **single** SQLite database (`NameAndPaths.DataFolder.DataFile`, `%LOCALAPPDATA%\SlideGenerator\Data\Data.db`)
shared by every module that needs SQLite — `Recipes` (`SlideGenerator.Recipe`'s `RecipeRepository`), `Requests`
(`RequestsRepository`), and `Jobs` (`JobsRepository`), all in the same file. Each repository independently registers
its own `SqliteConnectionStringBuilder(NameAndPaths.DataFolder.DataFile.ConnectionString)` singleton in its own
module's `Registration.cs` (both `SlideGenerator.Recipe/Registration.cs` and
`SlideGenerator.Generator/Registration.cs` do this — functionally harmless, since both point at the identical
connection string). Schema creation is **centralized**: `SlideGenerator.Settings.Database.DatabaseMigrator.Migrate`
(`src/SlideGenerator.Settings/Database/DatabaseMigrator.cs`) runs 3 embedded DbUp scripts
(`Database/Scripts/0001_CreateRecipes.sql`/`0002_CreateRequests.sql`/`0003_CreateJobs.sql`, one `CREATE TABLE IF NOT
EXISTS` each, `PRAGMA journal_mode=WAL;` prepended to the first) against the connection string, tracked via DbUp's
own `SchemaVersions` table. Called once in `SlideGenerator.Stdio/Program.cs`'s `Main`, right after
`BootstrapSystemLogger` and before `Host.CreateApplicationBuilder` (with `Directory.CreateDirectory(NameAndPaths
.DataFolder.FolderPath)` first, since `NameAndPaths.InitializeDirectories()` — which also creates it — doesn't run
until later, inside `StartupAsync`). None of the 3 repositories create their own tables anymore (`DbEnsureCreated`
has been removed from all of them); DbUp's logging is forwarded to Serilog via a small `IUpgradeLog` adapter
(`DatabaseMigrator.SerilogUpgradeLog`) since `LogToConsole()` would collide with stdout (owned by StreamJsonRpc) and
`LogToAutodetectedLog()` doesn't exist in `dbup-core`. Short-lived-connection-per-operation (open/close per call)
is unchanged for all 3 repositories' CRUD paths.

- **`Jobs`** — every `JobSpecification` field gets its own explicit column, with one deliberate, *named* exception:
  `UsedColumnsJson`/`TextInstructionsJson`/`ImageInstructionsJson` are stored as JSON text, since they're
  variable-length lists of nested (sometimes polymorphic) objects that would otherwise need several normalized
  child tables serving a query pattern nobody actually uses (a job's spec is read once, whole, when it runs) —
  mirrors how `Recipes` already stores an entire `Recipe` graph under one JSON column. `RowFilter` (a small closed
  set of 3 shapes: `AllRowFilter`/`IndexRangeFilter`/`PartitionBlockFilter`) instead gets `RowFilterType` +
  4 nullable scalar columns, since it's small and closed enough not to need JSON. Composite primary key
  `(RequestId, JobId)`.
- **`Requests`** — one explicit column per `Request` DTO field (`RecipeId`/`Name`/`OutputType`/`SaveFolder`/
  `AllowLocalPaths`) plus `LogPath` (the one `.log` file shared by every job of the request) and `CreatedAt`.
  `RecipeId` here is purely informational (`Summary.Request.RecipeId` for display/history) — **not** used at
  resume, since `JobSpecification` is already fully resolved. `RequestId TEXT PRIMARY KEY`.
- There is **no `Rows` table** — per-row progress is never persisted (see **Progress model** above).

`Service.DeleteAsync`/`DeleteAllCompletedAsync` call `jobsRepository.DeleteByRequestIdAsync` and
`requestsRepository.DeleteAsync` so a deleted request doesn't leave orphaned rows in either table.

### Logging scope notifications

Log lines are **not** a separate persisted store — they still go to a per-request `.log` file (`RequestRecord.LogPath`,
one file shared by every job of a request), with a parseable scope path on every line. `SlideGenerator.Logging`'s
`IFileLoggerFactory.CreateFile(filePath, scopePropertyNames, onLogEvent)` takes `scopePropertyNames` (an ordered list
of ambient `LogContext.PushProperty` names to join into each event's scope path — Logging itself has **no** notion
of what a scope means, so it doesn't hardcode `RequestId`/`JobId`/`RowIndex` anywhere) and `onLogEvent` (an
`Action<LogNotification>` invoked once per log line, wired alongside the file sink via `ScopeNotifyingSink`).
`FileLogFormatter` writes the same scope path into the on-disk line so it can be parsed back out later.
`LogNotification.Level` is `Serilog.Events.LogEventLevel` (not a string) — `JobRunner.RunJobAsync` converts it to
the file's 3-letter abbreviation (`"INF"`/`"WRN"`/…) at the point it builds the `LogEntry` handed to
`ILogNotifier.Publish` (this conversion used to live in the now-deleted `Middleware.cs` — there is no step
middleware anymore, `JobRunner.RunJobAsync` does the lazy `ILoggerFactory` init inline at the top of its `try`
block, once per job, using the same `??=`-free-but-equivalent one-shot pattern via a local `using` instead of a
persisted field).

`JobRunner.RunJobAsync` pushes `RequestId`/`JobId` onto `LogContext` for the duration of the whole job (and each
phase's per-row loop additionally pushes `RowIndex` for the duration of that row), so every log line written
anywhere during that scope automatically carries the right path.

`ILogNotifier`/`LogNotifier` (`SlideGenerator.Stdio/Implementations/LogNotifier.cs`) mirror `IEventBus`/
`GeneratingEventBus`'s `dep-interface-ownership` pattern exactly, for the one log-line event. `ProgressCoalescer`
subscribes to `LogNotifier.OnLogEntry` and buffers log lines in an **append-only** `ConcurrentQueue` (never
coalesced/dropped — every line matters) and drains the whole queue every ~1s tick as a `log/entries` notification.

`Service.ToSummaryAsync`/`ToJobSummary` populate `Summary.Logs`/`JobSummary.Logs` by reading the `.log` file straight
off disk on every call, via `ILogFileReader`/`LogFileReader` (`Progress/LogFileReader.cs` — a regex parser matching
`FileLogFormatter`'s line shape) and filtering by scope-path prefix. Deliberately **not** cached in RAM.

### Input mapping

`SlideGenerator.Recipe`'s `Recipe` (`Mappings/Recipe.cs`) is a **flat list of `Mapping`s** — there is no
graph/`Node`/`Edge`/id-lookup anymore:

```csharp
public sealed record Recipe(IReadOnlyList<Mapping> Mappings);

public sealed record Mapping(
    IReadOnlyList<WorksheetSource> Sources,
    PresentationIdentifier TemplatePresentation,
    SlideIdentifier TemplateSlide,
    IReadOnlyList<TextInstruction> TextInstructions,
    IReadOnlyList<ImageInstruction> ImageInstructions);

public sealed record WorksheetSource(
    WorkbookIdentifier Workbook,
    WorksheetIdentifier Worksheet,
    IReadOnlySet<ColumnIdentifier>? UsedColumns = null,
    RowFilter? RowFilter = null);
```

One `Mapping` = one template slide + its text/image instructions, fed by one or more `WorksheetSource`s (worksheets
that share the same template and instructions — the old graph's only real expressiveness need, now just a nested
list, no ids). `Service.BuildJobs` flattens `Mappings.SelectMany(m => m.Sources.Select(...))` — one
`JobSpecification` per (mapping × source) pair, with every value already resolved (`s.Workbook.BookPath`,
`m.TemplatePresentation.PresentationPath`, etc.) — there is no id left to look up against anything at job-run or
resume time. `TextInstruction`/`ImageInstruction`/`ImageEdits`/`RowFilter` (in `Mappings/`) are unchanged
from before — they were always the legitimate "render/execution config" part, never the part that was over-engineered.
`RecipeRepository`'s import path (`RecipeRepository.Package.cs`) normalizes a deserialized `Recipe` with a possibly
`null` `Mappings` (e.g. an archive whose `recipe.json` is `"{}"`, or crafted maliciously) to `[]` rather than letting
a `NullReferenceException` escape — see `imported = imported with { Mappings = imported.Mappings ?? [] };`.

`SummarizationService`/`ISummarizationService` (`SlideGenerator.Summarization`, synchronous) provides workbook and
presentation metadata (`WorkbookSummary`, `PresentationSummary`) used to validate instructions before running
generation — unrelated to `JobRunner`'s own recipe-flattening, used only by the `summarization.*` IPC methods for
the frontend's recipe editor.

## Testing

### Packages (all test projects)

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.*" />
<PackageReference Include="xunit.v3" Version="3.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="NSubstitute" Version="5.*" />
<PackageReference Include="FluentAssertions" Version="8.*" />
```

- **xUnit v3** — use `xunit.v3` package, NOT `xunit` v2.
- `PackageReference Remove="StyleCop.Analyzers"` at top of every test `.csproj` (inherited from `Directory.Build.props`
  but not wanted in tests).

### Test naming

`[Method]_[Scenario]_[ExpectedResult]` — e.g. `Load_SettingsFileNotFound_ReturnsFalse`.

### XML documentation

All test classes and test methods require full XML `<summary>` documentation in English.

### InternalsVisibleTo

When a test needs access to `internal` types, add to the **source** project's `.csproj`:

```xml
<ItemGroup>
    <InternalsVisibleTo Include="SlideGenerator.XYZ.Tests" />
</ItemGroup>
```

### NuGet transitivity pitfall

`Directory.Build.props` sets `PrivateAssets="all"` on **all** `ProjectReference` items globally. NuGet packages from
referenced projects do **not** flow transitively into test projects. Always add an explicit `PackageReference` for any
NuGet package the test project uses directly — even if the source project already references it.

Example: `SlideGenerator.Generator.Tests` must explicitly reference `Microsoft.Data.Sqlite` (used directly by
`JobsRepositoryTests.cs` to inspect the temp-file DB) even though `SlideGenerator.Generator` already references it.

### What NOT to unit test

`JobRunner`'s phase bodies (`RunPhasesAsync` and everything it calls in `JobRunner.Phases.cs`) require a Syncfusion
license + real `.xlsx`/`.pptx` files, plus real NetVips/OpenCV image work — they belong to integration tests, not
unit tests: opening a workbook/template/output presentation, face detection and image crop from a real file,
appending real slides. Do not create unit stubs that bypass this core behavior. What **is** safe to unit test
directly (see `tests/SlideGenerator.Generator.Tests/Unit/`):

- `PreflightCleanup` — plain `File`/`Directory` I/O, no Syncfusion (`PreflightCleanupTests.cs`)
- Pure helper functions lifted out of `JobRunner.Phases.cs` (e.g. `BuildRowTextValues`) — no I/O
  (`JobRunnerHelpersTests.cs`)
- `BufferedRepository<TKey,TValue>` — the buffer/flush mechanics are I/O-free and deterministic given a fake
  `UpsertBatchAsync` (`BufferedRepositoryTests.cs`)
- `JobsRepository`'s row mapping/schema against a real (temp-file) SQLite DB — Dapper + SQLite round-trips don't
  need Syncfusion (`JobsRepositoryTests.cs`)
- `Service`'s aggregation logic (`DeriveStatus`, `BuildJobs`, `FanOutAsync`) against mocked
  `IJobRunner`/`IJobsRepository`/`IRequestsRepository` (`ServiceTests.cs`)

## Development Patterns

### Folder structure

Two coexisting conventions, by module:

**Feature-folder** (all 9 non-host modules) — folders named after a business feature/concept, each folder free to
mix interfaces, implementations, models, and even multiple small related classes in one file (the old "1 file = 1
class" rule doesn't apply within a feature folder — e.g. `SlideGenerator.Document/Workbooks/Workbook.cs` holds both
the `IReadOnlyWorkbook`/`IWorkbook` interfaces and the `SfWorkbook` implementation). Folder names are plural when
the folder's own name would otherwise collide with a type living inside it (e.g. `Recipe/Mappings/` — a
`Mapping.cs` inside a `Mapping/` folder would be a namespace/type name collision, CS0118). Root-level loose files
are fine for cross-feature helpers that don't belong to any one folder (e.g. `SlideGenerator.Logging/Utilities.cs`,
used by both `ConsoleLogFormatter` and everything under `FileLogging/`) or for infra so widely shared that moving
it into a feature folder would be pure namespace churn for no benefit (e.g.
`SlideGenerator.Settings/Rules/NameAndPaths.cs` stayed at its old `Rules/` location — see **Solution Layout**
above). Examples of the convention in practice:

```
SlideGenerator.Document/
├── Workbooks/     — IWorkbookProvider + WorkbookOpener, IWorkbook/IWorksheet + Sf* impls, identifiers
├── Slides/        — IPresentationProvider + PresentationOpener, ISlide/IShape/... + Sf* impls, identifiers
├── Template/      — ITextComposer + TextComposer, ITemplateEngine + TemplateEngine (Mustache-based)
└── Registration.cs

SlideGenerator.Image/
├── FaceDetection/ — IFaceDetector, YuNet (OpenCV adapter), YuNetPool, Face
├── Cropping/      — ISmartCropper/IAnchorCropper/IInterestCropper + impls, RoiOption/RoiMode/AnchorType/InterestType
├── Loading/       — IImageLoader + ImageLoader, IImage/IImageInfo, VipsImage
├── AdapterConversions.cs — root: shared conversion helpers used by more than one feature folder above
└── Registration.cs

SlideGenerator.Recipe/
├── Mappings/      — Recipe/Mapping/WorksheetSource records, TextInstruction, ImageInstruction+ImageEdits, RowFilter
├── RecipeRepository.cs + .Package.cs — root: CRUD + export/import, RecipeEntry, RecipePackageRules
└── Registration.cs

SlideGenerator.Generator/
├── Job/           — IJobRunner + JobRunner (+ JobRunner.Phases.cs partial), PreflightCleanup, JobSpecification,
│                     JobRecord, JobPhase, Status
├── Persistence/   — BufferedRepository<TKey,TValue>, IJobsRepository + JobsRepository, IRequestsRepository +
│                     RequestsRepository, JobSpecificationJson (STJ options for the `*Json` columns)
├── Progress/      — IEventBus, ILogNotifier, ILogFileReader + LogFileReader, RequestProgress/RowProgress,
│                     RowStage, RowStatus
├── IService.cs + Service.cs — root: the IPC-facing facade, doesn't belong to one feature sub-folder
└── Request.cs, Summary.cs, PartialResult.cs, Registration.cs, Utilities.cs — root: shared DTOs/helpers
```

Namespace mirrors the physical folder path 1:1 (e.g. `Image/FaceDetection/YuNet.cs` →
`namespace SlideGenerator.Image.FaceDetection;`, `Generator/Job/JobRunner.cs` →
`namespace SlideGenerator.Generator.Job;`).

`SlideGenerator.Stdio` (the host) keeps its own shape — `Handlers/` (one class per IPC method group) and
`Implementations/` (event bus, log notifier, progress coalescer, JSON-RPC bootstrap, STJ adapters) — which was
already feature-shaped from the start and never needed a separate reorg pass.

### Partial classes for large single-concept services

`JobRunner` (`SlideGenerator.Generator`) is a `partial class` split across `JobRunner.cs` (lifecycle: init/shutdown/
start/pause/resume/stop, the in-memory `_running` registry, `PauseGate`) and `JobRunner.Phases.cs` (the 4-phase
pipeline body, one `#region` per phase) — same pattern the old `GenerateJobStep.*.cs` partials used, kept because
one job-execution concept genuinely needs more code than fits comfortably in one file, not because of any
WorkflowCore-specific convention (which no longer exists — see **Job Execution (JobRunner)** above).

### Coding Style

- `record` for DTOs/value objects; `sealed class` for services.
- File-scoped namespaces.
- `ConfigureAwait(false)` in all library/module async code.
- Primary constructors (C# 12) for services: `public sealed class Foo(IBar bar) : IFoo`.
- Extension members (C# 14) for `Registration.cs` and `Utilities.cs`.
- Class names: max three words.
- Use `#region`/`#endregion` to delimit logical sections within a file — never plain `//` comments for section
  separation.

## Security Patterns (CodeQL)

CodeQL config lives at `.github/codeql/codeql-config.yml` and excludes `backend/tests/**` — test fixtures use deliberate
hardcoded paths and are not production code.

### Path injection (`cs/path-injection`)

`Path.GetFullPath()` is CodeQL's recognized sanitizer. Apply it at every entry point that receives a user-supplied path:

```csharp
// method entry — breaks taint chain
filePath = Path.GetFullPath(filePath);
```

`NameAndPaths.UserPath` resolves to `%LOCALAPPDATA%\SlideGenerator` normally, or to `BasePath` (executable directory)
when the `--portable` flag is passed. Both branches are wrapped with `Path.GetFullPath` so all derived paths inherit the
sanitization. **Do not remove those wrappers.**

`NameAndPaths.IsPortable` (private) is checked at each property access — no caching — so the flag is respected even if
checked early at startup.

Sub-path layout under `UserPath`:

```
UserPath/
├── Settings.yaml          — SettingsFile
├── Instance.pid           — AppLocker
├── Logs/System/           — LogsFolder.SystemPath
├── Logs/Workflows/        — LogsFolder.WorkflowPath (per-request .log files; folder name predates the
│                             WorkflowCore removal, kept as-is — not worth the rename churn)
└── Data/
    └── Data.db            — DataFolder.DataFile (single shared SQLite DB: Recipes/Requests/Jobs tables —
                              see Job Execution (JobRunner) → Data.db above)

TempFolder.RootPath (%TEMP%\SlideGenerator) — per-job download cache, outside UserPath. Structured as
{RootPath}/{requestId}/{jobId}/{hash(url)}{ext} (see JobRunner.JobTempFolder) — deleted wholesale by JobRunner
once that specific job reaches a terminal (non-Paused) state; no shared/cross-job cache anymore.
```

### Resource injection (`cs/resource-injection`)

SQLite connection strings must use `SqliteConnectionStringBuilder`, not string interpolation — the interpolation is what
CodeQL tracks:

```csharp
// ✅
new SqliteConnectionStringBuilder { DataSource = filePath }.ConnectionString

// ❌ — trips cs/resource-injection even with sanitized filePath
$"Data Source={filePath}"
```

### Log forging (`cs/log-forging`)

Strip line endings from path values before logging. `SettingManager` has a `private static string L(string? s)` helper
for this; replicate the pattern in any new service that logs file paths from external input.

## Invariants Checklist

- [ ] Each module has root `Registration.cs` with DI setup
- [ ] Module dependencies flow downward only
- [ ] `JobRunner` phase bodies checkpoint (`PauseGate.CheckpointAsync` + `ct.ThrowIfCancellationRequested()`)
  before every row, never mid-row
- [ ] Async code uses `ConfigureAwait(false)`
- [ ] `record` for data, `sealed` for logic by default
- [ ] Image handling uses `IImage`/NetVips; byte arrays only at boundaries
- [ ] All public APIs have XML documentation comments
- [ ] IPC methods with a DTO param use `UseSingleObjectParameterDeserialization = true` (via the `Attr()` helper in
  `JsonRpcBootstrap.cs`)
- [ ] Serilog never writes to stdout — stderr only
- [ ] User-supplied file paths go through `Path.GetFullPath()` at method entry
- [ ] SQLite connection strings use `SqliteConnectionStringBuilder`, not string interpolation
- [ ] New SQLite tables land in the single shared `Data.db`, not a new per-purpose file
- [ ] Deserializing external/untrusted data (recipe imports, etc.) normalizes possibly-`null` collection fields to
  empty rather than letting a `NullReferenceException` escape (see `Recipe.Package.cs`'s `Mappings ?? []`)
