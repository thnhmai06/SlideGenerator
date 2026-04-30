# SlideGenerator Project Guidelines

## 1. Project Overview
SlideGenerator is an automation tool for generating PowerPoint presentations from structured data and templates. It uses a custom Workflow-as-Code engine to coordinate complex, multi-step processes involving cloud resources, AI-driven image processing, and document manipulation.

## 2. System Architecture (Clean Architecture)
The project strictly adheres to Clean Architecture with a unidirectional dependency rule.

| Layer | Project | Dependency Rule | Responsibility |
|---|---|---|---|
| **Domain** | `SlideGenerator.Domain` | No dependencies on other layers | Business Models, Domain Rules, and core Abstractions. |
| **Application** | `SlideGenerator.Application` | Depends on Domain | Use-cases, Registry systems, Workflow DSL, and Infrastructure interface definitions. |
| **Infrastructure** | `SlideGenerator.Infrastructure` | Depends on Application & Domain | Implementation of interfaces using third-party libraries (WorkflowCore, OpenXml, OpenCV, etc.). |

### Mandatory Architectural Rules:
- **Dependency Inversion**: Application must NOT reference Infrastructure implementations or specific engine libraries (e.g., WorkflowCore).
- **Domain Purity**: The Domain layer must remain free of external framework or library logic.
- **Service/Module Separation**:
    - `Modules/`: Feature-agnostic, reusable capabilities (e.g., Cloud, Download, Resources).
    - `Services/`: High-level feature orchestration that uses multiple modules.
- **Uniform Folder Structure**: Every module/service follows a consistent layout: `Abstractions/`, `Entities/`, `Models/`, `Rules/`, `Services/`, and where applicable, `Activities/` and `Workflows/`.

## 3. Workflow DSL
The system uses a custom DSL (located in `Modules/Workflows/DSL/`) for defining long-running processes. Interpretation is handled by `WcWorkflowService` and `WcInterpreterStep` in Infrastructure, backed by WorkflowCore.

### Lexical Scope Tree & Variables
State is managed via a hierarchical scope tree using `Handle<T>` keys.
- **`Handle<T>`**: A stateless, typed key used to store/retrieve checkpoint-able values from the execution context. **Never** store transient handles (file leases, locks, streams) in Variables.
- **Child Scopes**: Iterative nodes (like `ForEach`) create isolated child scopes. `GetVariable` searches the local scope before traversing up the parent chain.

### Core DSL Nodes
| Node | Purpose |
|---|---|
| `Inline.Activity<T>` | Invokes a leaf activity via DI resolution. |
| `Sequence` | Executes children in order. |
| `Parallel` | Executes children concurrently. |
| `ForEach` | Iterates a collection, creating a child scope for each item. |
| `Try` | Provides error isolation (swallows and logs exceptions). |
| `GateWrapper` | Throttles execution via `GateLocker` (semaphore per `GateType`). |
| `Inline` | Performs lightweight state mutations via lambdas. |
| `Condition` | Provides branching logic based on a predicate. |

### Workflow Invariants
- **Orchestration belongs to the Node Tree**: Leaf activities must not contain loops or parallel coordination; use the DSL nodes instead.
- **Variables are the only Scope-Crossing mechanism for checkpoint-able state**: Activities inject `Handle<T>` keys via constructors and read via `context.GetVariable(key)`. DSL lambdas use `ctx.Data` directly.
- **Disk-Bound Persistence**: Every activity should ideally correspond to a disk I/O or state boundary to support crash recovery.
- **Error Isolation**: Use `Try` for granular steps in loops where a single failure should not halt the entire process.

## 4. Execution State Model

All execution state lives under `Modules/Workflows/Models/States/` (base types) and `Services/*/Models/States/` (derived types).

### Core Types

| Type | Description |
|---|---|
| `ExecutionSnapshot` | Abstract base. Carries `Payload`, `Context`, `Status`, `Logger`, and child `Activities` map. |
| `WorkflowSnapshot` | Top-level snapshot for a workflow instance. |
| `ActivitySnapshot` | Snapshot for a single activity invocation. |
| `IExecutionPayload` | Named, checkpoint-able variable store (`GetVariable<T>` / `SetVariable<T>`). One per scope. |
| `IExecutionContext` | Marker interface for **transient** runtime-only state. Implementations add live-resource fields and provide `IServiceProvider` for DI. |

### Transient State via IExecutionContext
Transient resources (open file handles, in-flight locks) must **not** be stored in `Handle<T>`. Instead:
1. Define a class implementing `IExecutionContext` with the required fields (e.g., `WorksheetContext.PresentationLease`).
2. Pass an instance when constructing the scope's snapshot (e.g., `WorksheetSnapshot`).
3. Activities navigate to the snapshot via `context.Snapshot` and cast it to the concrete type to access the context.
4. The first activity that needs a resource acquires it lazily; the last one disposes and nulls it.

**Concrete implementation**: `WorksheetContext` — holds `Lease<IPresentation>? PresentationLease` for Phase B slide editing. Accessed via `((WorksheetSnapshot)snapshot).Context.PresentationLease`.

## 5. Design Patterns

### Registry & Leasing Pattern
Used for managing access to shared or reference-counted resources (e.g., file streams, cloud clients).
- **`Registry<TKey, TResource>`**: Manages resource instantiation and lifecycle.
- **`Lease<T>`**: Returned by `AcquireAsync()`. Implements `IDisposable` and `IAsyncDisposable`.
- **Short-lived leases**: Use `await using var lease = await registry.AcquireAsync(key);` for automatic release within one scope.
- **Long-lived leases**: Store on `IExecutionContext`; the owning activity calls `Dispose()` explicitly when done.

### Gate Locking (Throttling)
Controlled via `GateWrapper` and `GateLocker`. Limits are configured per `GateType` (e.g., Download, EditImage, Worksheet) to prevent resource exhaustion.
- `GateLocker`: semaphore-based, exposes `LockAsync` only (no read/write distinction).
- `FileLocker`: file-path reader-writer lock, exposes `ReadLockAsync` / `WriteLockAsync`.

### Service Injection
- **Activities**: Registered as **Singletons** — inject all required services via constructor.
- **Workflows**: Inject **only `IServiceProvider`** via constructor. Use it to resolve all required services (e.g., `GateLocker`, `ISettingProvider`, or other Workflows).
- **Workflow Metadata**: Workflows must also have a **parameterless constructor** for registration purposes.

## 6. Technical Standards

### Async/Await
- Always use `ConfigureAwait(false)` in library and service code.
- Propagate `CancellationToken` throughout the call chain.

### Logging
- Use `context.Snapshot.Logger.AddLog(...)` within workflows.
- Avoid direct dependency on framework loggers inside business logic modules.

### Coding Style
- **Namespaces**: File-scoped namespaces.
- **Types**: Prefer `record` for DTOs/Value Objects; `sealed` for classes by default.
- **Documentation**: XML documentation is required for all public APIs, specifically detailing Variable usage, DI requirements, and side effects.

### Invariants Checklist
- [ ] Domain has zero dependencies on other layers.
- [ ] Application has zero dependencies on Infrastructure libraries.
- [ ] Activities implement `Activity<TData>` (implicitly via `Inline.Activity`).
- [ ] Workflows implement `IWorkflow<TData>`.
- [ ] Business rules live in Domain or Application — never in Infrastructure.
- [ ] All async code uses `ConfigureAwait(false)`.
- [ ] Transient resources (leases, handles) are stored on `IExecutionContext`, never in `Handle<T>`.
- [ ] Workflows only inject `IServiceProvider`.
- [ ] Activities inject services via constructor.
