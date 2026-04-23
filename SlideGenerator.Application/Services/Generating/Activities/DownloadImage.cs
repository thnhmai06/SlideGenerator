using SlideGenerator.Application.Download;
using SlideGenerator.Application.Download.Models;
using SlideGenerator.Application.Download.Services;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>Downloads an image to the storage folder.</summary>
/// <remarks>Concurrency throttling is expected to be applied externally.</remarks>
/// <param name="downloadRegistry">The image download registry.</param>
/// <param name="settingProvider">The settings provider.</param>
public sealed class DownloadImage(
    DownloadRegistry downloadRegistry,
    ISettingProvider settingProvider) : Activity
{
    /// <inheritdoc />
    public override async ValueTask ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var (imageInstruction, downloadUrl) = context.GetVariable<KeyValuePair<SpecializedInstruction, string>>(WorksheetContextRules.DownloadItem);
        if (string.IsNullOrWhiteSpace(downloadUrl))
            return;

        // As before, hardcoding to 1 for the preparation phase.
        int rowIndex = 1;

        var worksheetInfo = imageInstruction.Source.Worksheet;
        var downloadFolder = settingProvider.Current.Download.DownloadFolder;
        var workbookName = NamingRules.NormalizeFileName(worksheetInfo.Workbook.Name, NamingRules.DefaultWorkbookName);
        var worksheetName = NamingRules.NormalizeFileName(worksheetInfo.Name, NamingRules.DefaultWorksheetName);
        var downloadRootFolder = Path.Combine(downloadFolder, workbookName, worksheetName);

        var columnName = imageInstruction.Source.Name;
        var itemDownloadFolder = Path.Combine(downloadRootFolder, columnName);
        var downloadRequest = new DownloadRequest(downloadUrl, itemDownloadFolder, rowIndex.ToString());

        if (downloadRegistry.TryGetOrCreateDownloader(
                downloadRequest,
                settingProvider.Current.Download.GetConfigurationObject(),
                out var downloader))
            await downloader.DownloadAsync().ConfigureAwait(false);

        _ = DownloadRegistry.TryGetCompletedDownloadFilePath(downloadRequest, out _);
    }
}
