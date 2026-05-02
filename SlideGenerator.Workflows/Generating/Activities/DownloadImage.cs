using SlideGenerator.Cloud.Services;
using SlideGenerator.Download.Services;
using SlideGenerator.Workflows.Generating.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Workflows.Generating.Activities;

public sealed class DownloadImage(
    MultiCloudResolver cloudResolver,
    SfWorkbookFactory workbookFactory,
    DownloadService downloadService,
    Setting settings) : StepBodyAsync
{
    public RowTask RowTask { get; set; } = null!;
    public SpecializedInstruction Result { get; set; } = null!;
    public Exception? Exception { get; set; }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            var downloadItem = RowTask.DownloadItem
                               ?? throw new ArgumentException("DownloadItem must be set on the RowTask.");

            await using var lease = await workbookFactory
                .AcquireAsync(RowTask.Workbook.FilePath, false, context.CancellationToken)
                .ConfigureAwait(false);
            var workbook = lease.Value.Workbook;

            var ws = workbook.Worksheets[RowTask.WorksheetName];
            if (ws == null)
                throw new InvalidOperationException($"Worksheet '{RowTask.WorksheetName}' does not exist.");

            // Get row content (simplified for this migration turn)
            var rowContent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i <= ws.Columns.Length; i++)
            {
                var colName = ws[1, i].Value;
                if (!string.IsNullOrEmpty(colName))
                    rowContent[colName] = ws[RowTask.RowIndex, i].Value;
            }

            var specialized = downloadItem.Flatten(rowContent).FirstOrDefault(x => x.Value != null);
            if (specialized?.Value == null)
                return ExecutionResult.Next();

            var resolvedUri = await cloudResolver
                .ResolveUriAsync(specialized.Value, context.CancellationToken)
                .ConfigureAwait(false);
            var finalInstruction = specialized with { Value = resolvedUri };

            var downloadRoot = Path.GetFullPath(settings.Download.DownloadFolder);
            var targetPath = finalInstruction.GetDownloadPath(downloadRoot, RowTask.Workbook, RowTask.WorksheetName, RowTask.RowIndex);

            if (File.Exists(targetPath))
            {
                Result = finalInstruction;
                return ExecutionResult.Next();
            }

            await downloadService.DownloadAsync(finalInstruction.Value, targetPath, context.CancellationToken).ConfigureAwait(false);

            Result = finalInstruction;
        }
        catch (Exception ex)
        {
            Exception = ex;
        }

        return ExecutionResult.Next();
    }
}