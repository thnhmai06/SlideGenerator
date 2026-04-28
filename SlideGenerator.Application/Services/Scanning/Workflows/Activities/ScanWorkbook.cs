using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Workflows;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Application.Workflows.DSL;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Services.Scanning.Workflows.Activities;

/// <summary>
///     Scans a single workbook file and stores the result in <see cref="WorkflowTask.WorkbookSummaries" />.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.WorkbookItem" /> — the workbook identifier to scan.<br/>
///     <b>Variables written:</b> none.<br/>
///     <b>Services:</b> <see cref="FileRegistry{IReadOnlyWorkbook}" />.<br/>
///     <b>Logging:</b> via <c>context.State.Logger</c>.<br/>
///     <b>CancellationToken:</b> propagated to registry acquire.
/// </remarks>
public sealed class ScanWorkbook(
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    Variable<WorkbookIdentifier> workbookVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var data = context.Data;
        var fullPath = Path.GetFullPath(context.GetVariable(workbookVar).FilePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Workbook file not found.", fullPath);

        using var workbookLease = await workbookRegistry
            .AcquireAsync(fullPath, false, context.CancellationToken)
            .ConfigureAwait(false);

        var workbook = workbookLease.Value;
        var worksheets = workbook.Worksheets
            .Select(ws => new WorksheetSummary(ws.Name, ws.Headers, ws.RowsCount))
            .ToList();

        data.WorkbookSummaries[fullPath] = new WorkbookSummary(workbook.FilePath, workbook.Name, worksheets);
    }
}
