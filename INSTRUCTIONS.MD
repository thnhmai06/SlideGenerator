# SlideGenerator Project Instructions & Guidelines

## 1. Project Overview
SlideGenerator is an automation tool for generating PowerPoint slides from templates. Main workflow:
- Read data from Excel (Workbook).
- Use an existing PowerPoint template.
- Replace placeholders (Mustache) with text and images (downloaded from the Cloud and processed via AI).
- Export the finalized PowerPoint files.

## 2. System Architecture (Clean Architecture)
The project strictly adheres to a 3-tier architecture with a unidirectional dependency rule:

| Layer | Project | Dependency Rule | Responsibility |
|---|---|---|---|
| **Domain** | `SlideGenerator.Domain` | No dependencies on other layers | Contains Business Models, Rules, and core Abstractions. |
| **Application** | `SlideGenerator.Application` | Depends on Domain | Business logic (Use-cases), Registry, Workflow DSL, and Interface definitions for Infrastructure. |
| **Infrastructure** | `SlideGenerator.Infrastructure` | Depends on Application & Domain | Implements Interfaces using third-party libraries (WorkflowCore, OpenXml, OpenCV...). |

### Mandatory Rules:
- **Application MUST NOT directly reference WorkflowCore.** The Application layer defines the DSL as a node record tree (`WorkflowNode`). The Infrastructure layer interprets this tree using `WcInterpreterStep` (a single `StepBodyAsync`).
- **State-driven Monitoring**: The system uses a hierarchical `ExecutionState` tree to track state and logs. Logging must always go through `context.State.Logger.AddLog(...)`, never directly through the framework logger.
- **Domain must be clean (framework-free)**: It must not contain technical logic from external libraries.

## 3. Tech Stack
- **Runtime**: .NET 10.0 (Nullable enabled, ImplicitUsings enabled).
- **Workflow**: WorkflowCore (Daniel Gerlag) — strictly resides in Infrastructure. Application uses a custom DSL.
- **Document Processing**:
    - Excel: `ClosedXML`.
    - PowerPoint: `DocumentFormat.OpenXml` (strictly NO Spire).
- **Image Processing**:
    - AI Face Detection: `OpenCvSharp4` + `YuNet`.
    - Image Editing: `Magick.NET-Q8`.
- **Others**:
    - Download: `Downloader`.
    - Template: `Stubble.Core`.
    - Serialization: `YamlDotNet`.

## 4. Folder Structure & Naming Convention

### Application Layer Split

`SlideGenerator.Application` is divided into two top-level directories:

| Directory | Purpose | Modules/Services inside |
|---|---|---|
| `Modules/` | Reusable, feature-agnostic capabilities | `Cloud`, `Download`, `Images`, `Resources`, `Settings`, `Slides`, `Systems`, `Workflows` |
| `Services/` | High-level feature services that orchestrate across modules | `Generating`, `Scanning` |

- **Modules** provide cross-cutting, infrastructure-agnostic abstractions: downloading, resource locking, image processing, the Workflow DSL itself, etc.
- **Services** contain feature-specific use-cases, their Workflows, Activities, and Models.

### Subfolder Convention

Each module/service follows this internal structure:
- `Abstractions/`: Interfaces (start with `I`).
- `Entities/`: Objects with identity/behavior.
- `Models/`: DTOs, Records, Value Objects.
    - `States/`: Contains classes inheriting from `ExecutionState`.
    - `Logging/`: Contains `Logger`, `LogEntry`, `LogLevel`.
- `Rules/`: Enums, Constants, Extension mapping.
- `Services/`: Business logic processing.
- `Activities/`: Leaf activities (in Services) — implement `ILeafActivity`.
- `Workflows/`: Process definitions — implement `IWorkflowDefinition<TData>`.
- `Adapters/`: Wrappers for external libraries (Infrastructure only).

## 5. Workflow DSL — Application Layer

The DSL is located in `Modules/Workflows/DSL/`. When adding a new step to the workflow:

### Lexical Scope Tree — Variable\<T\>

`Variable<T>` is a **pure typed key** (holds no state). Each `ForEachNode<TItem, TData>` iteration creates a **child scope** with its own local dictionary. `GetVariable` searches the local scope first, then traverses up the parent chain.

```csharp
// Keys declared once as static readonly — safe because they hold no state
public static class VariablesDeclaration
{
    public static readonly Variable<WorkbookIdentifier>     WorkbookItem     = new(nameof(WorkbookItem));
    public static readonly Variable<PresentationIdentifier> PresentationItem = new(nameof(PresentationItem));
    public static readonly Variable<WorksheetIdentifier>    WorksheetItem    = new(nameof(WorksheetItem));
    public static readonly Variable<RowIdentifier>          RowItem          = new(nameof(RowItem));
    public static readonly Variable<RowTask>                RowTaskItem      = new(nameof(RowTaskItem));
}
```

`WorkbookIdentifier` and `PresentationIdentifier` are domain types (not wrapper structs). They are mapped directly from `Request.Graph` — no intermediate wrappers are needed.

### Creating a New Activity

```csharp
// Activity at the worksheet scope — inject Variable<WorksheetIdentifier> key via constructor
public sealed class MyActivity(
    IMyService service,
    Variable<WorksheetIdentifier> worksheetVar) : ILeafActivity<WorkflowTask>
{
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var data = context.Data;                              // typed, no cast needed
        var ws = context.GetVariable(worksheetVar);           // read from scope chain
        // ...
    }
}
```

**Variable Scope Rules:**

| Scope in GeneratingWorkflow | Constructor Inject | Read Using |
|---|---|---|
| Workbook scan ForEach | `Variable<WorkbookIdentifier> workbookVar` | `context.GetVariable(workbookVar).FilePath` |
| Presentation scan ForEach | `Variable<PresentationIdentifier> presentationVar` | `context.GetVariable(presentationVar).FilePath` |
| Inside worksheet ForEach | `Variable<WorksheetIdentifier> worksheetVar` | `context.GetVariable(worksheetVar)` |
| Inside row ForEach | `Variable<RowIdentifier> rowVar` | `context.GetVariable(rowVar)` |
| Inside download/edit task ForEach | `Variable<RowTask> rowTaskVar` | `context.GetVariable(rowTaskVar)` |

**DSL Lambda Rules** (`ForEachNode.Items`, `InlineNode.Action`, `ConditionNode.Predicate`):
```csharp
// Lambdas receive IActivityContext<TData> — ctx.Data is strongly typed
new InlineNode<WorkflowTask>(ctx => {
    var data = ctx.Data;
    var ws   = ctx.GetVariable(VariablesDeclaration.WorksheetItem);
    return Task.CompletedTask;
})

// Generic ForEachNode — Variable is the key to store the item in the child scope
new ForEachNode<RowIdentifier, WorkflowTask>(
    VariablesDeclaration.RowItem,
    ctx => ctx.Data.SheetTasks[ctx.GetVariable(VariablesDeclaration.WorksheetItem)].RowIndices
               .Select(r => new RowIdentifier(ctx.GetVariable(VariablesDeclaration.WorksheetItem), r)),
    Parallel: false,
    Body: new TryNode(...)
)
```

### Available DSL Nodes

| Node | When to use |
|---|---|
| `ActivityNode<T>` | Calls an `ILeafActivity`. |
| `SequenceNode([...])` | Executes steps sequentially. |
| `ForEachNode<TItem, TData>(Variable, ctx => items, Parallel, Body)` | Loop; stores the item in the child scope via Variable. |
| `ParallelNode([...])` | Executes branches in parallel and waits for all to complete. |
| `TryNode(Body)` | Error isolation — swallows exceptions and logs them to the State. |
| `SlotGatedNode(SlotType.X, Body)` | Throttles execution using `IAsyncKeyedLocker<SlotType>`. |
| `InlineNode<TData>(ctx => ...)` | Small data mutations that do not require a separate class. |
| `ConditionNode<TData>(predicate, Then, Else?)` | Conditional branching (file: `IfNode.cs`; interface `IConditionNode`). |

### Invariants

- **Orchestration belongs to the node tree exclusively.** `ILeafActivity` never contains loops, `Task.WhenAll`, or any coordination logic — these always live in the node tree.
- **Scope crossing is done only via `Variable<T>`.** Activities inject `Variable<T>` keys via constructor and read via `context.GetVariable(key)`. DSL lambdas use `ctx.Data` directly. Direct data passing or object casting across scopes is prohibited.
- **Each `ForEachNode` iteration is physically isolated.** Each iteration gets its own child scope dictionary. Parallel branches are safe without `AsyncLocal`.
- **Concurrency throttling is expressed via `SlotGatedNode` only.** No manual semaphore acquisition inside an activity.
- **The row loop body is always wrapped in `TryNode`.** A failure in one row must never halt the entire worksheet.
- **Every `ILeafActivity` must correspond to a disk I/O boundary.** RAM-only activities are not permitted to exist.

## 6. Data Model — No Shared Mutable State

`WorkflowTask` (file: `Services/Generating/Workflows/Models/WorkflowTask.cs`) **does not have** and **must not reintroduce**:
- A `CurrentWorksheet` field or similar — this state must be read from the scope chain via `context.GetVariable(VariablesDeclaration.WorksheetItem)`.
- Any field that shares mutable state between parallel worksheet branches.

`WorkflowTask` contains:
- `WorkbookSummaries` — `ConcurrentDictionary<string, WorkbookSummary>` keyed by `Path.GetFullPath`. Used to filter `WorksheetKeys` and read `Headers`/`Count` in `SimplyInstructions`.
- `PresentationSummaries` — `ConcurrentDictionary<string, PresentationSummary>` (similar to above).
- `WorksheetKeys` — Filtered list containing only worksheets that actually exist in the scanned workbook.
- `WorksheetData` — `ConcurrentDictionary<WorksheetIdentifier, WorksheetData>` (One context per worksheet branch).

Each `WorksheetData` is completely independent among parallel worksheet branches. Isolation is guaranteed by the child scope tree.

## 7. Pipeline GeneratingWorkflow

```
ParallelNode
├── ForEach<WorkbookIdentifier>   [ScanWorkbook per unique workbook]
└── ForEach<PresentationIdentifier> [ScanPresentation per unique presentation]

InlineNode  → Filters WorksheetKeys, keeping only worksheets present in WorkbookSummaries

ForEach<WorksheetIdentifier> [Parallel, SlotType.Worksheet]
├── InlineNode → Initializes WorksheetData
├── ConditionNode (check if workbook file exists)
│   └── Then: SequenceNode
│       ├── CreateWorkingPresentation   ← calculates output path + copies template + strips slide
│       ├── SimplyInstructions          ← reads from summaries (no file open); resolves instructions
│       ├── ForEach<RowIdentifier> [Sequential, TryNode]
│       │   ├── InlineNode → resets RowSpecializedInstructions
│       │   ├── ForEach<RowTask> [Parallel, SlotType.Download] → DownloadImage
│       │   ├── ForEach<RowTask> [Parallel, SlotType.EditImage] → EditImage
│       │   │   (items built inline via disk scan for downloaded files)
│       │   └── SlotGatedNode(SlotType.EditSlide)
│       │       └── CloneTemplateSlide → EditSlide
│       ├── RemoveWorkingTemplateSlide  ← deletes template slide + Save()
│       └── InlineNode → disposes PresentationLease
```

## 8. Image Path Structure

```
{downloadFolder}/
  Downloaded/
    {workbookName}_{hash7}/        ← hash of the workbook's full path (removing non-alphanumeric)
      {worksheetName}_{hash7}/     ← hash of the worksheet name
        {columnName}_{hash7}/      ← hash of the column name
          {rowIndex}.{ext}
  Edited/
    {workbookName}_{hash7}/
      {worksheetName}_{hash7}/
        {columnName}_{hash7}/
          {rowIndex}.{ext}
```

*Hash: Base64 of UTF-8 bytes, taking the first 7 characters. `+/=` characters and any invalid filename characters are replaced with `-`.*

## 9. Infrastructure — Interpreter

`WcInterpreterStep<TDef, TData>` in `Infrastructure/Workflows/` is the sole bridge between WorkflowCore and the Application DSL. It:
- Is a single `StepBodyAsync` invoked by WorkflowCore.
- Runs `ExecuteNodeAsync(rootNode, ctx)` recursively.
- Handles all node types in a switch statement.
- `SlotGatedNode` → resolves `IAsyncKeyedLocker<SlotType>` from DI, acquiring/releasing it inside a try/finally block.

**WorkflowCore registration workaround**: `IWorkflow<TData>` (generic) and `IWorkflow` (= `IWorkflow<object>`) in WC 3.17 cannot be implemented together on the same class. `WcWorkflowService.RunAsync` uses reflection to invoke `RegisterWorkflow`.

## 10. Coding Style
- **Namespace**: Use file-scoped namespaces.
- **Class**: Prefer `sealed` unless intended for inheritance.
- **Models**: Use `record` for value objects.
- **Async**: Use `async/await` + `ConfigureAwait(false)` in all service/library code.
- **XML docs**: Mandatory for public APIs — clearly document variables read/written, injected services, logging behaviors, and CancellationToken usage.

## 11. System Invariants — Checklist

- [ ] Domain layer has zero references to Application or Infrastructure.
- [ ] Application layer has zero references to WorkflowCore.
- [ ] All Activities implement `ILeafActivity<TData>` — never the raw non-generic form or any engine-specific base class.
- [ ] All Workflow definitions implement `IWorkflowDefinition<TData>` — never `IWorkflow<TData>`.
- [ ] All Activities access workflow data via `context.Data` (typed) and scope variables via `context.GetVariable(xVar)` — no `object` casts.
- [ ] All DSL lambdas (`ForEachNode.Items`, `InlineNode.Action`, `ConditionNode.Predicate`) use `ctx.Data` directly — no `GetData<T>()` or raw `object`.
- [ ] All scoped Activities inject `Variable<T>` keys via constructor and are registered as Singleton in DI.
- [ ] All Activities perform disk I/O — RAM-only activities must not exist.
- [ ] All logging goes through `context.State.Logger` — never the framework logger.
- [ ] `ConfigureAwait(false)` is used in all async service and library code.
- [ ] Business rules never appear in the Infrastructure layer.