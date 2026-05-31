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

SDK: .NET 10.0 (`global.json` pins to `latestMajor`, allows prerelease). The solution uses the XML-based `SlideGenerator.slnx` (no `.sln`). A Syncfusion license is required at runtime: copy `.env.example` to `.env` and fill `SYNCFUSION_LICENSE_KEY` before running the Stdio sidecar.

**GitHub Packages**: `SlideGenerator.Image` depends on per-platform `SlideGenerator.OpenCvSharp4.runtime.*` packages hosted at `nuget.pkg.github.com/thnhmai06`. `backend/nuget.config` reads credentials from `%GITHUB_USERNAME%` and `%GITHUB_TOKEN%` env vars — set these before restoring.

## Solution Layout

```
backend/
├── src/                                — 12 source modules (slnx-tracked)
│   ├── SlideGenerator.Utilities/
│   ├── SlideGenerator.Settings/
│   ├── SlideGenerator.Cloud/
│   ├── SlideGenerator.Cryptography/
│   ├── SlideGenerator.Coordinator/
│   ├── SlideGenerator.Document/
│   ├── SlideGenerator.Logging/
│   ├── SlideGenerator.Image/
│   ├── SlideGenerator.Summarization/
│   ├── SlideGenerator.Recipe/
│   ├── SlideGenerator.Generator/
│   └── SlideGenerator.Stdio/
└── tests/                              — 11 test projects (mirrors src, standalone)
    ├── SlideGenerator.Utilities.Tests/
    ├── SlideGenerator.Cryptography.Tests/
    ├── SlideGenerator.Cloud.Tests/
    ├── SlideGenerator.Coordinator.Tests/
    ├── SlideGenerator.Settings.Tests/
    ├── SlideGenerator.Document.Tests/
    ├── SlideGenerator.Logging.Tests/
    ├── SlideGenerator.Image.Tests/
    ├── SlideGenerator.Recipe.Tests/
    ├── SlideGenerator.Generator.Tests/
    └── SlideGenerator.Stdio.Tests/
```

`src/SlideGenerator.Acquisition/` and `src/SlideGenerator.Collector/` exist on disk but are **not in `SlideGenerator.slnx`** — treat them as orphan/in-progress folders unless re-added to the solution.

`Summarization` has no dedicated test project.

## Architecture: Modular Monolith + IPC Sidecar

SlideGenerator automates PowerPoint generation from Excel data and templates. It is a **Modular Monolith** with independent modules coordinated by WorkflowCore, exposed to a Tauri frontend through a JSON-RPC 2.0 IPC sidecar.

### Module Map

```
Foundation Modules
├── SlideGenerator.Utilities     - Shared utilities (string normalization, helpers)
├── SlideGenerator.Cloud         - Multi-cloud URI resolver (Google Drive, OneDrive, SharePoint)
├── SlideGenerator.Cryptography  - AES-256 encryption + file hash registry
├── SlideGenerator.Coordinator   - Concurrency throttling; IGateLocker<TGate> + GateLocker<TGate>
├── SlideGenerator.Logging       - Serilog: IAppLogger, IFileLoggerFactory, ISystemLogger
├── SlideGenerator.Document      - Syncfusion Excel/PowerPoint abstractions + Mustache template engine
└── SlideGenerator.Image         - MagickImage processing; ROI + face detection (OpenCV YuNet)

Domain Modules
├── SlideGenerator.Settings      - YAML-based configuration; ISettingProvider
├── SlideGenerator.Summarization - Workbook/presentation/recipe metadata scanner
└── SlideGenerator.Recipe        - Recipe CRUD (SQLite) + export/import (*.recipe zip packages)

Application
├── SlideGenerator.Generator     - WorkflowCore generating pipeline (3-phase workflow)
└── SlideGenerator.Stdio         - JSON-RPC 2.0 IPC sidecar (StreamJsonRpc over stdin/stdout)
```

### Dependency Rules

- Dependencies flow downward only — no circular references.
- Each module has `Injection/Registration.cs` (or root `Registration.cs`) as DI entry point.
- `SlideGenerator.Stdio` is the executable that wires all modules.
- Exception: `SlideGenerator.Generator` permits `Application/` and `Domain/` layers to depend on WorkflowCore directly.

## DI Registration Methods

| Module                | Extension Method                                                                            |
|-----------------------|---------------------------------------------------------------------------------------------|
| Settings              | `AddSettingsServices()`                                                                     |
| Cloud                 | `AddCloudServices()`                                                                        |
| Cryptography          | `AddCryptographyServices()`                                                                 |
| Coordinator           | `AddCoordinatorServices()`                                                                  |
| Document              | `AddDocumentServices(ILogger systemLogger)`                                                 |
| Image                 | `AddImageServices()`                                                                        |
| Logging               | `AddLoggingServices(IConfiguration? configuration = null)`                                  |
| Summarization         | `AddSummarizationServices()`                                                                |
| Recipe                | `AddRecipeServices()`                                                                       |
| Generator             | `AddGeneratorServices()`                                                                    |
| Stdio                 | `AddIpcServices()`                                                                          |
| WorkflowCore + SQLite | `services.AddWorkflow(x => x.UseSqlite(NameAndPaths.WorkflowsFile.ConnectionString, true))` |

The system logger is created up-front in `Program.cs` via `SystemLoggerBootstrapper.Initialize(...)` (file Serilog sink → `stderr` only) and passed into `AddDocumentServices`. It is **not** added to DI through an `AddSystemLogging` helper.

`Registration.cs` files use C# 14 **extension member syntax**:
```csharp
extension(IServiceCollection services)
{
    public IServiceCollection AddFooServices() { ... }
}
```

## IPC Layer (SlideGenerator.Stdio)

JSON-RPC 2.0 over stdin/stdout using **StreamJsonRpc** with NDJSON framing (`NewLineDelimitedMessageHandler`) and STJ serialization (`SystemTextJsonFormatter`).

### Stream ownership

| Stream | Owner         | Purpose                                  |
|--------|---------------|------------------------------------------|
| stdin  | StreamJsonRpc | Incoming JSON-RPC requests               |
| stdout | StreamJsonRpc | Outgoing responses **and** notifications |
| stderr | Serilog       | System logs only                         |

### JsonRpc setup

`JsonRpc` is created **after** the DI container is built (raw stream access). Not registered in DI. Methods wired via `AddLocalRpcMethod`:

```csharp
// DTO param → UseSingleObjectParameterDeserialization = true (via local Attr() helper)
jsonRpc.AddLocalRpcMethod(method, handler, Attr("workflow.start"));

// No DTO param
jsonRpc.AddLocalRpcMethod(method, handler, new JsonRpcMethodAttribute("settings.get"));
```

### Progress notifications

`WorkflowProgressObserver` (in `Infrastructure/`) subscribes to `GeneratingEventBus.OnProgress` and forwards events as `workflow/progress` notifications via `JsonRpc.NotifyWithParameterObjectAsync`. Bound at runtime via `observer.Attach(bus, jsonRpc)` — not DI injected.

`GeneratingEventBus` is registered as both `GeneratingEventBus` (concrete) and `IGeneratingEventBus` (interface) in the Stdio `Registration.cs` so that `WorkflowProgressObserver.Attach` can receive the concrete type.

### STJ adapters

`Infrastructure/Adapters/` contains custom STJ converters registered in `BuildJsonSerializerOptions()`:
- `RoiOptionJsonAdapter` — polymorphic `RoiOption` discriminated by `"type"` (`"Center"` | `"RuleOfThirds"`)
- `RectangleFJsonAdapter` — `RectangleF` as `{"x", "y", "width", "height"}`

`JsonStringEnumConverter` is registered globally — all enums serialize as strings automatically.

### Registered methods

| Method                          | Handler                                           |
|---------------------------------|---------------------------------------------------|
| `generator.active.start`        | `GeneratingActiveHandler.StartAsync`              |
| `generator.active.cancel`       | `GeneratingActiveHandler.CancelAsync`             |
| `generator.active.pause`        | `GeneratingActiveHandler.PauseAsync`              |
| `generator.active.resume`       | `GeneratingActiveHandler.ResumeAsync`             |
| `generator.active.cancelAll`    | `GeneratingActiveHandler.CancelAllAsync`          |
| `generator.active.pauseAll`     | `GeneratingActiveHandler.PauseAllAsync`           |
| `generator.active.list`         | `GeneratingActiveHandler.ListAsync`               |
| `generator.active.query`        | `GeneratingActiveHandler.QueryAsync`              |
| `generator.completed.list`      | `GeneratingCompletedHandler.ListAsync`            |
| `generator.completed.query`     | `GeneratingCompletedHandler.QueryAsync`           |
| `generator.completed.delete`    | `GeneratingCompletedHandler.DeleteAsync`          |
| `generator.completed.deleteAll` | `GeneratingCompletedHandler.DeleteAllAsync`       |
| `recipe.list`                   | `RecipeHandler.ListAsync`                         |
| `recipe.query`                  | `RecipeHandler.QueryAsync`                        |
| `recipe.add`                    | `RecipeHandler.AddAsync`                          |
| `recipe.update`                 | `RecipeHandler.UpdateAsync`                       |
| `recipe.delete`                 | `RecipeHandler.DeleteAsync`                       |
| `recipe.export`                 | `RecipeHandler.ExportAsync`                       |
| `recipe.import`                 | `RecipeHandler.ImportAsync`                       |
| `summarization.workbook`        | `SummarizationHandler.SummarizeWorkbookAsync`     |
| `summarization.presentation`    | `SummarizationHandler.SummarizePresentationAsync` |
| `summarization.recipe`          | `SummarizationHandler.SummarizeRecipeAsync`       |
| `summarization.recipeById`      | `SummarizationHandler.SummarizeRecipeByIdAsync`   |
| `settings.get`                  | `SettingsHandler.GetAsync`                        |
| `settings.update`               | `SettingsHandler.UpdateAsync`                     |
| `settings.resetToDefaults`      | `SettingsHandler.ResetToDefaultsAsync`            |

Notifications emitted by the sidecar: `workflow/progress`.

## Concurrency: GateLocker

`GateLocker<TGate>` (in `SlideGenerator.Coordinator`) provides per-gate semaphores parameterized over any enum. The concrete `GateType` enum lives in `SlideGenerator.Generator.Domain.Models` and is Generator-specific. `IGateLocker<GateType>` is registered in **Generator's** `Registration.cs` — not in Coordinator — with a lambda that reads limits from `ISettingProvider.Current.Performance` at runtime.

```csharp
await gateLocker.AcquireAsync(GateType.DownloadImage, ct);
try { /* ... */ }
finally { gateLocker.Release(GateType.DownloadImage); }
```

Gate types (`SlideGenerator.Generator.Domain.Models.GateType`): `DownloadImage`, `EditImage`, `EditPresentation`, `ReadWorkbook`, `ReadPresentation`.

## Image Processing

Both `SlideGenerator.Image` and `SlideGenerator.Document` use **MagickImage** as primary type. Convert to/from `byte[]` only at system boundaries (file I/O, Syncfusion API).

- `Utilities.Decode(byte[])` → `MagickImage`
- `Utilities.Crop(MagickImage, Rectangle)` → `MagickImage`
- `Utilities.Resize(MagickImage, Size)` → `MagickImage`
- Face detection: `MagickImage.ToMat()` converts internally; `RoiResolver.CalculateRoiAsync()` accepts `MagickImage`
- Always use `using` for MagickImage disposal.

## Workflow System (WorkflowCore)

`GeneratingWorkflow` orchestrates the full slide generation pipeline. It begins with two preparation steps before the phased pipeline:

| Stage                        | Steps                                                                                                  |
|------------------------------|--------------------------------------------------------------------------------------------------------|
| Prep                         | `LoadRecipeSummary` → `PreflightCleanup`                                                               |
| Phase A – Validation & Setup | `ValidateRequest` → `CreateTemplate` (`.ForEach(ValidationItems)`)                                     |
| Phase B – Resource Prep      | `ExtractData` (`.ForEach(ValidWorksheets)`) → `CollectImage` → `EditImage` (`.ForEach(ImageContexts)`) |
| Phase C – Assembly & Cleanup | `ReplaceSlideData` (`.ForEach(SlideContexts)`) → `CloseAllHandles`                                     |

Phase boundaries are enforced with `ExecutionResult.Next()` barriers — all items in a phase must complete before the next phase begins.

**Strict iteration rule**: Use WorkflowCore `.ForEach()` for all collection iteration. **Never** use C# `foreach`, `Parallel.ForEach`, or `Task.WhenAll` inside a Step.

**Data model**: `GeneratingContext` is the workflow's state class. Intermediate contexts (`SheetContext`, `ImageContext`, `SlideContext`, `ValidationItem`) are populated per phase and fed into `.ForEach()` loops. All live in `Domain/Models/Contexts/`.

**Persistence**: WorkflowCore persists `GeneratingContext` to SQLite (`%LOCALAPPDATA%\SlideGenerator\Workflows.db`) via Newtonsoft.Json. Fields that cannot serialize (file handles, `ILoggerFactory`) carry `[Newtonsoft.Json.JsonIgnore]`. Handles are lazily reopened after resume via `GetOrOpenWorkbook`/`GetOrOpenPresentation`/`GetOrOpenOutput` extension methods in `Application/Utilities.cs`.

**Step middleware** (registered in `AddGeneratorServices`):
- `GeneratingMiddleware` — lazily initializes `GeneratingContext.LoggerFactory` (via `IFileLoggerFactory.CreateForFile`) before each step using `WorkflowLogPath`/`WorkflowScope` stored in context (survives persistence resume). Each step calls `data.LoggerFactory.CreateLogger(nameof(Step))` to get a named `ILogger`.
- `GeneratingProgressMiddleware` — publishes `GeneratingEvent.StepCompleted` + resolved `GeneratingPhase` after each step

**Lifecycle events**: `GeneratingService` subscribes to `IWorkflowHost.OnLifeCycleEvent` to publish `WorkflowCompleted`/`WorkflowError` via `IGeneratingEventBus`. Event types are in `WorkflowCore.Models.LifeCycleEvents`: `WorkflowCompleted`, `WorkflowError`, `WorkflowStarted`, `WorkflowSuspended`, `WorkflowResumed`, `WorkflowTerminated`.

**Progress enums** — each defined in the file where its concept lives:
- `GeneratingPhase` — in `Application/Workflows/GeneratingWorkflow.cs`
- `GeneratingEvent` — in `Application/Abstractions/IGeneratingEventBus.cs`
- `GeneratingStatus` — in `Domain/Models/GeneratingStatus.cs`

**Input mapping**: `Recipe.Nodes` defines the graph — each node maps a set of `Sheets` (Excel) to a presentation template. `TextInstruction` and `ImageInstruction` on each node drive placeholder replacement and image composition.

**Error resilience**: Each context class has a `ConcurrentDictionary<string, Exception> Errors`. Steps catch exceptions and record them, allowing partial success.

`ScanningService` (synchronous) provides workbook and presentation metadata (`WorkbookSummary`, `PresentationSummary`) used to validate instructions before running generation.

## Testing

### Packages (all test projects)

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.*" />
<PackageReference Include="xunit.v3" Version="1.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="NSubstitute" Version="5.*" />
<PackageReference Include="FluentAssertions" Version="8.*" />
```

- **xUnit v3** — use `xunit.v3` package, NOT `xunit` v2.
- `PackageReference Remove="StyleCop.Analyzers"` at top of every test `.csproj` (inherited from `Directory.Build.props` but not wanted in tests).

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

`Directory.Build.props` sets `PrivateAssets="all"` on **all** `ProjectReference` items globally. NuGet packages from referenced projects do **not** flow transitively into test projects. Always add an explicit `PackageReference` for any NuGet package the test project uses directly — even if the source project already references it.

Example: `SlideGenerator.Generator.Tests` must explicitly reference `WorkflowCore` even though `SlideGenerator.Generator` already does.

### WorkflowCore unit testing

`WorkflowInstance` is a **concrete class** (`WorkflowCore.Models`), not an interface. Use object initializer, not `Substitute.For<>()`:

```csharp
var workflow = new WorkflowInstance { Data = data };
var ctx = Substitute.For<IStepExecutionContext>();
ctx.Workflow.Returns(workflow);
```

### What NOT to unit test

Generator steps that require a Syncfusion license + real `.xlsx`/`.pptx` files belong to integration tests, not unit tests:

- `ValidateRequest` — opens workbook via Syncfusion
- `CreateTemplate` — copies and opens real .pptx
- `ExtractData` — reads Excel cells
- `EditImage` — face detection and MagickImage crop from a real file

These steps are covered by integration tests only. Do not create unit stubs that bypass their core behavior.

## Development Patterns

### Folder structure (Clean Architecture)

All modules follow a layered folder layout:

```
Domain/
  Abstractions/   — domain interfaces (port definitions owned by domain)
  Models/         — records, enums, value objects
Application/
  Abstractions/   — use-case interfaces (input/output ports)
  Services/       — use-case implementations
  Steps/          — WorkflowCore step bodies (Generator only)
  Workflows/      — WorkflowCore workflow definitions (Generator only)
Infrastructure/
  Adapters/       — anti-corruption wrappers around external libs
  Services/       — infrastructure implementations (DB, HTTP, file)
  Middleware/     — WorkflowCore step middleware (Generator only)
Injection/
  Registration.cs — DI entry point
```

### Step (WorkflowCore)

- Inherit `StepBody` or `StepBodyAsync`.
- Live in `Application/Steps/`.
- Process a single item (from `context.Item`); receive via `.Input()` mapping in `Build()`.
- Inject `IGateLocker<GateType>` via constructor; call `AcquireAsync`/`Release` around shared resource access.
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
- [ ] IPC methods with a DTO param use `UseSingleObjectParameterDeserialization = true` (via the `Attr()` helper in `Program.cs`)
- [ ] Serilog never writes to stdout — stderr only
