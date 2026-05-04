using System.Collections.Concurrent;
using SlideGenerator.Services.Generating.Models;
using SlideGenerator.Services.Generating.Models.Identifiers;
using SlideGenerator.Slides.Entities;
using Syncfusion.XlsIO;

namespace SlideGenerator.Services.Generating.Workflows.Models;

/// <summary>
///     Represents the state and data managed by the slide generation workflow.
/// </summary>
public sealed class GeneratingTask : IDisposable
{
    /// <summary>
    ///     The initial generation request.
    /// </summary>
    public GeneratingRequest Request { get; init; } = null!;

    /// <summary>
    ///     The collection of validated worksheets and their target output configurations.
    ///     Populated during Phase A.
    /// </summary>
    public ConcurrentDictionary<SheetIdentifier, SheetTask> ValidWorksheets { get; } = new();

    /// <summary>
    ///     The collection of slide generation tasks containing data replacements.
    /// </summary>
    public ConcurrentBag<SlideTask> SlideTasks { get; } = [];

    /// <summary>
    ///     The collection of image processing tasks combining download and edit requirements.
    /// </summary>
    public ConcurrentBag<ImageTask> ImageTasks { get; } = [];

    // Long-lived handles

    /// <summary>
    ///     Gets the collection of workbook handles used for reading data.
    ///     Key is the absolute file path.
    /// </summary>
    public ConcurrentDictionary<string, IWorkbook> WorkbookHandles { get; } = new();

    /// <summary>
    ///     Gets the collection of presentation handles used as templates.
    ///     Key is the absolute file path.
    /// </summary>
    public ConcurrentDictionary<string, SfPresentation> TemplateHandles { get; } = new();

    /// <summary>
    ///     Gets the collection of presentation handles used for output generation.
    ///     Key is the absolute file path.
    /// </summary>
    public ConcurrentDictionary<string, SfPresentation> OutputHandles { get; } = new();

    /// <summary>
    ///     Disposes of all open workbook and presentation handles.
    /// </summary>
    public void Dispose()
    {
        foreach (var handle in WorkbookHandles.Values)
            try { handle.Close(); } catch { /* ignore */ }
        WorkbookHandles.Clear();

        foreach (var handle in TemplateHandles.Values)
            try { handle.Dispose(); } catch { /* ignore */ }
        TemplateHandles.Clear();

        foreach (var handle in OutputHandles.Values)
            try { handle.Dispose(); } catch { /* ignore */ }
        OutputHandles.Clear();
    }
}