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
├── SlideGenerator.Settings      - YAML-based configuration; ISettingProvider
└── SlideGenerator.Cloud         - Multi-cloud URI resolver (Google Drive, OneDrive, SharePoint)

Core Services (depend on Settings)
├── SlideGenerator.Coordinator   - Concurrency throttling; GateLocker + GateType enum
├── SlideGenerator.Download      - HTTP downloading with throttling and progress reporting
└── SlideGenerator.Documents     - Excel/PowerPoint scanning and processing via Syncfusion

Feature Modules
├── SlideGenerator.Images        - MagickImage processing; ROI + face detection (OpenCV YuNet)
└── SlideGenerator.Logging       - Serilog sinks: async rolling file + database (EF Core)

Orchestration & Entry Point
├── SlideGenerator.Pipelines     - WorkflowCore workflows: Scanning + Generating
└── SlideGenerator.Ipc           - JSON-RPC 2.0 IPC sidecar (StreamJsonRpc over stdin/stdout)
```

### Dependency Rules

- Dependencies flow downward only — no circular references.
- Each module registers via its own `Registration.cs` (DI entry point).
- `SlideGenerator.Ipc` is the executable entry point that wires all modules together.

## DI Registration Methods

| Module | Extension Method |
|---|---|
| Settings | `AddSettingServices()` |
| Cloud | `AddCloudServices()` |
| Coordinator | `AddCoreServices()` |
| Download | `AddDownloadServices()` |
| Documents | `AddDocumentServices()` |
| Images | `AddImageServices()` |
| Logging | `AddSystemLogging(configuration, logFilePath)` / `AddWorkflowLogging()` |
| Pipelines | `AddGeneratingServices()` |
| Ipc | `AddIpcServices()` |

## IPC Layer (SlideGenerator.Ipc)

The IPC sidecar exposes 9 JSON-RPC 2.0 methods to the Tauri frontend over stdin/stdout using **StreamJsonRpc** with NDJSON framing (`NewLineDelimitedMessageHandler`) and STJ serialization (`SystemTextJsonFormatter`).

### Stream ownership

| Stream | Owner | Purpose |
|---|---|---|
| stdin | StreamJsonRpc | Incoming JSON-RPC requests |
| stdout | StreamJsonRpc | Outgoing responses **and** notifications |
| stderr | Serilog | System logs only |

### JsonRpc setup

`JsonRpc` is created **after** the DI container is built (in `Program.cs`) because it requires raw stream access. It is **not** registered in the DI container. Method handlers are wired via `AddLocalRpcMethod`:

```csharp
// DTO param → UseSingleObjectParameterDeserialization = true
jsonRpc.AddLocalRpcMethod(method, handler, new JsonRpcMethodAttribute("workflow.start") { UseSingleObjectParameterDeserialization = true });

// No DTO param (only CancellationToken)
jsonRpc.AddLocalRpcMethod(method, handler, new JsonRpcMethodAttribute("settings.get"));
```

### Progress notifications

`WorkflowProgressObserver` subscribes to `WorkflowEventBus.OnProgress` and forwards events as `workflow/progress` notifications via `JsonRpc.NotifyWithParameterObjectAsync`. It receives the `JsonRpc` instance through `Attach(bus, jsonRpc)` at startup — not via DI injection.

### STJ adapters

`Ipc/Adapters/` contains custom STJ converters registered in `BuildJsonSerializerOptions()`:
- `RoiOptionJsonAdapter` — polymorphic `RoiOption` discriminated by `"type"` (`"Center"` | `"RuleOfThirds"`)
- `RectangleFJsonAdapter` — `RectangleF` as `{"x", "y", "width", "height"}`

### Registered methods

| Method | Handler |
|---|---|
| `workflow.start` | `WorkflowHandler.StartAsync` |
| `workflow.cancel` | `WorkflowHandler.CancelAsync` |
| `workflow.pause` | `WorkflowHandler.PauseAsync` |
| `workflow.resume` | `WorkflowHandler.ResumeAsync` |
| `scanning.scanWorkbook` | `ScanningHandler.ScanWorkbookAsync` |
| `scanning.scanPresentation` | `ScanningHandler.ScanPresentationAsync` |
| `settings.get` | `SettingsHandler.GetAsync` |
| `settings.update` | `SettingsHandler.UpdateAsync` |
| `settings.resetToDefaults` | `SettingsHandler.ResetToDefaultsAsync` |

## Concurrency: GateLocker

`GateLocker` (in `SlideGenerator.Coordinator`) provides per-gate semaphores. Limits are read from settings at runtime:

```csharp
await gateLocker.AcquireAsync(GateType.DownloadImage, ct);
try { /* ... */ }
finally { gateLocker.Release(GateType.DownloadImage); }
```

Gate types: `DownloadImage`, `EditImage`, `EditPresentation`, `ReadWorkbook`, `ReadPresentation`.

## Image Processing

Both `SlideGenerator.Images` and `SlideGenerator.Documents` use **MagickImage** as primary type. Convert to/from `byte[]` only at system boundaries (file I/O, Syncfusion API).

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

**Strict iteration rule**: Use WorkflowCore `.ForEach()` for all collection iteration. **Never** use C# `foreach`, `Parallel.ForEach`, or `Task.WhenAll` inside an Activity.

**Data model**: `GeneratingTask` is the workflow's state class. Intermediate tasks (`SheetTask`, `ImageTask`, `SlideTask`) are populated per phase and fed into `.ForEach()` loops.

**Input mapping**: `Recipe.Nodes` defines the graph — each node maps a set of `Sheets` (Excel) to a presentation template. `TextInstruction` and `ImageInstruction` on each node drive placeholder replacement and image composition.

**Error resilience**: Each data class has a `ConcurrentDictionary<string, Exception> Errors`. Activities catch exceptions and record them, allowing partial success.

`ScanningService` (synchronous) provides workbook and presentation metadata (`WorkbookSummary`, `PresentationSummary`) used to validate instructions before running generation.

## Development Patterns

### Activity

- Inherit `StepBody` or `StepBodyAsync`.
- Process a single item (from `context.Item`); receive it via `.Input()` mapping in the workflow `Build()`.
- Inject `GateLocker` via constructor; call `AcquireAsync`/`Release` around shared resource access.
- Register as Singleton or Transient in the module's `Registration.cs`.

### Workflow

- Implement `IWorkflow<TData>`.
- Must have a **parameterless constructor** for WorkflowCore registration.
- Inject `IServiceProvider` or specific services via constructor.

### Coding Style

- `record` for DTOs/value objects; `sealed class` for services.
- File-scoped namespaces.
- `ConfigureAwait(false)` in all library/module async code.
- Uniform folder structure per module: `Abstractions/`, `Entities/`, `Models/`, `Rules/`, `Services/`, `Activities/`, `Workflows/` (as applicable).

## Invariants Checklist

- [ ] Each module has a `Registration.cs` with DI setup
- [ ] Module dependencies flow downward only
- [ ] Activities inherit from `StepBody` or `StepBodyAsync`
- [ ] Workflows implement `IWorkflow<TData>` with a parameterless constructor
- [ ] Async code uses `ConfigureAwait(false)`
- [ ] `record` for data, `sealed` for logic by default
- [ ] Services injected via constructor in Activities
- [ ] Image handling uses MagickImage; byte arrays only at boundaries
- [ ] All public APIs have XML documentation comments
- [ ] IPC methods with a DTO param use `UseSingleObjectParameterDeserialization = true`
- [ ] Serilog never writes to stdout — stderr only
