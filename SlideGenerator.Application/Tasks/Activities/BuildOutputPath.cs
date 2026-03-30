using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Slide.Rules;
using SlideGenerator.Domain.Sheet.Models;

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
///         This activity is idempotent and side-effect free.
///     </para>
/// </remarks>
public sealed class BuildOutputPath : Activity
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
            return ValueTask.CompletedTask;

        var workbookFolder = Utilities.NormalizeFileName(Path.GetFileNameWithoutExtension(worksheet.Workbook.FilePath));
        if (string.IsNullOrWhiteSpace(workbookFolder))
            workbookFolder = "workbook";

        var fileName = Utilities.NormalizeFileName(worksheet.Name);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "worksheet";

        var outputPath = Path.Combine(saveFolder, workbookFolder, fileName + extension);
        context.Set(OutputPath, outputPath);
        return ValueTask.CompletedTask;
    }
}