using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using SlideGenerator.Application.Services.Generating.Models.Texts;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using SpecializedInstruction = SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction;

namespace SlideGenerator.Application.Services.Generating.Workflows.Models;

/// <summary>
///     Holds all mutable state for one worksheet branch during a generation run.
///     One instance is created per worksheet by the parallel worksheet ForEach body.
/// </summary>
public class SheetTask
{
    /// <summary>The worksheet this task represents.</summary>
    public required WorksheetIdentifier Identifier { get; init; }

    /// <summary>1-based row indices to process, populated by <c>SimplyInstructions</c>.</summary>
    public List<int> RowIndices { get; set; } = [];

    /// <summary>Mustache placeholder tokens present on the template slide, filtered by <c>SimplyInstructions</c>.</summary>
    public List<string> SlideTextPlaceholders { get; set; } = [];

    /// <summary>Shape IDs of image shapes on the template slide, filtered by <c>SimplyInstructions</c>.</summary>
    public List<uint> SlideImageShapeIds { get; set; } = [];

    /// <summary>Text replacement instructions applicable to this worksheet's columns, set by <c>SimplyInstructions</c>.</summary>
    public List<GeneralInstruction> RowTextInstructions { get; set; } = [];

    /// <summary>Image replacement instructions applicable to this worksheet's columns, set by <c>SimplyInstructions</c>.</summary>
    public List<Generating.Models.Images.GeneralInstruction> RowImageInstructions { get; set; } = [];

    /// <summary>Absolute output path for the generated presentation, set by <c>CreateWorkingPresentation</c>.</summary>
    public string? OutputPath { get; set; }

    /// <summary>
    ///     Identifier of slot 1 in the working presentation copy (the template slide to clone per row),
    ///     set by <c>CreateWorkingPresentation</c>.
    /// </summary>
    public SlideIdentifier? WorkingTemplateSlide { get; set; }

    /// <summary>
    ///     Open registry lease for the working presentation file.
    ///     Set by <c>CreateWorkingPresentation</c>; disposed by the workflow's final <c>InlineNode</c>.
    /// </summary>
    [JsonIgnore]
    public IDisposable? PresentationLease { get; set; }

    /// <summary>
    ///     Resolved image instructions per row (cloud URL resolved, source column identified), collected by
    ///     <c>DownloadImage</c>. Keyed by 1-based row index; thread-safe for concurrent download tasks.
    /// </summary>
    public ConcurrentDictionary<int, List<SpecializedInstruction>> RowSpecializedInstructions { get; init; } = new();
}