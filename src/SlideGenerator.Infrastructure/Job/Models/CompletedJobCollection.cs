using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Job.Contracts.Collections;
using SlideGenerator.Domain.IO;
using SlideGenerator.Domain.Job.Entities;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Domain.Job.States;

namespace SlideGenerator.Infrastructure.Job.Models;

/// <inheritdoc />
/// <summary>
///     Manages completed jobs (finished, failed, cancelled)
/// </summary>
public class CompletedJobCollection(
    ILogger<CompletedJobCollection> logger,
    IJobStateStore jobStateStore,
    IFileSystem fileSystem)
    : ICompletedJobCollection
{
    private readonly ConcurrentDictionary<string, JobGroup> _groups = new();
    private readonly ConcurrentDictionary<string, JobSheet> _sheets = new();

    #region Internal Methods

    internal void AddGroup(JobGroup group)
    {
        _groups[group.Id] = group;
        foreach (var sheet in group.InternalJobs.Values)
            _sheets[sheet.Id] = sheet;

        logger.LogInformation("Added group {GroupId} to completed collection with status {Status}",
            group.Id, group.Status);
    }

    #endregion

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

    #region Remove Operations

    public bool RemoveGroup(string groupId)
    {
        if (_groups.TryRemove(groupId, out var group))
        {
            foreach (var sheet in group.InternalJobs.Values)
                TryDeleteOutputFile(sheet.OutputPath);
            foreach (var sheet in group.InternalJobs.Values)
                _sheets.TryRemove(sheet.Id, out _);

            jobStateStore.RemoveGroupAsync(groupId, CancellationToken.None).GetAwaiter().GetResult();
            logger.LogInformation("Removed completed group {GroupId}", groupId);
            return true;
        }

        return false;
    }

    public bool RemoveSheet(string sheetId)
    {
        if (_sheets.TryRemove(sheetId, out var sheet))
        {
            TryDeleteOutputFile(sheet.OutputPath);
            if (_groups.TryGetValue(sheet.GroupId, out var group))
            {
                group.RemoveJob(sheetId);
                if (group.InternalJobs.Count == 0)
                {
                    _groups.TryRemove(group.Id, out _);
                    jobStateStore.RemoveGroupAsync(group.Id, CancellationToken.None).GetAwaiter().GetResult();
                    logger.LogInformation("Removed completed group {GroupId} after clearing last sheet", group.Id);
                }
                else
                {
                    group.UpdateStatus();
                    PersistGroupState(group);
                }
            }

            jobStateStore.RemoveSheetAsync(sheetId, CancellationToken.None).GetAwaiter().GetResult();
            logger.LogInformation("Removed completed sheet {SheetId}", sheetId);
            return true;
        }

        return false;
    }

    public void ClearAll()
    {
        var count = _groups.Count;
        foreach (var group in _groups.Values)
        foreach (var sheet in group.InternalJobs.Values)
            TryDeleteOutputFile(sheet.OutputPath);
        foreach (var groupId in _groups.Keys)
            jobStateStore.RemoveGroupAsync(groupId, CancellationToken.None).GetAwaiter().GetResult();
        _groups.Clear();
        _sheets.Clear();
        logger.LogInformation("Cleared all {Count} completed groups", count);
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

    private void TryDeleteOutputFile(string outputPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(outputPath)) return;
            fileSystem.DeleteFile(outputPath);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Failed to delete output file {OutputPath}", outputPath);
        }
    }

    #endregion

    #region Query by Status

    public IReadOnlyDictionary<string, IJobGroup> GetSuccessfulGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Completed)
            .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetFailedGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Failed)
            .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetCancelledGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Cancelled)
            .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    #endregion
}