using System.Collections.Concurrent;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Download.Abstractions;
using SlideGenerator.Domain.Download.Models;
using SlideGenerator.Domain.Settings.Interfaces;
using SlideGenerator.Domain.Tasks.Models.Image;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Workflow step that downloads images from already resolved URLs.
/// </summary>
public class DownloadImages : Activity
{
    /// <summary>Input: Resolved image URLs keyed by specialized image instruction.</summary>
    public Input<IReadOnlyDictionary<SpecializedInstruction, string>> ResolvedImageUrls { get; set; } = null!;

    /// <summary>Input: 1-based row index in the target worksheet.</summary>
    public Input<int> RowIndex { get; set; } = new(1);

    /// <summary>Input: Target worksheet identifier used to derive workbook/worksheet names for download folder.</summary>
    public Input<WorksheetIdentifier> WorksheetInfo { get; set; } = null!;

    /// <summary>
    ///     Output downloaded file paths keyed by specialized image instruction.
    /// </summary>
    public Output<IReadOnlyDictionary<SpecializedInstruction, string>> ImagePaths { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the download registry dependency (injected by DI).
    /// </summary>
    public IDownloadRegistry DownloadRegistry { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the setting provider dependency (injected by DI).
    /// </summary>
    public ISettingProvider SettingProvider { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the workbook registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IReadOnlyWorkbook> WorkbookRegistry { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var worksheetInfo = context.Get(WorksheetInfo);
        var rowIndex = context.Get(RowIndex);

        if (worksheetInfo is null || rowIndex <= 0)
        {
            context.Set(ImagePaths, new Dictionary<SpecializedInstruction, string>());
            return;
        }

        var downloadFolder = SettingProvider.Current.Download.DownloadFolder;
        var worksheetName = Utilities.NormalizeFileName(worksheetInfo.Name);
        if (string.IsNullOrWhiteSpace(worksheetName))
            worksheetName = "worksheet";

        var workbook = WorkbookRegistry.GetOrOpen(worksheetInfo.Workbook.FilePath, isEditable: false);
        var workbookName = Utilities.NormalizeFileName(workbook.Name);
        if (string.IsNullOrWhiteSpace(workbookName))
            workbookName = "workbook";

        var downloadRootFolder = Path.Combine(downloadFolder, workbookName, worksheetName);
        var resolvedImageUrls = context.Get(ResolvedImageUrls) ?? new Dictionary<SpecializedInstruction, string>();
        var imagePaths = new ConcurrentDictionary<SpecializedInstruction, string>();

        foreach (var (imageInstruction, downloadUrl) in resolvedImageUrls)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                continue;

            var columnName = imageInstruction.Source.ColumnName;
            var itemDownloadFolder = Path.Combine(downloadRootFolder, columnName);
            var downloadRequest = new DownloadRequest(
                downloadUrl,
                itemDownloadFolder,
                rowIndex.ToString());

            if (DownloadRegistry.TryGetOrCreateDownloader(
                    downloadRequest,
                    SettingProvider.Current.Download.GetConfigurationObject(),
                    out var downloader))
                await downloader.DownloadAsync().ConfigureAwait(false);

            if (!DownloadRegistry.TryGetCompletedDownloadFilePath(downloadRequest, out var imagePath))
                continue;

            imagePaths.TryAdd(imageInstruction, imagePath);
        }

        context.Set(ImagePaths,
            new Dictionary<SpecializedInstruction, string>(imagePaths));
    }
}