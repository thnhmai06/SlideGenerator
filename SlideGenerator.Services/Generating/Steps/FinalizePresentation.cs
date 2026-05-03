using SlideGenerator.Gate.Models;
using SlideGenerator.Gate.Services;
using SlideGenerator.Services.Generating.Workflows;
using SlideGenerator.Slides.Entities;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Services.Generating.Steps;

/// <summary>
///     Finalizes an output presentation.
///     Now that we use the original slide (index 0) for the first row,
///     we no longer need to remove it.
/// </summary>
public sealed class FinalizePresentation(GateLocker gateLocker) : StepBodyAsync
{
    /// <summary>
    ///     The worksheet being finalized.
    ///     Mapped from the ForEach loop in the workflow.
    /// </summary>
    public ValidatedWorksheet Worksheet { get; set; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingData)context.Workflow.Data;

        await gateLocker.AcquireAsync(GateType.EditPresentation).ConfigureAwait(false);
        try
        {
            if (!File.Exists(Worksheet.OutputPresentationPath))
                throw new FileNotFoundException("Presentation file not found for finalization.", Worksheet.OutputPresentationPath);

            // Finalization might involve other tasks in the future, 
            // but for now, the slides are already assembled.
            // Just verifying existence is sufficient or we could add metadata/properties here.
        }
        catch (Exception ex)
        {
            data.Errors.TryAdd($"Finalize_{Worksheet.Identifier.SheetName}", ex);
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }

        return ExecutionResult.Next();
    }
}
