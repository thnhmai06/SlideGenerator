using System.Collections.Concurrent;
using Hangfire;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Job.Contracts.Collections;
using SlideGenerator.Application.Sheet.Contracts;
using SlideGenerator.Application.Slide.Contracts;
using SlideGenerator.Application.Slide.DTOs.Requests.Group;
using SlideGenerator.Domain.Job.Entities;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Domain.Sheet.Enums;
using SlideGenerator.Infrastructure.Utilities;

namespace SlideGenerator.Infrastructure.Job.Models;

/// <inheritdoc />
public class ActiveJobCollection(
    ILogger<ActiveJobCollection> logger,
    ISheetService sheetService,
    ISlideTemplateManager slideTemplateManager,
    IBackgroundJobClient backgroundJobClient,
    Action<JobGroup> onGroupCompleted) : IActiveJobCollection
{
    private readonly ConcurrentDictionary<string, JobGroup> _groups = new();
    private readonly ConcurrentDictionary<string, JobSheet> _sheets = new();

    #region IJobCollection Implementation

    public IJobGroup? GetGroup(string groupId)
    {
        return _groups.GetValueOrDefault(groupId);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetAllGroups()
    {
        return _groups.ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    public int GroupCount => _groups.Count;

    public IJobSheet? GetSheet(string sheetId)
    {
        return _sheets.GetValueOrDefault(sheetId);
    }

    public IReadOnlyDictionary<string, IJobSheet> GetAllSheets()
    {
        return _sheets.ToDictionary(kv => kv.Key, kv => (IJobSheet)kv.Value);
    }

    public int SheetCount => _sheets.Count;

    public bool ContainsGroup(string groupId)
    {
        return _groups.ContainsKey(groupId);
    }

    public bool ContainsSheet(string sheetId)
    {
        return _sheets.ContainsKey(sheetId);
    }

    public bool IsEmpty => _groups.IsEmpty;

    #endregion

    #region Group Lifecycle

    public IJobGroup CreateGroup(GenerateSlideGroupCreate request)
    {
        // Preparing
        var workbook = sheetService.OpenFile(request.SpreadsheetPath);
        var sheetsInfo = sheetService.GetSheetsInfo(workbook);

        slideTemplateManager.AddTemplate(request.TemplatePresentationPath);
        var template = slideTemplateManager.GetTemplate(request.TemplatePresentationPath);

        var bookName = PathUtils.SanitizeFileName(
            workbook.Name ?? Path.GetFileNameWithoutExtension(request.SpreadsheetPath));

        var sheetNames = request.SheetNames?.Length > 0
            ? sheetsInfo.Where(s => request.SheetNames.Contains(s.Key)).Select(s => s.Key)
            : sheetsInfo.Keys;

        var outputFolder = new DirectoryInfo(Path.Combine(request.FilePath, bookName));
        outputFolder.Create();

        // Creating
        var group = new JobGroup(
            workbook,
            template,
            outputFolder,
            request.TextConfigs,
            request.ImageConfigs);

        foreach (var sheetName in sheetNames)
        {
            var sanitizedSheetName = PathUtils.SanitizeFileName(sheetName);
            var outputPath = Path.Combine(outputFolder.FullName, $"{sanitizedSheetName}.pptx");
            var job = group.AddJob(sheetName, outputPath);
            _sheets[job.Id] = job;
        }

        _groups[group.Id] = group;
        logger.LogInformation("Created group {GroupId} with {JobCount} jobs", group.Id, group.Sheets.Count);

        return group;
    }

    public void StartGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group))
        {
            logger.LogWarning("Group {GroupId} not found", groupId);
            return;
        }

        group.SetStatus(GroupStatus.Running);

        foreach (var job in group.InternalJobs.Values.Where(j => j.Status == SheetJobStatus.Pending))
        {
            var hangfireJobId = backgroundJobClient.Enqueue<IJobExecutor>(executor =>
                executor.ExecuteJobAsync(job.Id, CancellationToken.None));
            job.JobId = hangfireJobId;
        }

        logger.LogInformation("Started group {GroupId}", groupId);
    }

    public void PauseGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values.Where(j => j.Status == SheetJobStatus.Running))
            PauseSheetInternal(job);

        group.SetStatus(GroupStatus.Paused);
        logger.LogInformation("Paused group {GroupId}", groupId);
    }

    public void ResumeGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values.Where(j => j.Status == SheetJobStatus.Paused))
            ResumeSheetInternal(job);

        group.SetStatus(GroupStatus.Running);
        logger.LogInformation("Resumed group {GroupId}", groupId);
    }

    public void CancelGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values.Where(j =>
                     j.Status is SheetJobStatus.Pending or SheetJobStatus.Running or SheetJobStatus.Paused))
            CancelSheetInternal(job);

        group.SetStatus(GroupStatus.Cancelled);
        logger.LogInformation("Cancelled group {GroupId}", groupId);

        MoveToCompletedIfDone(group);
    }

    #endregion

    #region Sheet Lifecycle

    public void PauseSheet(string sheetId)
    {
        if (_sheets.TryGetValue(sheetId, out var job))
            PauseSheetInternal(job);
    }

    public void ResumeSheet(string sheetId)
    {
        if (_sheets.TryGetValue(sheetId, out var job))
            ResumeSheetInternal(job);
    }

    public void CancelSheet(string sheetId)
    {
        if (_sheets.TryGetValue(sheetId, out var job))
        {
            CancelSheetInternal(job);
            CheckAndMoveGroupIfDone(job.GroupId);
        }
    }

    #endregion

    #region Bulk Operations

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

    #endregion

    #region Query

    public bool HasActiveJobs => !_groups.IsEmpty;

    public IReadOnlyDictionary<string, IJobGroup> GetRunningGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Running)
            .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetPausedGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Paused)
            .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetPendingGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Pending)
            .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    #endregion

    #region Internal Methods

    internal JobSheet? GetInternalSheet(string sheetId)
    {
        return _sheets.GetValueOrDefault(sheetId);
    }

    internal JobGroup? GetInternalGroup(string groupId)
    {
        return _groups.GetValueOrDefault(groupId);
    }

    internal void NotifySheetCompleted(string sheetId)
    {
        if (_sheets.TryGetValue(sheetId, out var job))
            CheckAndMoveGroupIfDone(job.GroupId);
    }

    private void PauseSheetInternal(JobSheet job)
    {
        job.SetStatus(SheetJobStatus.Paused);
        logger.LogInformation("Paused job {JobId}", job.Id);
    }

    private void ResumeSheetInternal(JobSheet job)
    {
        if (job.Status != SheetJobStatus.Paused) return;

        job.SetStatus(SheetJobStatus.Running);
        var hangfireJobId =
            backgroundJobClient.Enqueue<IJobExecutor>(executor =>
                executor.ExecuteJobAsync(job.Id, CancellationToken.None));
        job.JobId = hangfireJobId;

        logger.LogInformation("Resumed job {JobId}", job.Id);
    }

    private void CancelSheetInternal(JobSheet job)
    {
        job.CancellationTokenSource.Cancel();
        if (job.JobId != null)
            backgroundJobClient.Delete(job.JobId);
        job.SetStatus(SheetJobStatus.Cancelled);
        logger.LogInformation("Cancelled job {JobId}", job.Id);
    }

    private void CheckAndMoveGroupIfDone(string groupId)
    {
        if (_groups.TryGetValue(groupId, out var group))
        {
            group.UpdateStatus();
            MoveToCompletedIfDone(group);
        }
    }

    private void MoveToCompletedIfDone(JobGroup group)
    {
        if (!group.IsActive)
            if (_groups.TryRemove(group.Id, out _))
            {
                foreach (var sheet in group.InternalJobs.Values)
                    _sheets.TryRemove(sheet.Id, out _);

                onGroupCompleted(group);
                logger.LogInformation("Moved group {GroupId} to completed collection", group.Id);
            }
    }

    #endregion
}