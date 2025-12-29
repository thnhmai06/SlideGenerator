using System.Collections.Concurrent;
using Hangfire;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Job.Contracts.Collections;
using SlideGenerator.Application.Sheet;
using SlideGenerator.Application.Slide;
using SlideGenerator.Application.Slide.DTOs.Components;
using SlideGenerator.Application.Slide.DTOs.Requests.Group;
using SlideGenerator.Application.Utilities;
using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Domain.IO;
using SlideGenerator.Domain.Job.Components;
using SlideGenerator.Domain.Job.Entities;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Domain.Job.States;
using SlideGenerator.Infrastructure.Utilities;

namespace SlideGenerator.Infrastructure.Job.Models;

/// <summary>
///     Manages active jobs (pending/running/paused).
/// </summary>
public class ActiveJobCollection(
    ILogger<ActiveJobCollection> logger,
    ISheetService sheetService,
    ISlideTemplateManager slideTemplateManager,
    IBackgroundJobClient backgroundJobClient,
    IJobStateStore jobStateStore,
    IFileSystem fileSystem,
    IJobNotifier jobNotifier,
    Action<JobGroup> onGroupCompleted) : IActiveJobCollection
{
    private readonly ConcurrentDictionary<string, string> _groupIdByOutputPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, JobGroup> _groups = new();
    private readonly ConcurrentDictionary<string, JobSheet> _sheets = new();

    #region IJobCollection Implementation

    public IJobGroup? GetGroup(string groupId)
    {
        return _groups.GetValueOrDefault(groupId);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetAllGroups()
    {
        var result = new Dictionary<string, IJobGroup>(_groups.Count);
        foreach (var kv in _groups)
            result.Add(kv.Key, kv.Value);
        return result;
    }

    public IEnumerable<IJobGroup> EnumerateGroups()
    {
        return _groups.Values;
    }

    public int GroupCount => _groups.Count;

    public IJobSheet? GetSheet(string sheetId)
    {
        return _sheets.GetValueOrDefault(sheetId);
    }

    public IReadOnlyDictionary<string, IJobSheet> GetAllSheets()
    {
        var result = new Dictionary<string, IJobSheet>(_sheets.Count);
        foreach (var kv in _sheets)
            result.Add(kv.Key, kv.Value);
        return result;
    }

    public IEnumerable<IJobSheet> EnumerateSheets()
    {
        return _sheets.Values;
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
        var workbook = sheetService.OpenFile(request.SpreadsheetPath);
        var sheetsInfo = sheetService.GetSheetsInfo(workbook);

        var templatePath = request.GetTemplatePath();
        slideTemplateManager.AddTemplate(templatePath);
        var template = slideTemplateManager.GetTemplate(templatePath);

        var requestedSheets = request.SheetNames ?? request.CustomSheet;
        var sheetNames = requestedSheets?.Length > 0
            ? sheetsInfo.Where(s => requestedSheets.Contains(s.Key)).Select(s => s.Key)
            : sheetsInfo.Keys;

        var outputRoot = request.GetOutputPath();
        if (string.IsNullOrWhiteSpace(outputRoot))
            throw new InvalidOperationException("Output path is required.");

        var outputFolderPath = OutputPathUtils.NormalizeOutputFolderPath(outputRoot);
        var outputFolder = new DirectoryInfo(outputFolderPath);
        fileSystem.EnsureDirectory(outputFolder.FullName);

        var textConfigs = MapTextConfigs(request.TextConfigs);
        var imageConfigs = MapImageConfigs(request.ImageConfigs);

        var group = new JobGroup(
            workbook,
            template,
            outputFolder,
            textConfigs,
            imageConfigs);

        foreach (var sheetName in sheetNames)
        {
            var sanitizedSheetName = PathUtils.SanitizeFileName(sheetName);
            var outputPath = Path.Combine(outputFolder.FullName, $"{sanitizedSheetName}.pptx");
            var job = group.AddJob(sheetName, outputPath);
            _sheets[job.Id] = job;
        }

        _groups[group.Id] = group;
        _groupIdByOutputPath[outputFolder.FullName] = group.Id;

        PersistGroupState(group);
        foreach (var sheet in group.InternalJobs.Values)
            PersistSheetState(sheet);

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
        PersistGroupState(group);
        jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status).GetAwaiter().GetResult();

        foreach (var job in group.InternalJobs.Values.Where(j => j.Status == SheetJobStatus.Pending))
        {
            var hangfireJobId = backgroundJobClient.Enqueue<IJobExecutor>(executor =>
                executor.ExecuteJobAsync(job.Id, CancellationToken.None));
            job.HangfireJobId = hangfireJobId;
            PersistSheetState(job);
        }

        logger.LogInformation("Started group {GroupId}", groupId);
    }

    public void PauseGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values.Where(j => j.Status == SheetJobStatus.Running))
            PauseSheetInternal(job);

        group.SetStatus(GroupStatus.Paused);
        PersistGroupState(group);
        jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status).GetAwaiter().GetResult();
        logger.LogInformation("Paused group {GroupId}", groupId);
    }

    public void ResumeGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values.Where(j => j.Status == SheetJobStatus.Paused))
            ResumeSheetInternal(job);

        group.SetStatus(GroupStatus.Running);
        PersistGroupState(group);
        jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status).GetAwaiter().GetResult();
        logger.LogInformation("Resumed group {GroupId}", groupId);
    }

    public void CancelGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values.Where(j =>
                     j.Status is SheetJobStatus.Pending or SheetJobStatus.Running or SheetJobStatus.Paused))
            CancelSheetInternal(job);

        group.SetStatus(GroupStatus.Cancelled);
        PersistGroupState(group);
        jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status).GetAwaiter().GetResult();
        logger.LogInformation("Cancelled group {GroupId}", groupId);

        MoveToCompletedIfDone(group);
    }

    public void CancelAndRemoveGroup(string groupId)
    {
        if (!_groups.TryRemove(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values)
        {
            if (job.Status is SheetJobStatus.Pending or SheetJobStatus.Running or SheetJobStatus.Paused)
                CancelSheetInternal(job);

            _sheets.TryRemove(job.Id, out _);
        }

        group.SetStatus(GroupStatus.Cancelled);
        jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status).GetAwaiter().GetResult();

        _groupIdByOutputPath.TryRemove(group.OutputFolder.FullName, out _);
        group.Workbook.Dispose();
        jobStateStore.RemoveGroupAsync(group.Id, CancellationToken.None).GetAwaiter().GetResult();
        logger.LogInformation("Cancelled and removed group {GroupId}", group.Id);
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

    public void CancelAndRemoveSheet(string sheetId)
    {
        if (!_sheets.TryRemove(sheetId, out var job)) return;

        if (job.Status is SheetJobStatus.Pending or SheetJobStatus.Running or SheetJobStatus.Paused)
            CancelSheetInternal(job);

        jobStateStore.RemoveSheetAsync(job.Id, CancellationToken.None).GetAwaiter().GetResult();

        if (_groups.TryGetValue(job.GroupId, out var group))
        {
            group.RemoveJob(job.Id);
            if (group.InternalJobs.Count == 0)
            {
                _groups.TryRemove(group.Id, out _);
                _groupIdByOutputPath.TryRemove(group.OutputFolder.FullName, out _);
                group.Workbook.Dispose();
                jobStateStore.RemoveGroupAsync(group.Id, CancellationToken.None).GetAwaiter().GetResult();
                logger.LogInformation("Removed group {GroupId} after deleting last sheet", group.Id);
                return;
            }

            group.UpdateStatus();
            PersistGroupState(group);
            jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status).GetAwaiter().GetResult();
        }

        logger.LogInformation("Cancelled and removed sheet {SheetId}", job.Id);
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
        var result = new Dictionary<string, IJobGroup>();
        foreach (var kv in _groups)
            if (kv.Value.Status == GroupStatus.Running)
                result.Add(kv.Key, kv.Value);
        return result;
    }

    public IReadOnlyDictionary<string, IJobGroup> GetPausedGroups()
    {
        var result = new Dictionary<string, IJobGroup>();
        foreach (var kv in _groups)
            if (kv.Value.Status == GroupStatus.Paused)
                result.Add(kv.Key, kv.Value);
        return result;
    }

    public IReadOnlyDictionary<string, IJobGroup> GetPendingGroups()
    {
        var result = new Dictionary<string, IJobGroup>();
        foreach (var kv in _groups)
            if (kv.Value.Status == GroupStatus.Pending)
                result.Add(kv.Key, kv.Value);
        return result;
    }

    public IJobGroup? GetGroupByOutputPath(string outputFolderPath)
    {
        var normalizedPath = OutputPathUtils.NormalizeOutputFolderPath(outputFolderPath);
        if (_groupIdByOutputPath.TryGetValue(normalizedPath, out var groupId))
            return _groups.GetValueOrDefault(groupId);
        return null;
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

    internal JobGroup? GetInternalGroupByOutputPath(string outputFolderPath)
    {
        var normalizedPath = OutputPathUtils.NormalizeOutputFolderPath(outputFolderPath);
        if (_groupIdByOutputPath.TryGetValue(normalizedPath, out var groupId))
            return _groups.GetValueOrDefault(groupId);
        return null;
    }

    internal void NotifySheetCompleted(string sheetId)
    {
        if (_sheets.TryGetValue(sheetId, out var job))
            CheckAndMoveGroupIfDone(job.GroupId);
    }

    internal void RestoreGroup(JobGroup group)
    {
        _groups[group.Id] = group;
        _groupIdByOutputPath[group.OutputFolder.FullName] = group.Id;
        foreach (var sheet in group.InternalJobs.Values)
            _sheets[sheet.Id] = sheet;
    }

    private void PauseSheetInternal(JobSheet job)
    {
        job.Pause();
        PersistSheetState(job);
        jobNotifier.NotifyJobStatusChanged(job.Id, job.Status).GetAwaiter().GetResult();
        UpdateGroupStatus(job.GroupId);
        logger.LogInformation("Paused job {JobId}", job.Id);
    }

    private void ResumeSheetInternal(JobSheet job)
    {
        if (job.Status != SheetJobStatus.Paused) return;

        job.Resume();
        job.SetStatus(SheetJobStatus.Running);

        if (!job.IsExecuting)
        {
            var hangfireJobId =
                backgroundJobClient.Enqueue<IJobExecutor>(executor =>
                    executor.ExecuteJobAsync(job.Id, CancellationToken.None));
            job.HangfireJobId = hangfireJobId;
        }

        PersistSheetState(job);
        jobNotifier.NotifyJobStatusChanged(job.Id, job.Status).GetAwaiter().GetResult();
        UpdateGroupStatus(job.GroupId);
        logger.LogInformation("Resumed job {JobId}", job.Id);
    }

    private void CancelSheetInternal(JobSheet job)
    {
        job.CancellationTokenSource.Cancel();
        if (job.HangfireJobId != null)
            backgroundJobClient.Delete(job.HangfireJobId);
        job.SetStatus(SheetJobStatus.Cancelled);
        PersistSheetState(job);
        jobNotifier.NotifyJobStatusChanged(job.Id, job.Status).GetAwaiter().GetResult();
        logger.LogInformation("Cancelled job {JobId}", job.Id);
    }

    private void CheckAndMoveGroupIfDone(string groupId)
    {
        if (_groups.TryGetValue(groupId, out var group))
        {
            group.UpdateStatus();
            PersistGroupState(group);
            jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status).GetAwaiter().GetResult();
            MoveToCompletedIfDone(group);
        }
    }

    private void UpdateGroupStatus(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;
        group.UpdateStatus();
        PersistGroupState(group);
        jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status).GetAwaiter().GetResult();
    }

    private void MoveToCompletedIfDone(JobGroup group)
    {
        if (!group.IsActive)
            if (_groups.TryRemove(group.Id, out _))
            {
                foreach (var sheet in group.InternalJobs.Values)
                    _sheets.TryRemove(sheet.Id, out _);

                group.Workbook.Dispose();
                onGroupCompleted(group);
                logger.LogInformation("Moved group {GroupId} to completed collection", group.Id);
            }
    }

    private void PersistGroupState(JobGroup group)
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

        jobStateStore.SaveGroupAsync(state, CancellationToken.None).GetAwaiter().GetResult();
    }

    private void PersistSheetState(JobSheet sheet)
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

        jobStateStore.SaveSheetAsync(state, CancellationToken.None).GetAwaiter().GetResult();
    }

    private static JobTextConfig[] MapTextConfigs(SlideTextConfig[]? configs)
    {
        if (configs == null || configs.Length == 0) return [];
        return configs.Select(c => new JobTextConfig(c.Pattern, c.Columns)).ToArray();
    }

    private static JobImageConfig[] MapImageConfigs(SlideImageConfig[]? configs)
    {
        if (configs == null || configs.Length == 0) return [];

        return configs.Select(c => new JobImageConfig(
            c.ShapeId,
            c.RoiType ?? ImageRoiType.Center,
            c.CropType ?? ImageCropType.Crop,
            c.Columns)).ToArray();
    }

    #endregion
}