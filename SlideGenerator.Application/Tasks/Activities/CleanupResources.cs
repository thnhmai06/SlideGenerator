using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Models;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Disposes temporary presentation and workbook resources used by workflow execution.
/// </summary>
public sealed class CleanupResources(
    IRegistry<IReadOnlyWorkbook> workbookRegistry,
    IRegistry<IPresentation> slideRegistry) : Activity
{
    /// <summary>
    ///     Optional presentation key/path to close from presentation registry.
    /// </summary>
    public Input<PresentationIdentifier> Presentation { get; set; } = null!;

    /// <summary>
    ///     Optional workbook key/path to close from workbook registry.
    /// </summary>
    public Input<WorkbookIdentifier> Workbook { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var presentationIdentifier = context.Get(Presentation);
        if (presentationIdentifier is not null)
            slideRegistry.Close(presentationIdentifier.FilePath);

        var workbookIdentifier = context.Get(Workbook);
        if (workbookIdentifier is not null)
            workbookRegistry.Close(workbookIdentifier.FilePath);

        return ValueTask.CompletedTask;
    }
}
