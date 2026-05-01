using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Application.Services.Scanning.Models.Slides;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using ImageGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Images.GeneralInstruction;
using TextGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Texts.GeneralInstruction;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     A workflow activity that resolves the high-level generation request into specific row-by-row instructions.
/// </summary>
/// <remarks>
///     The simplification process includes:
///     <list type="bullet">
///         <item>
///             <description>Extracting placeholders from the template slide summary.</description>
///         </item>
///         <item>
///             <description>Extracting headers from the worksheet data summary.</description>
///         </item>
///         <item>
///             <description>Filtering text and image instructions based on existing placeholders and data headers.</description>
///         </item>
///         <item>
///             <description>Calculating the total number of rows to be processed for the specific worksheet.</description>
///         </item>
///     </list>
/// </remarks>
public sealed class SimplyInstructions : StepBody
{
    /// <summary>
    ///     Gets or sets the worksheet identifier currently being processed.
    /// </summary>
    public WorksheetIdentifier Worksheet { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the original generation request containing instruction mappings.
    /// </summary>
    public GeneratingRequest Request { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the workbook summaries obtained during the initial scan phase.
    /// </summary>
    public IDictionary<string, WorkbookSummary> WorkbookSummaries { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the presentation summaries obtained during the initial scan phase.
    /// </summary>
    public IDictionary<string, PresentationSummary> PresentationSummaries { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the list of row indices found in the worksheet.
    /// </summary>
    public List<int> RowIndices { get; set; } = [];

    /// <summary>
    ///     Gets or sets the filtered list of text instructions applicable to this worksheet.
    /// </summary>
    public List<TextGeneralInstruction> TextInstructions { get; set; } = [];

    /// <summary>
    ///     Gets or sets the filtered list of image instructions applicable to this worksheet.
    /// </summary>
    public List<ImageGeneralInstruction> ImageInstructions { get; set; } = [];

    /// <summary>
    ///     Gets or sets the exception if the instruction simplification failed.
    /// </summary>
    public Exception? Exception { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        try
        {
            var templateSlide = Request.Graph[Worksheet];

            var presentationPath = Path.GetFullPath(templateSlide.Presentation.FilePath);
            if (!PresentationSummaries.TryGetValue(presentationPath, out var presentationSummary))
                throw new InvalidOperationException($"Presentation summary for '{presentationPath}' was not found.");

            var slideSummary = presentationSummary.Slides.FirstOrDefault(s => s.Index == templateSlide.Index)
                               ?? throw new InvalidOperationException(
                                   $"Slide {templateSlide.Index} not found in presentation summary for '{presentationPath}'.");

            var placeholders = slideSummary.Placeholders.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var shapeIds = slideSummary.ImageShapes.Select(s => s.Id).ToHashSet();

            var workbookPath = Path.GetFullPath(Worksheet.Workbook.FilePath);
            if (!WorkbookSummaries.TryGetValue(workbookPath, out var workbookSummary))
                throw new InvalidOperationException($"Workbook summary for '{workbookPath}' was not found.");

            var worksheetSummary = workbookSummary.Worksheets
                                       .FirstOrDefault(w =>
                                           string.Equals(w.Name, Worksheet.Name, StringComparison.OrdinalIgnoreCase))
                                   ?? throw new InvalidOperationException(
                                       $"Worksheet '{Worksheet.Name}' not found in workbook summary for '{workbookPath}'.");

            var headers = worksheetSummary.Preview.Headers;

            TextInstructions = Request.TextInstructions
                .Where(x => placeholders.Contains(x.Placeholder)
                            && headers.Contains(x.Placeholder, StringComparer.OrdinalIgnoreCase))
                .ToList();

            ImageInstructions = Request.ImageInstructions
                .Where(x => shapeIds.Contains(x.Target.Id)
                            && x.Sources.Any(s =>
                                headers.Contains(s.Name, StringComparer.OrdinalIgnoreCase)))
                .ToList();

            RowIndices = Enumerable.Range(1, worksheetSummary.Count).ToList();
        }
        catch (Exception ex)
        {
            Exception = ex;
        }

        return ExecutionResult.Next();
    }
}
