using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Application.Workflows.DSL;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Workflows;

/// <summary>
///     Declares all <see cref="Variable{T}" /> identifiers used in <see cref="GeneratingWorkflow" />.
///     These are purely stateless keys — they hold no execution state and are therefore safe to be
///     declared as <c>public static readonly</c> fields.
///     <br/>
///     <b>Values are stored in and retrieved from the <see cref="IActivityContext" /> scope chain at runtime.</b>
/// </summary>
public static class VariablesDeclaration
{
    /// <summary>The workbook being scanned in the workbook scan ForEach loop.</summary>
    public static readonly Variable<WorkbookIdentifier> WorkbookItem = new(nameof(WorkbookItem));

    /// <summary>The current worksheet being processed in the outer ForEach loop.</summary>
    public static readonly Variable<WorksheetIdentifier> WorksheetItem = new(nameof(WorksheetItem));
    
    /// <summary>The current row context being processed in the inner row ForEach loop.</summary>
    public static readonly Variable<RowIdentifier> RowItem = new(nameof(RowItem));
    
    /// <summary>The presentation being scanned in the presentation scan ForEach loop.</summary>
    public static readonly Variable<PresentationIdentifier> PresentationItem = new(nameof(PresentationItem));

    /// <summary>The current row task being processed in the download or edit ForEach loops.</summary>
    public static readonly Variable<RowTask> RowTaskItem = new(nameof(RowTaskItem));
}
