# CLAUDE.md

This file provides high-signal guidance for working with the SlideGenerator repository.

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

## Architecture: Clean Architecture

| Layer | Project | Dependency Rule | Responsibility |
|---|---|---|---|
| **Domain** | `SlideGenerator.Domain` | No dependencies | Models, Rules, Abstractions |
| **Application** | `SlideGenerator.Application` | Depends on Domain | Use-cases, DSL, Infrastructure Interfaces |
| **Infrastructure** | `SlideGenerator.Infrastructure` | Depends on App & Domain | External Library Implementations |

### Structure
- `Modules/`: Reusable capabilities (Cloud, Download, Images, Resources, Settings, Slides, Systems, Workflows).
- `Services/`: Feature orchestration (Generating, Scanning).
- Internal folder convention: `Abstractions/`, `Entities/`, `Models/`, `Rules/`, `Services/`, `Activities/`, `Workflows/`.

## Workflow System (WorkflowCore)

The system uses **WorkflowCore** directly for orchestration.

**Core Steps**: `AcquireSlotStep`, `ReleaseSlotStep`, and specific domain activities (e.g., `ScanWorkbook`, `DownloadImage`).

**Data Persistence**:
- Workflows use strongly-typed data classes (e.g., `ScanningData`, `GeneratingData`).
- State is persisted via these classes; use `ConcurrentDictionary` for parallel safety.
- normalized absolute file paths are preferred as dictionary keys.

**Error Resilience**:
- All data classes include an `Errors` dictionary: `ConcurrentDictionary<string, Exception> Errors`.
- Activities use `try-catch` to capture full `Exception` objects, allowing partial success in parallel loops.

## Development Patterns

### Activity (StepBody)
- Inherit from `StepBody` or `StepBodyAsync`.
- Activities are **Singletons** — inject services via constructor.
- Inputs/Outputs are mapped in the workflow's `Build` method.

### PresentationStepBase
- Specialized base class for PowerPoint activities.
- Ensures `FileRegistry` leases are always disposed of, even on failure.

### Workflow Control
- `IGeneratingService` and `IScanningService` support `Stop`, `Pause`, and `Resume` operations via the underlying `IWorkflowHost`.

### Registry & Leasing
- Always acquire resources via `FileRegistry<TResource>.AcquireAsync()`.
- Returns `Lease<T>` (disposable). Use `await using` for short-lived leases.

### Locking
- `FileLocker`: file-path-based reader-writer lock. Exposes `ReadLockAsync` / `WriteLockAsync`.
- `GateLocker`: semaphore-based concurrency limit per `GateType`. Exposes `LockAsync` only.

### Service Injection
- Workflows inject `IServiceProvider` via constructor and resolve dependencies from it.
- Workflows must have a parameterless constructor for registration.

## Invariants Checklist
- [ ] Domain has zero dependencies on other layers.
- [ ] Application has zero dependencies on Infrastructure libraries (except WorkflowCore interface).
- [ ] Activities inherit from `StepBody` or `StepBodyAsync`.
- [ ] Workflows implement `IWorkflow<TData>`.
- [ ] Async code uses `ConfigureAwait(false)`.
- [ ] Use `record` for data, `sealed` for logic by default.
- [ ] Services are injected via constructor in Activities.
