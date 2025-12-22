using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Domain.Job.States;

namespace SlideGenerator.Infrastructure.Job.Services;

/// <summary>
///     Persists job state using Hangfire storage (SQLite).
/// </summary>
public sealed class HangfireJobStateStore(JobStorage storage) : IJobStateStore
{
    private const string GroupKeyPrefix = "slidegen:group:";
    private const string SheetKeyPrefix = "slidegen:sheet:";
    private const string ActiveGroupsSet = "slidegen:groups:active";
    private const string AllGroupsSet = "slidegen:groups:all";
    private const string JobLogKeyPrefix = "slidegen:joblog:";
    private const int MaxLogEntries = 2000;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc />
    public Task SaveGroupAsync(GroupJobState state, CancellationToken cancellationToken)
    {
        var key = GroupKeyPrefix + state.Id;
        var json = JsonSerializer.Serialize(state, SerializerOptions);

        using var connection = storage.GetConnection();
        using var tx = connection.CreateWriteTransaction();
        tx.SetRangeInHash(key, [new KeyValuePair<string, string>("data", json)]);
        tx.AddToSet(AllGroupsSet, state.Id);

        if (IsActive(state.Status))
            tx.AddToSet(ActiveGroupsSet, state.Id);
        else
            tx.RemoveFromSet(ActiveGroupsSet, state.Id);

        tx.Commit();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveSheetAsync(SheetJobState state, CancellationToken cancellationToken)
    {
        var key = SheetKeyPrefix + state.Id;
        var json = JsonSerializer.Serialize(state, SerializerOptions);

        using var connection = storage.GetConnection();
        using var tx = connection.CreateWriteTransaction();
        tx.SetRangeInHash(key, [new KeyValuePair<string, string>("data", json)]);
        tx.AddToSet(GroupSheetsSet(state.GroupId), state.Id);
        tx.Commit();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<GroupJobState?> GetGroupAsync(string groupId, CancellationToken cancellationToken)
    {
        using var connection = storage.GetConnection();
        var entries = connection.GetAllEntriesFromHash(GroupKeyPrefix + groupId);
        if (entries == null || !entries.TryGetValue("data", out var json))
            return Task.FromResult<GroupJobState?>(null);

        var state = JsonSerializer.Deserialize<GroupJobState>(json, SerializerOptions);
        return Task.FromResult(state);
    }

    /// <inheritdoc />
    public Task<SheetJobState?> GetSheetAsync(string sheetId, CancellationToken cancellationToken)
    {
        using var connection = storage.GetConnection();
        var entries = connection.GetAllEntriesFromHash(SheetKeyPrefix + sheetId);
        if (entries == null || !entries.TryGetValue("data", out var json))
            return Task.FromResult<SheetJobState?>(null);

        var state = JsonSerializer.Deserialize<SheetJobState>(json, SerializerOptions);
        return Task.FromResult(state);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GroupJobState>> GetActiveGroupsAsync(CancellationToken cancellationToken)
    {
        using var connection = storage.GetConnection();
        var ids = connection.GetAllItemsFromSet(ActiveGroupsSet);
        var result = new List<GroupJobState>();
        foreach (var id in ids)
        {
            var state = await GetGroupAsync(id, cancellationToken);
            if (state != null)
                result.Add(state);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GroupJobState>> GetAllGroupsAsync(CancellationToken cancellationToken)
    {
        using var connection = storage.GetConnection();
        var ids = connection.GetAllItemsFromSet(AllGroupsSet);
        var result = new List<GroupJobState>();
        foreach (var id in ids)
        {
            var state = await GetGroupAsync(id, cancellationToken);
            if (state != null)
                result.Add(state);
        }

        return result;
    }

    /// <inheritdoc />
    public Task AppendJobLogAsync(JobLogEntry entry, CancellationToken cancellationToken)
    {
        var key = JobLogKeyPrefix + entry.JobId;
        var logs = GetJobLogsInternal(entry.JobId);
        logs.Add(entry);

        if (logs.Count > MaxLogEntries)
            logs = logs.Skip(logs.Count - MaxLogEntries).ToList();

        var json = JsonSerializer.Serialize(logs, SerializerOptions);
        using var connection = storage.GetConnection();
        using var tx = connection.CreateWriteTransaction();
        tx.SetRangeInHash(key, [new KeyValuePair<string, string>("data", json)]);
        tx.Commit();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<JobLogEntry>> GetJobLogsAsync(string jobId, CancellationToken cancellationToken)
    {
        var logs = GetJobLogsInternal(jobId);
        return Task.FromResult<IReadOnlyList<JobLogEntry>>(logs);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SheetJobState>> GetSheetsByGroupAsync(string groupId,
        CancellationToken cancellationToken)
    {
        using var connection = storage.GetConnection();
        var ids = connection.GetAllItemsFromSet(GroupSheetsSet(groupId));
        var result = new List<SheetJobState>();
        foreach (var id in ids)
        {
            var state = await GetSheetAsync(id, cancellationToken);
            if (state != null)
                result.Add(state);
        }

        return result;
    }

    /// <inheritdoc />
    public Task RemoveGroupAsync(string groupId, CancellationToken cancellationToken)
    {
        using var connection = storage.GetConnection();
        var sheetIds = connection.GetAllItemsFromSet(GroupSheetsSet(groupId));

        using var tx = connection.CreateWriteTransaction();
        foreach (var sheetId in sheetIds)
        {
            tx.RemoveHash(SheetKeyPrefix + sheetId);
            tx.RemoveHash(JobLogKeyPrefix + sheetId);
            tx.RemoveFromSet(GroupSheetsSet(groupId), sheetId);
        }

        tx.RemoveFromSet(ActiveGroupsSet, groupId);
        tx.RemoveFromSet(AllGroupsSet, groupId);
        tx.RemoveHash(GroupKeyPrefix + groupId);
        tx.Commit();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveSheetAsync(string sheetId, CancellationToken cancellationToken)
    {
        var state = await GetSheetAsync(sheetId, cancellationToken);
        using var connection = storage.GetConnection();
        using var tx = connection.CreateWriteTransaction();
        tx.RemoveHash(SheetKeyPrefix + sheetId);
        tx.RemoveHash(JobLogKeyPrefix + sheetId);
        if (state != null)
            tx.RemoveFromSet(GroupSheetsSet(state.GroupId), sheetId);
        tx.Commit();
    }

    private List<JobLogEntry> GetJobLogsInternal(string jobId)
    {
        using var connection = storage.GetConnection();
        var entries = connection.GetAllEntriesFromHash(JobLogKeyPrefix + jobId);
        if (entries == null || !entries.TryGetValue("data", out var json))
            return [];

        var logs = JsonSerializer.Deserialize<List<JobLogEntry>>(json, SerializerOptions);
        return logs ?? [];
    }

    private static string GroupSheetsSet(string groupId)
    {
        return $"slidegen:group:{groupId}:sheets";
    }

    private static bool IsActive(GroupStatus status)
    {
        return status is GroupStatus.Pending or GroupStatus.Running or GroupStatus.Paused;
    }
}