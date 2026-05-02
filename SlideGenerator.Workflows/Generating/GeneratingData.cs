using System.Collections.Concurrent;
using SlideGenerator.Workflows.Generating.Models;
using SlideGenerator.Workflows.Scanning.Models;

namespace SlideGenerator.Workflows.Generating;

public sealed class GeneratingData
{
    public GeneratingRequest Request { get; set; } = null!;
    public ConcurrentDictionary<string, WorkbookSummary> WorkbookSummaries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public ConcurrentDictionary<string, PresentationSummary> PresentationSummaries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    
    public List<WorksheetMapping> WorksheetKeys { get; set; } = [];
    public List<RowTask> DownloadTasks { get; set; } = [];
    public List<RowTask> EditTasks { get; set; } = [];
    public List<RowTask> SlideTasks { get; set; } = [];

    public ConcurrentDictionary<string, string> WorksheetOutputPaths { get; set; } = new();
    public ConcurrentDictionary<string, SlideMapping> WorksheetTemplateSlides { get; set; } = new();
    public ConcurrentDictionary<string, List<Models.Texts.GeneralInstruction>> WorksheetTextInstructions { get; set; } = new();
    public ConcurrentDictionary<string, List<GeneralInstruction>> WorksheetImageInstructions { get; set; } = new();
    public ConcurrentDictionary<string, List<int>> WorksheetRowIndices { get; set; } = new();
    public ConcurrentDictionary<string, List<SpecializedInstruction>> RowResolvedInstructions { get; set; } = new();
    public ConcurrentDictionary<string, Exception> Errors { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static string GetWorksheetKey(WorksheetMapping mapping) => $"{mapping.Workbook.FilePath}|{mapping.WorksheetName}";
}