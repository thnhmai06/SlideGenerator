using System.Collections.Concurrent;
using Hangfire;
using Microsoft.Extensions.Logging;
using TaoSlideTotNghiep.Application.Job.Contracts;
using TaoSlideTotNghiep.Application.Sheet.Contracts;
using TaoSlideTotNghiep.Application.Slide.Contracts;
using TaoSlideTotNghiep.Application.Slide.DTOs.Requests.Group;
using TaoSlideTotNghiep.Domain.Job.Entities;
using TaoSlideTotNghiep.Domain.Job.Interfaces;
using TaoSlideTotNghiep.Domain.Sheet.Enums;
using TaoSlideTotNghiep.Infrastructure.Services.Base;
using TaoSlideTotNghiep.Infrastructure.Utilities;

namespace TaoSlideTotNghiep.Infrastructure.Services.Job;

public class JobManager(
    ILogger<JobManager> logger,
    ISheetService sheetService,
    ISlideTemplateService slideTemplateService,
    IBackgroundJobClient backgroundJobClient) : Service(logger), IJobManager
{
    private readonly ConcurrentDictionary<string, JobGroup> _groups = new();
    private readonly ConcurrentDictionary<string, JobSheet> _jobIndex = new();

    public IJobGroup CreateGroup(GenerateSlideGroupCreate request)
    {
        var workbook = sheetService.OpenFile(request.SpreadsheetPath);
        var sheetsInfo = sheetService.GetSheets(workbook);

        slideTemplateService.AddTemplate(request.TemplatePresentationPath);
        var template = slideTemplateService.GetTemplate(request.TemplatePresentationPath);

        var bookName = PathUtils.SanitizeFileName(
            workbook.Name ?? Path.GetFileNameWithoutExtension(request.SpreadsheetPath));
        var outputFolder = Path.Combine(request.FilePath, bookName);
        Directory.CreateDirectory(outputFolder);

        var group = new JobGroup(
            workbook,
            template,
            outputFolder,
            request.TextConfigs,
            request.ImageConfigs);

        var targetSheets = request.SheetNames?.Length > 0
            ? sheetsInfo.Where(s => request.SheetNames.Contains(s.Key)).Select(s => s.Key)
            : sheetsInfo.Keys;

        foreach (var sheetName in targetSheets)
        {
            var sanitizedSheetName = PathUtils.SanitizeFileName(sheetName);
            var outputPath = Path.Combine(outputFolder, $"{sanitizedSheetName}.pptx");
            var job = group.AddJob(sheetName, outputPath);
            _jobIndex[job.Id] = job;
        }

        _groups[group.Id] = group;
        Logger.LogInformation("Created group {GroupId} with {JobCount} jobs", group.Id, group.Jobs.Count);

        return group;
    }

    public IJobGroup? GetGroup(string groupId)
    {
        return _groups.GetValueOrDefault(groupId);
    }

    public IJobSheet? GetJob(string jobId)
    {
        return _jobIndex.GetValueOrDefault(jobId);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetAllGroups()
    {
        return _groups.ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    public void StartGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group))
        {
            Logger.LogWarning("Group {GroupId} not found", groupId);
            return;
        }

        group.SetStatus(GroupStatus.Running);

        foreach (var job in group.InternalJobs.Values.Where(j => j.Status == SheetJobStatus.Pending))
        {
            var hangfireJobId = backgroundJobClient.Enqueue<IJobExecutor>(executor =>
                executor.ExecuteJobAsync(job.Id, CancellationToken.None));
            job.HangfireJobId = hangfireJobId;
        }

        Logger.LogInformation("Started group {GroupId}", groupId);
    }

    public void PauseGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values.Where(j => j.Status == SheetJobStatus.Running))
            PauseJobInternal(job);

        group.SetStatus(GroupStatus.Paused);
        Logger.LogInformation("Paused group {GroupId}", groupId);
    }

    public void ResumeGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values.Where(j => j.Status == SheetJobStatus.Paused))
            ResumeJobInternal(job);

        group.SetStatus(GroupStatus.Running);
        Logger.LogInformation("Resumed group {GroupId}", groupId);
    }

    public void CancelGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values.Where(j =>
                     j.Status is SheetJobStatus.Pending or SheetJobStatus.Running or SheetJobStatus.Paused))
            CancelJobInternal(job);

        group.SetStatus(GroupStatus.Cancelled);
        Logger.LogInformation("Cancelled group {GroupId}", groupId);
    }

    public void PauseJob(string jobId)
    {
        if (_jobIndex.TryGetValue(jobId, out var job))
            PauseJobInternal(job);
    }

    public void ResumeJob(string jobId)
    {
        if (_jobIndex.TryGetValue(jobId, out var job))
            ResumeJobInternal(job);
    }

    public void CancelJob(string jobId)
    {
        if (_jobIndex.TryGetValue(jobId, out var job))
            CancelJobInternal(job);
    }

    public void PauseAll()
    {
        foreach (var group in _groups.Values.Where(g => g.Status == GroupStatus.Running))
            PauseGroup(group.Id);
    }

    public void ResumeAll()
    {
        foreach (var group in _groups.Values.Where(g => g.Status == GroupStatus.Paused))
            ResumeGroup(group.Id);
    }

    public void CancelAll()
    {
        foreach (var group in _groups.Values.Where(g =>
                     g.Status is GroupStatus.Pending or GroupStatus.Running or GroupStatus.Paused))
            CancelGroup(group.Id);
    }

    public bool HasActiveJobs()
    {
        return _groups.Values.Any(g => g.Status is GroupStatus.Pending or GroupStatus.Running or GroupStatus.Paused);
    }

    internal JobSheet? GetInternalJob(string jobId)
    {
        return _jobIndex.GetValueOrDefault(jobId);
    }

    internal JobGroup? GetInternalGroup(string groupId)
    {
        return _groups.GetValueOrDefault(groupId);
    }

    private void PauseJobInternal(JobSheet job)
    {
        job.SetStatus(SheetJobStatus.Paused);
        Logger.LogInformation("Paused job {JobId}", job.Id);
    }

    private void ResumeJobInternal(JobSheet job)
    {
        if (job.Status != SheetJobStatus.Paused) return;

        job.SetStatus(SheetJobStatus.Running);
        var hangfireJobId =
            backgroundJobClient.Enqueue<IJobExecutor>(executor =>
                executor.ExecuteJobAsync(job.Id, CancellationToken.None));
        job.HangfireJobId = hangfireJobId;

        Logger.LogInformation("Resumed job {JobId}", job.Id);
    }

    private void CancelJobInternal(JobSheet job)
    {
        job.CancellationTokenSource.Cancel();
        if (job.HangfireJobId != null)
            backgroundJobClient.Delete(job.HangfireJobId);
        job.SetStatus(SheetJobStatus.Cancelled);
        Logger.LogInformation("Cancelled job {JobId}", job.Id);
    }
}