using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Rules;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>
///     Builds the final output file path for one worksheet with a normalized file name.
///     Idempotent and side effect free.
/// </summary>
public sealed class BuildOutputPath(string saveFolder, PresentationExtension extension) : Activity
{
    /// <inheritdoc />
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
