using SlideGenerator.Application.Modules.Cloud.Abstractions;
using SlideGenerator.Application.Modules.Download.Models;
using SlideGenerator.Application.Modules.Download.Services;
using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Application.Modules.Settings.Interfaces;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Sheets.Entities;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     A workflow activity that downloads an image from a cloud or local source based on row data.
/// </summary>
/// <remarks>
///     The download process includes:
///     <list type="bullet">
///         <item>
///             <description>Resolving the specific image URL or path from the workbook row content.</description>
///         </item>
///         <item>
///             <description>Using a <see cref="ICloudResolver"/> to handle specialized cloud URIs (e.g., Google Drive, OneDrive).</description>
///         </item>
///         <item>
///             <description>Managing concurrent downloads through a <see cref="DownloadRegistry"/> to prevent duplicate or excessive requests.</description>
///         </item>
///         <item>
///             <description>Saving the downloaded file to a structured local path for subsequent processing.</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="cloudResolver">Service to resolve cloud-specific URIs to direct download links.</param>
/// <param name="workbookRegistry">Registry to safely access workbook data for URI resolution.</param>
/// <param name="downloadRegistry">Registry to coordinate and execute file downloads.</param>
/// <param name="settings">Provider for global application settings, such as download folders.</param>
public sealed class DownloadImage(
    ICloudResolver cloudResolver,
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    DownloadRegistry downloadRegistry,
    ISettingProvider settings) : StepBodyAsync
{
    /// <summary>
    ///     Gets or sets the row-specific task context, containing the worksheet, row index, and instruction to be resolved.
    /// </summary>
    public RowTask RowTask { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the specialized instruction containing the final resolved URI and target metadata.
    /// </summary>
    public SpecializedInstruction Result { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the exception if the download or resolution failed.
    /// </summary>
    public Exception? Exception { get; set; }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            var downloadItem = RowTask.DownloadItem
                               ?? throw new ArgumentException("DownloadItem must be set on the RowTask.");

            await using var lease = await workbookRegistry
                .AcquireAsync(RowTask.Worksheet.Workbook.FilePath, false, context.CancellationToken)
                .ConfigureAwait(false);
            var workbook = lease.Value;

            if (!workbook.TryGetWorksheet(RowTask.Worksheet.Name, out var ws))
                throw new InvalidOperationException(
                    $"Worksheet '{RowTask.Worksheet.Name}' does not exist in workbook.");

            var rowContent = ws.GetRowContent(RowTask.RowIndex);
            var specialized = downloadItem.Flatten(downloadItem, rowContent).FirstOrDefault(x => x.Value != null);
            if (specialized?.Value == null)
                return ExecutionResult.Next();

            var resolvedUri = await cloudResolver
                .ResolveUriAsync(specialized.Value, context.CancellationToken)
                .ConfigureAwait(false);
            var finalInstruction = specialized with { Value = resolvedUri };

            var downloadRoot = Path.GetFullPath(settings.Current.Download.DownloadFolder);
            var targetPath = finalInstruction.GetDownloadPath(downloadRoot, RowTask.Worksheet, RowTask.RowIndex);

            var request = new DownloadRequest(
                finalInstruction.Value.ToString(),
                Path.GetDirectoryName(targetPath)!,
                Path.GetFileNameWithoutExtension(targetPath));

            if (downloadRegistry.TryGetOrCreateDownloader(request, null, out var downloader))
                await downloader.DownloadAsync().ConfigureAwait(false);

            Result = finalInstruction;
        }
        catch (Exception ex)
        {
            Exception = ex;
        }

        return ExecutionResult.Next();
    }
}
