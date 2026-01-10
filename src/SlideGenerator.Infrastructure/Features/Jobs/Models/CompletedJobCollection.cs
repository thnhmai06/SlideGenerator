using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Features.Jobs.Contracts.Collections;
using SlideGenerator.Domain.Features.Jobs.Entities;
using SlideGenerator.Domain.Features.Jobs.Enums;
using SlideGenerator.Domain.Features.Jobs.Interfaces;
using SlideGenerator.Domain.Features.Jobs.States;

namespace SlideGenerator.Infrastructure.Features.Jobs.Models;

/// <inheritdoc />
/// <summary>
///     Manages completed jobs (finished, failed, cancelled)
/// </summary>
public class CompletedJobCollection(
    ILogger<CompletedJobCollection> logger,
    IJobStateStore jobStateStore)
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

    #region Remove Operations

    /// <inheritdoc />
    public bool RemoveGroup(string groupId)
    {
        if (_groups.TryRemove(groupId, out var group))
        {
            foreach (var sheet in group.InternalJobs.Values)
                _sheets.TryRemove(sheet.Id, out _);

            jobStateStore.RemoveGroupAsync(groupId, CancellationToken.None).GetAwaiter().GetResult();
            logger.LogInformation("Removed completed group {GroupId}", groupId);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool RemoveSheet(string sheetId)
    {
        if (_sheets.TryRemove(sheetId, out var sheet))
        {
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

    /// <inheritdoc />
    public void ClearAll()
    {
        var count = _groups.Count;
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

    #endregion

    #region Query by Status

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IJobGroup> GetSuccessfulGroups()
    {
        var result = new Dictionary<string, IJobGroup>();
        foreach (var kv in _groups)
            if (kv.Value.Status == GroupStatus.Completed)
                result.Add(kv.Key, kv.Value);
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IJobGroup> GetFailedGroups()
    {
        var result = new Dictionary<string, IJobGroup>();
        foreach (var kv in _groups)
            if (kv.Value.Status == GroupStatus.Failed)
                result.Add(kv.Key, kv.Value);
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IJobGroup> GetCancelledGroups()
    {
        var result = new Dictionary<string, IJobGroup>();
        foreach (var kv in _groups)
            if (kv.Value.Status == GroupStatus.Cancelled)
                result.Add(kv.Key, kv.Value);
        return result;
    }

    #endregion
}