using System.Collections.Concurrent;
using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Application.Services.Scanning.Models.Slides;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Models;

/// <summary>
///     Mutable workflow data shared across all branches of a <see cref="GeneratingWorkflow" /> run.
///     Concurrent dictionaries provide thread-safe isolation between parallel worksheet branches;
///     no shared mutable fields should be added.
/// </summary>
public class WorkflowTask
{
    /// <summary>The original generation request supplying the graph, instructions, and output settings.</summary>
    public GeneratingRequest Request { get; init; } = null!;

    /// <summary>
    ///     Workbook scan results keyed by normalized file path.
    ///     Populated by the initial parallel workbook scan loop.
    /// </summary>
    public ConcurrentDictionary<string, WorkbookSummary> WorkbookSummaries { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Presentation scan results keyed by normalized file path.
    ///     Populated by the initial parallel presentation scan loop.
    /// </summary>
    public ConcurrentDictionary<string, PresentationSummary> PresentationSummaries { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Ordered list of worksheet identifiers derived from <see cref="Request" />.Graph keys,
    ///     set once after initial scans complete.
    /// </summary>
    public List<WorksheetIdentifier> WorksheetKeys { get; set; } = [];

    /// <summary>
    ///     Per-worksheet task state — one entry per parallel worksheet branch.
    ///     Isolation is guaranteed by the child-scope tree; no AsyncLocal is needed.
    /// </summary>
    public ConcurrentDictionary<WorksheetIdentifier, SheetTask> SheetTasks { get; init; } = new();
}