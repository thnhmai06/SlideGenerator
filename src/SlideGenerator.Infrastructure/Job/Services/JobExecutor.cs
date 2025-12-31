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
        if (!TryGetSheetAndGroup(sheetId, out var sheet, out var group) || sheet == null || group == null)
            return;

        sheet.MarkExecuting(true);
        var executionContext = new JobExecutionContext();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, sheet.CancellationTokenSource.Token);
        var token = linkedCts.Token;

        var checkpoint = CreateCheckpoint(sheet);

        List<JobLogEntry>? bufferedLogs;

        try
        {
            if (sheet.Status == SheetJobStatus.Paused)
            {
                await sheet.WaitIfPausedAsync(token);
                token.ThrowIfCancellationRequested();
            }
            await StartJobAsync(sheet, group, sheetId);
            if (!await EnsureOutputFileReadyAsync(sheet, group, sheetId))
                return;
            await ProcessRowsAsync(sheet, group, sheetId, checkpoint, token, executionContext);
            await CompleteJobAsync(sheet, group, sheetId);
        }
        catch (OperationCanceledException)
        {
            bufferedLogs = executionContext.BufferedLogs;
            await HandleCancellationAsync(sheet, group, sheetId, bufferedLogs);
        }
        catch (Exception ex)
        {
            bufferedLogs = executionContext.BufferedLogs;
            var activeRow = executionContext.ActiveRow;
            await HandleFailureAsync(sheet, group, sheetId, ex, activeRow, bufferedLogs);
        }
        finally
        {
            sheet.MarkExecuting(false);
            if (sheet.Status is not SheetJobStatus.Pending and not SheetJobStatus.Running)
                slideWorkingManager.RemoveWorkingPresentation(sheet.OutputPath);
        }
    }

    private bool TryGetSheetAndGroup(string sheetId, out JobSheet? sheet, out JobGroup? group)
    {
        sheet = jobManager.GetInternalSheet(sheetId);
        if (sheet == null)
        {
            Logger.LogWarning("Sheet {SheetId} not found", sheetId);
            group = null;
            return false;
        }

        group = jobManager.GetInternalGroup(sheet.GroupId);
        if (group == null)
        {
            Logger.LogWarning("Group {GroupId} not found for job {JobId}", sheet.GroupId, sheetId);
            return false;
        }

        return true;
    }

    private static JobCheckpoint CreateCheckpoint(JobSheet sheet)
    {
        return async (_, ct) =>
        {
            await sheet.WaitIfPausedAsync(ct);
            ct.ThrowIfCancellationRequested();
        };
    }

    private async Task StartJobAsync(JobSheet sheet, JobGroup group, string sheetId)
    {
        sheet.SetStatus(SheetJobStatus.Running);
        await jobNotifier.NotifyJobStatusChanged(sheetId, SheetJobStatus.Running);
        await PersistSheetStateAsync(sheet);
        group.UpdateStatus();
        await PersistGroupStateAsync(group);
        await jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status);
    }

    private async Task<bool> EnsureOutputFileReadyAsync(JobSheet sheet, JobGroup group, string sheetId)
    {
        if (sheet.CurrentRow == 0)
        {
            slideWorkingManager.RemoveWorkingPresentation(sheet.OutputPath);
            fileSystem.CopyFile(group.Template.FilePath, sheet.OutputPath, true);
            return true;
        }

        if (fileSystem.FileExists(sheet.OutputPath))
            return true;

        sheet.SetStatus(SheetJobStatus.Failed, "Output file missing during resume.");
        await jobNotifier.NotifyJobStatusChanged(sheetId, sheet.Status, sheet.ErrorMessage);
        await PersistSheetStateAsync(sheet);
        group.UpdateStatus();
        await PersistGroupStateAsync(group);
        await jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status);
        jobManager.NotifySheetCompleted(sheetId);
        return false;
    }

    private async Task ProcessRowsAsync(
        JobSheet sheet,
        JobGroup group,
        string sheetId,
        JobCheckpoint checkpoint,
        CancellationToken token,
        JobExecutionContext context)
    {
        var startRow = sheet.NextRowIndex;
        for (var rowNum = startRow; rowNum <= sheet.TotalRows; rowNum++)
        {
            await checkpoint(JobCheckpointStage.BeforeRow, token);

            context.ActiveRow = rowNum;
            var buffer = new List<JobLogEntry>(4);
            context.BufferedLogs = buffer;
            await LogRowStartedAsync(sheet, rowNum, buffer);

            var rowData = sheet.Worksheet.GetRow(rowNum);
            var result = await slideServices.ProcessRowAsync(
                sheet.OutputPath,
                sheet.TextConfigs,
                sheet.ImageConfigs,
                rowData,
                checkpoint,
                token);

            await LogTextReplacementsAsync(sheet, rowNum, result.TextReplacements, buffer);
            await LogImageReplacementsAsync(sheet, rowNum, result.ImageReplacements, buffer);
            await LogRowCompletedAsync(sheet, rowNum, result, buffer);

            if (result.ImageErrorCount > 0)
                await LogRowWarningsAsync(sheet, rowNum, result, buffer);

            await FlushLogsAsync(context.BufferedLogs);
            context.BufferedLogs = null;

            sheet.UpdateProgress(rowNum);
            await checkpoint(JobCheckpointStage.BeforePersistState, token);
            await PersistSheetStateAsync(sheet);
            await jobNotifier.NotifyJobProgress(sheetId, rowNum, sheet.TotalRows, sheet.Progress, sheet.ErrorCount);
            await jobNotifier.NotifyGroupProgress(group.Id, group.Progress, group.ErrorCount);
        }
    }

    private async Task LogRowStartedAsync(JobSheet sheet, int rowNum, List<JobLogEntry> buffer)
    {
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
            }), buffer);
    }

    private async Task LogTextReplacementsAsync(
        JobSheet sheet,
        int rowNum,
        IReadOnlyCollection<TextReplacementDetail> details,
        List<JobLogEntry> buffer)
    {
        foreach (var detail in details)
            await StoreAndNotifyLogAsync(new JobEvent(
                sheet.Id,
                JobEventScope.Sheet,
                DateTimeOffset.UtcNow,
                "Info",
                $"Row {rowNum} text -> shape {detail.ShapeId}: {detail.Placeholder} = {detail.Value}",
                new Dictionary<string, object?>
                {
                    ["row"] = rowNum,
                    ["shapeId"] = detail.ShapeId,
                    ["placeholder"] = detail.Placeholder,
                    ["value"] = detail.Value,
                    ["kind"] = "text"
                }), buffer);
    }

    private async Task LogImageReplacementsAsync(
        JobSheet sheet,
        int rowNum,
        IReadOnlyCollection<ImageReplacementDetail> details,
        List<JobLogEntry> buffer)
    {
        foreach (var detail in details)
            await StoreAndNotifyLogAsync(new JobEvent(
                sheet.Id,
                JobEventScope.Sheet,
                DateTimeOffset.UtcNow,
                "Info",
                $"Row {rowNum} image -> shape {detail.ShapeId}: {detail.Source}",
                new Dictionary<string, object?>
                {
                    ["row"] = rowNum,
                    ["shapeId"] = detail.ShapeId,
                    ["source"] = detail.Source,
                    ["kind"] = "image"
                }), buffer);
    }

    private async Task LogRowCompletedAsync(
        JobSheet sheet,
        int rowNum,
        RowProcessResult result,
        List<JobLogEntry> buffer)
    {
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
            }), buffer);
    }

    private async Task LogRowWarningsAsync(
        JobSheet sheet,
        int rowNum,
        RowProcessResult result,
        List<JobLogEntry> buffer)
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
            }), buffer);
    }

    private async Task CompleteJobAsync(JobSheet sheet, JobGroup group, string sheetId)
    {
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

    private async Task HandleCancellationAsync(
        JobSheet sheet,
        JobGroup group,
        string sheetId,
        List<JobLogEntry>? bufferedLogs)
    {
        await FlushLogsAsync(bufferedLogs);

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

    private async Task HandleFailureAsync(
        JobSheet sheet,
        JobGroup group,
        string sheetId,
        Exception ex,
        int? activeRow,
        List<JobLogEntry>? bufferedLogs)
    {
        await FlushLogsAsync(bufferedLogs);

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

    private async Task StoreAndNotifyLogAsync(JobEvent jobEvent, List<JobLogEntry>? buffer = null)
    {
        var entry = new JobLogEntry(
            jobEvent.JobId,
            jobEvent.Timestamp,
            jobEvent.Level,
            jobEvent.Message,
            jobEvent.Data);
        if (buffer == null)
            await jobStateStore.AppendJobLogAsync(entry, CancellationToken.None);
        else
            buffer.Add(entry);
        await jobNotifier.NotifyLog(jobEvent);
    }

    private Task FlushLogsAsync(List<JobLogEntry>? buffer)
    {
        if (buffer == null || buffer.Count == 0)
            return Task.CompletedTask;

        return jobStateStore.AppendJobLogsAsync(buffer, CancellationToken.None);
    }

    private sealed class JobExecutionContext
    {
        public int? ActiveRow { get; set; }
        public List<JobLogEntry>? BufferedLogs { get; set; }
    }
}
