using SlideGenerator.Gate.Models;
using SlideGenerator.Workflows.Generating.Activities;
using SlideGenerator.Workflows.Scanning.Activities;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Workflows.Scanning;

public sealed class ScanningWorkflow : IWorkflow<ScanningData>
{
    public string Id => nameof(ScanningWorkflow);
    public int Version => 1;

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
                    .Input(step => step.Workbook, (data, context) => (SlideGenerator.Sheets.Models.WorkbookIdentifier)context.Item)
                    .Output((step, data) =>
                    {
                        if (step.Result != null)
                            data.WorkbookSummaries[step.Result.FilePath] = step.Result;
                        if (step.Exception != null)
                            data.Errors[step.Workbook.FilePath] = step.Exception;
                    })
                    .Then<ReleaseSlotStep>()
                    .Input(step => step.Gate, data => GateType.ReadWorkbook)))
            .Do(branch => branch
                .ForEach(data => data.Request.PresentationFilePaths)
                .Do(each => each
                    .StartWith<AcquireSlotStep>()
                    .Input(step => step.Gate, data => GateType.ReadPresentation)
                    .Then<ScanPresentation>()
                    .Input(step => step.PresentationFilePath, (data, context) => (string)context.Item)
                    .Output((step, data) =>
                    {
                        if (step.Result != null)
                            data.PresentationSummaries[step.Result.FilePath] = step.Result;
                        if (step.Exception != null)
                            data.Errors[step.PresentationFilePath] = step.Exception;
                    })
                    .Then<ReleaseSlotStep>()
                    .Input(step => step.Gate, data => GateType.ReadPresentation)))
            .Join();
    }
}