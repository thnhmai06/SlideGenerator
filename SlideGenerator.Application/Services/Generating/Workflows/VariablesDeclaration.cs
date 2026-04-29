using System.Collections.Concurrent;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Application.Services.Scanning.Models.Slides;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using ImageInstruction = SlideGenerator.Application.Services.Generating.Models.Images.GeneralInstruction;
using SpecializedInstruction = SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction;
using TextInstruction = SlideGenerator.Application.Services.Generating.Models.Texts.GeneralInstruction;

namespace SlideGenerator.Application.Services.Generating.Workflows;

/// <summary>
///     Declares all <see cref="Variable{T}" /> identifiers used in <see cref="GeneratingWorkflow" />.
///     Each variable is a stateless-typed key — values are stored in and retrieved from the
///     <see cref="IActivityContext" /> scope chain at runtime.
/// </summary>
/// <remarks>
///     <b>Variables are the persistence units of the workflow.</b> All computed states are stored in Variables
///     at the appropriate scope level so that, in case of a failure, restoring the Variables from a checkpoint
///     is enough to resume execution without restarting from scratch.
///     <br />
///     Variables are grouped by the scope in which they are set and from which they propagate downward:
///     <list type="bullet">
///         <item><b>Workflow root scope</b> — set once before parallel scans, readable by all descendant scopes.</item>
///         <item><b>Worksheet scope</b> — set per worksheet branch; each parallel branch has its own isolated copy.</item>
///         <item><b>Row scope</b> — set per row iteration; reset at the start of each row.</item>
///         <item><b>ForEach item scopes</b> — transient loop-iteration variables set by ForEach nodes.</item>
///     </list>
/// </remarks>
public static class VariablesDeclaration
{
    // =========================================================================
    // Workflow root scope
    // =========================================================================

    /// <summary>
    ///     Workbook scan results keyed by normalized file path.
    ///     Initialized as an empty <see cref="ConcurrentDictionary{TKey,TValue}" /> by the opening
    ///     <c>InlineNode</c> and populated by <c>ScanWorkbook</c> activities.
    /// </summary>
    public static readonly Variable<ConcurrentDictionary<string, WorkbookSummary>>
        WorkbookSummaries = new(nameof(WorkbookSummaries));

    /// <summary>
    ///     Presentation scan results keyed by normalized file path.
    ///     Initialized as an empty <see cref="ConcurrentDictionary{TKey,TValue}" /> by the opening
    ///     <c>InlineNode</c> and populated by <c>ScanPresentation</c> activities.
    /// </summary>
    public static readonly Variable<ConcurrentDictionary<string, PresentationSummary>>
        PresentationSummaries = new(nameof(PresentationSummaries));

    /// <summary>
    ///     Filtered list of worksheet identifiers confirmed to exist in their respective workbooks.
    ///     Set once after the initial parallel scans are complete.
    /// </summary>
    public static readonly Variable<List<WorksheetIdentifier>>
        WorksheetKeys = new(nameof(WorksheetKeys));

    // =========================================================================
    // ForEach item scopes (loop-iteration variables, set by ForEach nodes)
    // =========================================================================

    /// <summary>The workbook being scanned in the workbook scan ForEach loop.</summary>
    public static readonly Variable<WorkbookIdentifier> WorkbookItem = new(nameof(WorkbookItem));

    /// <summary>The presentation being scanned in the presentation scan ForEach loop.</summary>
    public static readonly Variable<PresentationIdentifier> PresentationItem = new(nameof(PresentationItem));

    /// <summary>The current worksheet being processed in the outer worksheet ForEach loop.</summary>
    public static readonly Variable<WorksheetIdentifier> WorksheetItem = new(nameof(WorksheetItem));

    /// <summary>The current row context being processed in the sequential row ForEach loop.</summary>
    public static readonly Variable<RowIdentifier> RowItem = new(nameof(RowItem));

    /// <summary>The current row task being processed in the download or edit ForEach loops.</summary>
    public static readonly Variable<RowTask> RowTaskItem = new(nameof(RowTaskItem));

    // =========================================================================
    // Worksheet scope (set by SimplyInstructions and CreateWorkingPresentation)
    // =========================================================================

    /// <summary>
    ///     1-based row indices to process for this worksheet.
    ///     Set by <c>SimplyInstructions</c> from the scanned workbook row count.
    /// </summary>
    public static readonly Variable<List<int>> RowIndices = new(nameof(RowIndices));

    /// <summary>
    ///     Text replacement instructions applicable to this worksheet's columns.
    ///     Set by <c>SimplyInstructions</c>; read by <c>EditSlide</c>.
    /// </summary>
    public static readonly Variable<List<TextInstruction>> RowTextInstructions = new(nameof(RowTextInstructions));

    /// <summary>
    ///     Image replacement instructions applicable to this worksheet's columns.
    ///     Set by <c>SimplyInstructions</c>; used to build the download ForEach item list.
    /// </summary>
    public static readonly Variable<List<ImageInstruction>> RowImageInstructions = new(nameof(RowImageInstructions));

    /// <summary>
    ///     Absolute path of the output presentation file for this worksheet.
    ///     Set by <c>CreateWorkingPresentation</c>.
    /// </summary>
    public static readonly Variable<string> OutputPath = new(nameof(OutputPath));

    /// <summary>
    ///     Identifier of the template slide (index 1) in the working presentation copy.
    ///     Set by <c>CreateWorkingPresentation</c>; read by <c>CloneTemplateSlide</c>, <c>EditSlide</c>,
    ///     and <c>RemoveWorkingTemplateSlide</c>.
    /// </summary>
    public static readonly Variable<SlideIdentifier> WorkingTemplateSlide = new(nameof(WorkingTemplateSlide));

    // =========================================================================
    // Worksheet scope (set per worksheet; persists across Phase A and Phase B)
    // =========================================================================

    /// <summary>
    ///     Per-row map of resolved image instructions, keyed by a 1-based row index.
    ///     Populated during Phase A (download loop); read during Phase B (slide edit loop).
    /// </summary>
    public static readonly Variable<Dictionary<int, List<SpecializedInstruction>>>
        RowInstructionsMap = new(nameof(RowInstructionsMap));

    // =========================================================================
    // Row scope (reset at the start of each row iteration)
    // =========================================================================

    /// <summary>
    ///     Resolved image instructions for the current row (cloud URL resolved, source column identified).
    ///     Reset to an empty list at the start of each Phase A row; populated by parallel <c>DownloadImage</c>
    ///     activities; saved to <see cref="RowInstructionsMap" /> after downloads, then restored per row in Phase B
    ///     for <c>EditSlide</c> to consume.
    /// </summary>
    public static readonly Variable<List<SpecializedInstruction>>
        SpecializedInstructions = new(nameof(SpecializedInstructions));
}
