using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Resolves placeholder and image-shape metadata from the pre-scanned presentation summary,
///     then filters text and image instructions to only those applicable to the current worksheet.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.WorksheetItem" />.<br/>
///     <b>Data read:</b> <see cref="WorkflowTask.WorkbookSummaries" />, <see cref="WorkflowTask.PresentationSummaries" />,
///     <see cref="WorkflowTask.Request" />.<br/>
///     <b>Data written:</b> <see cref="SheetTask.SlideTextPlaceholders" />, <see cref="SheetTask.SlideImageShapeIds" />,
///     <see cref="SheetTask.RowTextInstructions" />, <see cref="SheetTask.RowImageInstructions" />,
///     <see cref="SheetTask.RowIndices" />.<br/>
///     <b>Logging:</b> via <c>context.State.Logger</c>.<br/>
///     <b>CancellationToken:</b> not required — all data is read from pre-scanned in-memory summaries.
/// </remarks>
public sealed class SimplyInstructions(
    Variable<WorksheetIdentifier> worksheetVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var data = context.Data;
        var worksheet = context.GetVariable(worksheetVar);
        var sheetTask = data.SheetTasks[worksheet];
        var templateSlide = data.Request.Graph[worksheet];

        // Resolve placeholder lists from already-scanned PresentationSummary
        var presentationPath = Path.GetFullPath(templateSlide.Presentation.FilePath);
        if (!data.PresentationSummaries.TryGetValue(presentationPath, out var presentationSummary))
            throw new InvalidOperationException($"Presentation summary for '{presentationPath}' was not found.");

        var slideSummary = presentationSummary.Slides.FirstOrDefault(s => s.Index == templateSlide.Index)
                           ?? throw new InvalidOperationException(
                               $"Slide {templateSlide.Index} not found in presentation summary for '{presentationPath}'.");

        sheetTask.SlideTextPlaceholders = [.. slideSummary.Placeholders];
        sheetTask.SlideImageShapeIds = [.. slideSummary.ImageShapes.Select(s => s.Id)];

        // Read worksheet headers and row count from the already-scanned WorkbookSummary
        var workbookPath = Path.GetFullPath(worksheet.Workbook.FilePath);
        if (!data.WorkbookSummaries.TryGetValue(workbookPath, out var workbookSummary))
            throw new InvalidOperationException($"Workbook summary for '{workbookPath}' was not found.");

        var worksheetSummary = workbookSummary.Worksheets
                                   .FirstOrDefault(w => string.Equals(w.Name, worksheet.Name, StringComparison.OrdinalIgnoreCase))
                               ?? throw new InvalidOperationException(
                                   $"Worksheet '{worksheet.Name}' not found in workbook summary for '{workbookPath}'.");

        sheetTask.RowTextInstructions = data.Request.TextInstructions
            .Where(x => sheetTask.SlideTextPlaceholders.Contains(x.Placeholder, StringComparer.OrdinalIgnoreCase))
            .Where(x => worksheetSummary.Headers.Contains(x.Placeholder, StringComparer.OrdinalIgnoreCase))
            .ToList();

        sheetTask.RowImageInstructions = data.Request.ImageInstructions
            .Where(x => sheetTask.SlideImageShapeIds.Contains(x.Target.Id))
            .Where(x => x.Sources.Any(s => worksheetSummary.Headers.Contains(s.Name, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        sheetTask.RowIndices = Enumerable.Range(1, worksheetSummary.Count).ToList();

        return Task.CompletedTask;
    }
}
