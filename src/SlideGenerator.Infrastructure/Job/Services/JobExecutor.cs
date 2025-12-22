using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Slide;
using SlideGenerator.Domain.IO;
using SlideGenerator.Domain.Job.Entities;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Domain.Job.Notifications;
using SlideGenerator.Domain.Job.States;
using SlideGenerator.Infrastructure.Base;

namespace SlideGenerator.Infrastructure.Job.Services;

/// <inheritdoc cref="IJobExecutor" />
public class JobExecutor(
    ILogger<JobExecutor> logger,
    JobManager jobManager,
    ISlideServices slideServices,
    ISlideWorkingManager slideWorkingManager,
    IJobNotifier jobNotifier,
    IJobStateStore jobStateStore,
    IFileSystem fileSystem) : Service(logger), IJobExecutor
{
    /// <inheritdoc />
    public async Task ExecuteJobAsync(string sheetId, CancellationToken cancellationToken)
    {
        var sheet = jobManager.GetInternalSheet(sheetId);
        if (sheet == null)
        {
            Logger.LogWarning("Sheet {SheetId} not found", sheetId);
            return;
        }

        var group = jobManager.GetInternalGroup(sheet.GroupId);
        if (group == null)
        {
            Logger.LogWarning("Group {GroupId} not found for job {JobId}", sheet.GroupId, sheetId);
            return;
        }

        sheet.MarkExecuting(true);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, sheet.CancellationTokenSource.Token);
        var token = linkedCts.Token;

        JobCheckpoint checkpoint = async (_, ct) =>
        {
            await sheet.WaitIfPausedAsync(ct);
            ct.ThrowIfCancellationRequested();
        };

        int? activeRow = null;

        try
        {
            sheet.SetStatus(SheetJobStatus.Running);
            await jobNotifier.NotifyJobStatusChanged(sheetId, SheetJobStatus.Running);
            await PersistSheetStateAsync(sheet);
            group.UpdateStatus();
            await PersistGroupStateAsync(group);
            await jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status);

            if (sheet.CurrentRow == 0) // on start
            {
                slideWorkingManager.RemoveWorkingPresentation(sheet.OutputPath);
                fileSystem.CopyFile(group.Template.FilePath, sheet.OutputPath, true);
            }
            else
            {
                if (!fileSystem.FileExists(sheet.OutputPath))
                {
                    sheet.SetStatus(SheetJobStatus.Failed, "Output file missing during resume.");
                    await jobNotifier.NotifyJobStatusChanged(sheetId, sheet.Status, sheet.ErrorMessage);
                    await PersistSheetStateAsync(sheet);
                    group.UpdateStatus();
                    await PersistGroupStateAsync(group);
                    await jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status);
                    jobManager.NotifySheetCompleted(sheetId);
                    return;
                }
            }

            var startRow = sheet.NextRowIndex;
            for (var rowNum = startRow; rowNum <= sheet.TotalRows; rowNum++)
            {
                await checkpoint(JobCheckpointStage.BeforeRow, token);

                activeRow = rowNum;
                await StoreAndNotifyLogAsync(new JobEvent(
                    sheet.Id,
                    JobEventScope.Sheet,
                    DateTimeOffset.UtcNow,
                    "Info",
                    $"Processing row {rowNum}",
                    new Dictionary<string, object?>
                    {
                        ["row"] = rowNum,
                        ["rowStatus"] = "processing"
                    }));

                var rowData = sheet.Worksheet.GetRow(rowNum);
                var result = await slideServices.ProcessRowAsync(
                    sheet.OutputPath,
                    sheet.TextConfigs,
                    sheet.ImageConfigs,
                    rowData,
                    checkpoint,
                    token);

                await StoreAndNotifyLogAsync(new JobEvent(
                    sheet.Id,
                    JobEventScope.Sheet,
                    DateTimeOffset.UtcNow,
                    "Info",
                    $"Row {rowNum} completed (text: {result.TextReplacementCount}, images: {result.ImageReplacementCount}, image errors: {result.ImageErrorCount})",
                    new Dictionary<string, object?>
                    {
                        ["row"] = rowNum,
                        ["rowStatus"] = "completed",
                        ["textReplacements"] = result.TextReplacementCount,
                        ["imageReplacements"] = result.ImageReplacementCount,
                        ["imageErrors"] = result.ImageErrorCount
                    }));

                if (result.ImageErrorCount > 0)
                {
                    sheet.RegisterRowError(rowNum, string.Join("; ", result.Errors));
                    var detail = string.Join("; ", result.Errors);
                    var warningMessage = string.IsNullOrWhiteSpace(detail)
                        ? $"Row {rowNum} completed with {result.ImageErrorCount} image errors"
                        : $"Row {rowNum} completed with {result.ImageErrorCount} image errors: {detail}";
                    await StoreAndNotifyLogAsync(new JobEvent(
                        sheet.Id,
                        JobEventScope.Sheet,
                        DateTimeOffset.UtcNow,
                        "Warning",
                        warningMessage,
                        new Dictionary<string, object?>
                        {
                            ["row"] = rowNum,
                            ["rowStatus"] = "warning",
                            ["errors"] = result.Errors
                        }));
                }

                sheet.UpdateProgress(rowNum);
                await checkpoint(JobCheckpointStage.BeforePersistState, token);
                await PersistSheetStateAsync(sheet);
                await jobNotifier.NotifyJobProgress(sheetId, rowNum, sheet.TotalRows, sheet.Progress, sheet.ErrorCount);
                await jobNotifier.NotifyGroupProgress(group.Id, group.Progress, group.ErrorCount);
            }

            slideServices.RemoveFirstSlide(sheet.OutputPath);

            sheet.SetStatus(SheetJobStatus.Completed);
            await PersistSheetStateAsync(sheet);
            await jobNotifier.NotifyJobStatusChanged(sheetId, SheetJobStatus.Completed);
            Logger.LogInformation("Job {JobId} completed successfully", sheetId);

            group.UpdateStatus();
            await PersistGroupStateAsync(group);
            await jobNotifier.NotifyGroupProgress(group.Id, group.Progress, group.ErrorCount);
            await jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status);

            jobManager.NotifySheetCompleted(sheetId);
        }
        catch (OperationCanceledException)
        {
            if (sheet.Status != SheetJobStatus.Cancelled)
                sheet.SetStatus(SheetJobStatus.Paused);
            await PersistSheetStateAsync(sheet);
            await jobNotifier.NotifyJobStatusChanged(sheetId, sheet.Status);
            Logger.LogInformation("Job {JobId} was paused/cancelled", sheetId);

            group.UpdateStatus();
            await PersistGroupStateAsync(group);
            await jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status);

            if (sheet.Status == SheetJobStatus.Cancelled)
                jobManager.NotifySheetCompleted(sheetId);
        }
        catch (Exception ex)
        {
            sheet.SetStatus(SheetJobStatus.Failed, ex.Message);
            await PersistSheetStateAsync(sheet);
            await jobNotifier.NotifyJobError(sheetId, ex.Message);
            await jobNotifier.NotifyJobStatusChanged(sheetId, SheetJobStatus.Failed, ex.Message);
            await StoreAndNotifyLogAsync(new JobEvent(
                sheet.Id,
                JobEventScope.Sheet,
                DateTimeOffset.UtcNow,
                "Error",
                ex.Message,
                new Dictionary<string, object?>
                {
                    ["row"] = activeRow,
                    ["rowStatus"] = "error"
                }));
            Logger.LogError(ex, "Job {JobId} failed", sheetId);

            group.UpdateStatus();
            await PersistGroupStateAsync(group);
            await jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status);

            jobManager.NotifySheetCompleted(sheetId);
        }
        finally
        {
            sheet.MarkExecuting(false);
            if (sheet.Status is not SheetJobStatus.Pending and not SheetJobStatus.Running)
                slideWorkingManager.RemoveWorkingPresentation(sheet.OutputPath);
        }
    }

    private async Task PersistSheetStateAsync(JobSheet sheet)
    {
        var state = new SheetJobState(
            sheet.Id,
            sheet.GroupId,
            sheet.SheetName,
            sheet.OutputPath,
            sheet.Status,
            sheet.NextRowIndex,
            sheet.TotalRows,
            sheet.ErrorCount,
            sheet.ErrorMessage,
            sheet.TextConfigs,
            sheet.ImageConfigs);

        await jobStateStore.SaveSheetAsync(state, CancellationToken.None);
    }

    private async Task PersistGroupStateAsync(JobGroup group)
    {
        var state = new GroupJobState(
            group.Id,
            group.Workbook.FilePath,
            group.Template.FilePath,
            group.OutputFolder.FullName,
            group.Status,
            group.CreatedAt,
            group.InternalJobs.Keys.ToList(),
            group.ErrorCount);

        await jobStateStore.SaveGroupAsync(state, CancellationToken.None);
    }

    private async Task StoreAndNotifyLogAsync(JobEvent jobEvent)
    {
        var entry = new JobLogEntry(
            jobEvent.JobId,
            jobEvent.Timestamp,
            jobEvent.Level,
            jobEvent.Message,
            jobEvent.Data);
        await jobStateStore.AppendJobLogAsync(entry, CancellationToken.None);
        await jobNotifier.NotifyLog(jobEvent);
    }
}