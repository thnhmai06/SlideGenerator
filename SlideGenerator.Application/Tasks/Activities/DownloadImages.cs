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
using SlideGenerator.Domain.Tasks.Rules;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Workflow step that downloads images from already resolved URLs.
/// </summary>
public class DownloadImages(
    IDownloadRegistry downloadRegistry,
    ISettingProvider settingProvider,
    IRegistry<IReadOnlyWorkbook> workbookRegistry) : Activity
{
    /// <summary>Input: Resolved image URLs keyed by specialized image instruction.</summary>
    public Input<IReadOnlyDictionary<SpecializedInstruction, string>> ImageUrls { get; set; } = null!;

    /// <summary>Input: Target worksheet identifier used to derive workbook/worksheet names for download folder.</summary>
    public Input<WorksheetIdentifier> Worksheet { get; set; } = null!;
    
    /// <summary>Input: 1-based row index in the target worksheet.</summary>
    public Input<int> RowIndex { get; set; } = new(0);
    
    /// <summary>
    ///     Output downloaded file paths keyed by specialized image instruction.
    /// </summary>
    public Output<IReadOnlyDictionary<SpecializedInstruction, string>> ImagePaths { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var worksheetInfo = context.Get(Worksheet);
        var rowIndex = context.Get(RowIndex);

        if (worksheetInfo is null || rowIndex <= 0)
            throw new ArgumentException("Worksheet information and row index must be provided and valid.");

        var downloadFolder = settingProvider.Current.Download.DownloadFolder;
        var workbookName = Utilities.NormalizeFileName(worksheetInfo.Workbook.Name, NamingRules.DEFAULT_WORKBOOK_NAME);
        var worksheetName = Utilities.NormalizeFileName(worksheetInfo.Name, NamingRules.DEFAULT_WORKSHEET_NAME);

        var downloadRootFolder = Path.Combine(downloadFolder, workbookName, worksheetName);
        var resolvedImageUrls = context.Get(ImageUrls) ?? new Dictionary<SpecializedInstruction, string>();
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

            if (downloadRegistry.TryGetOrCreateDownloader(
                    downloadRequest,
                    settingProvider.Current.Download.GetConfigurationObject(),
                    out var downloader))
                await downloader.DownloadAsync().ConfigureAwait(false);

            if (!downloadRegistry.TryGetCompletedDownloadFilePath(downloadRequest, out var imagePath))
                continue;

            imagePaths.TryAdd(imageInstruction, imagePath);
        }

        context.Set(ImagePaths,
            new Dictionary<SpecializedInstruction, string>(imagePaths));
    }
}