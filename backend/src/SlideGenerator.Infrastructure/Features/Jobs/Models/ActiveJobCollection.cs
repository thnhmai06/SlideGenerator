using System.Collections.Concurrent;
using Hangfire;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Common.Utilities;
using SlideGenerator.Application.Features.Configs;
using SlideGenerator.Application.Features.Jobs.Contracts;
using SlideGenerator.Application.Features.Jobs.Contracts.Collections;
using SlideGenerator.Application.Features.Jobs.DTOs.Requests;
using SlideGenerator.Application.Features.Sheets;
using SlideGenerator.Application.Features.Slides;
using SlideGenerator.Application.Features.Slides.DTOs.Components;
using SlideGenerator.Domain.Features.Images.Enums;
using SlideGenerator.Domain.Features.IO;
using SlideGenerator.Domain.Features.Jobs.Components;
using SlideGenerator.Domain.Features.Jobs.Entities;
using SlideGenerator.Domain.Features.Jobs.Enums;
using SlideGenerator.Domain.Features.Jobs.Interfaces;
using SlideGenerator.Domain.Features.Jobs.States;
using SlideGenerator.Infrastructure.Common.Utilities;
using SlideGenerator.Infrastructure.Features.Jobs.Hangfire;

namespace SlideGenerator.Infrastructure.Features.Jobs.Models;

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

    /// <inheritdoc />
    public IJobGroup? GetGroup(string groupId)
    {
        return _groups.GetValueOrDefault(groupId);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IJobGroup> GetAllGroups()
    {
        var result = new Dictionary<string, IJobGroup>(_groups.Count);
        foreach (var kv in _groups)
            result.Add(kv.Key, kv.Value);
        return result;
    }

    /// <inheritdoc />
    public IEnumerable<IJobGroup> EnumerateGroups()
    {
        return _groups.Values;
    }

    /// <inheritdoc />
    public int GroupCount => _groups.Count;

    /// <inheritdoc />
    public IJobSheet? GetSheet(string sheetId)
    {
        return _sheets.GetValueOrDefault(sheetId);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IJobSheet> GetAllSheets()
    {
        var result = new Dictionary<string, IJobSheet>(_sheets.Count);
        foreach (var kv in _sheets)
            result.Add(kv.Key, kv.Value);
        return result;
    }

    /// <inheritdoc />
    public IEnumerable<IJobSheet> EnumerateSheets()
    {
        return _sheets.Values;
    }

    /// <inheritdoc />
    public int SheetCount => _sheets.Count;

    /// <inheritdoc />
    public bool ContainsGroup(string groupId)
    {
        return _groups.ContainsKey(groupId);
    }

    /// <inheritdoc />
    public bool ContainsSheet(string sheetId)
    {
        return _sheets.ContainsKey(sheetId);
    }

    /// <inheritdoc />
    public bool IsEmpty => _groups.IsEmpty;

    #endregion

    #region Group Lifecycle

    /// <inheritdoc />
    public IJobGroup CreateGroup(JobCreate request)
    {
        var workbook = sheetService.OpenFile(request.SpreadsheetPath);
        var sheetsInfo = sheetService.GetSheetsInfo(workbook);

        var templatePath = request.TemplatePath;
        slideTemplateManager.AddTemplate(templatePath);
        var template = slideTemplateManager.GetTemplate(templatePath);

        List<string> sheetNames;
        if (request.JobType == JobType.Sheet)
        {
            if (string.IsNullOrWhiteSpace(request.SheetName))
                throw new InvalidOperationException("SheetName is required for sheet jobs.");

            var resolvedSheet = sheetsInfo.Keys.FirstOrDefault(name =>
                string.Equals(name, request.SheetName, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(resolvedSheet))
                throw new InvalidOperationException($"Sheet '{request.SheetName}' not found in workbook.");

            sheetNames = [resolvedSheet];
        }
        else
        {
            var requestedSheets = request.SheetNames;
            if (requestedSheets?.Length > 0)
            {
                var requestedSet = new HashSet<string>(requestedSheets, StringComparer.OrdinalIgnoreCase);
                sheetNames = sheetsInfo.Keys.Where(name => requestedSet.Contains(name)).ToList();
                if (sheetNames.Count == 0)
                    throw new InvalidOperationException("No requested sheets found in workbook.");
            }
            else
            {
                sheetNames = sheetsInfo.Keys.ToList();
            }
        }

        var outputRoot = request.OutputPath;
        if (string.IsNullOrWhiteSpace(outputRoot))
            throw new InvalidOperationException("Output path is required.");

        var fullOutputPath = Path.GetFullPath(outputRoot);
        var outputFolderPath = OutputPathUtils.NormalizeOutputFolderPath(fullOutputPath);
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

        var outputOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (HasPptxExtension(fullOutputPath) && sheetNames.Count == 1)
            outputOverrides[sheetNames[0]] = fullOutputPath;

        foreach (var sheetName in sheetNames)
        {
            var sanitizedSheetName = PathUtils.SanitizeFileName(sheetName);
            var outputPath = outputOverrides.TryGetValue(sheetName, out var overriddenPath)
                ? overriddenPath
                : Path.Combine(outputFolder.FullName, $"{sanitizedSheetName}.pptx");
            var job = group.AddJob(sheetName, outputPath);
            _sheets[job.Id] = job;

            // Register display name for Hangfire dashboard
            RegisterJobDisplayName(group, job);
        }

        _groups[group.Id] = group;
        _groupIdByOutputPath[outputFolder.FullName] = group.Id;

        PersistGroupState(group);
        foreach (var sheet in group.InternalJobs.Values)
            PersistSheetState(sheet);

        logger.LogInformation("Created group {GroupId} with {JobCount} jobs", group.Id, group.Sheets.Count);

        return group;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void PauseGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values.Where(j =>
                     j.Status is SheetJobStatus.Pending or SheetJobStatus.Running))
            PauseSheetInternal(job);

        group.SetStatus(GroupStatus.Paused);
        PersistGroupState(group);
        jobNotifier.NotifyGroupStatusChanged(group.Id, group.Status).GetAwaiter().GetResult();
        logger.LogInformation("Paused group {GroupId}", groupId);
    }

    /// <inheritdoc />
    public void ResumeGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;

        var pausedJobs = group.InternalJobs.Values
            .Where(j => j.Status == SheetJobStatus.Paused)
            .ToList();
        var availableSlots = GetAvailableResumeSlots();
        var resumedCount = 0;
        var pendingCount = 0;

        foreach (var job in pausedJobs)
        {
            if (job.IsExecuting)
            {
                ResumeSheetInternal(job);
                resumedCount++;
                continue;
            }

            if (availableSlots > 0)
            {
                ResumeSheetInternal(job);
                availableSlots--;
                resumedCount++;
                continue;
            }

            job.Resume();
            QueueJobIfNeeded(job);
            job.SetStatus(SheetJobStatus.Pending);
            PersistSheetState(job);
            jobNotifier.NotifyJobStatusChanged(job.Id, job.Status).GetAwaiter().GetResult();
            pendingCount++;
        }

        UpdateGroupStatus(group.Id);
        logger.LogInformation(
            "Resumed group {GroupId} with {ResumedCount} jobs, {PendingCount} pending",
            groupId,
            resumedCount,
            pendingCount);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void CancelAndRemoveGroup(string groupId)
    {
        if (!_groups.TryRemove(groupId, out var group)) return;

        foreach (var job in group.InternalJobs.Values)
        {
            if (job.Status is SheetJobStatus.Pending or SheetJobStatus.Running or SheetJobStatus.Paused)
                CancelSheetInternal(job);

            _sheets.TryRemove(job.Id, out _);
            SheetJobNameRegistry.Unregister(job.Id);
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

    /// <inheritdoc />
    public void PauseSheet(string sheetId)
    {
        if (_sheets.TryGetValue(sheetId, out var job))
            PauseSheetInternal(job);
    }

    /// <inheritdoc />
    public void ResumeSheet(string sheetId)
    {
        if (_sheets.TryGetValue(sheetId, out var job))
            ResumeSheetInternal(job);
    }

    /// <inheritdoc />
    public void CancelSheet(string sheetId)
    {
        if (_sheets.TryGetValue(sheetId, out var job))
        {
            CancelSheetInternal(job);
            CheckAndMoveGroupIfDone(job.GroupId);
        }
    }

    /// <inheritdoc />
    public void CancelAndRemoveSheet(string sheetId)
    {
        if (!_sheets.TryRemove(sheetId, out var job)) return;
        SheetJobNameRegistry.Unregister(job.Id);

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

    /// <inheritdoc />
    public void PauseAll()
    {
        foreach (var group in _groups.Values.Where(g => g.Status == GroupStatus.Running))
            PauseGroup(group.Id);
    }

    /// <inheritdoc />
    public void ResumeAll()
    {
        foreach (var group in _groups.Values.Where(g => g.Status == GroupStatus.Paused))
            ResumeGroup(group.Id);
    }

    /// <inheritdoc />
    public void CancelAll()
    {
        foreach (var group in _groups.Values.Where(g =>
                     g.Status is GroupStatus.Pending or GroupStatus.Running or GroupStatus.Paused))
            CancelGroup(group.Id);
    }

    #endregion

    #region Query

    /// <inheritdoc />
    public bool HasActiveJobs => !_groups.IsEmpty;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IJobGroup> GetRunningGroups()
    {
        var result = new Dictionary<string, IJobGroup>();
        foreach (var kv in _groups)
            if (kv.Value.Status == GroupStatus.Running)
                result.Add(kv.Key, kv.Value);
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IJobGroup> GetPausedGroups()
    {
        var result = new Dictionary<string, IJobGroup>();
        foreach (var kv in _groups)
            if (kv.Value.Status == GroupStatus.Paused)
                result.Add(kv.Key, kv.Value);
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IJobGroup> GetPendingGroups()
    {
        var result = new Dictionary<string, IJobGroup>();
        foreach (var kv in _groups)
            if (kv.Value.Status == GroupStatus.Pending)
                result.Add(kv.Key, kv.Value);
        return result;
    }

    /// <inheritdoc />
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
        {
            _sheets[sheet.Id] = sheet;

            // Register display name for Hangfire dashboard
            RegisterJobDisplayName(group, sheet);
        }
    }

    private void PauseSheetInternal(JobSheet job)
    {
        job.Pause();
        PersistSheetState(job);
        jobNotifier.NotifyJobStatusChanged(job.Id, job.Status).GetAwaiter().GetResult();
        UpdateGroupStatus(job.GroupId);
        logger.LogInformation("Paused job {JobId}{HangfireSuffix}", job.Id,
            FormatHangfireSuffix(job.HangfireJobId));
    }

    private void ResumeSheetInternal(JobSheet job)
    {
        if (job.Status != SheetJobStatus.Paused) return;

        job.Resume();
        job.SetStatus(SheetJobStatus.Running);

        QueueJobIfNeeded(job);

        PersistSheetState(job);
        jobNotifier.NotifyJobStatusChanged(job.Id, job.Status).GetAwaiter().GetResult();
        UpdateGroupStatus(job.GroupId);
        logger.LogInformation("Resumed job {JobId}{HangfireSuffix}", job.Id,
            FormatHangfireSuffix(job.HangfireJobId));
    }

    private void CancelSheetInternal(JobSheet job)
    {
        job.CancellationTokenSource.Cancel();
        if (job.HangfireJobId != null)
            backgroundJobClient.Delete(job.HangfireJobId);
        job.SetStatus(SheetJobStatus.Cancelled);
        PersistSheetState(job);
        jobNotifier.NotifyJobStatusChanged(job.Id, job.Status).GetAwaiter().GetResult();
        logger.LogInformation("Cancelled job {JobId}{HangfireSuffix}", job.Id,
            FormatHangfireSuffix(job.HangfireJobId));
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
                {
                    _sheets.TryRemove(sheet.Id, out _);
                    SheetJobNameRegistry.Unregister(sheet.Id);
                }

                group.Workbook.Dispose();
                onGroupCompleted(group);
                logger.LogInformation("Moved group {GroupId} to completed collection", group.Id);
            }
    }

    private int GetAvailableResumeSlots()
    {
        var maxConcurrentJobs = ConfigHolder.Value.Job.MaxConcurrentJobs;
        var executingJobs = _sheets.Values.Count(job => job.IsExecuting);
        return Math.Max(0, maxConcurrentJobs - executingJobs);
    }

    private void QueueJobIfNeeded(JobSheet job)
    {
        if (job.IsExecuting || job.HangfireJobId != null) return;
        var hangfireJobId =
            backgroundJobClient.Enqueue<IJobExecutor>(executor =>
                executor.ExecuteJobAsync(job.Id, CancellationToken.None));
        job.HangfireJobId = hangfireJobId;
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

    private static bool HasPptxExtension(string path)
    {
        return string.Equals(Path.GetExtension(path), ".pptx", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatHangfireSuffix(string? hangfireJobId)
    {
        return string.IsNullOrWhiteSpace(hangfireJobId) ? string.Empty : $" (#{hangfireJobId})";
    }

    private static void RegisterJobDisplayName(JobGroup group, JobSheet sheet)
    {
        var workbookName = Path.GetFileName(group.Workbook.FilePath);
        SheetJobNameRegistry.Register(sheet.Id, workbookName, sheet.SheetName);
    }

    #endregion
}