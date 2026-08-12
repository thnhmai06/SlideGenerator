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
│   ├── SlideGenerator.Utilities/
│   ├── SlideGenerator.Settings/
│   ├── SlideGenerator.Cloud/
│   ├── SlideGenerator.Document/
│   ├── SlideGenerator.Logging/
│   ├── SlideGenerator.Image/
│   ├── SlideGenerator.Summarization/
│   ├── SlideGenerator.Recipe/
│   ├── SlideGenerator.Generator/
│   └── SlideGenerator.Stdio/
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

SlideGenerator automates PowerPoint generation from Excel data and templates. It is a **Modular Monolith** with
independent modules coordinated by WorkflowCore, exposed to a Tauri frontend through a JSON-RPC 2.0 IPC sidecar.

### Module Map

```
Foundation Modules
├── SlideGenerator.Utilities     - Shared utilities (string normalization, Sha256, generic Pool<T>, helpers)
├── SlideGenerator.Cloud         - Multi-cloud URI resolver (Google Drive, OneDrive, SharePoint)
├── SlideGenerator.Logging       - Serilog: IAppLogger, IFileLoggerFactory, ISystemLogger
├── SlideGenerator.Document      - Syncfusion Excel/PowerPoint abstractions + Mustache template engine
└── SlideGenerator.Image         - MagickImage processing; ROI + face detection (OpenCV YuNet)

Domain Modules
├── SlideGenerator.Settings      - YAML-based configuration; ISettingProvider
├── SlideGenerator.Summarization - Workbook/presentation/recipe metadata scanner
└── SlideGenerator.Recipe        - Recipe CRUD (SQLite) + export/import (*.recipe zip packages)

Application
└── SlideGenerator.Generator     - WorkflowCore generating pipeline (one JobWorkflow instance per job; spawn phase
                                    is plain code, not itself a workflow)

Host
└── SlideGenerator.Stdio         - JSON-RPC 2.0 IPC sidecar (StreamJsonRpc over stdin/stdout)
```

### Dependency Rules

- Dependencies flow downward only — no circular references.
- Each module has a root `Registration.cs` as DI entry point.
- `SlideGenerator.Stdio` is the executable that wires all modules.
- Exception: `SlideGenerator.Generator`'s `Steps/`, `Workflows/`, and `Models/` folders permit depending on
  WorkflowCore directly.

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
| WorkflowCore + SQLite | `services.AddWorkflow(x => x.UseSqlite(NameAndPaths.DataFolder.WorkflowsFile.ConnectionString, true))` |

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

Progress is scoped to 3 levels — Request/Job/Row (see **Progress model** under **Workflow System** below) — and is
coalesced (current-state, last-write-wins) rather than streamed as raw events, so a late-attaching client is never
stuck without state. `ProgressCoalescer` (`Implementations/ProgressCoalescer.cs`) subscribes to `GeneratingEventBus`'s
3 progress events plus `LogNotifier`'s log event, buffers dirty Request/Job/Row entries in `ConcurrentDictionary`s
(coalesced) and log lines in a `ConcurrentQueue` (append-only, never coalesced), then on a 1s `PeriodicTimer` tick:
upserts the dirty Progress into `IStudioRepository` (Studio.db) and forwards each non-empty batch as a JSON-RPC
notification via `JsonRpc.NotifyAsync(method, payload)` — **not** `NotifyWithParameterObjectAsync`, which marshals a
`List<T>` argument by reflecting over its public properties (named-parameter convention) and would serialize an
empty `{}` instead of the array; `NotifyAsync` binds it positionally instead (`"params": [[...]]`). Bound at runtime
via `JsonRpcBootstrap.AttachProgressCoalescer(services, jsonRpc)` → `coalescer.Attach(bus, logNotifier, jsonRpc)` —
not DI injected, since `JsonRpc` doesn't exist until after the DI container is built. `DetachAsync()` (called during
shutdown in `Program.Startup.cs`) flushes one last time before cancelling the timer, so the final <1s window of
buffered progress/logs isn't dropped.

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
- `NodeJsonConverter` (from `SlideGenerator.Recipe.Adapters`)

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

Notifications emitted by the sidecar: `progress/request`, `progress/jobs`, `progress/rows`, `log/entries` — each
batched, at most 1/s (see **Progress notifications** above). Every payload is a single positional array argument
(`params[0]`), a list of `RequestProgress`/`JobProgress`/`RowProgress`/`LogEntry` respectively — not a named-object
param.

## Concurrency: MaxConcurrentJobs

There is no per-operation concurrency gate anywhere in the pipeline anymore (the old `GateType`
`DownloadImage`/`EditImage`/`EditPresentation` gates, `GateLocker<TGate>`, and the `SlideGenerator.Coordinator` module
that hosted them have all been removed — downloading, image editing, and presentation saving run uncontended within a
job). The **sole** concurrency/RAM control is at the job level: each `Job` runs as its own `JobWorkflow` instance (see
**Workflow System** below), so WorkflowCore's built-in `WorkflowOptions.MaxConcurrentWorkflows` directly caps the
number of jobs — and therefore the number of concurrently open `Workbook`/`Presentation` instances — running in
parallel across the whole app.

`WorkflowOptions.MaxConcurrentWorkflows` is an `internal` field read live every poll cycle by WorkflowCore's
`WorkflowConsumer` (not snapshotted at `Start()`), set via the public `WorkflowOptions.UseMaxConcurrentWorkflows(int)`
method — never assign the field directly (it isn't accessible outside the WorkflowCore assembly).

`Service.ApplyMaxConcurrentJobs()` (private, `Services/Service.cs`) calls
`workflowOptions.UseMaxConcurrentWorkflows((int)settingProvider.Current.Performance.MaxConcurrentJobs)` — called once
in `InitializeAsync` (startup) and again at the start of every `CreateAsync` call. It is **not** on `IService` and
`SettingsHandler` does not call it — `Service` reads `ISettingProvider` itself rather than being told to re-apply
externally, so a `settings.performance.update` takes effect the next time a request is created (no restart needed).
`Setting.PerformanceSetting.MaxConcurrentJobs` (default 5) is the
**only** field left on `PerformanceSetting` — the old `MaxParallelDownloadImage`/`MaxParallelEditImage`/
`MaxParallelEditPresentation`/`MaxParallelReadWorkbook`/`MaxParallelReadPresentation` fields and the whole
hardware/network probing system that calibrated them (`SettingProbe`, `SettingTuner`, `SettingCalibrator`,
`ISettingCalibrator`, the `settings.performance.calibrate` IPC method) were deleted along with the gates — there is
nothing left to calibrate.

Because `JobWorkflow` is the **only** workflow type registered with WorkflowCore, this cap only throttles job
execution — it never delays *accepting* a new generation request, since `Service.CreateAsync`'s spawn phase (recipe
read, job-list computation) is plain C# code, not itself a WorkflowCore workflow (see below).

`SlideGenerator.Image`'s `FaceDetectorPool` (a separate concern — pools actual `IFaceDetector`/OpenCV instances, not a
throughput gate) is unrelated to `MaxConcurrentJobs`; it is bounded by a static `Environment.ProcessorCount` limit
(CPU-bound native work) via the generic `Pool<T>` now living in `SlideGenerator.Utilities/Pool.cs`.

## Image Processing

Both `SlideGenerator.Image` and `SlideGenerator.Document` use **MagickImage** as primary type. Convert to/from `byte[]`
only at system boundaries (file I/O, Syncfusion API).

- `Utilities.Decode(byte[])` → `MagickImage`
- `Utilities.Crop(MagickImage, Rectangle)` → `MagickImage`
- `Utilities.Resize(MagickImage, Size)` → `MagickImage`
- Face detection: `MagickImage.ToMat()` converts internally; `RoiResolver.CalculateRoiAsync()` accepts `MagickImage`
- Always use `using` for MagickImage disposal.

## Workflow System (WorkflowCore)

**One `Job` = one WorkflowCore instance.** `JobWorkflow` (`Workflows/JobWorkflow.cs`, implements
`IWorkflow<JobContext>`) is the **only** workflow type registered with WorkflowCore:

```
PreflightCleanup → InspectUrlsStep → GenerateJobStep
```

There is no `ForEach`/barrier orchestration inside WorkflowCore anymore — each job runs to completion independently
in its own instance. `Phase` enum (`Workflows/JobWorkflow.cs`) has 2 values: `Preparation` (only
`PreflightCleanup` maps here now), `Generation` (`GenerateJobStep`); `InspectUrlsStep` still maps to no phase (`null`,
a pre-existing gap, out of scope).

**Spawn phase — deliberately NOT a WorkflowCore workflow**: `Service.CreateAsync` (`Services/Service.cs`)
reads the recipe, computes the job list (`Service.BuildJobs`, internal static — a plain recipe-graph-to-`List<Job>`
flattening), and loops `workflowHost.StartWorkflow(nameof(JobWorkflow), 1, jobContext)` once per job — all as plain
async C# code, not itself a workflow instance. This is intentional: if spawning were itself a WorkflowCore workflow,
it would queue behind running `JobWorkflow` instances under `MaxConcurrentWorkflows` (see **Concurrency:
MaxConcurrentJobs** above), delaying acceptance of new requests by however long existing jobs take to finish. Because
`Service.CreateAsync` runs synchronously to completion (build jobs → guard check → spawn all → return), there's no
persisted "spawn in progress" state to resume if the process crashes mid-loop — any jobs already spawned before a
crash just keep running as ordinary independent `JobWorkflow` instances; jobs not yet spawned are simply never
started, and the client never receives a `requestId` for that failed call (call it again). Multiple active requests
for the **same recipe** are allowed to run concurrently — there is no recipe-level guard (deleting/updating a recipe
definition doesn't need one either, since every in-progress request already holds its own frozen `Recipe` snapshot in
`JobPersistContext.Recipe`). Instead, `Service.CreateAsync` guards at the **output-path** level via the private
`FindConflictingOutputPathAsync` (not exposed on `IService`): after computing the new request's job list, it checks
every already-active (running/paused) job across all requests for an `OutputPath` collision and throws if one is
found — this is what actually prevents the (very narrow) race where jobs orphaned by a mid-spawn crash could have
`PreflightCleanup` from a retry (same or different recipe) delete their in-progress output file.

**Strict iteration rule**: Use WorkflowCore `.ForEach()` for all collection iteration **inside a Step**. **Never** use
C# `foreach`, `Parallel.ForEach`, or `Task.WhenAll` inside a Step. (This no longer applies to the top-level job list —
that's spawned via a plain loop in `Service.CreateAsync`, not inside a Step.)

**Data model** (`Models/Data/`): `JobContext` splits into `JobPersistContext` (serialized: `RequestId` —
groups every `JobWorkflow` instance spawned for the same request, `Request`, `Recipe`, `LogPath`, `Specification` —
a single `JobSpecification`, not a dictionary, `InspectedUrls: ConcurrentDictionary<string, ContentInfo?>` — scoped
to just this one job now) and `TransientContext` (`LoggerFactory`, `TemplateSlides`). `JobSpecification`
(`Models/Data/JobSpecification.cs`) has nested `WorkbookRef`/`PresentationRef` records.

**Persistence**: WorkflowCore persists `JobPersistContext` to SQLite (`%LOCALAPPDATA%\SlideGenerator\Data\Workflows.db`)
via Newtonsoft.Json, one row per job. Fields that cannot serialize (file handles, `ILoggerFactory`) live on
`TransientContext` and/or carry `[Newtonsoft.Json.JsonIgnore]`. Handles are lazily reopened after resume via
`GetOrOpenWorkbook`/`GetOrOpenPresentation`/`GetOrOpenOutput` extension methods in `Utilities.cs`.

**Step middleware** (registered in `AddGeneratorServices`), in `Middleware/Middleware.cs` — the only
step middleware left (`ProgressMiddleware` was removed; step-completion is no longer a Progress concept, see
**Progress model** below):

- `Middleware` — lazily initializes the context's `LoggerFactory` (via `IFileLoggerFactory.CreateFile`) before each
  step, using the log path stored in context (survives persistence resume). Passes a callback that converts each
  `LogNotification` into a `LogEntry` and forwards it via the injected `ILogNotifier` — this is how log lines reach
  the frontend in real time (see **Logging scope notifications** below). Each step calls
  `data.Transient.LoggerFactory.CreateLogger(nameof(Step))` to get a named `ILogger`.

**Request/job aggregation**: A client-facing "request" (the key of `ListActiveAsync`/`ListCompletedAsync`'s returned
dictionary) is a `requestId` GUID grouping N
`JobWorkflow` instances — it is **not** a WorkflowCore instance id itself. `Service.ListGroupsAsync` (internal) builds
an `IReadOnlyDictionary<string, IReadOnlyList<WorkflowInstance>>` on demand by listing every `JobWorkflow` instance
and grouping by `Persist.RequestId` — no dedicated record type; the `RequestId` lives only as the dictionary key,
mirroring how `Summary` itself carries no `RequestId` field (see below). `Service.DeriveStatus` (internal static)
aggregates each group's N `WorkflowInstance.Status` values into one request-level `Status`: any `Runnable` →
`Running`; else any `Suspended` → `Paused`; else all `Terminated` → `Cancelled`; else → `Complete` (covers
all-`Complete` and mixed `Complete`+`Terminated`). `Summary` (`Models/Data/Summary.cs`) has no `RequestId` field of
its own — `IService.ListActiveAsync`/`ListCompletedAsync` return `IReadOnlyDictionary<string, Summary>` keyed by
`RequestId` instead, so the id lives only as the dictionary key, never duplicated onto the value. `Summary` is
otherwise two-level: request-level fields (the original submitted `Request` — carries `Name`/`RecipeId`/etc., no
duplicate scalar fields for those — aggregate `Status`, `CreatedAt`/`CompletedAt`) plus `Jobs` — an
`IReadOnlyDictionary<string, JobSummary>` keyed by job id (the WorkflowCore instance id), one entry per job workflow
instance in the group. `JobSummary` (`Status`, `OutputPath`, `CompletedAt` — no `CreatedAt`, since a job is
created at essentially the same time as its request, already covered by `Summary.CreatedAt`) is built by
`Service.ToJobSummary`; per-job `Status` comes from `Service.ToJobStatus`, a direct 1:1 map off the single job's
`WorkflowStatus` (`Runnable`→`Running`, `Suspended`→`Paused`, `Terminated`→`Cancelled`, else `Complete`) — distinct
from `DeriveStatus`, which aggregates across *all* jobs of a group for the request-level `Status`.
`IService.StopAsync`/`PauseAsync`/`ResumeAsync` (request-scoped — take a `requestId`, not a raw WorkflowCore instance
id) fan out best-effort over a request's job list, returning `PartialResult(Succeeded, Skipped)` — jobs already in
a terminal/non-eligible state count as skipped, not failed. There is no per-job (single WorkflowCore instance id)
variant of these anymore — `IService`'s only surface is request/recipe-scoped: `CreateAsync`, `StopAsync`/`PauseAsync`/
`ResumeAsync` (+ `StopAllAsync`/`PauseAllAsync` bulk variants), `ListActiveAsync`/`ListCompletedAsync`, `DeleteAsync`
(stops the request first if still active), and `DeleteAllCompletedAsync`. There is no single-request query method —
a client looks up one request by indexing the `ListActiveAsync`/`ListCompletedAsync` result dictionary by
`requestId`.
`IsRecipeInUseAsync` and `ApplyMaxConcurrentJobs` are **not** on the interface — the former was removed outright (see
above), the latter is a private method `Service` calls on itself.

**Lifecycle events**: `Service.HandleLifeCycleEvent` (`async void` — deliberate, since `IWorkflowHost.OnLifeCycleEvent`
is a synchronous delegate but resolving the job's `RequestId` needs an async persistence lookup; exceptions are
caught locally so a failed lookup can't crash the process) subscribes to `IWorkflowHost.OnLifeCycleEvent` and, for
`WorkflowStarted`/`WorkflowCompleted`/`WorkflowError`, publishes a `JobProgress` (`Status.Running`/`Complete`/`Error`
respectively) via `IEventBus`. `GeneratingEventBus` (concrete class implementing `IEventBus`) lives in
**`SlideGenerator.Stdio`** (`Implementations/GeneratingEventBus.cs`), not in Generator.

**JobId identity**: there is no custom/deterministic job id anywhere in the system — WorkflowCore always
self-generates `WorkflowInstance.Id` (a GUID) at `CreateNewWorkflow` and gives no way to override it (confirmed: no
`StartWorkflow` overload accepts a caller-supplied instance id, only a separate free-form `reference` string, which
is unused here). Rather than minting a second, redundant job id, **the WorkflowCore-assigned instance id IS the
job's identity** everywhere in this codebase. `Service.CreateAsync` captures it from `StartWorkflow`'s return value
at spawn time and logs `Job spawned | JobId: {JobId} | OutputPath: {OutputPath}` — this is the only place a job's id
first becomes known, before it's later referenced as `JobId` in `Progress` (see below) or as `WorkflowInstance.Id`
inside a `Summary.Jobs` entry.

**Progress model** (`Models/Data/Progress.cs`) is 3 separate records, one per scope, each published through a
matching `IEventBus.Publish` overload — there is no single flat event type anymore:

- `RequestProgress` — `RequestId`, `Phase` (`RequestPhase`: `PreparationStarted` | `ProcessingStarted` | `Completed`,
  monotonically increasing), `Timestamp`. Published by `Service.CreateAsync` (`PreparationStarted`, right before the
  spawn loop) and inferred by `ProgressCoalescer` (`ProcessingStarted`/`Completed`, see below) — never by a Step.
- `JobProgress` — `RequestId`, `JobId`, `Status` (`Status` enum — `Pending`/`Running`/`Complete`/`Paused`/`Cancelled`/
  `Error`), `Timestamp`. Published by `Service.CreateAsync` (`Pending`, right after `StartWorkflow` returns the new
  job id), `Service.HandleLifeCycleEvent` (`Running`/`Complete`/`Error`), and `Service.FanOutAsync`
  (`Paused`/`Cancelled`/`Running` for Pause/Stop/Resume).
- `RowProgress` — `RequestId`, `JobId`, `RowIndex` (1-based), `Status` (`RowStatus`: `Waiting`/`Processing`/`Done`/
  `Error`), `Stage` (`RowStage`: `None`/`Downloading`/`CroppingImage`/`SavingOutput` — only the 3 sub-actions that
  happen inside the per-row loop; the old job-level stages `OpeningWorkbook`/`OpeningPresentation`/`LoadTemplate`/
  `CleaningUpOutput` are now plain `ILogger` calls, not Progress, since `JobProgress` carries no `Stage`), `Note`
  (free text — e.g. the URL being downloaded, or a row's failure message), `Timestamp`. Published exclusively via
  `StepProgress` (`StepProgress.cs`) — a per-step-execution helper constructed once via
  `StepProgress.From(eventBus, requestId, jobId)` at the top of `GenerateJobStep.RunAsync`, then threaded through
  partial-file helper methods as a captured field, exposing `ReportRow(rowIndex, status, stage, note)` (one call per
  row/image, like a logger call) and `SeedRows(rowIndices)` (pre-seeds every remaining row as `Waiting` in one batch
  before the per-row loop starts, so the frontend sees the full row set immediately instead of it growing one row at
  a time). The per-row loop body wraps `GenerateRowSlideAsync` in try/catch: on any non-cancellation exception it
  logs `Error`, reports `RowStatus.Error` with the exception message as `Note`, then rethrows unchanged — a row
  failure still fails the whole job (WorkflowCore's normal retry/error handling), `RowProgress.Error` is only the
  last-recorded state before the crash, not a "log and continue" mechanism.

**`RequestPhase` aggregation** lives entirely in `ProgressCoalescer` (Stdio), not `Service` — a per-request
`RequestAggregateState` (`ExpectedJobCount`/`StartedJobs`/`TerminalJobs`) tracks every `JobProgress` it sees.
`ExpectedJobCount` comes from `IEventBus.AnnounceExpectedJobCount(requestId, jobs.Count)`, called by
`Service.CreateAsync` right before its spawn loop — using it (rather than however many jobs have been observed so
far) as the denominator avoids a race where job 1 is already `Running` while job 2 hasn't even been spawned yet,
since the spawn loop `await`s each `StartWorkflow` sequentially while WorkflowCore fires `WorkflowStarted` from its
own background poller. `ProcessingStarted` fires once every announced job has left `Pending`; `Completed` fires once
every announced job has reached a terminal `Status`. This aggregate state is in-memory only (not in Studio.db) — a
mid-request Stdio process restart loses the real-time phase-transition inference (though Studio.db still holds the
last successfully persisted `Phase`, so `Summary.Phase` itself isn't wrong, just possibly stale until the next
transition).

### Studio.db — Progress persistence

`IStudioRepository`/`StudioRepository` (`Abstractions/IStudioRepository.cs`,
`Services/StudioRepository.cs`) persist current-state Progress (UPSERT semantics) to
`NameAndPaths.DataFolder.StudioFile` (`%LOCALAPPDATA%\SlideGenerator\Data\Studio.db`, WAL mode — same
short-lived-connection-per-operation pattern as `SqliteCache`/`RecipeRepository`), 3 tables (`Requests`/`Jobs`/`Rows`,
composite primary keys, `ON CONFLICT DO UPDATE`). Every upsert method is batch-shaped — `ProgressCoalescer` gathers
every dirty item since the last flush tick and issues one transaction, not one round-trip per item. This is what lets
`Service.ToSummaryAsync`/`ToJobSummaryAsync` answer `ListActiveAsync`/`ListCompletedAsync` with full current
Row/Phase/Status state even for a client that only just attached — Progress is no longer purely a fire-and-forget
event stream. `Service.DeleteAsync`/`DeleteAllCompletedAsync` call `studioRepository.DeleteRequestAsync` alongside
the `Workflows.db` cleanup so a deleted request doesn't leave orphaned Studio.db rows.

### Logging scope notifications

Log lines are **not** a separate persisted store — they still go to the existing per-request `.log` file
(`JobPersistContext.LogPath`, one file shared by every job of a request, unchanged), just with a parseable scope path
on every line now. `SlideGenerator.Logging`'s `IFileLoggerFactory.CreateFile(filePath, scopePropertyNames, onLogEvent)`
takes two new optional parameters: `scopePropertyNames` (an ordered list of ambient `LogContext.PushProperty` names to
join into each event's scope path — Logging itself has **no** notion of what a scope means, so it doesn't hardcode
`RequestId`/`JobId`/`RowIndex` anywhere; `Middleware.cs` in Generator supplies that ordered list, since Generator is
the module that owns the Request/Job/Row concept) and `onLogEvent` (an `Action<LogNotification>` invoked once per log
line, wired alongside the file sink via `Serilog.Core.ILogEventSink`'s `ScopeNotifyingSink`). `FileLogFormatter`
writes the same scope path into the on-disk line (`[{loggerName}/{path}] {levelAbbr}: {message}`) so it can be parsed
back out later. `LogNotification.Level` is `Serilog.Events.LogEventLevel` (not a string) — `ScopeNotifyingSink` just
forwards `logEvent.Level` as-is; `Middleware.cs` converts it to the file's 3-letter abbreviation (`"INF"`/`"WRN"`/…)
only at the Generator↔Logging boundary, when building the `LogEntry` it hands to `ILogNotifier.Publish`, since
Generator's own `LogEntry.Level` is a plain string that has to match what `LogFileReader` parses back out of the file.

`Middleware.cs` pushes each step's `RequestId`/`JobId` onto `LogContext` for the duration of the step (and
`GenerateJobStep.Row.cs` additionally pushes `RowIndex` for the duration of `GenerateRowSlideAsync`), so every log
line written anywhere during that scope automatically carries the right path — no log-line call site has to pass
scope information explicitly.

`ILogNotifier`/`LogNotifier` (`SlideGenerator.Stdio/Implementations/LogNotifier.cs`) mirror `IEventBus`/
`GeneratingEventBus`'s `dep-interface-ownership` pattern exactly, for the one log-line event instead of 3 progress
events. `ProgressCoalescer` subscribes to `LogNotifier.OnLogEntry` the same way it subscribes to the 3 progress
events, but buffers log lines in an **append-only** `ConcurrentQueue` (never coalesced/dropped — unlike Progress,
where only the latest state per key matters) and drains the whole queue every flush tick as a `log/entries`
notification.

`Service.ToSummaryAsync`/`ToJobSummaryAsync` populate `Summary.Logs`/`JobSummary.Logs`/`RowSummary.Logs` by reading
the `.log` file straight off disk on every call, via `ILogFileReader`/`LogFileReader`
(`Services/LogFileReader.cs` — a regex parser matching `FileLogFormatter`'s line shape) and filtering
by scope-path prefix. Deliberately **not** cached in RAM — accumulating every log line for a long-running, high-volume
job risks unbounded memory growth, and `ListActiveAsync`/`ListCompletedAsync` isn't polled often enough for the
per-call file scan to matter.

**Input mapping**: `Recipe.Nodes` defines the graph — each node maps a set of `Sheets` (Excel) to a presentation
template. `TextInstruction` and `ImageInstruction` on each node drive placeholder replacement and image composition.

`SummarizationService`/`ISummarizationService` (`SlideGenerator.Summarization`, synchronous) provides workbook and
presentation metadata (`WorkbookSummary`, `PresentationSummary`) used to validate instructions before running
generation.

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

Example: `SlideGenerator.Generator.Tests` must explicitly reference `WorkflowCore` even though
`SlideGenerator.Generator` already does.

### WorkflowCore unit testing

`WorkflowInstance` is a **concrete class** (`WorkflowCore.Models`), not an interface. Use object initializer, not
`Substitute.For<>()`:

```csharp
var workflow = new WorkflowInstance { Data = data };
var ctx = Substitute.For<IStepExecutionContext>();
ctx.Workflow.Returns(workflow);
```

### What NOT to unit test

Generator steps that require a Syncfusion license + real `.xlsx`/`.pptx` files belong to integration tests, not unit
tests:

- `InspectUrlsStep` — opens workbook via Syncfusion to read image-source cells
- `GenerateJobStep` — opens workbook + template + output presentation, face detection and MagickImage crop from a
  real file, appends real slides

These steps are covered by integration tests only. Do not create unit stubs that bypass their core behavior.
`PreflightCleanup` is plain `File`/`Directory` I/O (no Syncfusion) — safe to unit test directly against real temp
files (see `PreflightCleanupTests.cs`).

## Development Patterns

### Folder structure (flat, functional)

Every module (10/10, no exceptions) uses a flat, functional-folder layout at its project root — no
Domain/Application/Infrastructure/Injection layer split:

```
Abstractions/    — interfaces (domain contracts and use-case ports alike)
Models/          — records, enums, value objects
Rules/           — domain/business rule helpers
Services/        — implementations (use-case and infrastructure alike — DB, HTTP, file)
Adapters/        — anti-corruption wrappers around external libs
Steps/           — WorkflowCore step bodies (Generator only)
Workflows/       — WorkflowCore workflow definitions (Generator only)
Middleware/      — WorkflowCore step middleware (Generator only)
Registration.cs  — DI entry point, at module root, namespace = bare `SlideGenerator.{Module}`
```

Not every module has every folder — a module only has the folders its content needs (e.g.
`SlideGenerator.Utilities` is loose files with no subfolders at all; `SlideGenerator.Summarization` has no
`Adapters/`). Sub-folders under a functional folder (e.g. `Models/Sheet/`, `Models/Slide/`,
`Models/Components/`) are common where a module's types split by concept. Namespace mirrors the physical
folder path 1:1 (e.g. `Models/Sheet/WorkbookSummary.cs` → `namespace SlideGenerator.Summarization.Models.Sheet;`).

### Step (WorkflowCore)

- Inherit `StepBody` or `StepBodyAsync`.
- Live in `Steps/`.
- Process a single item (from `context.Item`); receive via `.Input()` mapping in `Build()`.
- Register as `Transient` in `Registration.cs`.

### Workflow

- Implement `IWorkflow<TData>`.
- Must have a **parameterless constructor** for WorkflowCore registration.

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
├── Logs/Workflows/        — LogsFolder.WorkflowPath
└── Data/
    ├── Workflows.db       — DataFolder.WorkflowsFile
    ├── Recipes.db         — DataFolder.RecipesFile
    └── Cache.db           — DataFolder.CacheFile (shared URL-resolution/download cache; tables via
                              DataFolder.CacheFile.TableNames.InspectedUrlsTable/DownloadedFilesTable)

TempFolder.RootPath (%TEMP%\SlideGenerator) — shared download cache, outside UserPath.
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
- [ ] Steps inherit from `StepBody` or `StepBodyAsync`; live in `Steps/`
- [ ] Workflows implement `IWorkflow<TData>` with a parameterless constructor
- [ ] Async code uses `ConfigureAwait(false)`
- [ ] `record` for data, `sealed` for logic by default
- [ ] `[Newtonsoft.Json.JsonIgnore]` on any non-serializable field in WorkflowCore data classes
- [ ] Image handling uses MagickImage; byte arrays only at boundaries
- [ ] All public APIs have XML documentation comments
- [ ] IPC methods with a DTO param use `UseSingleObjectParameterDeserialization = true` (via the `Attr()` helper in
  `Program.cs`)
- [ ] Serilog never writes to stdout — stderr only
- [ ] User-supplied file paths go through `Path.GetFullPath()` at method entry
- [ ] SQLite connection strings use `SqliteConnectionStringBuilder`, not string interpolation
