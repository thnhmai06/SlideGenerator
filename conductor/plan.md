# Implementation Plan: Generating Workflow Rewrite

## Background & Motivation
The current slide generation pipeline is being completely rebuilt ("ĐẬP ĐI, XÂY LẠI") to adhere purely to a new 3-phase, 6-step logical pipeline documented in `Pipeline.md`, utilizing `WorkflowCore` natively. The new system emphasizes strict phase boundaries, idempotency in resource preparation, and high concurrency control via `GateLocker`.

## Scope & Impact
- **Target Folder:** `SlideGenerator.Services/Generating`
- **Modifications:** Only workflows, steps (StepBody/StepBodyAsync), and the workflow data state class will be created.
- **Constraints:** Existing models (e.g., `Recipe`, `GeneratingRequest`) and other projects will **not** be modified.

## Proposed Solution

### 1. State Management (`GeneratingData.cs`)
A unified, strongly-typed state class to orchestrate the WorkflowCore process:
- `GeneratingRequest Request`: The initial input.
- `ConcurrentBag<DownloadTask> DownloadTasks`: Aggregated image download tasks across all sheets.
- `ConcurrentBag<EditTask> EditTasks`: Aggregated image edit tasks.
- `ConcurrentBag<AssemblyTask> AssemblyTasks`: Aggregated slide assembly tasks (each representing a row to be converted into a slide).
- `ConcurrentBag<FinalizeTask> FinalizeTasks`: Tasks to finalize each output presentation.
- `ConcurrentDictionary<string, Exception> Errors`: Global error tracking for resilience.

### 2. Workflow Steps (StepBody/StepBodyAsync)
- The "Activity" suffix will be completely omitted from step names.
- `GateLocker` lock logic (`LockAsync` and `DisposeAsync`) will be implemented **internally within the `ExecuteAsync` methods** of the steps instead of being separate workflow steps.
- **NO internal `foreach` or `Task.WhenAll` loops** over rows/worksheets. Every iteration will be managed purely by WorkflowCore's `.ForEach()` construct, meaning each Step instance processes exactly ONE item.

**Phase A: Validation & Template Setup**
- `ValidateRequest`: Checks workbooks, sheets, presentations, and shapes. Filters out invalid configurations.
- `CreateTemplate`: Copies the template PPT to the target location and clears all slides except the template slide (index 1).

**Phase B: Resource Preparation (Idempotent)**
- `PrepareDownloadTasks`: Translates valid rows into `DownloadTasks`.
- `DownloadImage`: Processes **one** `DownloadTask`. Checks idempotency. Internally acquires `GateType.DownloadImage`.
- `PrepareEditTasks`: Translates downloaded items and shape bounds into `EditTasks`.
- `EditImage`: Processes **one** `EditTask`. Crops/resizes image. Internally acquires `GateType.EditImage`.

**Phase C: Assembly & Finalization**
- `PrepareAssemblyTasks`: Translates valid rows into `AssemblyTasks`, computing the exact target `SlideIndex` for each row so that even if WorkflowCore runs `.ForEach()` in parallel, slides are inserted at their deterministic indices to maintain Excel row order.
- `AssembleSlide`: Processes **one** `AssemblyTask`. Clones the template slide, inserts at the computed index, and replaces text/images. Internally acquires `GateType.EditPresentation` to prevent concurrent file corruption for the same presentation.
- `PrepareFinalizeTasks`: Prepares tasks to finalize each presentation.
- `FinalizePresentation`: Removes the original template slide (index 1) and saves the file.

### 3. Workflow Definition (`GeneratingWorkflow.cs`)
```csharp
public void Build(IWorkflowBuilder<GeneratingData> builder)
{
    builder
        // Phase A: Validation & Template Setup
        .StartWith<ValidateRequest>()
        .Then<CreateTemplate>()
        
        // Phase B: Resource Preparation
        .Then<PrepareDownloadTasks>()
        .ForEach(data => data.DownloadTasks)
            .Do(x => x.StartWith<DownloadImage>())
                
        .Then<PrepareEditTasks>()
        .ForEach(data => data.EditTasks)
            .Do(x => x.StartWith<EditImage>())

        // Phase C: Assembly & Finalization
        .Then<PrepareAssemblyTasks>()
        .ForEach(data => data.AssemblyTasks)
            .Do(x => x.StartWith<AssembleSlide>())
            
        .Then<PrepareFinalizeTasks>()
        .ForEach(data => data.FinalizeTasks)
            .Do(x => x.StartWith<FinalizePresentation>());
}
```

## Migration & Rollback
- Since this is a rewrite within the domain services, the old workflow files can be safely replaced or deleted. If rollback is necessary, the previous git commit can be restored.

## Verification & Testing
- Verify `GateLocker` throttling is correctly applied inside the steps.
- Verify slide order matches Excel rows despite WorkflowCore's parallel `ForEach` (by checking deterministic slide insertion indices).