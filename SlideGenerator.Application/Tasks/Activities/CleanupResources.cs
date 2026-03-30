using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Slide.Entities;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Disposes temporary presentation and workbook resources used by workflow execution.
/// </summary>
public sealed class CleanupResources : Activity
{
    /// <summary>
    ///     Optional presentation key/path to close from presentation registry.
    /// </summary>
    public Input<string?> PresentationKey { get; set; } = null!;

    /// <summary>
    ///     Optional workbook key/path to close from workbook registry.
    /// </summary>
    public Input<string?> WorkbookKey { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the workbook registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IReadOnlyWorkbook> WorkbookRegistry { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the slide registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IPresentation> SlideRegistry { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var presentationKey = context.Get(PresentationKey);
        if (!string.IsNullOrWhiteSpace(presentationKey))
            SlideRegistry.Close(presentationKey);

        var workbookKey = context.Get(WorkbookKey);
        if (!string.IsNullOrWhiteSpace(workbookKey))
            WorkbookRegistry.Close(workbookKey);

        return ValueTask.CompletedTask;
    }
}
