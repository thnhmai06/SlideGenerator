using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Models;

namespace SlideGenerator.Application.Tasks.Generation.Activities;

/// <summary>
///     Disposes temporary presentation and workbook resources used by workflow execution.
/// </summary>
public sealed class CleanupResources(
    IRegistry<IReadOnlyWorkbook> workbookRegistry,
    IRegistry<IPresentation> slideRegistry) : Activity
{
    /// <summary>
    ///     Optional collection of presentation keys/paths to close from presentation registry.
    /// </summary>
    public Input<IReadOnlySet<PresentationIdentifier>>? Presentations { get; init; }

    /// <summary>
    ///     Optional collection of workbook keys/paths to close from workbook registry.
    /// </summary>
    public Input<IReadOnlySet<WorkbookIdentifier>>? Workbooks { get; init; }

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Presentations
        var presentationPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var presentations = Presentations is null ? null : context.Get(Presentations);
        if (presentations is not null)
        {
            foreach (var identifier in presentations)
                _ = presentationPaths.Add(identifier.FilePath);
        }
        foreach (var presentationPath in presentationPaths)
            slideRegistry.Close(presentationPath);

        // Workbooks
        var workbookPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var workbooks = Workbooks is null ? null : context.Get(Workbooks);
        if (workbooks is not null)
        {
            foreach (var identifier in workbooks)
                _ = workbookPaths.Add(identifier.FilePath);
        }
        foreach (var workbookPath in workbookPaths)
            workbookRegistry.Close(workbookPath);

        return ValueTask.CompletedTask;
    }
}