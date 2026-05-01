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

## 3. Workflow System (WorkflowCore)
The project uses the **WorkflowCore** engine directly in the Application layer for orchestrating long-running processes.

### Workflow Definition
Workflows are defined by implementing `IWorkflow<TData>`.
- **`TData`**: A strongly-typed class (e.g., `ScanningData`, `GeneratingData`) used to persist the state of the workflow.
- **`Build` method**: Uses the Fluent API to define the sequence, parallel branches, and loops.

### Throttling (Gates & Slots)
To manage resource-intensive operations (I/O, Network, CPU), the system uses a custom throttling mechanism:
- **`GateType`**: Defines the type of resource being throttled (e.g., `DownloadImage`, `EditPresentation`).
- **`AcquireSlotStep`**: Acquires a permit from the `SlotManager`. If no slots are available, it suspends and waits for a "SlotAvailable" event.
- **`ReleaseSlotStep`**: Releases the permit and triggers the event for waiting workflows.

## 4. Execution State & Persistence
State management has shifted from a scoped variable system to a direct data persistence model.

### Data Classes
All workflow state lives in `Services/*/Workflows/*Data.cs` files.
- **Concurrent Access**: Use `ConcurrentDictionary` for state shared across parallel branches.
- **Normalized Paths**: Use absolute, normalized file paths as keys for consistency.

### Error Handling & Resilience
The system prioritizes continuity over stopping on the first failure.
- **`Errors` Dictionary**: Each data class contains a `ConcurrentDictionary<string, Exception> Errors`.
- **Activity Wrapping**: Activities wrap their logic in `try-catch` blocks.
- **Full Exceptions**: Store the entire `Exception` object (including stack traces) for better diagnostics.
- **Partial Success**: The workflow continues to process remaining items (e.g., other rows or files) even if one specific task fails.

## 5. Development Patterns

### Activity (StepBody) Model
Activities inherit from `StepBody` (synchronous) or `StepBodyAsync` (asynchronous).
- **Dependency Injection**: Use standard constructor injection for services.
- **Inputs/Outputs**: Map properties in the workflow's `Build` method.
- **PresentationStepBase**: A specialized base class for PowerPoint operations that ensures file leases are always disposed of.

### Workflow Control
`IScanningService` and `IGeneratingService` provide methods to manage active workflows:
- **`Stop`**: Terminates the workflow immediately.
- **`Pause`**: Suspends execution; current state is persisted.
- **`Resume`**: Continues a previously paused workflow.

### Registry & Leasing Pattern
Used for managing access to shared resources (e.g., file streams).
- **FileRegistry<TResource>**: Manages file-backed resource lifecycle with integrated reader-writer locking.
- **Lease<T>**: Returned by `AcquireAsync()`. Ensure `await using` or explicit disposal to prevent file locks.

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
