using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Resolves placeholder and image-shape metadata from the pre-scanned presentation summary,
///     then filters text and image instructions to only those applicable to the current worksheet.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.WorksheetItem" />,
///     <see cref="VariablesDeclaration.WorkbookSummaries" />, <see cref="VariablesDeclaration.PresentationSummaries" />.
///     <br />
///     <b>Variables written:</b> <see cref="VariablesDeclaration.RowIndices" />,
///     <see cref="VariablesDeclaration.RowTextInstructions" />,
///     <see cref="VariablesDeclaration.RowImageInstructions" /> — all set in the current worksheet scope.<br />
///     <b>Data read:</b> <see cref="WorkflowTask.Request" /> — text and image instruction lists.<br />
///     <b>Logging:</b> via <c>context.State.Logger</c>.<br />
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
        var templateSlide = data.Request.Graph[worksheet];

        var presentationPath = Path.GetFullPath(templateSlide.Presentation.FilePath);
        if (!context.GetVariable(VariablesDeclaration.PresentationSummaries)
                .TryGetValue(presentationPath, out var presentationSummary))
            throw new InvalidOperationException($"Presentation summary for '{presentationPath}' was not found.");

        var slideSummary = presentationSummary.Slides.FirstOrDefault(s => s.Index == templateSlide.Index)
                           ?? throw new InvalidOperationException(
                               $"Slide {templateSlide.Index} not found in presentation summary for '{presentationPath}'.");

        // These are intermediate filter sets — not persisted as Variables
        var placeholders = slideSummary.Placeholders.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var shapeIds = slideSummary.ImageShapes.Select(s => s.Id).ToHashSet();

        var workbookPath = Path.GetFullPath(worksheet.Workbook.FilePath);
        if (!context.GetVariable(VariablesDeclaration.WorkbookSummaries)
                .TryGetValue(workbookPath, out var workbookSummary))
            throw new InvalidOperationException($"Workbook summary for '{workbookPath}' was not found.");

        var worksheetSummary = workbookSummary.Worksheets
                                   .FirstOrDefault(w =>
                                       string.Equals(w.Name, worksheet.Name, StringComparison.OrdinalIgnoreCase))
                               ?? throw new InvalidOperationException(
                                   $"Worksheet '{worksheet.Name}' not found in workbook summary for '{workbookPath}'.");

        var headers = worksheetSummary.Headers;

        context.SetVariable(VariablesDeclaration.RowTextInstructions,
            data.Request.TextInstructions
                .Where(x => placeholders.Contains(x.Placeholder)
                            && headers.Contains(x.Placeholder, StringComparer.OrdinalIgnoreCase))
                .ToList());

        context.SetVariable(VariablesDeclaration.RowImageInstructions,
            data.Request.ImageInstructions
                .Where(x => shapeIds.Contains(x.Target.Id)
                            && x.Sources.Any(s =>
                                headers.Contains(s.Name, StringComparer.OrdinalIgnoreCase)))
                .ToList());

        context.SetVariable(VariablesDeclaration.RowIndices,
            Enumerable.Range(1, worksheetSummary.Count).ToList());

        return Task.CompletedTask;
    }
}