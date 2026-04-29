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
///     local download folder, then appends the resolved
///     <see cref="SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction" />
///     to the row's <see cref="VariablesDeclaration.SpecializedInstructions" /> Variable.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.RowTaskItem" />.<br />
///     <b>Variables written:</b> <see cref="VariablesDeclaration.SpecializedInstructions" /> — the resolved
///     instruction is appended (thread-safe via <c>lock</c>) to the row-scope list.<br />
///     <b>Services:</b> <see cref="ICloudResolver" />, <see cref="FileRegistry{IReadOnlyWorkbook}" />,
///     <c>DownloadRegistry</c>, <c>ISettingProvider</c>.<br />
///     <b>CancellationToken:</b> propagated to cloud resolver; workbook lease acquired via registry.
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
        var rowTask = context.GetVariable(rowTaskVar);

        var downloadItem = rowTask.DownloadItem
                           ?? throw new ArgumentException("DownloadItem must be set on the RowTask.");

        await using var lease = await workbookRegistry
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

        var list = context.GetVariable(VariablesDeclaration.SpecializedInstructions);
        lock (list)
        {
            list.Add(finalInstruction);
        }
    }
}
