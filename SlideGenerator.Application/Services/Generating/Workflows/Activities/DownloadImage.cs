using SlideGenerator.Application.Modules.Cloud.Abstractions;
using SlideGenerator.Application.Modules.Download.Models;
using SlideGenerator.Application.Modules.Download.Services;
using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Settings.Interfaces;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Sheets.Entities;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Resolves the cloud storage URL for the current download task, downloads the image to the
///     local download folder, then records the resolved
///     <see cref="SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction" />
///     in the row's accumulated instruction list.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.RowTaskItem" />.<br />
///     <b>Data written:</b> <see cref="SheetTask.RowSpecializedInstructions" /> — the resolved instruction
///     is appended (thread-safe via <c>lock</c>) to the entry for the current row.<br />
///     <b>Services:</b> <see cref="ICloudResolver" />, <see cref="FileRegistry{IReadOnlyWorkbook}" />,
///     <c>DownloadRegistry</c>, <c>ISettingProvider</c>.<br />
///     <b>CancellationToken:</b> propagated to cloud resolver and registry acquire.
/// </remarks>
public sealed class DownloadImage(
    ICloudResolver cloudResolver,
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    DownloadRegistry downloadRegistry,
    ISettingProvider settings,
    Variable<RowTask> rowTaskVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var data = context.Data;
        var rowTask = context.GetVariable(rowTaskVar);
        var sheetTask = data.SheetTasks[rowTask.Worksheet];

        var downloadItem = rowTask.DownloadItem
                           ?? throw new ArgumentException("DownloadItem must be set on the RowTask.");

        using var lease = await workbookRegistry
            .AcquireAsync(rowTask.Worksheet.Workbook.FilePath, false, context.CancellationToken)
            .ConfigureAwait(false);

        var workbook = lease.Value;
        if (!workbook.TryGetWorksheet(rowTask.Worksheet.Name, out var ws))
            throw new InvalidOperationException(
                $"Worksheet '{rowTask.Worksheet.Name}' does not exist in workbook.");

        var rowContent = ws.GetRowContent(rowTask.RowIndex);
        var specialized = downloadItem.Flatten(downloadItem, rowContent).FirstOrDefault(x => x.Value != null);
        if (specialized?.Value == null)
            return;

        var resolvedUri = await cloudResolver
            .ResolveUriAsync(specialized.Value, context.CancellationToken)
            .ConfigureAwait(false);
        var finalInstruction = specialized with { Value = resolvedUri };

        var downloadRoot = Path.GetFullPath(settings.Current.Download.DownloadFolder);
        var targetPath = finalInstruction.GetDownloadPath(downloadRoot, rowTask.Worksheet, rowTask.RowIndex);

        var request = new DownloadRequest(
            finalInstruction.Value.ToString(),
            Path.GetDirectoryName(targetPath)!,
            Path.GetFileNameWithoutExtension(targetPath));

        if (downloadRegistry.TryGetOrCreateDownloader(request, null, out var downloader))
            await downloader.DownloadAsync().ConfigureAwait(false);

        var rowList = sheetTask.RowSpecializedInstructions.GetOrAdd(rowTask.RowIndex, _ => []);
        lock (rowList)
        {
            rowList.Add(finalInstruction);
        }
    }
}