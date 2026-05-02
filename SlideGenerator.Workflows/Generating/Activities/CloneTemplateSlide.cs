using SlideGenerator.Workflows.Generating.Rules;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Workflows.Generating.Activities;

public sealed class CloneTemplateSlide(SfPresentationRegistry presentationRegistry) 
    : PresentationStepBase(presentationRegistry)
{
    public int RowIndex { get; set; }
    public string OutputPath { get; set; } = null!;

    protected override async Task<ExecutionResult> ExecuteStepAsync(IStepExecutionContext context)
    {
        var presentationWrapper = await AcquirePresentationAsync(OutputPath, context.CancellationToken).ConfigureAwait(false);
        var presentation = presentationWrapper.Value;

        var sourceSlide = presentation.Slides[WorkflowConstants.WorkingTemplateSlideIndex - 1];
        var cloned = sourceSlide.Clone();
        
        var targetIndex = WorkflowConstants.WorkingTemplateSlideIndex + RowIndex;
        
        if (targetIndex > presentation.Slides.Count)
            presentation.Slides.Add(cloned);
        else
            presentation.Slides.Insert(targetIndex - 1, cloned);

        presentationWrapper.Save();
        return ExecutionResult.Next();
    }
}