using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Rules;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>Builds the final output file path.</summary>
/// <remarks>Uses normalized file names based on workbook and worksheet identifiers.</remarks>
/// <param name="saveFolder">The destination folder.</param>
/// <param name="extension">The presentation file extension.</param>
public sealed class BuildOutputPath(string saveFolder, PresentationExtension extension) : Activity
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if save folder or worksheet is missing.</exception>
    public override ValueTask ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var worksheet = context.GetVariable<WorksheetIdentifier>(WorksheetContextRules.Worksheet);
        if (string.IsNullOrWhiteSpace(saveFolder) || worksheet is null)
            throw new InvalidOperationException("Save folder and worksheet must be provided.");

        var workbookName = NamingRules.NormalizeFileName(worksheet.Workbook.Name, NamingRules.DefaultWorkbookName);
        var worksheetName = NamingRules.NormalizeFileName(worksheet.Name, NamingRules.DefaultWorksheetName);
        context.SetVariable(WorksheetContextRules.OutputPath, Path.Combine(saveFolder, workbookName, worksheetName + extension.ToFileExtension()));

        return ValueTask.CompletedTask;
    }
}
