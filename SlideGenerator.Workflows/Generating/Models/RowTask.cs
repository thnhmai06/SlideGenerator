using SlideGenerator.Sheets.Models;

namespace SlideGenerator.Workflows.Generating.Models;

public sealed record RowTask(
    WorkbookIdentifier Workbook,
    string WorksheetName,
    int RowIndex,
    GeneralInstruction? DownloadItem = null,
    KeyValuePair<SpecializedInstruction, string>? EditItem = null)
{
    public List<Texts.GeneralInstruction> TextInstructions { get; set; } = [];
    public List<GeneralInstruction> ImageInstructions { get; set; } = [];
    public List<SpecializedInstruction> ResolvedInstructions { get; set; } = [];
    public string OutputPath { get; set; } = string.Empty;
    public int TemplateSlideIndex { get; set; }
}