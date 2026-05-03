using System.Collections.Concurrent;
using System.Drawing;
using SlideGenerator.Services.Generating.Models;
using SlideGenerator.Services.Generating.Models.Identifiers;

namespace SlideGenerator.Services.Generating.Workflows;

/// <summary>
///     Represents the state and data managed by the slide generation workflow.
/// </summary>
public sealed class GeneratingData
{
    /// <summary>
    ///     The initial generation request.
    /// </summary>
    public GeneratingRequest Request { get; init; } = null!;

    /// <summary>
    ///     The collection of validated worksheets and their target output configurations.
    ///     Populated during Phase A.
    /// </summary>
    public ConcurrentDictionary<SheetIdentifier, ValidatedWorksheet> ValidWorksheets { get; } = new();

    // Iteration lists generated during Prep phase
    public ConcurrentBag<RowTask> RowTasks { get; } = [];
    public ConcurrentBag<ShapeTask> ShapeTasks { get; } = [];
    public ConcurrentBag<RowShapeTask> RowShapeTasks { get; } = [];

    public ConcurrentDictionary<uint, RectangleF> ShapeBounds { get; } = new();

    /// <summary>
    ///     The collection of image processing tasks combining download and edit requirements.
    /// </summary>
    public ConcurrentBag<ImageTask> ImageTasks { get; } = [];

    /// <summary>
    ///     The collection of errors encountered during the workflow.
    /// </summary>
    public ConcurrentDictionary<string, Exception> Errors { get; } = new();
}

/// <summary>Represents a worksheet that has been validated and assigned an output path.</summary>
public sealed record ValidatedWorksheet(
    SheetIdentifier Identifier,
    string OutputPresentationPath,
    SlideIdentifier TemplateSlide,
    MapNode MapNode);

public sealed record RowTask(ValidatedWorksheet Worksheet, int RowIndex);

public sealed record ShapeTask(ValidatedWorksheet Worksheet, int ShapeIndex);

public sealed record RowShapeTask(RowTask RowTask, ShapeTask ShapeTask);

/// <summary>Represents a comprehensive task for downloading and editing a single image.</summary>
public sealed record ImageTask(
    SheetIdentifier Sheet,
    int RowIndex,
    string ColumnName,
    uint ShapeId,
    Uri? SourceUri,
    string DownloadPath,
    string EditPath,
    double Width,
    double Height,
    EditOptions EditOptions);