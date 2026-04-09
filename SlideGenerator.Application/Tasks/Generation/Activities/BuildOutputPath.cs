using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Slide.Rules;
using SlideGenerator.Domain.Tasks.Rules;

namespace SlideGenerator.Application.Tasks.Generation.Activities;

/// <summary>
///     Builds the final output file path for one worksheet with normalized file name.
/// </summary>
/// <remarks>
///     <para>State usage:</para>
///     <list type="bullet">
///         <item><description>Reads lightweight inputs only.</description></item>
///         <item><description>Writes final output path string to <see cref="OutputPath"/>.</description></item>
///     </list>
///     <para>
///         This activity is idempotent and side effect free.
///     </para>
/// </remarks>
public sealed class BuildOutputPath : Activity
{
    /// <summary>
    ///     Input output root folder.
    /// </summary>
    public required Input<string> SaveFolder { get; init; }

    /// <summary>
    ///     Input worksheet identifier used to derive output file name.
    /// </summary>
    public required Input<WorksheetIdentifier> Worksheet { get; init; }

    /// <summary>
    ///     Input output extension.
    /// </summary>
    public required Input<PresentationExtension> Extension { get; init; }

    /// <summary>
    ///     Output normalized full file path.
    /// </summary>
    public Output<string> OutputPath { get; init; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var saveFolder = context.Get(SaveFolder);
        var worksheet = context.Get(Worksheet);
        var extension = context.Get(Extension).ToFileExtension();

        if (string.IsNullOrWhiteSpace(saveFolder) || worksheet is null)
            throw new InvalidOperationException("Save folder and worksheet must be provided.");

        var workbookName = Utilities.NormalizeFileName(worksheet.Workbook.Name, NamingRules.DefaultWorkbookName);
        var worksheetName = Utilities.NormalizeFileName(worksheet.Name, NamingRules.DefaultWorksheetName);

        var outputPath = Path.Combine(saveFolder, workbookName, worksheetName + extension);
        context.Set(OutputPath, outputPath);
        return ValueTask.CompletedTask;
    }
}