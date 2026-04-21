using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Download;
using SlideGenerator.Application.Download.Models;
using SlideGenerator.Application.Download.Services;
using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Workflows.Generating.Models.Images;
using SlideGenerator.Application.Workflows.Generating.Rules;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Downloads one image item to the configured storage folder.
///     Slot acquisition is handled externally (e.g., via <see cref="AcquireSlot" />).
/// </summary>
public sealed class DownloadImage(
    DownloadRegistry downloadRegistry,
    ISettingProvider settingProvider) : Activity
{
    public required Input<KeyValuePair<SpecializedInstruction, string>> Item { get; init; }
    public required Input<int> RowIndex { get; init; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var rowIndex = context.Get(RowIndex);
        if (rowIndex <= 0)
            throw new ArgumentException("Row index must be greater than 0.");

        var (imageInstruction, downloadUrl) = context.Get(Item);
        if (string.IsNullOrWhiteSpace(downloadUrl))
            return;

        var worksheetInfo = imageInstruction.Source.Worksheet;
        var downloadFolder = settingProvider.Current.Download.DownloadFolder;
        var workbookName = Utilities.NormalizeFileName(worksheetInfo.Workbook.Name, NamingRules.DefaultWorkbookName);
        var worksheetName = Utilities.NormalizeFileName(worksheetInfo.Name, NamingRules.DefaultWorksheetName);
        var downloadRootFolder = Path.Combine(downloadFolder, workbookName, worksheetName);

        var columnName = imageInstruction.Source.Name;
        var itemDownloadFolder = Path.Combine(downloadRootFolder, columnName);
        var downloadRequest = new DownloadRequest(
            downloadUrl,
            itemDownloadFolder,
            rowIndex.ToString());

        if (downloadRegistry.TryGetOrCreateDownloader(
                downloadRequest,
                settingProvider.Current.Download.GetConfigurationObject(),
                out var downloader))
            await downloader.DownloadAsync().ConfigureAwait(false);

        _ = DownloadRegistry.TryGetCompletedDownloadFilePath(downloadRequest, out _);
    }
}
