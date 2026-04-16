using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Resources;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Disposes temporary presentation and workbook resources used by workflow execution.
/// </summary>
public sealed class CleanupResources(
    Registry<IReadOnlyWorkbook> workbookRegistry,
    Registry<IPresentation> slideRegistry) : Activity
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
            foreach (var identifier in presentations)
                _ = presentationPaths.Add(identifier.FilePath);
        foreach (var presentationPath in presentationPaths)
            slideRegistry.Close(presentationPath);

        // Workbooks
        var workbookPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var workbooks = Workbooks is null ? null : context.Get(Workbooks);
        if (workbooks is not null)
            foreach (var identifier in workbooks)
                _ = workbookPaths.Add(identifier.FilePath);
        foreach (var workbookPath in workbookPaths)
            workbookRegistry.Close(workbookPath);

        return ValueTask.CompletedTask;
    }
}