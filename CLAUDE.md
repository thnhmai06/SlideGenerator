# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Does

SlideGenerator automates PowerPoint generation from templates. Given an Excel workbook and a PowerPoint template, it iterates over worksheet rows and produces one slide per row — replacing Mustache placeholders with text values and image shapes with downloaded/edited images.

## Build Commands

```bash
# Build (solution uses SlideGenerator.slnx)
dotnet build

# Build release
dotnet build -c Release

# Clean
dotnet clean
```

SDK: .NET 10.0 (`global.json` pins to `latestMajor`, prereleases allowed). No test projects currently exist.

## Architecture: Clean Architecture

Three layers with strict dependency rules:

| Layer | Projects | Dependency Rule | Responsibility |
|---|---|---|---|
| **Domain** | `SlideGenerator.Domain` | No dependencies on other layers | Business Models, Rules, core Abstractions |
| **Application** | `SlideGenerator.Application` | Depends on Domain only | Use-cases, Registry, Workflow DSL, Interface definitions for Infrastructure |
| **Infrastructure** | `SlideGenerator.Infrastructure` | Depends on Application & Domain | Implements interfaces using third-party libs (WorkflowCore, OpenXml, OpenCV…) |

**Application must NOT reference WorkflowCore directly.** It defines its own Workflow-as-Code DSL; Infrastructure interprets this tree via a single `WcInterpreterStep`.

### Module Folder Convention

Each module uses consistent subfolders:
- `Abstractions/` — interfaces/contracts (`I`-prefixed)
- `Entities/` — business objects with identity/behavior
- `Models/` — DTOs, value objects, records
  - `States/` — classes inheriting from `ExecutionState`
  - `Logging/` — `Logger`, `LogEntry`, `LogLevel`
- `Rules/` — constants, enums, domain rules, extension mappings
- `Services/` — use-case/module logic
- `Activities/` — Application-layer workflow leaf activities (implement `ILeafActivity`, no engine dependency)
- `Workflows/` — workflow pipeline definitions (implement `IWorkflowDefinition<TData>`)
- `Adapters/` — external library wrappers or context converters (Infrastructure only)

## Key Technologies

| Library | Purpose |
|---|---|
| **WorkflowCore** (Daniel Gerlag) | Owned by Infrastructure only; Application defines its own DSL |
| **ClosedXML** | Reading Excel workbooks |
| **DocumentFormat.OpenXml** | PowerPoint XML manipulation (never use Spire for slide editing) |
| **OpenCvSharp4 + YuNet** | Face detection for image ROI |
| **Magick.NET-Q8** | Image processing/editing |
| **Downloader** | File download with retry |
| **YamlDotNet** | YAML config deserialization |
| **Stubble.Core** | Mustache template rendering |

## Core Design Patterns

### Registry Pattern (Reference-Counted Resources)
`Registry<TKey, TResource>` base with `FileRegistry<T>`. Always acquire resources via `AcquireAsync()` — auto-disposes when reference count hits zero. Never hold raw file handles across activities.

### Workflow-as-Code (Application DSL)

The DSL lives in `Application/Workflows/DSL/` and consists of:

**Core interfaces:**
- `ILeafActivity<TData>` — `Task ExecuteAsync(IActivityContext<TData> context)` — implement for all leaf steps; also extends non-generic `ILeafActivity` via a default bridge so `ActivityNode<T>` dispatch works without knowing `TData`
- `IActivityContext` (non-generic) — base scope environment; provides `GetVariable<TVar>(Variable<TVar>)`, `SetVariable<TVar>(Variable<TVar>, TVar)`, `CancellationToken`, `State`, `GetRequiredService<T>()` — **no `object` members, no item/data access**
- `IActivityContext<out TData>` (generic) — extends `IActivityContext`; adds typed `TData Data { get; }` and `CreateChildScope()` which returns a child scope for ForEach iterations; received by `ILeafActivity<TData>` implementations
- `IWorkflowDefinition<TData>` — `WorkflowNode Build()` — implement for workflow definitions

**Lexical Scope Tree — Variable<T> system:**
- `Variable<T>` (`DSL/Variable.cs`) — a pure stateless typed key: `sealed record Variable<T>(string Name)`. Carries no runtime state. Safe to use as `static readonly` field.
- `VariablesDeclaration` — static class of keys for `GeneratingWorkflow` ForEach scopes (file: `Generating/Workflows/VariablesDeclaration.cs`):
  - `WorkbookItem: Variable<WorkbookIdentifier>` — workbook being scanned
  - `PresentationItem: Variable<PresentationIdentifier>` — presentation being scanned
  - `WorksheetItem: Variable<WorksheetIdentifier>` — current worksheet branch
  - `RowItem: Variable<RowIdentifier>` — current row (`RowIdentifier(WorksheetIdentifier Worksheet, int Index)` in Domain)
  - `RowTaskItem: Variable<RowTask>` — current download or edit task
- Each `ForEachNode<TItem, TData>` iteration calls `ctx.CreateChildScope()` creating a child `WcInterpreterContext<TData>` with an isolated local dictionary + parent reference. Items are stored via `SetVariable`; `GetVariable` walks the parent chain (lexical scoping).
- Activities inject `Variable<T>` via constructor (registered as Singletons — stateless), then call `context.GetVariable(xVar)` to read the value from the scope chain.

**Node types** (all `abstract record WorkflowNode` in `DSL/Nodes/`):

| Node | Purpose |
|---|---|
| `ActivityNode<T>` | Invokes `T : ILeafActivity` resolved from DI |
| `SequenceNode` | Executes steps sequentially |
| `ForEachNode<TItem, TData>` | Iterates items; lambda `Func<IActivityContext<TData>, IEnumerable<TItem>>`; stores each item via `Variable<TItem>` in a child scope |
| `ParallelNode` | Runs branches concurrently via `Task.WhenAll` |
| `TryNode` | Catches exceptions; logs to `State.Logger`; swallows so loop continues |
| `SlotGatedNode` | Acquires `IAsyncKeyedLocker<SlotType>` before body; releases after |
| `InlineNode<TData>` | Runs an inline `Func<IActivityContext<TData>, Task>` for lightweight state mutations |
| `ConditionNode<TData>` | Conditional branch (file: `IfNode.cs`); predicate is `Func<IActivityContext<TData>, bool>`; interface `IConditionNode` |

**Key rules:**
- **100% Activity Architecture**: never put orchestration loops or `Task.WhenAll` inside an `ILeafActivity` — all coordination must be expressed through the node tree
- **No `object` in Application DSL**: never use raw `object` in interface members — activities receive typed `IActivityContext<TData>` with `context.Data`; DSL lambdas receive `IActivityContext<TData>` directly; `Variable<T>` is the only crossing point between scopes
- **No `RequireItem`, no `GetData`**: these are removed. Activities inject `Variable<T>` keys and read via `context.GetVariable(key)`. DSL lambdas use `ctx.Data` (typed).
- `ForEachNode<TItem, TData>` creates a child scope per iteration — parallel branches are physically isolated, no AsyncLocal needed
- `SlotGatedNode` replaces the old `AcquireSlot`/`ReleaseSlot` activity pair
- `TryNode` provides per-item error isolation; use it to wrap the row body

**`context.GetVariable(key)` convention in GeneratingWorkflow:**

| Scope | Variable key injected | Read with |
|---|---|---|
| Workbook scan ForEach body | `Variable<WorkbookIdentifier> workbookVar` | `context.GetVariable(workbookVar).FilePath` |
| Presentation scan ForEach body | `Variable<PresentationIdentifier> presentationVar` | `context.GetVariable(presentationVar).FilePath` |
| Worksheet ForEach body | `Variable<WorksheetIdentifier> worksheetVar` | `context.GetVariable(worksheetVar)` |
| Row ForEach body | `Variable<RowIdentifier> rowVar` | `context.GetVariable(rowVar)` |
| Download/Edit task ForEach body | `Variable<RowTask> rowTaskVar` | `context.GetVariable(rowTaskVar)` |

**Infrastructure execution:**
Infrastructure's `WcInterpreterStep<TDef, TData>` (a single `StepBodyAsync`) interprets the entire node tree recursively — no WorkflowCore fluent builder API is used in Application. WorkflowCore registration uses reflection in `WcWorkflowService.RunAsync` to bypass a type-system conflict between `IWorkflow<TData>` and `IWorkflow<object>` in WorkflowCore 3.17.

### State-driven Monitoring
- Each workflow instance owns a `WorkflowState` containing a hierarchical `ExecutionState` tree
- Logging uses `context.State.Logger.AddLog(...)` — never use a framework logger directly inside an Activity
- Thread-safe logging: `SortedSet<LogEntry>` + `lock` inside `Logger` keeps entries sorted by time
- `IWorkflowService.Workflows` returns the list of active `WorkflowState` instances

### Cloud Resolver Chain
`CloudResolversManager` routes cloud storage URLs (Google Drive, Google Photos, OneDrive, SharePoint) to direct download URLs before `DownloadRegistry` fetches them.

## Workflow: GeneratingWorkflow

**Input**: `GeneratingRequest` — `Graph` (worksheet→slide template mapping), `TextInstructions`, `ImageInstructions`, `OutputExtension`, `SaveFolder`

**Data model**: `WorkflowTask` (file: `Generating/Workflows/Models/WorkflowTask.cs`) holds:
- `WorkbookSummaries` — `ConcurrentDictionary<string, WorkbookSummary>` keyed by `Path.GetFullPath`. Populated by initial workbook scan.
- `PresentationSummaries` — `ConcurrentDictionary<string, PresentationSummary>` similarly.
- `WorksheetKeys` — filtered list: only worksheets that exist in their workbook's scan result.
- `SheetTasks` — `ConcurrentDictionary<WorksheetIdentifier, SheetTask>` per worksheet branch.

There are **no shared mutable fields** for current worksheet or current-row tasks — worksheet branches are fully isolated via child scope variables.

**Pipeline:**
1. **Parallel initial scans**: two concurrent ForEach loops — one per unique `WorkbookIdentifier`, one per unique `PresentationIdentifier` — each slot-gated by `SlotType.ScanWorkbook` / `SlotType.ScanPresentation`. Results stored in `WorkbookSummaries` / `PresentationSummaries`.
2. **Filter `WorksheetKeys`** via `InlineNode`: only keep worksheets whose name exists in the corresponding `WorkbookSummary`. Worksheets referencing missing or unscannable workbooks are silently dropped here.
3. **Parallel** worksheet loop, each branch slot-gated by `SlotType.Worksheet`:
   - Init `SheetTask`
   - Skip if workbook file missing (`ConditionNode`)
   - `CreateWorkingPresentation` (computes output path, copies template, strips to single slide)
   - `SimplyInstructions` (reads headers/row count from `WorkbookSummaries`; resolves instructions from `PresentationSummaries` — no file I/O)
   - **Sequential** row loop wrapped in `TryNode`:
     - Reset `RowSpecializedInstructions` for current row
     - Parallel download ForEach (`SlotType.Download`) → `DownloadImage`
     - Parallel edit ForEach (`SlotType.EditImage`) — items built inline by scanning disk for downloaded files → `EditImage`
     - `SlotGatedNode(SlotType.EditSlide)`: `CloneTemplateSlide` → `EditSlide`
   - `RemoveWorkingTemplateSlide` (removes template slot 1, saves file)
   - `InlineNode`: dispose presentation lease

**Checkpoint-safe design**: every activity corresponds to a disk I/O boundary. RAM-only activities have been eliminated — if the process crashes between any two activities, restarting resumes at the correct disk state.

**Slide editing must go through XML (`DocumentFormat.OpenXml`), never Spire.**

### Image Path Structure

Downloaded and edited images use a deterministic hash-based folder structure:

```
{downloadFolder}/
  Downloaded/
    {workbookName}_{hash7}/        ← hash of full workbook path (non-alphanumeric stripped)
      {worksheetName}_{hash7}/     ← hash of worksheet name
        {columnName}_{hash7}/      ← hash of column name
          {rowIndex}.{ext}
  Edited/
    {workbookName}_{hash7}/
      {worksheetName}_{hash7}/
        {columnName}_{hash7}/
          {rowIndex}.{ext}
```

Hash: Base64 of UTF-8 bytes, first 7 chars. Special chars (`+/=` and invalid filename chars) replaced with `-`. Workbook hash source is the full path with all non-alphanumeric chars stripped.

## C# Conventions

- **Framework**: `net10.0`, `Nullable: enable`, `ImplicitUsings: enable`, file-scoped namespaces
- **Models**: prefer `record` for data-centric types (identifier, instruction, request)
- **Classes**: prefer `sealed` when inheritance not needed; name concretely (`YamlSerializer`, not `Serializer`)
- **Properties**: use `required` modifier for mandatory properties
- **Async**: use `async/await` + `ConfigureAwait(false)` in service/library code
- **Null safety**: validate inputs early; prefer `TryGet`/`Try...` patterns over throwing for expected failures
- **Concurrency**: use `ConcurrentDictionary`/locks for shared state; clean up registries on completion
- **XML docs**: required for all public APIs — must document:
  - **Variables (Read/Write)**: context variables the activity reads or writes
  - **Services**: services consumed via `context.GetRequiredService<T>()`
  - **Logging**: note that logging goes through `context.State.Logger.AddLog(...)`
  - **CancellationToken**: propagate where async I/O occurs; note when not required (pure in-memory)
- StyleCop.Analyzers is applied solution-wide; SA1402 (one type per file) is set to suggestion

## Dependency Checklist (Self-Check Before Committing)

- [ ] Does Domain reference Application or Infrastructure? → **must not**
- [ ] Is business logic placed correctly in Domain/Application? → **yes**
- [ ] Does Application reference WorkflowCore? → **must not**
- [ ] Does the new activity implement `ILeafActivity<TData>` (not `StepBodyAsync` or bare `ILeafActivity`)? → **required**
- [ ] Does the new workflow implement `IWorkflowDefinition<TData>` (not `IWorkflow<TData>`)? → **required**
- [ ] Does the new activity use `context.Data` (typed) and `context.GetVariable(xVar)` for scope variables instead of casts? → **required**
- [ ] Are DSL lambdas (ForEachNode/InlineNode/ConditionNode) using `ctx.Data` directly (generic `TData`) instead of `GetData<T>()` or `object`? → **required**
- [ ] Does the new activity inject `Variable<T>` keys via constructor and register them as Singletons in DI? → **required for scoped activities**
- [ ] Is all logging done via `context.State.Logger`? → **required**
- [ ] Is `ConfigureAwait(false)` used in async service/library code? → **required**
- [ ] Are business rules in Infrastructure? → **must not be**
- [ ] Does the new activity perform disk I/O (justifying its existence as a checkpoint)? → **required; RAM-only activities must not exist**
