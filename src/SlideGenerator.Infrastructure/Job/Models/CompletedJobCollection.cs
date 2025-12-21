using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Job.Contracts.Collections;
using SlideGenerator.Domain.Job.Entities;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Infrastructure.Job.Models;

/// <inheritdoc />
/// <summary>
///     Manages completed jobs (finished, failed, cancelled)
/// </summary>
public class CompletedJobCollection(ILogger<CompletedJobCollection> logger, IJobStateStore jobStateStore)
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
        return _groups.ToDictionary(kv => kv.Key, IJobGroup (kv) => kv.Value);
    }

    public int GroupCount => _groups.Count;

    public IJobSheet? GetSheet(string sheetId)
    {
        return _sheets.GetValueOrDefault(sheetId);
    }

    public IReadOnlyDictionary<string, IJobSheet> GetAllSheets()
    {
        return _sheets.ToDictionary(kv => kv.Key, IJobSheet (kv) => kv.Value);
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
            jobStateStore.RemoveSheetAsync(sheetId, CancellationToken.None).GetAwaiter().GetResult();
            logger.LogInformation("Removed completed sheet {SheetId}", sheetId);
            return true;
        }

        return false;
    }

    public void ClearAll()
    {
        var count = _groups.Count;
        foreach (var groupId in _groups.Keys)
            jobStateStore.RemoveGroupAsync(groupId, CancellationToken.None).GetAwaiter().GetResult();
        _groups.Clear();
        _sheets.Clear();
        logger.LogInformation("Cleared all {Count} completed groups", count);
    }

    #endregion

    #region Query by Status

    public IReadOnlyDictionary<string, IJobGroup> GetSuccessfulGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Completed)
            .ToDictionary(kv => kv.Key, IJobGroup (kv) => kv.Value);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetFailedGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Failed)
            .ToDictionary(kv => kv.Key, IJobGroup (kv) => kv.Value);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetCancelledGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Cancelled)
            .ToDictionary(kv => kv.Key, IJobGroup (kv) => kv.Value);
    }

    #endregion
}