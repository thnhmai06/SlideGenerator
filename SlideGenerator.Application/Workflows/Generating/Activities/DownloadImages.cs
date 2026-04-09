using System.Collections.Concurrent;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Download.Abstractions;
using SlideGenerator.Domain.Download.Models;
using SlideGenerator.Domain.Settings.Interfaces;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Workflows.Models.Generating.Images;
using SlideGenerator.Domain.Workflows.Rules;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Workflow step that downloads images from already resolved URLs.
/// </summary>
public class DownloadImages(
    IDownloadRegistry downloadRegistry,
    ISettingProvider settingProvider) : Activity
{
    /// <summary>Input: Resolved image URLs keyed by specialized image instruction.</summary>
    public required Input<IReadOnlyDictionary<SpecializedInstruction, string>> ImageUrls { get; init; }

    /// <summary>Input: Target worksheet identifier used to derive workbook/worksheet names for download folder.</summary>
    public required Input<WorksheetIdentifier> Worksheet { get; init; }
    
    /// <summary>Input: 1-based row index in the target worksheet.</summary>
    public required Input<int> RowIndex { get; init; }
    
    /// <summary>
    ///     Output downloaded file paths keyed by specialized image instruction.
    /// </summary>
    public Output<IReadOnlyDictionary<SpecializedInstruction, string>> ImagePaths { get; init; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var worksheetInfo = context.Get(Worksheet);
        var rowIndex = context.Get(RowIndex);

        if (worksheetInfo is null || rowIndex <= 0)
            throw new ArgumentException("Worksheet information and row index must be provided and valid.");

        var downloadFolder = settingProvider.Current.Download.DownloadFolder;
        var workbookName = Utilities.NormalizeFileName(worksheetInfo.Workbook.Name, NamingRules.DefaultWorkbookName);
        var worksheetName = Utilities.NormalizeFileName(worksheetInfo.Name, NamingRules.DefaultWorksheetName);

        var downloadRootFolder = Path.Combine(downloadFolder, workbookName, worksheetName);
        var resolvedImageUrls = context.Get(ImageUrls) ?? new Dictionary<SpecializedInstruction, string>();
        var imagePaths = new ConcurrentDictionary<SpecializedInstruction, string>();

        foreach (var (imageInstruction, downloadUrl) in resolvedImageUrls)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                continue;

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

            if (!downloadRegistry.TryGetCompletedDownloadFilePath(downloadRequest, out var imagePath))
                continue;

            imagePaths.TryAdd(imageInstruction, imagePath);
        }

        context.Set(ImagePaths,
            new Dictionary<SpecializedInstruction, string>(imagePaths));
    }
}