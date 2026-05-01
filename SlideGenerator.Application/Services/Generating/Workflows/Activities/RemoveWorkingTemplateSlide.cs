using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     A workflow activity that removes the temporary working template slide and saves the final presentation.
/// </summary>
/// <remarks>
///     The finalization process includes:
///     <list type="bullet">
///         <item>
///             <description>Acquiring a write-enabled lease on the presentation file.</description>
///         </item>
///         <item>
///             <description>Removing the initial template slide (usually at index 1) that was used for cloning.</description>
///         </item>
///         <item>
///             <description>Saving the presentation to the final output format (e.g., .pptx or .pdf).</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="presentationRegistry">Registry to manage concurrent write access to the presentation file.</param>
public sealed class RemoveWorkingTemplateSlide(FileRegistry<IPresentation> presentationRegistry) 
    : PresentationStepBase(presentationRegistry)
{
    /// <summary>
    ///     Gets or sets the identifier of the worksheet being finalized.
    /// </summary>
    public WorksheetIdentifier Worksheet { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the identifier of the working template slide to be removed.
    /// </summary>
    public SlideIdentifier WorkingTemplateSlide { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the absolute path to the working presentation file.
    /// </summary>
    public string OutputPath { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the original generation request containing output extension settings.
    /// </summary>
    public GeneratingRequest Request { get; set; } = null!;

    protected override async Task<ExecutionResult> ExecuteStepAsync(IStepExecutionContext context)
    {
        var presentation = await AcquirePresentationAsync(OutputPath, context.CancellationToken).ConfigureAwait(false);

        presentation.RemoveSlide(WorkingTemplateSlide.Index);
        presentation.Save(Request.OutputExtension);
        
        return ExecutionResult.Next();
    }
}
