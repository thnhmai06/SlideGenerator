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

## Workflow DSL (Application Layer)

The DSL resides in `Modules/Workflows/DSL/`. Interpretation happens in Infrastructure via `WorkflowInterpreter` (singleton, injected via DI).

**Core Nodes**: `ActivityNode<T>`, `SequenceNode`, `ParallelNode`, `ForEachNode`, `TryNode`, `GateNode`, `InlineNode`, `ConditionNode`.

**Variable System**:
- `Variable<T>` is a stateless typed key. Only for checkpoint-able values — never store transient handles (leases, streams) in Variables.
- `ForEachNode` creates a child scope for isolation.
- Activities inject `Variable<T>` keys via constructor; read via `context.GetVariable(key)`.
- Lambdas access `context.Data` (typed) directly.

## Execution State Model (`Modules/Workflows/Models/States/`)

| Type | Role |
|---|---|
| `ExecutionSnapshot` | Abstract base; carries `Payload`, `Context`, `Status`, `Logger`, child `Activities`. |
| `WorkflowSnapshot` | Top-level workflow snapshot. |
| `ActivitySnapshot` | Per-activity snapshot. |
| `IExecutionPayload` | Named get/set store for checkpoint-able values (string-keyed). |
| `IExecutionContext` | Marker interface for transient runtime-only state. Implementations add fields for live resources (leases, handles). Never checkpoint. |

Derived generating snapshots (`GeneratingSnapshot`, `WorkbookSnapshot`, `WorksheetSnapshot`) live in `Services/Generating/Models/States/`.

**`WorksheetContext`** (`IExecutionContext`) is the only concrete context so far — holds `PresentationLease` for Phase B slide editing.

## Development Patterns

### Registry & Leasing
- Always acquire resources via `Registry<TKey, TResource>.AcquireAsync()`.
- Returns `Lease<T>` (disposable). Use `await using` for short-lived leases.
- For leases that must survive across multiple activities, store on the scope's `IExecutionContext` implementation (e.g., `WorksheetContext.PresentationLease`), not in `Variable<T>`.

### Transient State via IExecutionContext
- Transient resources (open file handles, in-flight locks) live on `IExecutionContext` implementations, not in the `Variable<T>` system.
- The first activity that needs the resource acquires it lazily; the last one disposes and nulls it.
- Access via `((ConcreteSnapshot)context.State).Context.FieldName`.

### Locking
- `FileLocker`: file-path-based reader-writer lock. Exposes `ReadLockAsync` / `WriteLockAsync`.
- `GateLocker`: semaphore-based concurrency limit per `GateType`. Exposes `LockAsync` only (no read/write distinction).
- `ISlotLocker<TKey>` has been removed — do not reintroduce it.

### Service Injection in Activities & Workflows
- Activities are Singletons — inject services via constructor.
- **No `GetRequiredService` on `IActivityContext`** — that method has been removed.
- `WorkflowDefinition` classes that need services: add both a parameterless ctor (metadata only, used by `WcWorkflowAdapter`) and a DI ctor. The `Build()` method is only called from the DI-resolved instance.

### Invariants
- **No Direct Engine Reference**: Application must not reference WorkflowCore or other infrastructure libraries.
- **Orchestration in DSL**: Loops, parallel tasks, and error isolation (`TryNode`) belong in the workflow definition, not in `ILeafActivity`.
- **Stateless Activities**: Activities are Singletons; all runtime state goes through `IActivityContext` (variables or `IExecutionContext`).
- **Logging**: Use `context.State.Logger.AddLog(...)`.
- **Async**: Use `ConfigureAwait(false)` in all library/service code.

## System Invariants Checklist
- [ ] Domain has zero dependencies on other layers.
- [ ] Application has zero dependencies on Infrastructure libraries.
- [ ] Activities implement `ILeafActivity<TData>`.
- [ ] Workflows implement `IWorkflowDefinition<TData>`.
- [ ] Async code uses `ConfigureAwait(false)`.
- [ ] Every Activity corresponds to a disk I/O or state boundary.
- [ ] Use `record` for data, `sealed` for logic by default.
- [ ] Transient resources (leases, handles) are stored on `IExecutionContext`, never in `Variable<T>`.
- [ ] Services are injected via constructor — never via `context.GetRequiredService`.
