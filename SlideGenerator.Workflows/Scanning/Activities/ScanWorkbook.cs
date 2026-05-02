using SlideGenerator.Workflows.Scanning.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Workflows.Scanning.Activities;

public sealed class ScanWorkbook(SfWorkbookFactory workbookFactory) : StepBodyAsync
{
    public SlideGenerator.Sheets.Models.WorkbookIdentifier Workbook { get; set; } = null!;
    public WorkbookSummary Result { get; set; } = null!;
    public Exception? Exception { get; set; }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            var fullPath = Path.GetFullPath(Workbook.FilePath);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Workbook not found.", fullPath);

            await using var lease = await workbookFactory.AcquireAsync(fullPath, false, context.CancellationToken).ConfigureAwait(false);
            var workbook = lease.Value.Workbook;

            var worksheets = new List<WorksheetSummary>(workbook.Worksheets.Count);
            foreach (Syncfusion.XlsIO.IWorksheet ws in workbook.Worksheets)
            {
                var headers = ws.GetHeaders();
                var count = ws.CountRows();
                var rows = new List<IReadOnlyDictionary<string, string>>();
                
                for (var i = 1; i <= Math.Min(10, count); i++)
                {
                    rows.Add(ws.GetRow(i, headers));
                }

                worksheets.Add(new WorksheetSummary(ws.Name, new WorksheetPreview(headers, rows), count));
            }

            Result = new WorkbookSummary(fullPath, Path.GetFileNameWithoutExtension(fullPath), worksheets);
        }
        catch (Exception ex)
        {
            Exception = ex;
        }

        return ExecutionResult.Next();
    }
}