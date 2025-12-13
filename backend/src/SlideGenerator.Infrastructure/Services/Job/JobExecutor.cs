using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Slide.Contracts;
using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Domain.Sheet.Enums;
using SlideGenerator.Infrastructure.Services.Base;

namespace SlideGenerator.Infrastructure.Services.Job;

public class JobExecutor(
    ILogger<JobExecutor> logger,
    JobManager jobManager,
    ISlideGenerator slideGenerator,
    IJobNotifier jobNotifier) : Service(logger), IJobExecutor
{
    public async Task ExecuteJobAsync(string jobId, CancellationToken cancellationToken)
    {
        var job = jobManager.GetInternalJob(jobId);
        if (job == null)
        {
            Logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        var group = jobManager.GetInternalGroup(job.GroupId);
        if (group == null)
        {
            Logger.LogWarning("Group {GroupId} not found for job {JobId}", job.GroupId, jobId);
            return;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, job.CancellationTokenSource.Token);
        var token = linkedCts.Token;

        try
        {
            job.SetStatus(SheetJobStatus.Running);
            await jobNotifier.NotifyJobStatusChanged(jobId, SheetJobStatus.Running);

            if (File.Exists(job.OutputPath)) File.Delete(job.OutputPath);
            File.Copy(job.Template.FilePath, job.OutputPath);

            var startRow = job.CurrentRow + 1;

            for (var rowNum = startRow; rowNum <= job.TotalRows; rowNum++)
            {
                if (token.IsCancellationRequested)
                {
                    if (job.Status != SheetJobStatus.Cancelled)
                        job.SetStatus(SheetJobStatus.Paused);
                    await jobNotifier.NotifyJobStatusChanged(jobId, job.Status);
                    return;
                }

                while (job.Status == SheetJobStatus.Paused)
                {
                    await Task.Delay(500, token);
                    if (token.IsCancellationRequested) return;
                }

                var rowData = job.Worksheet.GetRow(rowNum);
                var defaultRoiType = job.ImageConfigs.FirstOrDefault()?.RoiType ?? ImageRoiType.Center;

                await slideGenerator.ProcessRowAsync(
                    job.OutputPath,
                    job.Template.FilePath,
                    rowData,
                    job.TextConfigs,
                    job.ImageConfigs,
                    defaultRoiType,
                    token);

                job.UpdateProgress(rowNum);
                await jobNotifier.NotifyJobProgress(jobId, rowNum, job.TotalRows, job.Progress);
            }

            job.SetStatus(SheetJobStatus.Completed);
            await jobNotifier.NotifyJobStatusChanged(jobId, SheetJobStatus.Completed);
            Logger.LogInformation("Job {JobId} completed successfully", jobId);

            group.UpdateStatus();
            await jobNotifier.NotifyGroupProgress(group.Id, group.Progress);
            await jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status);
        }
        catch (OperationCanceledException)
        {
            if (job.Status != SheetJobStatus.Cancelled)
                job.SetStatus(SheetJobStatus.Paused);
            await jobNotifier.NotifyJobStatusChanged(jobId, job.Status);
            Logger.LogInformation("Job {JobId} was paused/cancelled", jobId);
        }
        catch (Exception ex)
        {
            job.SetStatus(SheetJobStatus.Failed, ex.Message);
            await jobNotifier.NotifyJobError(jobId, ex.Message);
            await jobNotifier.NotifyJobStatusChanged(jobId, SheetJobStatus.Failed, ex.Message);
            Logger.LogError(ex, "Job {JobId} failed", jobId);

            group.UpdateStatus();
            await jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status);
        }
    }
}