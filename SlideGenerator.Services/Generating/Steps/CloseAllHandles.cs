using SlideGenerator.Services.Generating.Workflows.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Services.Generating.Steps;

/// <summary>
///     Disposes of all long-lived workbook and presentation handles.
///     This step should be executed at the very end of the workflow.
/// </summary>
public sealed class CloseAllHandles : StepBody
{
    /// <inheritdoc />
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;
        data.Dispose();
        return ExecutionResult.Next();
    }
}