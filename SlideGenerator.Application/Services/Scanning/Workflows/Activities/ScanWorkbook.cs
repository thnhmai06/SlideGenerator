using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Application.Services.Scanning.Workflows.Activities;

/// <summary>
///     A workflow activity that scans a workbook file to extract worksheet summaries and previews.
/// </summary>
/// <remarks>
///     The scanning process includes:
///     <list type="bullet">
///         <item>
///             <description>Acquiring a read-only lease on the workbook file using the <see cref="FileRegistry{T}"/>.</description>
///         </item>
///         <item>
///             <description>Iterating through all worksheets in the workbook.</description>
///         </item>
///         <item>
///             <description>Generating a data preview (first 10 rows) for each worksheet.</description>
///         </item>
///         <item>
///             <description>Capturing metadata such as worksheet names and total row counts.</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="workbookRegistry">The registry used to manage concurrent access to workbook files.</param>
public sealed class ScanWorkbook(FileRegistry<IReadOnlyWorkbook> workbookRegistry) : StepBodyAsync
{
    /// <summary>
    ///     Gets or sets the identifier of the workbook to be scanned.
    /// </summary>
    public WorkbookIdentifier Workbook { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the resulting summary of the scanned workbook.
    /// </summary>
    public WorkbookSummary Result { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the exception if the scan failed.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <inheritdoc />
    /// <exception cref="FileNotFoundException">Thrown when the workbook file does not exist at the specified path.</exception>
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            var fullPath = Path.GetFullPath(Workbook.FilePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Workbook file not found.", fullPath);

            await using var lease = await workbookRegistry.AcquireAsync(fullPath, false, context.CancellationToken)
                .ConfigureAwait(false);
            var workbook = lease.Value;

            var worksheets = new List<WorksheetSummary>(workbook.Worksheets.Count);
            foreach (var ws in workbook.Worksheets)
            {
                var preview = await ws.GetPreview(from: 1, to: 10, skipPreview: false, ct: context.CancellationToken)
                    .ConfigureAwait(false);
                worksheets.Add(new WorksheetSummary(ws.Name, preview, ws.RowsCount));
            }

            Result = new WorkbookSummary(workbook.FilePath, workbook.Name, worksheets);
        }
        catch (Exception ex)
        {
            Exception = ex;
        }

        return ExecutionResult.Next();
    }
}
