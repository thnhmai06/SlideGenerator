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

## Architecture: Modular Monolith

SlideGenerator automates PowerPoint generation from Excel data and templates. It uses a **Modular Monolith** with independent modules coordinated via WorkflowCore orchestration.

### Module Map

```
Foundation Modules (no external dependencies)
├── SlideGenerator.Settings      - YAML-based configuration; ISettingProvider
└── SlideGenerator.Cloud         - Multi-cloud URI resolver (Google Drive, OneDrive, SharePoint)

Core Services (depend on Settings)
├── SlideGenerator.Coordinator   - Concurrency throttling; GateLocker + GateType enum
├── SlideGenerator.Download      - HTTP downloading with throttling and progress reporting
└── SlideGenerator.Sheets        - Read-only Excel scanning via Syncfusion

Feature Modules
├── SlideGenerator.Images        - MagickImage processing; ROI + face detection (OpenCV YuNet)
├── SlideGenerator.Slides        - PowerPoint operations; Syncfusion + MagickImage
└── SlideGenerator.Logging       - Database-backed Serilog sink via ILogDbContext (EF Core)

Orchestration
└── SlideGenerator.Services      - WorkflowCore workflows: Scanning + Generating
```

### Dependency Rules

- Dependencies flow downward only — no circular references.
- Each module registers via its own `Registration.cs` (DI entry point).
- `SlideGenerator.Services` is the only module that wires all others together.

## DI Registration Methods

| Module | Extension Method |
|---|---|
| Settings | `SettingsRegistration.AddSettings()` |
| Cloud | `Registration.AddCloudServices()` |
| Coordinator | `Registration.AddCoreServices()` |
| Download | `Registration.AddDownloadServices()` |
| Documents | `Registration.AddDocumentServices()` |
| Logging | `Registration.AddWorkflowLogging()` |

## Concurrency: GateLocker

`GateLocker` (in `SlideGenerator.Coordinator`) provides per-gate semaphores. Limits are read from settings at runtime:

```csharp
await gateLocker.AcquireAsync(GateType.DownloadImage, ct);
try { /* ... */ }
finally { gateLocker.Release(GateType.DownloadImage); }
```

Gate types: `DownloadImage`, `EditImage`, `EditPresentation`, `ReadWorkbook`, `ReadPresentation`.

## Image Processing

Both `SlideGenerator.Images` and `SlideGenerator.Slides` use **MagickImage** as primary type. Convert to/from `byte[]` only at system boundaries (file I/O, Syncfusion API).

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
