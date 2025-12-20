using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Slide.Contracts;
using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Domain.Sheet.Enums;
using SlideGenerator.Infrastructure.Base;

namespace SlideGenerator.Infrastructure.Job.Services;

/// <inheritdoc />
public class JobExecutor(
    ILogger<JobExecutor> logger,
    JobManager jobManager,
    ISlideServices slideServices,
    IJobNotifier jobNotifier) : Service(logger), IJobExecutor
{
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

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, sheet.CancellationTokenSource.Token);
        var token = linkedCts.Token;

        try
        {
            sheet.SetStatus(SheetJobStatus.Running);
            await jobNotifier.NotifyJobStatusChanged(sheetId, SheetJobStatus.Running);

            File.Copy(sheet.Template.FilePath, sheet.OutputPath, true);

            var startRow = sheet.CurrentRow + 1;
            for (var rowNum = startRow; rowNum <= sheet.TotalRows; rowNum++)
            {
                sheet.WaitIfPaused(token);

                if (token.IsCancellationRequested)
                {
                    if (sheet.Status != SheetJobStatus.Cancelled)
                        sheet.SetStatus(SheetJobStatus.Paused);
                    await jobNotifier.NotifyJobStatusChanged(sheetId, sheet.Status);
                    return;
                }

                var rowData = sheet.Worksheet.GetRow(rowNum);
                var defaultRoiType = sheet.ImageConfigs.FirstOrDefault()?.RoiType ?? ImageRoiType.Center;

                await slideServices.ProcessRowAsync(
                    sheet.OutputPath,
                    sheet.Template.FilePath,
                    sheet.TextConfigs,
                    sheet.ImageConfigs,
                    rowData,
                    defaultRoiType, token);

                sheet.UpdateProgress(rowNum);
                await jobNotifier.NotifyJobProgress(sheetId, rowNum, sheet.TotalRows, sheet.Progress);
            }

            sheet.SetStatus(SheetJobStatus.Completed);
            await jobNotifier.NotifyJobStatusChanged(sheetId, SheetJobStatus.Completed);
            Logger.LogInformation("Job {JobId} completed successfully", sheetId);

            group.UpdateStatus();
            await jobNotifier.NotifyGroupProgress(group.Id, group.Progress);
            await jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status);

            // Notify job manager to check if group should move to completed
            jobManager.NotifySheetCompleted(sheetId);
        }
        catch (OperationCanceledException)
        {
            if (sheet.Status != SheetJobStatus.Cancelled)
                sheet.SetStatus(SheetJobStatus.Paused);
            await jobNotifier.NotifyJobStatusChanged(sheetId, sheet.Status);
            Logger.LogInformation("Job {JobId} was paused/cancelled", sheetId);
        }
        catch (Exception ex)
        {
            sheet.SetStatus(SheetJobStatus.Failed, ex.Message);
            await jobNotifier.NotifyJobError(sheetId, ex.Message);
            await jobNotifier.NotifyJobStatusChanged(sheetId, SheetJobStatus.Failed, ex.Message);
            Logger.LogError(ex, "Job {JobId} failed", sheetId);

            group.UpdateStatus();
            await jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status);

            // Notify job manager to check if group should move to completed
            jobManager.NotifySheetCompleted(sheetId);
        }
    }
}