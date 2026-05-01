using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     A workflow activity that clones the template slide to create a new slide for a specific data row.
/// </summary>
/// <remarks>
///     The cloning process includes:
///     <list type="bullet">
///         <item>
///             <description>Acquiring a write-enabled lease on the working presentation file.</description>
///         </item>
///         <item>
///             <description>Duplicating the working template slide (located at a fixed index).</description>
///         </item>
///         <item>
///             <description>Placing the cloned slide at an offset corresponding to the data row index.</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="presentationRegistry">Registry to manage concurrent write access to the presentation file.</param>
public sealed class CloneTemplateSlide(FileRegistry<IPresentation> presentationRegistry) 
    : PresentationStepBase(presentationRegistry)
{
    /// <summary>
    ///     Gets or sets the row identifier (worksheet and index) for which the slide is being cloned.
    /// </summary>
    public RowIdentifier Row { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the absolute path to the working presentation file.
    /// </summary>
    public string OutputPath { get; set; } = null!;

    protected override async Task<ExecutionResult> ExecuteStepAsync(IStepExecutionContext context)
    {
        var presentation = await AcquirePresentationAsync(OutputPath, context.CancellationToken).ConfigureAwait(false);

        presentation.CopySlide(WorkflowConstants.WorkingTemplateSlideIndex,
            WorkflowConstants.WorkingTemplateSlideIndex + Row.Index);
            
        return ExecutionResult.Next();
    }
}
