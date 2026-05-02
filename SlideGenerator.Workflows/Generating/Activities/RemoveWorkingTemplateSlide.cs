using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Workflows.Generating.Activities;

public sealed class RemoveWorkingTemplateSlide(SfPresentationRegistry presentationRegistry) 
    : PresentationStepBase(presentationRegistry)
{
    public string OutputPath { get; set; } = null!;

    protected override async Task<ExecutionResult> ExecuteStepAsync(IStepExecutionContext context)
    {
        var wrapper = await AcquirePresentationAsync(OutputPath, context.CancellationToken).ConfigureAwait(false);
        var presentation = wrapper.Value;

        if (presentation.Slides.Count > 0)
        {
            presentation.Slides.RemoveAt(0);
            wrapper.Save();
        }
        
        return ExecutionResult.Next();
    }
}