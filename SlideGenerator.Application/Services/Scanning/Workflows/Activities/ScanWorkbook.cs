using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Generating.Workflows;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Services.Scanning.Workflows.Activities;

/// <summary>
///     Scans a single workbook file and stores the result in the <see cref="VariablesDeclaration.WorkbookSummaries" />
///     Variable.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.WorkbookItem" /> — the workbook identifier to scan.<br />
///     <b>Variables written:</b> <see cref="VariablesDeclaration.WorkbookSummaries" /> — adds the scan result entry.<br />
///     <b>Services:</b> <see cref="FileRegistry{T}" /> — acquires a fresh read lease; released on completion.<br />
///     <b>Logging:</b> via <c>context.State.Logger</c>.<br />
///     <b>CancellationToken:</b> propagated to lease acquire.
/// </remarks>
public sealed class ScanWorkbook(
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    Variable<WorkbookIdentifier> workbookVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var fullPath = Path.GetFullPath(context.GetVariable(workbookVar).FilePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Workbook file not found.", fullPath);

        await using var lease = await workbookRegistry.AcquireAsync(fullPath, false, context.CancellationToken)
            .ConfigureAwait(false);
        var workbook = lease.Value;

        var worksheets = workbook.Worksheets
            .Select(ws => new WorksheetSummary(ws.Name, ws.Headers, ws.RowsCount))
            .ToList();

        context.GetVariable(VariablesDeclaration.WorkbookSummaries)[fullPath] =
            new WorkbookSummary(workbook.FilePath, workbook.Name, worksheets);
    }
}
