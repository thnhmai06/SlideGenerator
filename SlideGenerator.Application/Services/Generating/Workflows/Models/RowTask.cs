using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using ImageGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Images.GeneralInstruction;
using ImageSpecializedInstruction = SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction;
using TextGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Texts.GeneralInstruction;

namespace SlideGenerator.Application.Services.Generating.Workflows.Models;

/// <summary>
///     Represents a single atomic operation within a worksheet row.
///     Contains all necessary context for download, edit, and slide operations.
/// </summary>
public sealed record RowTask(
    WorksheetIdentifier Worksheet,
    int RowIndex,
    ImageGeneralInstruction? DownloadItem = null,
    KeyValuePair<ImageSpecializedInstruction, string>? EditItem = null)
{
    // Metadata for the task, pre-populated during setup
    public List<TextGeneralInstruction> TextInstructions { get; set; } = [];
    public List<ImageGeneralInstruction> ImageInstructions { get; set; } = [];
    public List<ImageSpecializedInstruction> ResolvedInstructions { get; set; } = [];
    public string OutputPath { get; set; } = string.Empty;
    public SlideIdentifier TemplateSlide { get; set; } = null!;
}
