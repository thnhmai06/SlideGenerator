using System.Collections.Concurrent;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Application.Services.Scanning.Models.Slides;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Scanning.Workflows;

/// <summary>
///     Declares <see cref="Variable{T}" /> identifiers used for Scanning workflows and activities.
/// </summary>
public static class ScanningVariables
{
    /// <summary>
    ///     Workbook scan results keyed by normalized file path.
    /// </summary>
    public static readonly Variable<ConcurrentDictionary<string, WorkbookSummary>>
        WorkbookSummaries = new(nameof(WorkbookSummaries));

    /// <summary>
    ///     Presentation scan results keyed by normalized file path.
    /// </summary>
    public static readonly Variable<ConcurrentDictionary<string, PresentationSummary>>
        PresentationSummaries = new(nameof(PresentationSummaries));

    /// <summary>The workbook being scanned in a workbook scan loop.</summary>
    public static readonly Variable<WorkbookIdentifier> WorkbookItem = new(nameof(WorkbookItem));

    /// <summary>The presentation being scanned in a presentation scan loop.</summary>
    public static readonly Variable<PresentationIdentifier> PresentationItem = new(nameof(PresentationItem));
}
