using SlideGenerator.Cloud.Services;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Download.Services;
using SlideGenerator.Services.Generating.Workflows.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Services.Generating.Steps;

/// <summary>
///     Downloads a single image from a cloud URI to a local temporary path.
///     Implements idempotency by skipping existing files.
/// </summary>
public sealed class DownloadImage(
    DownloadService downloadService,
    MultiCloudResolver multiCloudResolver,
    GateLocker gateLocker,
    HttpClient httpClient) : StepBodyAsync
{
    /// <summary>
    ///     The download task to process.
    ///     Mapped from the ForEach loop in the workflow.
    /// </summary>
    public ImageTask Task { get; set; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;

        // Idempotency: skip if file already exists
        if (File.Exists(Task.DownloadPath))
            return ExecutionResult.Next();

        // Ensure directory exists
        var dir = Path.GetDirectoryName(Task.DownloadPath);
        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        await gateLocker.AcquireAsync(GateType.DownloadImage).ConfigureAwait(false);
        try
        {
            if (Task.SourceUri == null)
            {
                data.Errors.TryAdd($"Download_Row{Task.RowIndex}_{Task.ColumnName}",
                    new Exception($"[Warning] URI is not valid for {Task.ColumnName}:{Task.RowIndex}. Skipping."));
                return ExecutionResult.Next();
            }

            var resolvedUri =
                await multiCloudResolver.ResolveUriAsync(Task.SourceUri, httpClient).ConfigureAwait(false);
            await downloadService.DownloadAsync(resolvedUri, Task.DownloadPath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            data.Errors.TryAdd($"Download_Row{Task.RowIndex}_{Task.ColumnName}", ex);
        }
        finally
        {
            gateLocker.Release(GateType.DownloadImage);
        }

        return ExecutionResult.Next();
    }
}