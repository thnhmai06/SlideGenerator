using SlideGenerator.Application.Modules.Lock.Steps;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Services.Scanning.Workflows.Activities;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Application.Services.Scanning.Workflows;

/// <summary>
///     Defines the workflow for scanning multiple workbooks and presentations in parallel.
/// </summary>
/// <remarks>
///     The pipeline operates as follows:
///     <list type="number">
///         <item>
///             <description>Initiates two parallel branches: one for workbooks and one for presentations.</description>
///         </item>
///         <item>
///             <description>For each file, it acquires a semaphore slot (Gate) to limit concurrent I/O or memory-intensive operations.</description>
///         </item>
///         <item>
///             <description>Executes the specific scan activity (<see cref="ScanWorkbook"/> or <see cref="ScanPresentation"/>) inside a try-catch wrapper.</description>
///         </item>
///         <item>
///             <description>Captures successful results in concurrent dictionaries or records <see cref="Exception"/> details if a file fails.</description>
///         </item>
///         <item>
///             <description>Releases the semaphore slot regardless of success or failure.</description>
///         </item>
///         <item>
///             <description>Joins both branches to complete the workflow.</description>
///         </item>
///     </list>
/// </remarks>
public sealed class ScanningWorkflow : IWorkflow<ScanningData>
{
    /// <inheritdoc />
    public string Id => nameof(ScanningWorkflow);

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public void Build(IWorkflowBuilder<ScanningData> builder)
    {
        builder
            .StartWith(_ => ExecutionResult.Next())
            .Parallel()
            .Do(branch => branch
                .ForEach(data => data.Request.Workbooks)
                .Do(each => each
                    .StartWith<AcquireSlotStep>()
                    .Input(step => step.Gate, data => GateType.ReadWorkbook)
                    .Then<ScanWorkbook>()
                    .Input(step => step.Workbook,
                        (data, context) => (Domain.Sheets.Models.Identifiers.WorkbookIdentifier)context.Item)
                    .Output((step, data) =>
                    {
                        data.WorkbookSummaries[step.Result.FilePath] = step.Result;
                        if (step.Exception != null)
                            data.Errors[step.Workbook.FilePath] = step.Exception;
                    })
                    .Then<ReleaseSlotStep>()
                    .Input(step => step.Gate, data => GateType.ReadWorkbook)))
            .Do(branch => branch
                .ForEach(data => data.Request.Presentations)
                .Do(each => each
                    .StartWith<AcquireSlotStep>()
                    .Input(step => step.Gate, data => GateType.ReadPresentation)
                    .Then<ScanPresentation>()
                    .Input(step => step.Presentation,
                        (data, context) => (Domain.Slides.Models.Identifiers.PresentationIdentifier)context.Item)
                    .Output((step, data) =>
                    {
                        data.PresentationSummaries[step.Result.FilePath] = step.Result;
                        if (step.Exception != null)
                            data.Errors[step.Presentation.FilePath] = step.Exception;
                    })
                    .Then<ReleaseSlotStep>()
                    .Input(step => step.Gate, data => GateType.ReadPresentation)))
            .Join();
    }
}