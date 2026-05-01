using System.Collections.Concurrent;
using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Application.Services.Scanning.Models.Slides;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using ImageGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Images.GeneralInstruction;
using ImageSpecializedInstruction = SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction;
using TextGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Texts.GeneralInstruction;

namespace SlideGenerator.Application.Services.Generating.Workflows;

/// <summary>
///     Represents the persistent data state for a generation workflow.
/// </summary>
public sealed class GeneratingData
{
    /// <summary>
    ///     Gets or sets the original generation request.
    /// </summary>
    public GeneratingRequest Request { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the summaries of workbooks involved in the generation, keyed by normalized path.
    /// </summary>
    public ConcurrentDictionary<string, WorkbookSummary> WorkbookSummaries { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets or sets the summaries of presentations involved in the generation, keyed by normalized path.
    /// </summary>
    public ConcurrentDictionary<string, PresentationSummary> PresentationSummaries { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets or sets the list of worksheet keys to be processed.
    /// </summary>
    public List<WorksheetIdentifier> WorksheetKeys { get; set; } = [];

    /// <summary>
    ///     Gets or sets the flat list of tasks for downloading images.
    /// </summary>
    public List<RowTask> DownloadTasks { get; set; } = [];

    /// <summary>
    ///     Gets or sets the flat list of tasks for editing images.
    /// </summary>
    public List<RowTask> EditTasks { get; set; } = [];

    /// <summary>
    ///     Gets or sets the flat list of tasks for generating slides.
    /// </summary>
    public List<RowTask> SlideTasks { get; set; } = [];

    /// <summary>
    ///     Gets or sets the output paths for processed worksheets, keyed by worksheet identifier.
    /// </summary>
    public ConcurrentDictionary<string, string> WorksheetOutputPaths { get; set; } = new();

    /// <summary>
    ///     Gets or sets the template slides resolved for each worksheet, keyed by worksheet identifier.
    /// </summary>
    public ConcurrentDictionary<string, SlideIdentifier> WorksheetTemplateSlides { get; set; } = new();

    /// <summary>
    ///     Gets or sets the general text instructions resolved for each worksheet, keyed by worksheet identifier.
    /// </summary>
    public ConcurrentDictionary<string, List<TextGeneralInstruction>> WorksheetTextInstructions { get; set; } = new();

    /// <summary>
    ///     Gets or sets the general image instructions resolved for each worksheet, keyed by worksheet identifier.
    /// </summary>
    public ConcurrentDictionary<string, List<ImageGeneralInstruction>> WorksheetImageInstructions { get; set; } = new();

    /// <summary>
    ///     Gets or sets the list of row indices to be processed for each worksheet, keyed by worksheet identifier.
    /// </summary>
    public ConcurrentDictionary<string, List<int>> WorksheetRowIndices { get; set; } = new();
    
    /// <summary>
    ///     Gets or sets the resolved specialized image instructions for specific rows, keyed by "WorksheetKey|RowIndex".
    /// </summary>
    public ConcurrentDictionary<string, List<ImageSpecializedInstruction>> RowResolvedInstructions { get; set; } = new();

    /// <summary>
    ///     Gets or sets any errors encountered during the generation, keyed by a descriptive identifier.
    /// </summary>
    public ConcurrentDictionary<string, Exception> Errors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
