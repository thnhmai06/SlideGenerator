# SlideGenerator Project Guidelines

> **Note**: This file contains core project rules. For detailed architectural explanations, setup guides, and module references, see the [Documents/](./Documents/README.md) folder.

## 1. Project Overview
SlideGenerator is an automation tool for generating PowerPoint presentations from structured data and templates. It uses a **Modular Monolith** architecture with independent modules coordinated via WorkflowCore orchestration.

## 2. System Architecture: Modular Monolith

The project uses **Modular Monolith** architecture—a single deployment unit composed of independent, loosely-coupled modules with clear dependency boundaries.

### Module Hierarchy

```
Foundation (Zero Dependencies)
↓
Core Services
↓
Feature Modules
↓
Orchestration
```

| Category | Modules | Responsibility |
|---|---|---|
| **Foundation** | Settings, Resolver, Common | No external dependencies; core infrastructure |
| **Core Services** | Coordinator, Download, Cryptography | Depend on Settings; shared capabilities |
| **Features** | Image, Document, Logging | Domain-specific operations |
| **Orchestration** | Scanning, Generating | Coordinates module composition |

### Architectural Rules

**Mandatory:**
- **No Circular Dependencies**: Module A cannot depend on a module that (directly or transitively) depends on A.
- **DI-Based Composition**: Modules register via `Registration.cs`; use C# 14 extension members.
- **Module Independence**: Each module must be understandable and testable in isolation.
- **Clear Contracts**: All public APIs MUST have XML documentation.

## 3. Critical Warnings & Best Practices

### ⚠️ Concurrency & File Deletion (Race Conditions)
In Phase B (Resource Preparation), many items process in parallel. 
- **Rule**: Never delete a shared asset (like a downloaded image) within a parallel loop if other tasks might still need it. 
- **Current Issue**: `EditImage` step deletes the source file if it's a temporary download. This is a known risk for shared URLs across multiple shapes.

### 🖼️ Image Replacements
Always use `IImageComposer` (from `SlideGenerator.Document`) to replace image content in shapes. Do not write directly to `shape.ImageData` as `IImageComposer` handles both `IPicture` and `PictureFill` cases correctly.

### 🧵 Thread Safety
- Always use `IGateLocker` when accessing shared resources (Syncfusion objects, file I/O).
- Workflows MUST use the native `.ForEach()` iterator for parallel processing.

## 4. Technical Standards (C# 14 / .NET 10)

For full details, see [Development Standards](./Documents/Guides/Development-Standards.md).

- **Primary Constructors**: Mandatory for service DI.
- **Extension Members**: Mandatory for `Registration.cs`.
- **Async/Await**: Always use `ConfigureAwait(false)`.
- **Types**: `record` for data, `sealed class` for logic.

## 5. Workflow System (WorkflowCore)

### Workflow Model: Phase-Sequential, Item-Parallel
Workflows are structured into 3 logical phases: **Phase A (Setup)**, **Phase B (Resources)**, and **Phase C (Assembly)**.
- **Phase Boundaries**: Phases are separated by `ExecutionResult.Next()` barriers.
- **Strict Iteration (MANDATORY)**: Iteration over local data MUST use WorkflowCore's native `.ForEach()` iterator. **PROHIBITED**: Using C# `foreach` or `Parallel.ForEach` for parallel items inside an Activity.

## 6. Execution State & Persistence

- **State Persistence**: All state lives in the `GeneratingContext` and is persisted to SQLite.
- **Serialization**: Non-serializable fields (loggers, file handles) must be marked `[Newtonsoft.Json.JsonIgnore]` and reopened lazily after resume.

## 7. Invariants Checklist

- [ ] Each module has a `Registration.cs` with DI setup using C# 14 extension syntax.
- [ ] Module dependencies flow downward only.
- [ ] Activities implement `StepBody` or `StepBodyAsync`.
- [ ] Workflows implement `IWorkflow<TData>` with a parameterless constructor.
- [ ] All async code uses `ConfigureAwait(false)`.
- [ ] `IGateLocker` is used for all shared resource access (Workbook/Presentation/Image processing).
- [ ] All public APIs have comprehensive XML documentation.
- [ ] Image processing uses `MagickImage` and is disposed via `using`.
- [ ] Module isolation is maintained.
- [ ] JSON-RPC IPC methods with DTO params use `UseSingleObjectParameterDeserialization = true`.
