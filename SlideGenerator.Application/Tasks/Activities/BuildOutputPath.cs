using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Slide.Rules;
using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Tasks.Rules;

namespace SlideGenerator.Application.Tasks.Activities;

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
public sealed class BuildOutputPath(IRegistry<IReadOnlyWorkbook> workbookRegistry) : Activity
{
    /// <summary>
    ///     Input output root folder.
    /// </summary>
    public Input<string> SaveFolder { get; set; } = null!;

    /// <summary>
    ///     Input worksheet identifier used to derive output file name.
    /// </summary>
    public Input<WorksheetIdentifier> Worksheet { get; set; } = null!;

    /// <summary>
    ///     Input output extension.
    /// </summary>
    public Input<PresentationExtension> Extension { get; set; } = null!;

    /// <summary>
    ///     Output normalized full file path.
    /// </summary>
    public Output<string> OutputPath { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var saveFolder = context.Get(SaveFolder);
        var worksheet = context.Get(Worksheet);
        var extension = context.Get(Extension).ToFileExtension();

        if (string.IsNullOrWhiteSpace(saveFolder) || worksheet is null)
            throw new InvalidOperationException("Save folder and worksheet must be provided.");

        var workbookName = Utilities.NormalizeFileName(worksheet.Workbook.Name, NamingRules.DEFAULT_WORKBOOK_NAME);
        var worksheetName = Utilities.NormalizeFileName(worksheet.Name, NamingRules.DEFAULT_WORKSHEET_NAME);

        var outputPath = Path.Combine(saveFolder, workbookName, worksheetName + extension);
        context.Set(OutputPath, outputPath);
        return ValueTask.CompletedTask;
    }
}