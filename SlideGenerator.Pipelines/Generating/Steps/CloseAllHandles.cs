using Serilog;
using SlideGenerator.Pipelines.Generating.Workflows.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Pipelines.Generating.Steps;

/// <summary>
///     Disposes of all long-lived workbook and presentation handles.
///     This step should be executed at the very end of the workflow.
/// </summary>
public sealed class CloseAllHandles(ILogger logger) : StepBody
{
    /// <inheritdoc />
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;
        data.TryInitLogger(logger, context.Workflow.Id);

        data.Logger.Information("Closing all workbook and presentation handles. Workflow complete.");

        data.Dispose();
        return ExecutionResult.Next();
    }
}