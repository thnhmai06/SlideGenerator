using SlideGenerator.Application.Download;
using SlideGenerator.Application.Download.Models;
using SlideGenerator.Application.Download.Services;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>
///     Downloads an image to the storage folder.
/// </summary>
/// <param name="downloadRegistry">The image download registry.</param>
/// <param name="settingProvider">The settings provider.</param>
public sealed class DownloadImage(
    DownloadRegistry downloadRegistry,
    ISettingProvider settingProvider) : Activity
{
    /// <inheritdoc />
    public override async ValueTask ExecuteAsync(IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var instruction = context.GetVariable<SpecializedInstruction>(WorksheetContextRules.DownloadItem);
        if (instruction?.Value == null)
            return;

        var downloadUrl = instruction.Value.ToString();
        var rowIndex = 1; // Preparation phase uses row 1

        var downloadFolder = settingProvider.Current.Download.DownloadFolder;
        var itemDownloadPath = instruction.GetDownloadPath(downloadFolder, rowIndex);
        var itemDownloadFolder = Path.GetDirectoryName(itemDownloadPath)!;

        var downloadRequest = new DownloadRequest(downloadUrl, itemDownloadFolder, rowIndex.ToString());

        if (downloadRegistry.TryGetOrCreateDownloader(
                downloadRequest,
                settingProvider.Current.Download.GetConfigurationObject(),
                out var downloader))
            await downloader.DownloadAsync().ConfigureAwait(false);

        _ = DownloadRegistry.TryGetCompletedDownloadFilePath(downloadRequest, out _);
    }
}