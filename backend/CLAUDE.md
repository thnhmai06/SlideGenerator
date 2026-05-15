# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build (uses SlideGenerator.slnx)
dotnet build

# Build release
dotnet build -c Release

# Clean
dotnet clean
```

SDK: .NET 10.0 (`global.json` pins to `latestMajor`).

## Architecture: Modular Monolith + IPC Sidecar

SlideGenerator automates PowerPoint generation from Excel data and templates. It uses a **Modular Monolith** with independent modules coordinated via WorkflowCore orchestration, exposed to a Tauri frontend through a JSON-RPC 2.0 IPC sidecar.

### Module Map

```
Foundation Modules (no external dependencies)
├── SlideGenerator.Common        - Shared utilities (string normalization)
├── SlideGenerator.Settings      - YAML-based configuration; ISettingProvider
└── SlideGenerator.Resolver      - Multi-cloud URI resolver (Google Drive, OneDrive, SharePoint)

Core Services (depend on Settings)
├── SlideGenerator.Cryptography  - AES-256 encryption + file hash registry
├── SlideGenerator.Coordinator   - Concurrency throttling; IGateLocker + GateType
├── SlideGenerator.Download      - HTTP downloading with throttling and progress
├── SlideGenerator.Document      - Syncfusion Excel/PowerPoint abstractions + Mustache template engine
└── SlideGenerator.Logging       - Serilog: IAppLogger, IAppLoggerFactory, ISystemLogger

Feature Modules
└── SlideGenerator.Image         - MagickImage processing; ROI + face detection (OpenCV YuNet)

Orchestration
├── SlideGenerator.Summarization - Workbook/presentation metadata scanner; IRecipeSummarizer (TODO stub)
├── SlideGenerator.Recipe        - Recipe CRUD (SQLite) + export/import (*.recipe zip packages)
└── SlideGenerator.Generator     - WorkflowCore generating pipeline (3-phase workflow)

Entry Point
└── SlideGenerator.Ipc           - JSON-RPC 2.0 IPC sidecar (StreamJsonRpc over stdin/stdout)
```

### Dependency Rules

- Dependencies flow downward only — no circular references.
- Each module has `Injection/Registration.cs` (or root `Registration.cs`) as DI entry point.
- `SlideGenerator.Ipc` is the executable that wires all modules.
- Exception: `SlideGenerator.Generator` permits `Application/` and `Domain/` layers to depend on WorkflowCore directly.

## DI Registration Methods

| Module | Extension Method |
|---|---|
| Settings | `AddSettingServices()` |
| Resolver | `AddCloudServices()` |
| Cryptography | `AddCryptographyServices()` |
| Coordinator | `AddCoordinatorServices()` |
| Download | `AddDownloadServices()` |
| Document | `AddDocumentServices()` |
| Image | `AddImageServices()` |
| Logging (with config) | `AddLoggingModule(IConfiguration)` |
| Logging (defaults) | `AddLoggingModule()` |
| Logging (pre-built logger) | `AddSystemLogging(ISystemLogger)` |
| Summarization | `AddSummarizationServices()` |
| Recipe | `AddRecipeServices()` |
| Generating (Generator) | `AddGeneratingServices()` |
| Ipc | `AddIpcServices()` |
| WorkflowCore + SQLite | `services.AddWorkflow(x => x.UseSqlite(NameAndPaths.WorkflowsFile.ConnectionString, true))` |

`Registration.cs` files use C# 14 **extension member syntax**:
```csharp
extension(IServiceCollection services)
{
    public IServiceCollection AddFooServices() { ... }
}
```

## IPC Layer (SlideGenerator.Ipc)

JSON-RPC 2.0 over stdin/stdout using **StreamJsonRpc** with NDJSON framing (`NewLineDelimitedMessageHandler`) and STJ serialization (`SystemTextJsonFormatter`).

### Stream ownership

| Stream | Owner | Purpose |
|---|---|---|
| stdin | StreamJsonRpc | Incoming JSON-RPC requests |
| stdout | StreamJsonRpc | Outgoing responses **and** notifications |
| stderr | Serilog | System logs only |

### JsonRpc setup

`JsonRpc` is created **after** the DI container is built (raw stream access). Not registered in DI. Methods wired via `AddLocalRpcMethod`:

```csharp
// DTO param → UseSingleObjectParameterDeserialization = true
jsonRpc.AddLocalRpcMethod(method, handler, new JsonRpcMethodAttribute("workflow.start") { UseSingleObjectParameterDeserialization = true });

// No DTO param
jsonRpc.AddLocalRpcMethod(method, handler, new JsonRpcMethodAttribute("settings.get"));
```

### Progress notifications

`WorkflowProgressObserver` (in `Infrastructure/`) subscribes to `GeneratingEventBus.OnProgress` and forwards events as `workflow/progress` notifications via `JsonRpc.NotifyWithParameterObjectAsync`. Bound at runtime via `observer.Attach(bus, jsonRpc)` — not DI injected.

`GeneratingEventBus` is registered as both `GeneratingEventBus` (concrete) and `IGeneratingEventBus` (interface) in the Ipc `Registration.cs` so that `WorkflowProgressObserver.Attach` can receive the concrete type.

### STJ adapters

`Infrastructure/Adapters/` contains custom STJ converters registered in `BuildJsonSerializerOptions()`:
- `RoiOptionJsonAdapter` — polymorphic `RoiOption` discriminated by `"type"` (`"Center"` | `"RuleOfThirds"`)
- `RectangleFJsonAdapter` — `RectangleF` as `{"x", "y", "width", "height"}`

`JsonStringEnumConverter` is registered globally — all enums serialize as strings automatically.

### Registered methods

| Method | Handler |
|---|---|
| `generator.active.start` | `GeneratingActiveHandler.StartAsync` |
| `generator.active.cancel` | `GeneratingActiveHandler.CancelAsync` |
| `generator.active.pause` | `GeneratingActiveHandler.PauseAsync` |
| `generator.active.resume` | `GeneratingActiveHandler.ResumeAsync` |
| `generator.active.cancelAll` | `GeneratingActiveHandler.CancelAllAsync` |
| `generator.active.pauseAll` | `GeneratingActiveHandler.PauseAllAsync` |
| `generator.active.list` | `GeneratingActiveHandler.ListAsync` |
| `generator.active.query` | `GeneratingActiveHandler.QueryAsync` |
| `generator.completed.list` | `GeneratingCompletedHandler.ListAsync` |
| `generator.completed.query` | `GeneratingCompletedHandler.QueryAsync` |
| `generator.completed.delete` | `GeneratingCompletedHandler.DeleteAsync` |
| `generator.completed.deleteAll` | `GeneratingCompletedHandler.DeleteAllAsync` |
| `recipe.list` | `RecipeHandler.ListAsync` |
| `recipe.query` | `RecipeHandler.QueryAsync` |
| `recipe.add` | `RecipeHandler.AddAsync` |
| `recipe.update` | `RecipeHandler.UpdateAsync` |
| `recipe.delete` | `RecipeHandler.DeleteAsync` |
| `recipe.export` | `RecipeHandler.ExportAsync` |
| `recipe.import` | `RecipeHandler.ImportAsync` |
| `summarization.workbook` | `SummarizationHandler.SummarizeWorkbookAsync` |
| `summarization.presentation` | `SummarizationHandler.SummarizePresentationAsync` |
| `settings.get` | `SettingsHandler.GetAsync` |
| `settings.update` | `SettingsHandler.UpdateAsync` |
| `settings.resetToDefaults` | `SettingsHandler.ResetToDefaultsAsync` |

## Concurrency: GateLocker

`GateLocker` (in `SlideGenerator.Coordinator`) provides per-gate semaphores. Limits read from settings at runtime:

```csharp
await gateLocker.AcquireAsync(GateType.DownloadImage, ct);
try { /* ... */ }
finally { gateLocker.Release(GateType.DownloadImage); }
```

Gate types: `DownloadImage`, `EditImage`, `EditPresentation`, `ReadWorkbook`, `ReadPresentation`.

## Image Processing

Both `SlideGenerator.Image` and `SlideGenerator.Document` use **MagickImage** as primary type. Convert to/from `byte[]` only at system boundaries (file I/O, Syncfusion API).

- `Utilities.Decode(byte[])` → `MagickImage`
- `Utilities.Crop(MagickImage, Rectangle)` → `MagickImage`
- `Utilities.Resize(MagickImage, Size)` → `MagickImage`
- Face detection: `MagickImage.ToMat()` converts internally; `RoiResolver.CalculateRoiAsync()` accepts `MagickImage`
- Always use `using` for MagickImage disposal.

## Workflow System (WorkflowCore)

`GeneratingWorkflow` orchestrates the full slide generation pipeline in 3 phases:

| Phase | Steps |
|---|---|
| A – Validation & Setup | `ValidateRequest` → `CreateTemplate` |
| B – Resource Prep | `ExtractData` → `DownloadImage` → `EditImage` |
| C – Assembly & Cleanup | `ReplaceSlideData` → `CloseAllHandles` |

Phase boundaries are enforced with `ExecutionResult.Next()` barriers — all items in a phase must complete before the next phase begins.

**Strict iteration rule**: Use WorkflowCore `.ForEach()` for all collection iteration. **Never** use C# `foreach`, `Parallel.ForEach`, or `Task.WhenAll` inside a Step.

**Data model**: `GeneratingContext` is the workflow's state class. Intermediate contexts (`SheetContext`, `ImageContext`, `SlideContext`) are populated per phase and fed into `.ForEach()` loops. All live in `Domain/Models/Contexts/`.

**Persistence**: WorkflowCore persists `GeneratingContext` to SQLite (`%LOCALAPPDATA%\SlideGenerator\Workflows.db`) via Newtonsoft.Json. Fields that cannot serialize (file handles, `IAppLogger`) carry `[Newtonsoft.Json.JsonIgnore]`. Handles are lazily reopened after resume via `GetOrOpenWorkbook`/`GetOrOpenPresentation`/`GetOrOpenOutput` extension methods in `Application/Utilities.cs`.

**Step middleware** (registered in `GeneratingServices`):
- `GeneratingLoggerMiddleware` — lazily initializes `GeneratingContext.Logger` before each step using `WorkflowLogPath`/`WorkflowScope` stored in context (survives persistence resume)
- `GeneratingProgressMiddleware` — publishes `GeneratingEvent.StepCompleted` + resolved `GeneratingPhase` after each step

**Lifecycle events**: `GeneratingService` subscribes to `IWorkflowHost.OnLifeCycleEvent` to publish `WorkflowCompleted`/`WorkflowError` via `IGeneratingEventBus`. Event types are in `WorkflowCore.Models.LifeCycleEvents`: `WorkflowCompleted`, `WorkflowError`, `WorkflowStarted`, `WorkflowSuspended`, `WorkflowResumed`, `WorkflowTerminated`.

**Progress enums** — each defined in the file where its concept lives:
- `GeneratingPhase` — in `Application/Workflows/GeneratingWorkflow.cs`
- `GeneratingEvent` — in `Application/Abstractions/IGeneratingEventBus.cs`
- `GeneratingStatus` — in `Domain/Models/GeneratingStatus.cs`

**Input mapping**: `Recipe.Nodes` defines the graph — each node maps a set of `Sheets` (Excel) to a presentation template. `TextInstruction` and `ImageInstruction` on each node drive placeholder replacement and image composition.

**Error resilience**: Each context class has a `ConcurrentDictionary<string, Exception> Errors`. Steps catch exceptions and record them, allowing partial success.

`ScanningService` (synchronous) provides workbook and presentation metadata (`WorkbookSummary`, `PresentationSummary`) used to validate instructions before running generation.

## Development Patterns

### Folder structure (Clean Architecture)

All modules follow layered folder layout:

```
Domain/
  Abstractions/   — domain interfaces (port definitions owned by domain)
  Models/         — records, enums, value objects
Application/
  Abstractions/   — use-case interfaces (input/output ports)
  Services/       — use-case implementations
  Steps/          — WorkflowCore step bodies (Generating only)
  Workflows/      — WorkflowCore workflow definitions (Generating only)
Infrastructure/
  Adapters/       — anti-corruption wrappers around external libs
  Services/       — infrastructure implementations (DB, HTTP, file)
  Middleware/     — WorkflowCore step middleware (Generating only)
Injection/
  Registration.cs — DI entry point
```

### Step (WorkflowCore)

- Inherit `StepBody` or `StepBodyAsync`.
- Live in `Application/Steps/`.
- Process a single item (from `context.Item`); receive via `.Input()` mapping in `Build()`.
- Inject `IGateLocker` via constructor; call `AcquireAsync`/`Release` around shared resource access.
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
- Class names: max 3 words.
- Use `#region`/`#endregion` to delimit logical sections within a file — never plain `//` comments for section separation.

## Invariants Checklist

- [ ] Each module has `Injection/Registration.cs` with DI setup
- [ ] Module dependencies flow downward only
- [ ] Steps inherit from `StepBody` or `StepBodyAsync`; live in `Application/Steps/`
- [ ] Workflows implement `IWorkflow<TData>` with a parameterless constructor
- [ ] Async code uses `ConfigureAwait(false)`
- [ ] `record` for data, `sealed` for logic by default
- [ ] `[Newtonsoft.Json.JsonIgnore]` on any non-serializable field in WorkflowCore data classes
- [ ] Image handling uses MagickImage; byte arrays only at boundaries
- [ ] All public APIs have XML documentation comments
- [ ] IPC methods with a DTO param use `UseSingleObjectParameterDeserialization = true`
- [ ] Serilog never writes to stdout — stderr only
